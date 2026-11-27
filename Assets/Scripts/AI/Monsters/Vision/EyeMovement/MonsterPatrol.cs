using UnityEngine;

public class MonsterPatrol
{
    private MonsterMovement movement;

    public void Execute()
    {
        if (movement == null || !movement.HasPatrolPoints)
            return;

        if(movement.HasReachedDestination || !isMoving)
        {
            movement.MoveToNearestPatrolPoint();
            isMoving = true;
        }
    }

    private bool isMoving = false;

    public void Stop()
    {
        if (movement == null)
            return;

        movement.StopMovement();
        isMoving = false;
    }
}
