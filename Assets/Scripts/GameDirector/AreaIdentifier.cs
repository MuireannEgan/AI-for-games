using UnityEngine;

public class AreaIdentifier : MonoBehaviour
{
    [SerializeField] private AreaType areaType;

    private void Awake()
    {
        GameDirector.Instance.SetCurrentArea(areaType);
    }
}