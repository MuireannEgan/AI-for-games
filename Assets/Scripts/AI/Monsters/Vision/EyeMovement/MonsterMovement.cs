using UnityEngine;

public class EyeMonsterMovement : MonoBehaviour{

    [Header("Movement Speeds")]
    public float patrolSpeed = 2f;
    public float investigateSpeed = 2.5f;
    public float chaseSpeed = 4.5f;
    public float searchSpeed = 2f;
    public float returnSpeed = 3f;

    [Header("Movement Settings")]
    public float stoppingDistance = 0.2f;
    public float rotationSpeed = 5f;


    [Header("Patrol Points")]
    public Transform[] patrolPoints;

    private int currentPatrolIndex;
    private Vector3 destination;
    private float currentSpeed;

    private bool isMoving;
    private bool hasDestination;

    public bool HasPatrolPoints =>
        patrolPoints != null &&
        patrolPoints.Length > 0;

    public bool HasReachedDestination =>
        hasDestination && !isMoving;

   private void Update()
    {
        if (!isMoving )
            return;

        Vector3 target = new Vector3(destination.x, transform.position.y, destination.z);

        Vector3 direction = target - transform.position;

        if(direction.magnitude <= stoppingDistance)
        {
            transform.position = target;
            isMoving = false;
            return;
        }
        direction.Normalize();

        transform.position = Vector3.MoveTowards(transform.position, target, currentSpeed * Time.deltaTime);

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    public void MoveTo(Vector3 target, float speed)
    {
        destination = target;
        currentSpeed = speed;

        isMoving = true;
        hasDestination = true;
    }

    public void StopMovement(){
        isMoving = false;
        hasDestination = false;
    }

    public void MoveToNextPatrolPoint(){
        if (patrolPoints == null || patrolPoints.Length == 0){
            StopMovement();
            return;
        }

        Transform point = patrolPoints[currentPatrolIndex];
        currentPatrolIndex =(currentPatrolIndex + 1) % patrolPoints.Length;
        MoveTo(point.position, patrolSpeed);
    }

    public void MoveToNearestPatrolPoint(){
        if (patrolPoints == null || patrolPoints.Length == 0){
            StopMovement();
            return;
        }

        int nearestIndex = -1;

        float shortestDistance = Mathf.Infinity;

        for (int i = 0; i < patrolPoints.Length; i++){
            if (patrolPoints[i] == null)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                patrolPoints[i].position
            );

            if (distance < shortestDistance) {
                shortestDistance = distance;
                nearestIndex = i;
            }
        }

        if(nearestIndex == -1)
        {
            StopMovement();
            return;
        }

        currentPatrolIndex = (nearestIndex + 1) % patrolPoints.Length;

        MoveTo(patrolPoints[nearestIndex].position, returnSpeed);
    }
}