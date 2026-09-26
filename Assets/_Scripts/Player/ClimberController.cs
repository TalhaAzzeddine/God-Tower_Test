using System.Collections;
using UnityEngine;

public class ClimberController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ClimberInput input;
    [SerializeField] private GokuClimbingController climbingController;

    [Header("Movement")]
    [SerializeField] private float climbStepHeight = 0.55f;
    [SerializeField] private float climbStepDuration = 0.15f;

    [Header("Vertical Limits")]
    [SerializeField] private bool useVerticalLimits = true;
    [SerializeField] private float minHeight = -10f;
    [SerializeField] private float maxHeight = 10f;
    [SerializeField] private float limitEpsilon = 0.01f;

    private bool isMoving;

    public bool IsMoving => isMoving;

    public bool CanMoveUp
    {
        get
        {
            return !useVerticalLimits ||
                   transform.position.y < maxHeight - limitEpsilon;
        }
    }

    public bool CanMoveDown
    {
        get
        {
            return !useVerticalLimits ||
                   transform.position.y > minHeight + limitEpsilon;
        }
    }

    public float CurrentHeight => transform.position.y;

    public float MinHeight => minHeight;

    public float MaxHeight => maxHeight;

    public float HeightProgress
    {
        get
        {
            if (!useVerticalLimits)
                return 0f;

            return Mathf.InverseLerp(
                minHeight,
                maxHeight,
                transform.position.y
            );
        }
    }

    private void Awake()
    {
        if (input == null)
            input = GetComponent<ClimberInput>();

        if (climbingController == null)
            climbingController = GetComponent<GokuClimbingController>();
    }

    private void Update()
    {
        UpdateClimbingAnimation();
    }

    private void UpdateClimbingAnimation()
    {
        if (input == null || climbingController == null)
            return;

        float direction = input.VerticalInput;

        if (Mathf.Abs(direction) > 0.01f)
        {
            if (direction > 0f && !CanMoveUp)
            {
                climbingController.StopClimbing();
                return;
            }

            if (direction < 0f && !CanMoveDown)
            {
                climbingController.StopClimbing();
                return;
            }

            climbingController.StartClimbing();
        }
        else
        {
            climbingController.StopClimbing();
        }
    }

    public float GetClimbDirection()
    {
        if (input == null)
            return 0f;

        float direction = input.VerticalInput;

        if (direction > 0f && !CanMoveUp)
            return 0f;

        if (direction < 0f && !CanMoveDown)
            return 0f;

        return Mathf.Sign(direction);
    }

    public IEnumerator MoveClimbStep(float direction)
    {
        // Do not start another step while one is active.
        if (isMoving)
            yield break;

        if (Mathf.Abs(direction) < 0.01f)
            yield break;

        isMoving = true;

        float startY = transform.position.y;

        float targetY =
            startY +
            climbStepHeight * direction;

        if (useVerticalLimits)
        {
            targetY = Mathf.Clamp(
                targetY,
                minHeight,
                maxHeight
            );
        }

        if (Mathf.Approximately(startY, targetY))
        {
            isMoving = false;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < climbStepDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / climbStepDuration
                );

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            Vector3 position =
                transform.position;

            position.y =
                Mathf.Lerp(
                    startY,
                    targetY,
                    t
                );

            transform.position = position;

            yield return null;
        }

        Vector3 finalPosition =
            transform.position;

        finalPosition.y = targetY;

        transform.position = finalPosition;

        isMoving = false;
    }

    // IMPORTANT:
    // Used when GokuClimbingController is stopped
    // while a climb step is still running.
    public void CancelCurrentClimbStep()
    {
        isMoving = false;
    }

    public void StopImmediately()
    {
        CancelCurrentClimbStep();

        if (input != null)
            input.ReleaseAll();

        if (climbingController != null)
            climbingController.StopClimbing();
    }

    public void SetClimbLimits(
        float playerMinHeight,
        float playerMaxHeight)
    {
        minHeight = playerMinHeight;
        maxHeight = playerMaxHeight;

        // Keep the player inside the new level bounds.
        Vector3 position = transform.position;

        position.y = Mathf.Clamp(
            position.y,
            minHeight,
            maxHeight
        );

        transform.position = position;

        CancelCurrentClimbStep();
    }
}