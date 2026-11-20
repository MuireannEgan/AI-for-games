using System;
using UnityEngine;

public class HearingNoiseEmitter : MonoBehaviour
{
    public static event Action<Vector3, float> NoiseEmitted;

    [Header("Footsteps")]
    [Min(0f)] public float minimumMovementSpeed = 0.2f;
    [Min(0f)] public float runningSpeedThreshold = 4f;
    [Min(0.05f)] public float walkingStepInterval = 0.5f;
    [Min(0.05f)] public float runningStepInterval = 0.3f;
    [Min(0f)] public float walkingNoiseRadius = 7f;
    [Min(0f)] public float runningNoiseRadius = 13f;

    [Header("Jump and Landing")]
    [SerializeField] private CharacterController characterController;
    [Min(0f)] public float jumpNoiseRadius = 10f;
    [Min(0f)] public float landingNoiseRadius = 15f;
    [Min(0f)] public float minimumAirTime = 0.12f;
    [Min(0f)] public float takeoffVerticalSpeed = 0.5f;

    private bool wasGrounded;
    private float airStartTime;
    private Vector3 previousPosition;
    private float nextFootstepTime;

    private void OnEnable()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        wasGrounded = characterController != null && characterController.isGrounded;
        previousPosition = transform.position;
        nextFootstepTime = Time.time;
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f)
            return;

        Vector3 displacement = transform.position - previousPosition;
        float verticalSpeed = displacement.y / Time.deltaTime;
        bool grounded = characterController != null && characterController.isGrounded;
        if (characterController != null)
        {
            if (wasGrounded && !grounded)
            {
                airStartTime = Time.time;
                if (verticalSpeed >= takeoffVerticalSpeed)
                    EmitNoise(jumpNoiseRadius);
            }
            else if (!wasGrounded && grounded)
            {
                if (Time.time - airStartTime >= minimumAirTime)
                    EmitNoise(landingNoiseRadius);
            }
            wasGrounded = grounded;
        }
        displacement.y = 0f;
        previousPosition = transform.position;

        float speed = displacement.magnitude / Time.deltaTime;
        if (speed < minimumMovementSpeed || Time.time < nextFootstepTime)
            return;

        bool running = speed >= runningSpeedThreshold;
        EmitNoise(running ? runningNoiseRadius : walkingNoiseRadius);
        nextFootstepTime = Time.time + (running ? runningStepInterval : walkingStepInterval);
    }

    public void EmitNoise(float radius)
    {
        if (radius > 0f)
            NoiseEmitted?.Invoke(transform.position, radius);
    }
}