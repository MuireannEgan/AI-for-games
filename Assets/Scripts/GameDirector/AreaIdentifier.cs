using UnityEngine;

public class AreaIdentifier : MonoBehaviour
{
    [SerializeField] private AreaType areaType;

    private void Start()
    {
        GameDirector.Instance.SetCurrentArea(areaType);
    }
}