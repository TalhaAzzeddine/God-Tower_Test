using System.Collections;
using UnityEngine;

public class GokuClimbingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ClimberController climber;

    [Header("IK Targets")]
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;

    [Header("Left Hand Positions")]
    [SerializeField] private Vector3 leftHandRestPosition;
    [SerializeField] private Vector3 leftHandClimbPosition;

    [Header("Right Hand Positions")]
    [SerializeField] private Vector3 rightHandRestPosition;
    [SerializeField] private Vector3 rightHandClimbPosition;

    [Header("Hand Timing")]
    [SerializeField] private float reachDuration = 0.21f;
    [SerializeField] private float grabPause = 0.07f;

    [Header("Body Tilt")]
    [SerializeField] private float rightHandTilt = -10f;
    [SerializeField] private float leftHandTilt = 10f;
    [SerializeField] private float tiltSmoothTime = 0.09f;

    private enum ClimbPhase
    {
        None,
        Reaching,
        Grabbed,
        Pulling,
        Returning
    }

    private bool isClimbing;
    private bool isHanging;

    private bool activeHandIsRight = true;

    private Transform activeHandTarget;
    private Vector3 activeClimbPosition;
    private Vector3 activeRestPosition;
    private float activeTilt;

    private ClimbPhase phase = ClimbPhase.None;

    private float targetTilt;
    private float currentTilt;
    private float tiltVelocity;

    private Quaternion initialRotation;

    private Coroutine climbingRoutine;
    private Coroutine returnRoutine;

    private void Awake()
    {
        if (climber == null)
            climber = GetComponent<ClimberController>();

        initialRotation = transform.localRotation;

        ResetHands();
    }

    private void Update()
    {
        UpdateBodyTilt();
    }

    private void UpdateBodyTilt()
    {
        currentTilt = Mathf.SmoothDamp(
            currentTilt,
            targetTilt,
            ref tiltVelocity,
            tiltSmoothTime
        );

        transform.localRotation =
            initialRotation *
            Quaternion.Euler(
                0f,
                0f,
                currentTilt
            );
    }

    public void StartClimbing()
    {
        // Recover from a stale state.
        if (isClimbing && climbingRoutine == null)
            isClimbing = false;

        if (isClimbing)
            return;

        isClimbing = true;

        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        // Continue an unfinished hanging grab.
        if (isHanging)
        {
            climbingRoutine =
                StartCoroutine(ResumeFromHang());

            return;
        }

        climbingRoutine =
            StartCoroutine(ClimbingLoop());
    }

    public void StopClimbing()
    {
        if (!isClimbing)
            return;

        isClimbing = false;

        /*
         * CRITICAL:
         *
         * If the player releases while the body is being pulled,
         * the Goku climbing coroutine will be stopped.
         *
         * We must also cancel the movement coroutine state,
         * otherwise ClimberController remains isMoving = true.
         */
        if (phase == ClimbPhase.Pulling)
        {
            if (climber != null)
                climber.CancelCurrentClimbStep();

            isHanging = true;

            targetTilt = activeTilt;

            StopClimbingCoroutine();

            return;
        }

        /*
         * Already grabbed but body has not started pulling.
         */
        if (phase == ClimbPhase.Grabbed)
        {
            isHanging = true;

            targetTilt = activeTilt;

            StopClimbingCoroutine();

            return;
        }

        /*
         * Released while hand is still reaching.
         *
         * Finish the reach and leave the hand there.
         */
        if (phase == ClimbPhase.Reaching)
        {
            isHanging = false;

            targetTilt = activeTilt;

            StopClimbingCoroutine();

            if (returnRoutine != null)
                StopCoroutine(returnRoutine);

            returnRoutine =
                StartCoroutine(
                    FinishReachAndHang()
                );

            return;
        }

        /*
         * Normal stop.
         */
        isHanging = false;
         
        targetTilt = 0f;

        StopClimbingCoroutine();

        if (returnRoutine != null)
            StopCoroutine(returnRoutine);

        returnRoutine =
            StartCoroutine(
                ReturnToRest()
            );
    }

    private IEnumerator ClimbingLoop()
    {
        while (isClimbing)
        {
            SetActiveHand();

            float climbDirection =
                climber.GetClimbDirection();

            if (Mathf.Abs(climbDirection) < 0.01f)
            {
                StopClimbingRoutine();
                yield break;
            }

            // -----------------------------------------
            // 1. Reach for the grab
            // -----------------------------------------

            phase = ClimbPhase.Reaching;

            targetTilt = activeTilt;

            yield return MoveHand(
                activeHandTarget,
                activeClimbPosition
            );

            if (!isClimbing)
                yield break;

            // -----------------------------------------
            // 2. Grab
            // -----------------------------------------

            phase = ClimbPhase.Grabbed;

            yield return new WaitForSeconds(
                grabPause
            );

            if (!isClimbing)
                yield break;

            // -----------------------------------------
            // 3. Pull the body
            // -----------------------------------------

            phase = ClimbPhase.Pulling;

            climbDirection =
                climber.GetClimbDirection();

            if (Mathf.Abs(climbDirection) < 0.01f)
            {
                isHanging = true;
                StopClimbingRoutine();
                yield break;
            }

            yield return climber.MoveClimbStep(
                climbDirection
            );

            if (!isClimbing)
                yield break;

            // -----------------------------------------
            // 4. Climb completed
            // -----------------------------------------

            isHanging = false;

            phase = ClimbPhase.Returning;

            yield return MoveHand(
                activeHandTarget,
                activeRestPosition
            );

            if (!isClimbing)
                yield break;

            // -----------------------------------------
            // 5. Center body
            // -----------------------------------------

            targetTilt = 0f;

            yield return new WaitForSeconds(
                0.04f
            );

            phase = ClimbPhase.None;

            // Switch hand.
            activeHandIsRight =
                !activeHandIsRight;
        }

        StopClimbingRoutine();
    }

    private IEnumerator ResumeFromHang()
    {
        SetActiveHand();

        // The hand is already at the grab position.
        phase = ClimbPhase.Grabbed;

        targetTilt = activeTilt;

        float climbDirection =
            climber.GetClimbDirection();

        if (Mathf.Abs(climbDirection) < 0.01f)
        {
            StopClimbingRoutine();
            yield break;
        }

        // -----------------------------------------
        // Continue the unfinished pull
        // -----------------------------------------

        phase = ClimbPhase.Pulling;

        yield return climber.MoveClimbStep(
            climbDirection
        );

        if (!isClimbing)
            yield break;

        // -----------------------------------------
        // Pull finished
        // -----------------------------------------

        isHanging = false;

        phase = ClimbPhase.Returning;

        yield return MoveHand(
            activeHandTarget,
            activeRestPosition
        );

        if (!isClimbing)
            yield break;

        targetTilt = 0f;

        yield return new WaitForSeconds(
            0.04f
        );

        phase = ClimbPhase.None;

        activeHandIsRight =
            !activeHandIsRight;

        /*
         * Continue using the SAME coroutine chain.
         * We do not start a second ClimbingLoop here.
         */
        yield return ClimbingLoop();
    }

    private IEnumerator FinishReachAndHang()
    {
        if (activeHandTarget == null)
        {
            isHanging = false;
            targetTilt = 0f;

            returnRoutine = null;

            yield break;
        }

        /*
         * The player released while the hand was still reaching.
         *
         * Finish the reach smoothly, then keep the hand there.
         */
        yield return MoveHand(
            activeHandTarget,
            activeClimbPosition,
            true
        );

        phase = ClimbPhase.Grabbed;

        isHanging = true;

        targetTilt = activeTilt;

        returnRoutine = null;
    }

    private void SetActiveHand()
    {
        if (activeHandIsRight)
        {
            activeHandTarget =
                rightHandTarget;

            activeClimbPosition =
                rightHandClimbPosition;

            activeRestPosition =
                rightHandRestPosition;

            activeTilt =
                rightHandTilt;
        }
        else
        {
            activeHandTarget =
                leftHandTarget;

            activeClimbPosition =
                leftHandClimbPosition;

            activeRestPosition =
                leftHandRestPosition;

            activeTilt =
                leftHandTilt;
        }
    }

    private IEnumerator MoveHand(
        Transform handTarget,
        Vector3 targetPosition,
        bool allowWhileStopped = false)
    {
        if (handTarget == null)
            yield break;

        Vector3 startPosition =
            handTarget.localPosition;

        float elapsed = 0f;

        while (elapsed < reachDuration)
        {
            if (!isClimbing && !allowWhileStopped)
                yield break;

            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / reachDuration
                );

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            handTarget.localPosition =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                );

            yield return null;
        }

        handTarget.localPosition =
            targetPosition;
    }

    private IEnumerator ReturnToRest()
    {
        Vector3 leftStart =
            leftHandTarget != null
                ? leftHandTarget.localPosition
                : leftHandRestPosition;

        Vector3 rightStart =
            rightHandTarget != null
                ? rightHandTarget.localPosition
                : rightHandRestPosition;

        Quaternion startRotation =
            transform.localRotation;

        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            if (leftHandTarget != null)
            {
                leftHandTarget.localPosition =
                    Vector3.Lerp(
                        leftStart,
                        leftHandRestPosition,
                        t
                    );
            }

            if (rightHandTarget != null)
            {
                rightHandTarget.localPosition =
                    Vector3.Lerp(
                        rightStart,
                        rightHandRestPosition,
                        t
                    );
            }

            transform.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    initialRotation,
                    t
                );

            yield return null;
        }

        ResetHands();

        phase = ClimbPhase.None;
        isHanging = false;

        returnRoutine = null;
    }

    private void StopClimbingCoroutine()
    {
        if (climbingRoutine != null)
        {
            StopCoroutine(climbingRoutine);
            climbingRoutine = null;
        }
    }

    private void StopClimbingRoutine()
    {
        isClimbing = false;
        climbingRoutine = null;
    }

    private void ResetHands()
    {
        if (leftHandTarget != null)
        {
            leftHandTarget.localPosition =
                leftHandRestPosition;
        }

        if (rightHandTarget != null)
        {
            rightHandTarget.localPosition =
                rightHandRestPosition;
        }

        currentTilt = 0f;
        tiltVelocity = 0f;
        targetTilt = 0f;

        activeHandIsRight = true;

        isHanging = false;

        phase = ClimbPhase.None;

        transform.localRotation =
            initialRotation;
    }

    private void OnDisable()
    {
        if (climbingRoutine != null)
            StopCoroutine(climbingRoutine);

        if (returnRoutine != null)
            StopCoroutine(returnRoutine);

        climbingRoutine = null;
        returnRoutine = null;

        isClimbing = false;
        isHanging = false;
    }
}