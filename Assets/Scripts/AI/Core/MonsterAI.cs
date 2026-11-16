using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    [Header("AI Components")]
    public MonsterPerception perception;
    public DecisionMaker decisionMaker;
    public MonsterMovement movement;

    [Header("Memory Settings")]
    public MonsterMemory memory = new MonsterMemory();

    public float memoryDuration = 10f;

    [Header("Decision Settings")]
    public float decisionInterval = 0.25f;

    [Header("Search Settings")]
    public float searchDuration = 4f;

    [Header("Current AI State")]
    public MonsterState currentState = MonsterState.Idle;

    private float nextDecisionTime;
    private float searchEndTime;



    private void Awake(){
        if (perception == null)
            perception = GetComponent<MonsterPerception>();

        if (decisionMaker == null)
            decisionMaker = GetComponent<DecisionMaker>();

        if (movement == null)
            movement = GetComponent<MonsterMovement>();

        if (decisionMaker != null && decisionMaker.utilityCalculator == null){
            decisionMaker.utilityCalculator = GetComponent<UtilityCalculator>();
        }
    }

    private void Start(){
        if (movement != null && movement.HasPatrolPoints){
            ChangeState(MonsterState.Patrol);
        }else{
            ChangeState(MonsterState.Idle);
        }
    }

    private void Update(){
        if (Time.time >= nextDecisionTime){
            EvaluateAI();
            nextDecisionTime = Time.time + decisionInterval;
        }
        ExecuteCurrentState();
    }

    private void EvaluateAI(){
        if (decisionMaker == null || movement == null)
            return;

        if (currentState == MonsterState.Search && Time.time >= searchEndTime){
            memory.ClearMemory();
        }

        bool currentlyDetected = false;
        Vector3 detectedPosition = Vector3.zero;
        float detectedConfidence = 0f;

        if (perception != null){
            currentlyDetected = perception.TryGetDetection(
                out detectedPosition,
                out detectedConfidence
            );
        }

        if (currentlyDetected){
            memory.RecordDetection(
                detectedPosition,
                detectedConfidence
            );
        }

        float memoryAge = memory.GetTimeSinceDetection();
        float confidence = memory.GetCurrentConfidence(memoryDuration);

        if (memory.hasLKP && memoryAge >= memoryDuration){
            memory.ClearMemory();
        }

        bool hasMemory = memory.hasLKP;

        if (!hasMemory){
            memoryAge = 0f;
            confidence = 0f;
        }

        float distance = hasMemory
            ? Vector3.Distance(
                transform.position,
                memory.LKP
            )
            : 0f;

        MonsterAction selectedAction = decisionMaker.Decide(
            currentlyDetected,
            confidence,
            memoryAge,
            memoryDuration,
            distance,
            hasMemory,
            currentState
        );


        switch (selectedAction){
            case MonsterAction.Patrol:
                ChangeState(MonsterState.Patrol);
                break;

            case MonsterAction.Investigate:
                ChangeState(MonsterState.Investigate);
                break;

            case MonsterAction.Chase:
                ChangeState(MonsterState.Chase);
                break;

            case MonsterAction.Search:
                ChangeState(MonsterState.Search);
                break;

            case MonsterAction.Return:
                ChangeState(MonsterState.Return);
                break;
        }
    }

    private void ChangeState(MonsterState newState){
        if (currentState == newState)
            return;

        currentState = newState;

        Debug.Log(gameObject.name + " changed state to: " + newState);


        switch (newState){
            case MonsterState.Idle:
                movement.StopMovement();
                break;

            case MonsterState.Patrol:
                movement.MoveToNextPatrolPoint();
                break;

            case MonsterState.Investigate:
                break;

            case MonsterState.Chase:
                break;

            case MonsterState.Search:
                searchEndTime = Time.time + searchDuration;
                break;

            case MonsterState.Return:
                movement.MoveToNearestPatrolPoint();
                break;

            case MonsterState.Attack:
                movement.StopMovement();
                break;
        }
    }

    private void ExecuteCurrentState(){
        if (movement == null)
            return;

        switch (currentState){
            case MonsterState.Idle:
                movement.StopMovement();
                break;

            case MonsterState.Patrol:
                if (movement.HasReachedDestination){
                    movement.MoveToNextPatrolPoint();
                }
                break;

            case MonsterState.Investigate:
                MoveTowardsLKP(
                    movement.investigateSpeed
                );
                break;

            case MonsterState.Chase:
                MoveTowardsLKP(
                    movement.chaseSpeed
                );
                break;

            case MonsterState.Search:
                MoveTowardsLKP(
                    movement.searchSpeed
                );
                break;

            case MonsterState.Return:
                if (movement.HasReachedDestination){
                    ChangeState(MonsterState.Patrol);
                }
                break;

            case MonsterState.Attack:
                movement.StopMovement();
                break;
        }
    }

    private void MoveTowardsLKP(float speed){
        if (!memory.hasLKP)
            return;

        if (movement.HasReachedDestination){
            movement.StopMovement();
            return;
        }

        movement.MoveTo(
            memory.LKP,
            speed
        );
    }
}