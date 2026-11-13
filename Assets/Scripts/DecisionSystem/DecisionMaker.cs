using UnityEngine;

public class DecisionMaker : MonoBehaviour{
    public UtilityCalculator utilityCalculator;

    public MonsterAction Decide(
        bool currentlyDetected,
        float confidence,
        float memoryAge,
        float memoryDuration,
        float distance,
        bool hasMemory,
        MonsterState currentState
    ){
        MonsterAction[] possibleActions ={
            MonsterAction.Chase,
            MonsterAction.Investigate,
            MonsterAction.Search,
            MonsterAction.Patrol,
            MonsterAction.Return
        };

        MonsterAction bestAction = MonsterAction.None;

        float highestScore = -1f;

        foreach (MonsterAction action in possibleActions){
            float score = utilityCalculator.ScoreAction(
                action,
                currentlyDetected,
                confidence,
                memoryAge,
                memoryDuration,
                distance,
                hasMemory
            );

            if (action == MonsterAction.Return &&
                (currentState == MonsterState.Patrol ||
                 currentState == MonsterState.Idle)){
                continue;
            }


            if (action == MonsterAction.Patrol &&
                currentState != MonsterState.Patrol &&
                currentState != MonsterState.Idle){
                continue;
            }


            if (ActionMatchesState(action, currentState)){
                score += 0.05f;
            }

            if (score > highestScore){
                highestScore = score;
                bestAction = action;
            }
        }

        return bestAction;
    }

    private bool ActionMatchesState(MonsterAction action, MonsterState state){
        return
            (action == MonsterAction.Chase && state == MonsterState.Chase) ||

            (action == MonsterAction.Investigate && state == MonsterState.Investigate) ||

            (action == MonsterAction.Search && state == MonsterState.Search) ||

            (action == MonsterAction.Patrol && state == MonsterState.Patrol) ||

            (action == MonsterAction.Return && state == MonsterState.Return);
    }
}