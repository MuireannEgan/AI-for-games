using UnityEngine;

public class HearingMonsterAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animation Thresholds")]
    [SerializeField] private float idleThreshold = 0.1f;
    [SerializeField] private float runThreshold = 1.2f;

    [Header("Movement Smoothing")]
    [SerializeField] private float smoothingTime = 0.12f;

    [Header("Animation Playback")]
    [SerializeField] private bool adjustPlaybackSpeed = true;

    [SerializeField] private string walkStateName = "Walk";
    [SerializeField] private string runStateName = "Run";

    [SerializeField] private float referenceWalkSpeed = 0.35f;
    [SerializeField] private float referenceRunSpeed = 1.45f;

    [SerializeField] private float minPlaybackSpeed = 0.65f;
    [SerializeField] private float maxPlaybackSpeed = 2f;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private Vector3 previousPosition;
    private float smoothedSpeed;
    private float smoothingVelocity;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError(
                "HearingMonsterAnimator: Animator not found.",
                this
            );

            enabled = false;
            return;
        }

        previousPosition = transform.position;
    }

    private void OnEnable()
    {
        previousPosition = transform.position;
        smoothedSpeed = 0f;
        smoothingVelocity = 0f;
    }

    private void LateUpdate()
    {
        if (animator == null || Time.deltaTime <= 0f)
            return;

        UpdateMovementSpeed();
        UpdateAnimationState();
        UpdatePlaybackSpeed();
    }

    private void UpdateMovementSpeed()
    {
        Vector3 displacement =
            transform.position - previousPosition;

        // Ignore vertical movement when calculating
        // walking and running speed.
        displacement.y = 0f;

        float actualSpeed =
            displacement.magnitude / Time.deltaTime;

        smoothedSpeed = Mathf.SmoothDamp(
            smoothedSpeed,
            actualSpeed,
            ref smoothingVelocity,
            Mathf.Max(0.001f, smoothingTime)
        );

        previousPosition = transform.position;
    }

    private void UpdateAnimationState()
    {
        float animationValue;

        if (smoothedSpeed < idleThreshold)
        {
            animationValue = 0f;
        }
        else if (smoothedSpeed < runThreshold)
        {
            animationValue = 0.5f;
        }
        else
        {
            animationValue = 2f;
        }

        animator.SetFloat(SpeedHash, animationValue);
    }

    private void UpdatePlaybackSpeed()
    {
        if (!adjustPlaybackSpeed)
        {
            animator.speed = 1f;
            return;
        }

        AnimatorStateInfo state =
            animator.GetCurrentAnimatorStateInfo(0);

        float playbackSpeed = 1f;

        if (state.IsName(walkStateName))
        {
            playbackSpeed =
                smoothedSpeed /
                Mathf.Max(0.01f, referenceWalkSpeed);
        }
        else if (state.IsName(runStateName))
        {
            playbackSpeed =
                smoothedSpeed /
                Mathf.Max(0.01f, referenceRunSpeed);
        }

        animator.speed = Mathf.Clamp(
            playbackSpeed,
            minPlaybackSpeed,
            maxPlaybackSpeed
        );
    }

    private void OnDisable()
    {
        if (animator != null)
            animator.speed = 1f;
    }
}