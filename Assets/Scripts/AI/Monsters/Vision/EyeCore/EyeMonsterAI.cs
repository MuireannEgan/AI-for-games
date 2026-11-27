using UnityEngine;

public class EyeMonsterAI : MonoBehaviour
{

    [System.NonSerialized] public MonsterInvestigate investigate = new MonsterInvestigate();
    [Header("AI Components")]
    public EyePerception perception;
    public DecisionMaker decisionMaker;
    public EyeMonsterMovement movement;

    private MonsterChase chase;
    private Transform currentSearchPoint;
    private Vector3 lastLKP;
    private float attackRange = 1.2f;

    private bool hasLastLKP = false;
    public float lostSightDelay = 0.4f;


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

    private void Awake()
    {
        if (perception == null)
            perception = GetComponent<EyePerception>();

        if (decisionMaker == null)
            decisionMaker = GetComponent<DecisionMaker>();

        if (movement == null)
            movement = GetComponent<EyeMonsterMovement>();

        if (decisionMaker != null && decisionMaker.calculator == null)
        {
            decisionMaker.calculator = GetComponent<Calculator>();
        }

        if (perception != null && movement != null)
        {
            chase = new MonsterChase(perception.target, movement);
        }
    }

    private void Start()
    {
        if (movement != null && movement.HasPatrolPoints)
        {
            ChangeState(MonsterState.Patrol);
        }
        else
        {
            ChangeState(MonsterState.Idle);
        }
    }

    private void Update()
    {
        if (Time.time >= nextDecisionTime)
        {
            EvaluateAI();
            nextDecisionTime = Time.time + decisionInterval;
        }
        ExecuteCurrentState();
    }

    private void EvaluateAI()
    {
        if (decisionMaker == null || movement == null)
            return;

        bool currentlyDetected = false;
        Vector3 detectedPosition = Vector3.zero;
        float detectedConfidence = 0f;

        if (perception != null)
        {
            currentlyDetected = perception.TryGetDetection(
                out detectedPosition,
                out detectedConfidence
            );
        }

        if (currentlyDetected)
        {
            memory.RecordDetection(
                detectedPosition,
                detectedConfidence
            );
        }
        if (!currentlyDetected && currentState == MonsterState.Chase && memory.hasLKP && Time.time - memory.lastDetectionTime < lostSightDelay)
        {
            currentlyDetected = true;
        }
        if (currentState == MonsterState.Search && !currentlyDetected)
        {
            return;

        }

        float memoryAge = memory.GetTimeSinceDetection();
        float confidence = memory.GetCurrentConfidence(memoryDuration);

        if (memory.hasLKP && memoryAge >= memoryDuration)
        {
            memory.ClearMemory();
        }

        bool hasMemory = memory.hasLKP;

        if (!hasMemory)
        {
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


        switch (selectedAction)
        {
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

            case MonsterAction.Attack:
                ChangeState(MonsterState.Attack);
                break;

            case MonsterAction.Return:
                ChangeState(MonsterState.Return);
                break;
        }
    }

    private void ChangeState(MonsterState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        hasLastLKP = false;

        Debug.Log(gameObject.name + " changed state to: " + newState);


        switch (newState)
        {
            case MonsterState.Idle:
                movement.StopMovement();
                break;

            case MonsterState.Patrol:
                movement.MoveToNextPatrolPoint();
                break;

            case MonsterState.Investigate:
                investigate.create(memory.LKP, movement.patrolPoints);

                currentSearchPoint = null;
                break;

            case MonsterState.Chase:
                break;

            case MonsterState.Search:
                break;

            case MonsterState.Return:
                movement.MoveToNearestPatrolPoint();
                break;

            case MonsterState.Attack:
                movement.StopMovement();
                break;
        }
    }

    private void ExecuteCurrentState()
    {
        if (movement == null)
            return;

        switch (currentState)
        {
            case MonsterState.Idle:
                movement.StopMovement();
                break;

            case MonsterState.Patrol:
                if (movement.HasReachedDestination)
                {
                    movement.MoveToNextPatrolPoint();
                }
                break;

            case MonsterState.Investigate:
                ExecuteSearch();
                break;

            case MonsterState.Chase:
                if (perception != null || perception.target != null)
                {
                    ChangeState(MonsterState.Search);
                    break;
                }
                if (!perception.CanSee)
                {
                    ChangeState(MonsterState.Search);
                }
                chase.Execute();
                float distanceToPlayer = Vector3.Distance(perception.target.position, transform.position);
                if (distanceToPlayer <= attackRange)
                {
                    ChangeState(MonsterState.Attack);
                }
                break;

            case MonsterState.Return:
                if (movement.HasReachedDestination)
                {
                    ChangeState(MonsterState.Patrol);
                }
                break;

            case MonsterState.Search:
                MoveTowardsLKP(movement.searchSpeed);
                if (movement.HasReachedDestination)
                {
                    ChangeState(MonsterState.Investigate);
                }

                break;

            case MonsterState.Attack:
                movement.StopMovement();
                if (perception.target == null)
                {
                    ChangeState(MonsterState.Return);
                    break;
                }

                float ToPlayer = Vector3.Distance(perception.target.position, transform.position);
                if (ToPlayer > attackRange)
                {
                    ChangeState(MonsterState.Chase);
                }
                break;
        }
    }

    private void MoveTowardsLKP(float speed)
    {
        if (!memory.hasLKP)
            return;

        if (!hasLastLKP || Vector3.Distance(memory.LKP, lastLKP) > 0.5f)
        {
            movement.MoveTo(
                memory.LKP,
                speed
            );
            lastLKP = memory.LKP;
            hasLastLKP = true;
        }
    }

    private void ExecuteSearch()
    {


        if (currentSearchPoint == null)
        {
            if (!movement.HasReachedDestination)
                return;

            investigate.MarkChecked();

            currentSearchPoint = investigate.search();

            if (currentSearchPoint != null)
            {
                movement.MoveTo(currentSearchPoint.position, movement.searchSpeed);
            }
            else
            {
                investigate.destroy();
                memory.ClearMemory();

                ChangeState(MonsterState.Return);

            }
        }
    }
}
