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

        ScreenTransition transition = ScreenTransition.Instance;
        if (transition == null || transition.IsBusy)
            return;

        bool leavingCave = SceneManager.GetActiveScene().name == "Cave_Level" &&
                           destinationScene == "Forest_Level";

        if (leavingCave && ProgressManager.Instance != null &&
            ProgressManager.Instance.progress == ProgressState.KeyCollected)
        {
            isTransitioning = transition.TryShowEnding(true);
        }
        else
        {
            isTransitioning = transition.TryLoadScene(destinationScene);
        }
    }
}