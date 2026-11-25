using UnityEngine;
using UnityEngine.SceneManagement;

public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance;

    public ProgressState progress;
    
    public Transform[] keySpawns = new Transform[3];
    public GameObject exitBlock;
    public Transform selectedKeySpawn;
    public GameObject keyPrefab;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        
    }

    public void ChangeState(ProgressState state)
    {
        progress = state;

        switch (progress)
        {
            case ProgressState.Forest:
                break;

            case ProgressState.SpawnKey:
                AssignKeySpawns();
                RegisterExitBlock();
                SpawnKey();
                break;

            case ProgressState.KeyCollected:
                UnlockDoor();
                break;

        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        keySpawns[0] = null;
        keySpawns[1] = null;
        keySpawns[2] = null;
        selectedKeySpawn = null;
        exitBlock = null;

        if (GameDirector.Instance.currentArea == AreaType.Forest)
        {
            ChangeState(ProgressState.Forest);
        }
        else
        {
            ChangeState(ProgressState.SpawnKey);
        }
        
    }

    private void AssignKeySpawns()
    {
        
        keySpawns[0] = GameObject.Find("KeySpawn_01").transform;
        keySpawns[1] = GameObject.Find("KeySpawn_02").transform;
        keySpawns[2] = GameObject.Find("KeySpawn_03").transform;
        
    }

    private void RegisterExitBlock()
    {
        exitBlock = GameObject.Find("ExitBlock");
    }

    private void SpawnKey()
    {
        int random = Random.Range(0, keySpawns.Length);
        selectedKeySpawn = keySpawns[random];
        Instantiate(keyPrefab, selectedKeySpawn.position, Quaternion.identity);
    }

    private void UnlockDoor()
    {
        Destroy(exitBlock);
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
