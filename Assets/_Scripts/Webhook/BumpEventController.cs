using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BumpEventController : MonoBehaviour
{
    public enum BumpDirection
    {
        BottomToTop,
        TopToBottom
    }


    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform player;
    [SerializeField] private ClimberController climber;

    [SerializeField] private Transform gloveContainer;

    [Header("Glove")]
    [SerializeField] private GameObject glovePrefab;
    [SerializeField] private int minGloveCount = 4;
    [SerializeField] private int maxGloveCount = 6;
    private int gloveCount;
    private readonly List<float> usedScreenLanes = new();


    [Header("Glove Timing")]
    [SerializeField] private float gloveSpawnStagger = 0.07f;
    [SerializeField] private float gloveTravelDuration = 0.22f;
    [SerializeField] private float gloveExitDuration = 0.18f;

    [Header("Screen Position")]
    [SerializeField] private float spawnScreenOffset = 0.15f;
    [SerializeField] private float exitScreenOffset = 0.20f;

    [SerializeField, Range(0f, 0.45f)]
    private float screenLaneMin = 0.18f;

    [SerializeField, Range(0.1f, 1f)]
    private float screenLaneMax = 0.82f;

    [Header("Player Hit")]
    [SerializeField] private float gloveTargetOffset = 0.5f;
    [SerializeField] private float minClimbImpact = 100f;
    [SerializeField] private float maxClimbImpact = 500f;

    [SerializeField] private float bumpImpactDuration = 0.2f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 15f;

    [Header("Impact")]
    [SerializeField] private ParticleSystem impactHitEffect;
    [SerializeField] private ParticleSystem impactFireHitEffect;
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private AudioSource audioSource;



    private bool victoryStarted;
    private Coroutine victoryRoutine;
    private Coroutine bumpRoutine;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (player == null)
            Debug.LogWarning("[Bump] Player reference is missing.");

        if (glovePrefab == null)
            Debug.LogWarning("[Bump] Glove prefab is missing.");

        if (gloveContainer == null)
            gloveContainer = transform;
    }

    private void Update()
    {
#if UNITY_EDITOR

        if (Keyboard.current == null)
            return;

        // T = Top -> Bottom
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            TestTopBump();
        }

        // B = Bottom -> Top
        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            TestBottomBump();
        }

