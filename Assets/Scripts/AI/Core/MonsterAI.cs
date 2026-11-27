using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Attack settings")]
    public Transform player;
    public string respawnScene = "Forest_Level";
    public float attackRange = 1.5f;
    public LayerMask attackObstacleMask;

    private float nextDecisionTime;
    private float searchEndTime;



    private void Awake(){
        if (perception == null)
            perception = GetComponent<MonsterPerception>();

        if (decisionMaker == null)
            decisionMaker = GetComponent<DecisionMaker>();

        if (movement == null)
            movement = GetComponent<MonsterMovement>();

        if (decisionMaker != null && decisionMaker.calculator == null){
            decisionMaker.calculator = GetComponent<Calculator>();
        }
    }

    private void Start(){
        currentState = MonsterState.Idle;

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

        if(TryAttackPlayer())
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
        Debug.Log("Perception assigned : "+ (perception != null));
        Debug.Log("Scent detected : "+ currentlyDetected);
        Debug.Log("Detection confidence : "+ detectedConfidence);
        Debug.Log("Detected position : " + detectedPosition);

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
        Debug.Log("Monster changing state to : "+ newState);
        if (currentState == newState)
            return;

        currentState = newState;

        Debug.Log(gameObject.name +" changed state to : " + newState);


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


    private bool TryAttackPlayer(){
        if(player == null || string.IsNullOrEmpty(respawnScene)){
            return false;
        }

        if(currentState != MonsterState.Chase){
            return false;
        }


        float distance = Vector3.Distance(transform.position, player.position);
        if(distance> attackRange){
            return false;
        }

        if(Physics.Linecast(transform.position+Vector3.up*0.5f, player.position + Vector3.up *0.5f, attackObstacleMask)){
                return false;
            }

        ChangeState(MonsterState.Attack);
        Debug.Log("player attcaked");
        RespawnPlayer();
        return true;

    }

    private void RespawnPlayer(){
        memory.ClearMemory();
        Debug.Log("Player respawning in : "+ respawnScene);
        SceneManager.LoadScene(respawnScene);
        ChangeState(MonsterState.Return);

    }

}