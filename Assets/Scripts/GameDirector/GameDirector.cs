
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameDirector : MonoBehaviour
{
    public static GameDirector Instance;

    public AreaType currentArea;
    public EnemyType currentEnemy;
    public DirectorState currentState;

    public Vector3 playerPosition;
    public Vector3 enemyPosition;

    private Transform player;
    private Transform enemy;

    public GameObject soundTrigger;
    public GameObject smellTrigger;
    public GameObject sightTrigger;
    private GameObject spawnedTrigger;
    private bool triggerSpawned = false;

    public float encounterStartDistance = 5f;
    public float encounterEndDistance = 10f;

    public bool encounterActive;

    private float encounterEndTime;
    public float timeSinceLastEncounter;

    public int totalEncounters;

    private bool hasPreviousEncounter; 


    [Header("Monster spawn sEttings")]
    public int houndCave;
    public int currentCave = -1;


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
        houndCave = Random.Range(1,4);
        Debug.Log("Hound cave is : "+houndCave);
    }

    private void Update()
    {
        UpdatePositions();
        UpdateEncounter();
        UpdateDirectorState();
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
            if(encounterActive)
            {
                EndEncounter();
            }

            return;
        }

        float distance = Vector3.Distance(playerPosition, enemyPosition);

        if(!encounterActive)
        {
            if(distance <= encounterStartDistance)
            {
                StartEncounter();
            }

        } else
        {
            if(distance > encounterEndDistance)
            {
                EndEncounter();
            } 
        }

        if(!encounterActive && hasPreviousEncounter)
        {
            timeSinceLastEncounter = Time.time - encounterEndTime;
        }
        
     }

    private void UpdateDirectorState()
    {
        if(currentArea == AreaType.Forest)
        {
            ChangeState(DirectorState.Idle);
            return;
        }

        if(encounterActive)
        {
            return;
        }

        
        if(hasPreviousEncounter && timeSinceLastEncounter >= 10f)
        {
            if(currentEnemy == EnemyType.Sound)
            {
                ChangeState(DirectorState.TriggerSound);
                return;
            } 
            else if(currentEnemy == EnemyType.Smell)
            {
                ChangeState(DirectorState.TriggerSmell);
                return;
            } 
            else if(currentEnemy == EnemyType.Sight)
            {
                ChangeState(DirectorState.TriggerSight);
                return;
            }
        }

    }

    public void ChangeState(DirectorState state)
    {
        if(currentState == state)
        {
            return;
        }
        
        currentState = state;

        switch(currentState)
        {
            case DirectorState.Idle:
                //No enemy in hub so no action needed
                break;

            case DirectorState.Active:
                //Player in encounter so no action needed
                break;

            case DirectorState.Searching:
                //Not currently in encounter
                break;

            case DirectorState.TriggerSound:
                SpawnTrigger(soundTrigger);
                break;

            case DirectorState.TriggerSmell:
                SpawnTrigger(smellTrigger);
                break;

            case DirectorState.TriggerSight:
                SpawnTrigger(sightTrigger);
                break;
        }
    }

    private void SpawnTrigger(GameObject trigger)
    {
        if(triggerSpawned)
        {
            return;
        }
        
        if(player == null || trigger == null)
        {
            return;
        }

        spawnedTrigger = Instantiate(trigger, playerPosition, Quaternion.identity);
        Debug.Log("trigger spawned");

        triggerSpawned = true;

    }

    private void StartEncounter()
    {
        encounterActive = true;

        ChangeState(DirectorState.Active);

        if(spawnedTrigger != null)
        {
            Destroy(spawnedTrigger);
        }

        spawnedTrigger = null;
        triggerSpawned = false;

        totalEncounters++;
    }

    private void EndEncounter()
    {
        encounterActive = false;
        encounterEndTime = Time.time;
        timeSinceLastEncounter = 0f;
        hasPreviousEncounter = true;

        ChangeState(DirectorState.Searching);
    }

    public void SetCurrentArea(AreaType areatype)
    {
        currentArea = areatype;

        if(currentArea == AreaType.Forest)
        {
            if(encounterActive)
            {
                EndEncounter();
            }

            enemy = null;
            enemyPosition = Vector3.zero;
            currentEnemy = EnemyType.None;

            encounterActive = false;
            totalEncounters = 0;

            timeSinceLastEncounter = 0f;

            encounterEndTime = 0f;

            hasPreviousEncounter = false;

            ChangeState(DirectorState.Idle);
        }
        else
        {
            ChangeState(DirectorState.Searching);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        player = null;
        enemy = null;

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

    public void setCurrentCave(int caveID){
        currentCave = caveID;
        Debug.Log("Player entering cave : "+currentCave);
    }

    public bool spawnHound(){
        return currentArea == AreaType.Cave && currentCave == houndCave;
    }
}
