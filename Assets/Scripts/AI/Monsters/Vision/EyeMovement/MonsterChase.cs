using UnityEngine;

public class MonsterChase
{
private Transform target;

public EyeMonsterMovement movement;

public MonsterChase(Transform player, EyeMonsterMovement monsterMovement)
    {
        target = player;
        movement = monsterMovement;
    }


public void Execute()
{
    if (target == null || movement == null)
        return;

    movement.MoveTo(target.position, movement.chaseSpeed);
}

   

}
