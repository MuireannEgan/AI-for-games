
using UnityEngine;

public class GameDirector : MonoBehaviour
{
    public static GameDirector Instance;

    public AreaType currentArea;

    public Vector3 playerPosition;
    public Vector3 enemyPosition;

    private Transform player;
    private Transform enemy;

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

    private void Update()
    {
        UpdatePositions();
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

        //if (enemy == null)
        //{
        //    GameObject obj =
        //        GameObject.FindGameObjectWithTag("Enemy");
        //
        //    if (obj != null)
        //        enemy = obj.transform;
        //}

        if (player != null)
            playerPosition = player.position;

        if (enemy != null)
            enemyPosition = enemy.position;
    }

    public void SetCurrentArea(AreaType areatype)
    {
        currentArea = areatype;
    }
}
