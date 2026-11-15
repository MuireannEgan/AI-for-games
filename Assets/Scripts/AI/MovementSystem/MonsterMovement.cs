using UnityEngine;
using UnityEngine.AI;

public class MonsterMovement : MonoBehaviour{
    private NavMeshAgent agent;

    [Header("Movement Speeds")]
    public float patrolSpeed = 2f;
    public float investigateSpeed = 2.5f;
    public float chaseSpeed = 4.5f;
    public float searchSpeed = 2f;
    public float returnSpeed = 3f;

    [Header("Patrol Points")]
    public Transform[] patrolPoints;

    private int currentPatrolIndex;

    public bool HasPatrolPoints =>
        patrolPoints != null &&
        patrolPoints.Length > 0;

    public bool HasReachedDestination{
        get{
            return agent != null &&
                   !agent.pathPending &&
                   agent.hasPath &&
                   agent.remainingDistance <=
                   agent.stoppingDistance;
        }
    }

    private void Awake(){
        agent = GetComponent<NavMeshAgent>();

        if (agent == null){
            Debug.LogError(
                "MonsterMovement requires a NavMeshAgent"
            );
        }
    }

    public void MoveTo(Vector3 destination, float speed){
        if (agent == null || !agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        agent.speed = speed;
        agent.SetDestination(destination);
    }

    public void StopMovement(){
        if (agent == null)
            return;

        agent.isStopped = true;
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

        int nearestIndex = 0;

        float shortestDistance = Mathf.Infinity;

        for (int i = 0; i < patrolPoints.Length; i++){
            if (patrolPoints[i] == null)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                patrolPoints[i].position
            );

            if (patrolPoints[nearestIndex] == null || shortestDistance == Mathf.Infinity){
                StopMovement();
                return;
            }

            if (distance < shortestDistance) {
                shortestDistance = distance;
                nearestIndex = i;
            }
        }

        currentPatrolIndex = nearestIndex;

        MoveTo(patrolPoints[nearestIndex].position, returnSpeed);
    }
}