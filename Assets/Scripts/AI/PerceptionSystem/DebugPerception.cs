using UnityEngine;

public class DebugPerception : MonsterPerception{
    [Header("Test Target")]
    public Transform target;

    [Header("Detection Settings")]
    public float detectionRadius = 10f;

    public override bool TryGetDetection(out Vector3 detectedPosition,out float confidence){
        detectedPosition = Vector3.zero;
        confidence = 0f;

        if(target == null)
            return false;

        float distance = Vector3.Distance(transform.position, target.position);

        if(distance > detectionRadius)
            return false;

        confidence = 1f-(distance/detectionRadius);

        detectedPosition = target.position;

        return true;

    }

}