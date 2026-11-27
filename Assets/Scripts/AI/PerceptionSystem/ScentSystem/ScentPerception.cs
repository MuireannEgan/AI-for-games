using UnityEngine;

public class ScentPerception : MonsterPerception{

    [Header("Scent Settings")]
    public ScentTrailManager trailManager;

    [Header("Detection Settinsgs")]
    public float detectionRadius = 10f;

    private void Awake(){
        if(trailManager == null){
            trailManager = FindAnyObjectByType<ScentTrailManager>();
        }
    }

    public override bool TryGetDetection(out Vector3 detectedPosition, out float confidence){

    detectedPosition = Vector3.zero;
    confidence = 0;

    if(trailManager == null)
        return false;

    bool foundScent = trailManager.TryGetStrongestScent(
        transform.position,
        detectionRadius,
        out detectedPosition,
        out float scentStrength
    );

    if(!foundScent)
    return false;

    confidence = scentStrength;

    return true;
    }

    private void OnDrawGizmosSelected(){
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

}