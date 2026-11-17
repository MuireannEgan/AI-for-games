
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameDirector : MonoBehaviour
{
    public static GameDirector Instance;

    public AreaType currentArea;
    public EnemyType currentEnemy;

    public Vector3 playerPosition;
    public Vector3 enemyPosition;

    private Transform player;
    private Transform enemy;

    public float encounterDistance = 15f;
    public bool encounterActive;
    public float currentEncounterDuration;
    public float lastEncounterDuration;

    public float timeSinceLastEncounter;
    public float lastTimeBetweenEncounters;

    public int totalEncounters;

    private float encounterStartTime;
    private float encounterEndTime;

    private bool hasPreviousEncounter;



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        UpdatePositions();
        UpdateEncounter();
    }

    private void UpdatePositions()
    {
        if(player == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if(obj != null)
            {
                player = obj.transform;
            }
        }

        if (currentArea == AreaType.Cave && enemy == null)
        {
            GameObject obj =
                GameObject.FindGameObjectWithTag("Enemy");
        
            if (obj != null)
            {
                enemy = obj.transform;
                EnemyIdentifier identity = enemy.GetComponent<EnemyIdentifier>();
                if(identity != null)
                {
                    currentEnemy = identity.enemyType;
                }
            }
                
        }

        if (player != null)
            playerPosition = player.position;

        if (enemy != null)
            enemyPosition = enemy.position;
    }

    private void UpdateEncounter()
    {
        if(player == null || enemy == null)
        {
            return;
        }

        float distance = Vector3.Distance(playerPosition, enemyPosition);
        bool withinRange = distance <= encounterDistance;

        if(withinRange && !encounterActive)
        {
            StartEncounter();
        }

        if(encounterActive)
        {
            if(withinRange)
            {
                currentEncounterDuration = Time.time - encounterStartTime;
            } else
            {
                EndEncounter();
            }
        }

        if(!encounterActive && hasPreviousEncounter)
        {
            timeSinceLastEncounter = Time.time - encounterEndTime;
        }
    
    }

    private void StartEncounter()
    {
        encounterActive = true;
        encounterStartTime = Time.time;
        currentEncounterDuration = 0f;

        if(hasPreviousEncounter)
        {
            lastTimeBetweenEncounters = Time.time - encounterEndTime;
        }

        totalEncounters++;
    }

    private void EndEncounter()
    {
        encounterActive = false;
        lastEncounterDuration = currentEncounterDuration;
        encounterEndTime = Time.time;
        currentEncounterDuration = 0f;
        timeSinceLastEncounter = 0f;
        hasPreviousEncounter = true;
    }

    public void SetCurrentArea(AreaType areatype)
    {
        currentArea = areatype;

        if(currentArea == AreaType.Forest)
        {
            enemy = null;
            enemyPosition = Vector3.zero;
            currentEnemy = EnemyType.None;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        player = null;
        enemy = null;

        encounterActive = false;
        currentEncounterDuration = 0f;

        UpdatePositions();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if(Instance == this)
        {
            Instance = null;
        }
    }
}
