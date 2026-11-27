using UnityEngine;

public class AreaIdentifier : MonoBehaviour
{
    [SerializeField] private AreaType areaType;

    [Header("MoNSTER SPAWN")]
    [SerializeField] private GameObject hound;

    private void Awake()
    {
        GameDirector.Instance.SetCurrentArea(areaType);

        if(areaType == AreaType.Cave && hound != null){
            hound.SetActive(GameDirector.Instance.spawnHound());
        }
    }
}