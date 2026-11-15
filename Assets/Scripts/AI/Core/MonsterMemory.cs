
using UnityEngine;

[System.Serializable]
public class MonsterMemory{
    public bool hasLKP;
    public Vector3 LKP;
    public float lastDetectionTime;
    public float detectionConfidence;

    public void RecordDetection(Vector3 position, float confidence){
        hasLKP = true;
        LKP = position;
        lastDetectionTime = Time.time;
        detectionConfidence = Mathf.Clamp01(confidence);
    }

    public float GetTimeSinceDetection(){
        if(!hasLKP)
            return Mathf.Infinity;

        return Time.time - lastDetectionTime;
    }

    public float GetCurrentConfidence(float memoryDuration){
        if(!hasLKP || memoryDuration <=0f)
            return 0f;

        float age = GetTimeSinceDetection();
        float decay = 1f-(age/memoryDuration);

        return detectionConfidence * Mathf.Clamp01(decay);

    }

    public void ClearMemory(){
        hasLKP = false;
        LKP = Vector3.zero;
        lastDetectionTime = 0f;
    detectionConfidence = 0f;

    }




}