#endif
    }


    // =========================================================
    // TEST
    // =========================================================

    [Button("TEST Bottom Bump")]
    public void TestBottomBump()
    {
        TriggerBump(BumpDirection.BottomToTop);
    }

    [Button("TEST Top Bump")]
    public void TestTopBump()
    {
        TriggerBump(BumpDirection.TopToBottom);
    }

    // =========================================================
    // PUBLIC BUMP
    // =========================================================

    public void TriggerBump(BumpDirection direction)
    {
        if (bumpRoutine != null)
        {
            StopCoroutine(bumpRoutine);
            bumpRoutine = null;
        }

        bumpRoutine = StartCoroutine(
            BumpRoutine(direction)
        );
    }

    // =========================================================
    // MAIN BUMP
    // =========================================================

    private IEnumerator BumpRoutine(BumpDirection direction)
    {
        Debug.Log(
            $"[Bump] Starting bump: {direction}"
        );

        if (targetCamera == null)
        {
            Debug.LogError(
                "[Bump] Target Camera is NULL."
            );

            bumpRoutine = null;
            yield break;
        }

        if (player == null)
        {
            Debug.LogError(
                "[Bump] Player is NULL."
            );

            bumpRoutine = null;
            yield break;
        }

        if (glovePrefab == null)
        {
            Debug.LogError(
                "[Bump] Glove Prefab is NULL."
            );

            bumpRoutine = null;
            yield break;
        }

        // -----------------------------------------------------
        // Spawn gloves quickly one by one
        // -----------------------------------------------------

        gloveCount = Random.Range(minGloveCount, maxGloveCount + 1);

        usedScreenLanes.Clear();

        for (int i = 0; i < gloveCount; i++)
        {
            SpawnGlove(
                i,
                direction
            );

            if (i < gloveCount - 1)
            {
                yield return new WaitForSeconds(
                    gloveSpawnStagger
                );
            }
        }

        // Wait for the last glove to leave.
        yield return new WaitForSeconds(
            gloveTravelDuration +
            gloveExitDuration +
            0.15f
        );

        Debug.Log(
            "[Bump] Bump finished."
        );

        bumpRoutine = null;
    }

    // =========================================================
    // SPAWN GLOVE
    // =========================================================

    private void SpawnGlove(
        int index,
        BumpDirection direction)
    {
        if (glovePrefab == null ||
            player == null ||
            targetCamera == null)
        {
            return;
        }

        // -----------------------------------------------------
        // Player hit height
        // -----------------------------------------------------

        Vector3 playerTarget =
            player.position +
            Vector3.up *
            gloveTargetOffset;

        Vector3 playerScreen =
            targetCamera.WorldToScreenPoint(
                playerTarget
            );

        float depth =
            playerScreen.z;

        // -----------------------------------------------------
        // Random horizontal lane
        // -----------------------------------------------------

        float screenX = GetUniqueScreenLane();

        // -----------------------------------------------------
        // Spawn / exit Y
        // -----------------------------------------------------

        float spawnY;
        float exitY;

        if (direction ==
            BumpDirection.BottomToTop)
        {
            spawnY =
                -spawnScreenOffset;

            exitY =
                1f +
                exitScreenOffset;
        }
        else
        {
            spawnY =
                1f +
                spawnScreenOffset;

            exitY =
                -exitScreenOffset;
        }

        // -----------------------------------------------------
        // Spawn position
        // -----------------------------------------------------

        Vector3 spawnPosition =
            targetCamera.ViewportToWorldPoint(
                new Vector3(
                    screenX,
                    spawnY,
                    depth
                )
            );

        // -----------------------------------------------------
        // Hit position
        //
        // IMPORTANT:
        // X stays exactly the same as spawn.
        // This prevents diagonal movement.
        // -----------------------------------------------------

        Vector3 hitPosition =
            targetCamera.ViewportToWorldPoint(
                new Vector3(
                    screenX,
                    playerScreen.y /
                    Screen.height,
                    depth
                )
            );

        hitPosition.y =
            playerTarget.y;

        // -----------------------------------------------------
        // Exit position
        //
        // SAME X again.
        // -----------------------------------------------------

        Vector3 exitPosition =
            targetCamera.ViewportToWorldPoint(
                new Vector3(
                    screenX,
                    exitY,
                    depth
                )
            );

        // -----------------------------------------------------
        // Create glove
        // -----------------------------------------------------

        Quaternion spawnRotation;

        if (direction == BumpDirection.BottomToTop)
        {
            // Coming from below
            spawnRotation = Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            // Coming from above
            spawnRotation = Quaternion.Euler(180f, 0f, 0f);
        }

        GameObject glove =
            Instantiate(
                glovePrefab,
                spawnPosition,
                spawnRotation,
                gloveContainer
            );

        glove.name =
            $"Bump_Glove_{index + 1}";

        glove.name =
            $"Bump_Glove_{index + 1}";

        Debug.Log(
            $"[Bump] Glove {index + 1}/{gloveCount} " +
            $"spawned | Lane: {screenX:F2} | " +
            $"Direction: {direction}"
        );

        StartCoroutine(
            AnimateGlove(
                glove,
                hitPosition,
                exitPosition,
                direction
            )
        );
    }

    private float GetUniqueScreenLane()
    {
        const float minimumDistance = 0.10f;

        int maxAttempts = 30;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float candidate =
                Random.Range(
                    screenLaneMin,
                    screenLaneMax
                );

            bool valid = true;

            for (int i = 0; i < usedScreenLanes.Count; i++)
            {
                if (Mathf.Abs(
                        candidate -
                        usedScreenLanes[i]
                    ) < minimumDistance)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                usedScreenLanes.Add(candidate);
                return candidate;
            }
        }

        /*
         * If we cannot find a sufficiently distant lane,
         * return the least-used area by spreading it based
         * on the glove index.
         */
        float fallbackStep =
            (screenLaneMax - screenLaneMin) /
            Mathf.Max(1, gloveCount - 1);

        float fallback =
            screenLaneMin +
            fallbackStep *
            usedScreenLanes.Count;

        fallback =
            Mathf.Clamp(
                fallback,
                screenLaneMin,
                screenLaneMax
            );

        usedScreenLanes.Add(fallback);

        return fallback;
    }

    // =========================================================
    // ANIMATE GLOVE
    // =========================================================

    private IEnumerator AnimateGlove(
        GameObject glove,
        Vector3 hitPosition,
        Vector3 exitPosition,
        BumpDirection direction)
    {
        if (glove == null)
            yield break;

        Transform gloveTransform =
            glove.transform;

        Vector3 startPosition =
            gloveTransform.position;

        // -----------------------------------------------------
        // PHASE 1
        // Spawn -> Player
        // -----------------------------------------------------

        float elapsed = 0f;

        while (
            elapsed <
            gloveTravelDuration)
        {
            if (glove == null)
                yield break;

            elapsed +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    gloveTravelDuration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            gloveTransform.position =
                Vector3.Lerp(
                    startPosition,
                    hitPosition,
                    smoothT
                );



            yield return null;
        }

        if (glove == null)
            yield break;

        // Force exact hit position.
        gloveTransform.position =
            hitPosition;

        // -----------------------------------------------------
        // IMPACT
        // -----------------------------------------------------

        Vector3 impactPosition =
            player != null
                ? player.position +
                  Vector3.up *
                  gloveTargetOffset
                : hitPosition;

        TriggerGloveImpact(
            impactPosition,
            direction
        );

        // -----------------------------------------------------
        // Small punch effect
        // -----------------------------------------------------

        yield return StartCoroutine(
            GloveImpactPunch(
                gloveTransform
            )
        );

        // -----------------------------------------------------
        // PHASE 2
        // Player -> Offscreen
        //
        // SAME X.
        // -----------------------------------------------------

        elapsed = 0f;

        Vector3 impactGlovePosition =
            gloveTransform.position;

        while (
            elapsed <
            gloveExitDuration)
        {
            if (glove == null)
                yield break;

            elapsed +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    gloveExitDuration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            gloveTransform.position =
                Vector3.Lerp(
                    impactGlovePosition,
                    exitPosition,
                    smoothT
                );



            yield return null;
        }

        if (glove != null)
            Destroy(glove);
    }

    // =========================================================
    // IMPACT
    // =========================================================

    private void TriggerGloveImpact(
        Vector3 impactPosition,
        BumpDirection direction)
    {
        Debug.Log(
            $"[Bump] GLOVE IMPACT! " +
            $"Direction: {direction}"
        );

        // -----------------------------------------------------
        // Particle
        // -----------------------------------------------------

        if (impactHitEffect != null)
        {
            ParticleSystem fx =
                Instantiate(
                    impactHitEffect,
                    impactPosition,
                    Quaternion.identity
                );

            fx.Play();

            Destroy(
                fx.gameObject,
                3f
            );
        }

        if (impactFireHitEffect != null)
        {
            ParticleSystem fx =
                Instantiate(
                    impactFireHitEffect,
                    impactPosition,
                    Quaternion.identity
                );

            fx.Play();

            Destroy(
                fx.gameObject,
                3f
            );
        }

        // -----------------------------------------------------
        // Sound
        // -----------------------------------------------------

        if (audioSource != null &&
            impactSound != null)
        {
            audioSource.PlayOneShot(
                impactSound
            );
        }

        // -----------------------------------------------------
        // Climb impact
        // -----------------------------------------------------

        float impact =
            Random.Range(
                minClimbImpact,
                maxClimbImpact
            );

        if (climber != null)
        {
            if (direction ==
                BumpDirection.BottomToTop)
            {
                climber.ApplyBumpImpact(
                    impact, bumpImpactDuration
                );

                Debug.Log(
                    $"[Bump] UP impact: +{impact:F0}"
                );
            }
            else
            {
                climber.ApplyBumpImpact(
                    -impact, bumpImpactDuration
                );

                Debug.Log(
                    $"[Bump] DOWN impact: -{impact:F0}"
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "[Bump] Climber reference is NULL."
            );
        }
    }

    // =========================================================
    // GLOVE PUNCH
    // =========================================================

    private IEnumerator GloveImpactPunch(
        Transform glove)
    {
        if (glove == null)
            yield break;

        Vector3 originalScale =
            glove.localScale;

        Vector3 enlargedScale =
            originalScale * 1.15f;

        float duration = 0.06f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (glove == null)
                yield break;

            elapsed +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );

            glove.localScale =
                Vector3.Lerp(
                    originalScale,
                    enlargedScale,
                    t
                );

            yield return null;
        }

        if (glove != null)
        {
            glove.localScale =
                originalScale;
        }
    }



    public IEnumerator PlayVictoryBumpSequence(
    float duration,
    float interval)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            TriggerBump(
                BumpDirection.BottomToTop
            );

            yield return new WaitForSeconds(
                interval
            );

            elapsed += interval;
        }
    }



}