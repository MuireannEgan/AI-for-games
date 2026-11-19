using UnityEngine;

public class ScentEmitter : MonoBehaviour{
    [Header("Scent Trail")]
    public ScentTrailManager trailManager;

    [Header("Emission Settings")]
    public float emissionInterval = 0.5f;
    public float minimumMovementSpeed = 0.15f;

    private Vector3 previousPosition;
    private float timeSinceLastEmission;
    private float scentMaskedUntil;

    public bool isScentMasked => Time.time < scentMaskedUntil;

    private void Awake(){
        if(trailManager == null){
            trailManager = FindAnyObjectByType<ScentTrailManager>();
        }
    }

    private void Start(){
        previousPosition = transform.position;
    }

    private void Update(){

        float distanceMoved = Vector3.Distance( transform.position, previousPosition);
        float movementSpeed = Time.deltaTime > 0f ? distanceMoved/Time.deltaTime : 0f;
        previousPosition = transform.position;

        //this will be do to with fountainslater
        if(isScentMasked)
            return;

        if(movementSpeed < minimumMovementSpeed)
            return;

        if(trailManager == null)
            return;

        timeSinceLastEmission += Time.deltaTime;


        if(timeSinceLastEmission >= emissionInterval){
            trailManager.AddScentPoint(transform.position);
            timeSinceLastEmission = 0f;

        }
    }

    public void MaskScent(float duration){
        if(duration <= 0f)
            return;

        scentMaskedUntil = Mathf.Max(scentMaskedUntil, Time.time + duration);
        timeSinceLastEmission = 0f;
    }


}

