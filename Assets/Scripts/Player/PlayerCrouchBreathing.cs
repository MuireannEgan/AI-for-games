using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerCrouchBreathing : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private FirstPersonController firstPersonController;
    [SerializeField] private StarterAssetsInputs playerInput;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private HearingNoiseEmitter noiseEmitter;

    [Header("Crouching")]
    [Min(0.5f)] [SerializeField] private float crouchHeight = 1.1f;
    [Min(0.1f)] [SerializeField] private float crouchSpeed = 1.4f;
    [Min(0.1f)] [SerializeField] private float crouchTransitionSpeed = 8f;

    [Header("Breathing")]
    [SerializeField] private AudioSource slowBreathingSource;
    [SerializeField] private AudioSource fastBreathingSource;
    [SerializeField] private AudioClip slowBreathingClip;
    [SerializeField] private AudioClip fastBreathingClip;
    [Range(0f, 1f)] [SerializeField] private float breathingVolume = 0.35f;
    [Min(1f)] [SerializeField] private float crouchBreathingDelay = 8f;
    [Min(0.1f)] [SerializeField] private float breathingBuildTime = 12f;
    [Min(0f)] [SerializeField] private float maximumBreathingNoiseRadius = 12f;
    [Min(0.2f)] [SerializeField] private float breathingNoiseInterval = 1.5f;

    private float standingHeight;
    private Vector3 standingCenter;
    private Vector3 cameraStandingPosition;
    private float normalMoveSpeed;
    private float normalSprintSpeed;
    private float crouchTime;
    private float nextBreathNoiseTime;
    private bool crouching;
    private readonly Collider[] clearanceHits = new Collider[24];

    private void Awake()
    {
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (firstPersonController == null) firstPersonController = GetComponent<FirstPersonController>();
        if (playerInput == null) playerInput = GetComponent<StarterAssetsInputs>();
        if (noiseEmitter == null) noiseEmitter = GetComponent<HearingNoiseEmitter>();
        if (cameraTarget == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "CinemachineCameraTarget" ||
                    child.name == "PlayerCameraRoot")
                {
                    cameraTarget = child;
                    break;
                }
            }
        }
        if (cameraTarget == null)
            Debug.LogWarning("PlayerCrouchBreathing: assign Camera Target to the target used by FirstPersonController.", this);

        standingHeight = characterController.height;
        standingCenter = characterController.center;
        if (cameraTarget != null) cameraStandingPosition = cameraTarget.localPosition;

        if (firstPersonController != null)
        {
            normalMoveSpeed = firstPersonController.MoveSpeed;
            normalSprintSpeed = firstPersonController.SprintSpeed;
        }

        SetupBreathingSource(slowBreathingSource, slowBreathingClip);
        SetupBreathingSource(fastBreathingSource, fastBreathingClip);
    }

    private void Update()
    {
        bool wantsCrouch = Keyboard.current != null &&
            (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed);

        if (wantsCrouch)
            crouching = true;
        else if (crouching && CanStand())
            crouching = false;

        if (noiseEmitter != null) noiseEmitter.IsCrouching = crouching;

        if (firstPersonController != null)
        {
            firstPersonController.MoveSpeed = crouching ? crouchSpeed : normalMoveSpeed;
            firstPersonController.SprintSpeed = crouching ? crouchSpeed : normalSprintSpeed;
        }
        if (crouching && playerInput != null) playerInput.sprint = false;

        float height = crouching ? Mathf.Min(crouchHeight, standingHeight) : standingHeight;
        characterController.height = Mathf.MoveTowards(characterController.height,
            height, crouchTransitionSpeed * Time.deltaTime);
        float difference = standingHeight - characterController.height;
        characterController.center = standingCenter + Vector3.down * (difference * 0.5f);

        if (cameraTarget != null)
        {
            Vector3 target = cameraStandingPosition + Vector3.down *
                (standingHeight - height);
            cameraTarget.localPosition = Vector3.MoveTowards(
                cameraTarget.localPosition, target,
                crouchTransitionSpeed * Time.deltaTime);
        }

        crouchTime = crouching ? crouchTime + Time.deltaTime : 0f;
        float crouchStress = Mathf.Clamp01(
            (crouchTime - crouchBreathingDelay) / breathingBuildTime);

        float slowVolume = crouching ? breathingVolume * (1f - crouchStress) : 0f;
        float fastVolume = crouching ? breathingVolume * crouchStress : 0f;

        UpdateBreathingSource(slowBreathingSource, slowVolume);
        UpdateBreathingSource(fastBreathingSource, fastVolume);

        if (!crouching) nextBreathNoiseTime = 0f;

        if (crouching && crouchStress > 0f && noiseEmitter != null &&
            Time.time >= nextBreathNoiseTime)
        {
            noiseEmitter.EmitNoise(maximumBreathingNoiseRadius * crouchStress);
            nextBreathNoiseTime = Time.time + breathingNoiseInterval;
        }
    }

    private bool CanStand()
    {
        Vector3 currentTop = transform.TransformPoint(characterController.center) +
            Vector3.up * (characterController.height * 0.5f);
        Vector3 standingTop = transform.TransformPoint(standingCenter) +
            Vector3.up * (standingHeight * 0.5f);
        float radius = characterController.radius * 0.9f;
        int count = Physics.OverlapCapsuleNonAlloc(
            currentTop - Vector3.up * radius,
            standingTop - Vector3.up * radius,
            radius, clearanceHits, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider hit = clearanceHits[i];
            if (hit != null && !hit.transform.IsChildOf(transform)) return false;
        }
        return true;
    }

    private static void SetupBreathingSource(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null) return;
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.Stop();
    }

    private static void UpdateBreathingSource(AudioSource source, float targetVolume)
    {
        if (source == null || source.clip == null) return;
        if (targetVolume > 0f && !source.isPlaying)
        {
            source.volume = 0f;
            source.Play();
        }

        source.volume = Mathf.MoveTowards(source.volume, targetVolume, Time.deltaTime);
        if (targetVolume == 0f && source.volume == 0f && source.isPlaying)
            source.Stop();
    }

    private void OnDisable()
    {
        crouching = false;
        crouchTime = 0f;
        nextBreathNoiseTime = 0f;
        if (characterController != null)
        {
            characterController.height = standingHeight;
            characterController.center = standingCenter;
        }
        if (cameraTarget != null) cameraTarget.localPosition = cameraStandingPosition;
        if (firstPersonController != null)
        {
            firstPersonController.MoveSpeed = normalMoveSpeed;
            firstPersonController.SprintSpeed = normalSprintSpeed;
        }
        if (noiseEmitter != null) noiseEmitter.IsCrouching = false;
        if (slowBreathingSource != null) slowBreathingSource.Stop();
        if (fastBreathingSource != null) fastBreathingSource.Stop();
    }
}