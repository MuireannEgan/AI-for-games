using UnityEngine;

public class HearingPerception : MonsterPerception
{
    [Header("Hearing")]
    [Min(0f)] public float hearingRadius = 15f;
    [Min(0f)] public float detectionHoldTime = 0.5f;

    private Vector3 lastSoundPosition;
    private float soundConfidence;
    private float lastSoundTime = float.NegativeInfinity;

    private void OnEnable()
    {
        HearingNoiseEmitter.NoiseEmitted += HearNoise;
    }

    private void OnDisable()
    {
        HearingNoiseEmitter.NoiseEmitted -= HearNoise;
    }

    private void HearNoise(Vector3 position, float noiseRadius)
    {
        float effectiveRadius = Mathf.Min(hearingRadius, noiseRadius);
        if (effectiveRadius <= 0f)
            return;

        float distance = Vector3.Distance(transform.position, position);
        if (distance > effectiveRadius)
            return;

        lastSoundPosition = position;
        soundConfidence = Mathf.Clamp01(1f - distance / effectiveRadius);
        lastSoundTime = Time.time;
    }

    public override bool TryGetDetection(out Vector3 detectedPosition, out float confidence)
    {
        detectedPosition = Vector3.zero;
        confidence = 0f;

        if (Time.time - lastSoundTime > detectionHoldTime)
            return false;

        detectedPosition = lastSoundPosition;
        confidence = soundConfidence;
        return true;
    }
}