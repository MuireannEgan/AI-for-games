
using UnityEngine;

public class GameDirector : MonoBehaviour
{
    public static GameDirector Instance;

    [SerializeField] private AreaType currentArea;
    [SerializeField] private Transform player;

    [SerializeField] private bool hasKey1;
    [SerializeField] private bool hasKey2;
    [SerializeField] private bool hasKey3;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetCurrentArea(AreaType area)
    {
        currentArea = area;
    }

    public AreaType GetCurrentArea()
    {
        return currentArea;
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
    }

    public Transform GetPlayer()
    {
        return player;
    }

    public Vector3 GetPlayerPosition()
    { 
        return player.position;
    }

    public void CollectKey(int keyNumber)
    {
        switch(keyNumber)
        {
            case 1:
                hasKey1 = true;
                break;

            case 2:
                hasKey2 = true;
                break;

            case 3:
                hasKey3 = true;
                break;
        }
    }

    public bool hasAllKeys()
    {
        return hasKey1 && hasKey2 && hasKey3;
    }
}
