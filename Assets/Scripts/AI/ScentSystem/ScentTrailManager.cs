using System.Collections.Generic;
using UnityEngine;

public class ScentTrailManager : MonoBehaviour{

    [Header("Scent Settinfs")]

    [Range(0f,1f)]
    public float InitialScentStrength = 1f;

    [Min(0.1f)]
    public float scentLifetime = 20f;

    [Header("Cleanup settings")]
    public float cleanupInterval = 1f;

    private List<ScentPoint> scentPoints = new List<ScentPoint>();
    private float nextCleanupTime;
    public int ActiveScentPointCount => scentPoints.Count;

    private void Update(){
        if(Time.time >= nextCleanupTime){
            RemoveExpiredScent();
            nextCleanupTime = Time.time + cleanupInterval;
        }
    }

    public void AddScentPoint(Vector3 position){
        ScentPoint newPoint = new ScentPoint(position, 1f, 20f);
        scentPoints.Add(newPoint);
    }

    public void ClearScentTrail(){
        scentPoints.Clear();
    }

    private void RemoveExpiredScent(){
        scentPoints.RemoveAll(point => point.HasExpired());
    }

    public bool TryGetStrongestScent(
        Vector3 searchPoints,
        float detectionRadius,
        out Vector3 scentPosition,
        out float scentStrength){

            scentPosition = Vector3.zero;
            scentStrength = 0f;

            float strongestScent = 0f;
            float radiusSquared = detectionRadius*detectionRadius;

            foreach(ScentPoint point in scentPoints){
                float distanceSquared = (point.Position - searchPoints).sqrMagnitude;

                if(distanceSquared > radiusSquared)
                    continue;

                float currentStrength = point.GetCurrentStrength();
                if(currentStrength > strongestScent){
                    strongestScent = currentStrength;
                    scentPosition = point.Position;
                }
            }

            scentStrength = strongestScent;
            return strongestScent > 0f;

        }


}