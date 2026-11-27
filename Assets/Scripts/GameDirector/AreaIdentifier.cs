using UnityEngine;

public class AreaIdentifier : MonoBehaviour
{
    [SerializeField] private AreaType areaType;

    [Header("MoNSTER SPAWN")]
    [SerializeField] private GameObject hound;
    [SerializeField] private GameObject eye;


    private void Awake()
    {
        GameDirector.Instance.SetCurrentArea(areaType);

        if (hound != null){
            hound.SetActive(GameDirector.Instance.spawnHound());
        }

        if (eye != null){
            eye.SetActive(GameDirector.Instance.spawnEye());
        }
    }
}