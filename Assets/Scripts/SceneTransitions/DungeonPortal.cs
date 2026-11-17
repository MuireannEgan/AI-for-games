using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class DungeonPortal : MonoBehaviour
{
    [Header("Scene Transition")]
    [SerializeField] private string destinationScene = "Forest_Level";

    private bool isTransitioning;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTransitioning)
            return;

        CharacterController player =
            other.GetComponentInParent<CharacterController>();

        if (player == null || !player.CompareTag("Player"))
            return;

        if (!Application.CanStreamedLevelBeLoaded(destinationScene))
        {
            Debug.LogError(
                $"DungeonPortal: Scene '{destinationScene}' " +
                "is not in the build scene list.");
            return;
        }

        isTransitioning = true;
        SceneManager.LoadScene(destinationScene);
    }
}