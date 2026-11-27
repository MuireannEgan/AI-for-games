using UnityEngine;

public class Calculator : MonoBehaviour{
    [Header("Action Weights")]
    [Range(0f, 1f)]
    public float chaseWeight = 1f;

    [Range(0f, 1f)]
    public float investigateWeight = 0.8f;

    [Range(0f, 1f)]
    public float searchWeight = 0.6f;

    [Range(0f, 1f)]
    public float patrolWeight = 0.3f;

    [Range(0f, 1f)]
    public float returnWeight = 0.5f;

    [Header("Decision Settings")]
    public float maxDecisionDistance = 20f;

    public float ScoreAction(
        MonsterAction action,
        bool currentlyDetected,
        float confidence,
        float memoryAge,
        float memoryDuration,
        float distance,
        bool hasMemory
    ){
        confidence = Mathf.Clamp01(confidence);

        float recency = memoryDuration > 0f
            ? Mathf.Clamp01(1f - memoryAge / memoryDuration)
            : 0f;

        float proximity = maxDecisionDistance > 0f
            ? Mathf.Clamp01(1f - distance / maxDecisionDistance)
            : 0f;

        switch (action){
            case MonsterAction.Chase:
                if (!currentlyDetected)
                    return -1f;
                return chaseWeight *
                       confidence *
                       (0.7f + 0.3f * proximity);

            case MonsterAction.Investigate:
                if (!hasMemory || currentlyDetected)
                    return -1f;
                return investigateWeight *
                       confidence *
                       recency;

            case MonsterAction.Search:
                if (!hasMemory || currentlyDetected)
                    return -1f;
                return searchWeight *
                       confidence *
                       (1f - recency);

            case MonsterAction.Patrol:
                if (hasMemory)
                    return -1f;
                return patrolWeight;

            case MonsterAction.Return:
                if (hasMemory)
                    return -1f;
                return returnWeight;

            default:
                return 0f;
        }
    }
}