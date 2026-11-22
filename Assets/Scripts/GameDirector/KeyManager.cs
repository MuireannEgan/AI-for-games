using UnityEngine;
using UnityEngine.SceneManagement;

public class KeyManager : MonoBehaviour
{
    public static KeyManager Instance;
    
    public Transform[] keySpawns = new Transform[3];
    public Transform selectedKeySpawn;
    public GameObject keyPrefab;

    private void Awake()
    {
        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (GameDirector.Instance.currentArea == AreaType.Forest)
        {
            return;
        }
        else
        {
            AssignKeySpawns();
            SpawnKey();
        }
        
    }

    private void AssignKeySpawns()
    {
        
        keySpawns[0] = GameObject.Find("KeySpawn_01").transform;
        keySpawns[1] = GameObject.Find("KeySpawn_02").transform;
        keySpawns[2] = GameObject.Find("KeySpawn_03").transform;
        
    }

    private void SpawnKey()
    {
        int random = Random.Range(0, keySpawns.Length);
        selectedKeySpawn = keySpawns[random];
        Instantiate(keyPrefab, selectedKeySpawn.position, Quaternion.identity);
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
