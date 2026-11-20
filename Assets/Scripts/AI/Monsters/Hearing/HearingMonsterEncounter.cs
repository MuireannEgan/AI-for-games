using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MonsterAI), typeof(MonsterMovement))]
public class HearingMonsterEncounter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MonsterAI monsterAI;
    [SerializeField] private MonsterMovement movement;
    [SerializeField] private HearingMonsterAnimator monsterAnimator;
    [SerializeField] private Transform player;

    [Header("Attack")]
    [Min(0.1f)] [SerializeField] private float attackRange = 1.1f;
    [Min(0.1f)] [SerializeField] private float attackDuration = 1.1f;
    [Min(0f)] [SerializeField] private float attackCooldown = 1.5f;
    [Min(0f)] [SerializeField] private float recentSoundWindow = 2f;

    [Header("Scream")]
    [Min(0.1f)] [SerializeField] private float screamDuration = 1.4f;
    [Min(0f)] [SerializeField] private float screamCooldown = 5f;

    private MonsterState previousState;
    private float nextAttackTime;
    private float nextScreamTime;
    private bool performingAction;
    private Coroutine currentAction;

    private void Awake()
    {
        if (monsterAI == null) monsterAI = GetComponent<MonsterAI>();
        if (movement == null) movement = GetComponent<MonsterMovement>();
        if (monsterAnimator == null) monsterAnimator = GetComponent<HearingMonsterAnimator>();
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }
        previousState = monsterAI.currentState;
    }

    private void Update()
    {
        if (performingAction || monsterAI == null || movement == null)
            return;

        MonsterState state = monsterAI.currentState;
        bool engaged = state == MonsterState.Chase || state == MonsterState.Investigate;
        bool recentSound = monsterAI.memory.hasLKP &&
                           monsterAI.memory.GetTimeSinceDetection() <= recentSoundWindow;

        if (engaged && recentSound && player != null && Time.time >= nextAttackTime)
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;
            if (direction.magnitude <= attackRange)
            {
                StartAction(Attack());
                return;
            }
        }

        if (state == MonsterState.Chase && previousState != MonsterState.Chase &&
            Time.time >= nextScreamTime)
        {
            StartAction(Scream());
            return;
        }

        previousState = state;
    }

    private void StartAction(IEnumerator action)
    {
        performingAction = true;
        previousState = monsterAI.currentState;
        monsterAI.enabled = false;
        movement.StopMovement();
        currentAction = StartCoroutine(action);
    }

    private IEnumerator Scream()
    {
        nextScreamTime = Time.time + screamDuration + screamCooldown;
        if (monsterAnimator != null) monsterAnimator.PlayScream();
        yield return new WaitForSeconds(screamDuration);
        EndAction();
    }

    private IEnumerator Attack()
    {
        nextAttackTime = Time.time + attackDuration + attackCooldown;
        monsterAI.currentState = MonsterState.Attack;
        if (monsterAnimator != null) monsterAnimator.PlayAttack();
        yield return new WaitForSeconds(attackDuration);
        monsterAI.currentState = MonsterState.Idle;
        EndAction();
    }

    private void EndAction()
    {
        movement.StopMovement();
        performingAction = false;
        currentAction = null;
        if (monsterAI != null) monsterAI.enabled = true;
    }

    private void OnDisable()
    {
        if (currentAction != null) StopCoroutine(currentAction);
        currentAction = null;
        performingAction = false;
        if (monsterAI != null && !monsterAI.enabled) monsterAI.enabled = true;
    }
}