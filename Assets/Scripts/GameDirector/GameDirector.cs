
using UnityEngine;

public class GameDirector : MonoBehaviour
{
    public static GameDirector Instance;

    [SerializeField] private AreaType currentArea;
    [SerializeField] private Transform player;

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
}
