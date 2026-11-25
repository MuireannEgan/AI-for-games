using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class DungeonPortal : MonoBehaviour
{
    [Header("Scene Transition")]
    [SerializeField] private string destinationScene = "Forest_Level";

    [Header("Return Spawn")]
    [SerializeField, Min(0.5f)]
    private float returnClearance = 1.5f;

    private bool isTransitioning;
    private BoxCollider portalCollider;

    private static bool hasReturnSpawn;
    private static Vector3 returnPosition;
    private static Quaternion returnRotation;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ClearReturnSpawn()
    {
        hasReturnSpawn = false;
        returnPosition = Vector3.zero;
        returnRotation = Quaternion.identity;
    }

    private void Awake()
    {
        portalCollider = GetComponent<BoxCollider>();
        portalCollider.isTrigger = true;
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

        if (SceneManager.GetActiveScene().name == "Forest_Level")
        {
            SaveReturnSpawn(player);
        }

        isTransitioning = true;
        SceneManager.LoadScene(destinationScene);
    }

    private void SaveReturnSpawn(CharacterController player)
    {
        Vector3 outward = transform.right;
        outward.y = 0f;
        outward.Normalize();

        Vector3 portalCentre =
            transform.TransformPoint(portalCollider.center);

        float halfWidth =
            portalCollider.size.x * 0.5f *
            Mathf.Abs(transform.lossyScale.x);

        returnPosition = portalCentre +
            outward * (halfWidth + returnClearance + player.radius);

        returnPosition.y = player.transform.position.y;

        returnRotation = Quaternion.LookRotation(outward);

        hasReturnSpawn = true;

        Debug.Log(
            $"DungeonPortal: Saved return position {returnPosition}");
    }

    public static bool TryRestoreReturnSpawn()
    {
        if (!hasReturnSpawn)
            return false;

        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning(
                "DungeonPortal: Unable to restore return spawn. " +
                "Player not found.");
            return false;
        }

        CharacterController controller =
            player.GetComponent<CharacterController>();

        bool wasEnabled =
            controller != null && controller.enabled;

        if (wasEnabled)
            controller.enabled = false;

        player.transform.SetPositionAndRotation(
            returnPosition,
            returnRotation);

        Physics.SyncTransforms();

        if (wasEnabled)
            controller.enabled = true;

        Debug.Log(
            $"DungeonPortal: Restored player to {returnPosition}");

        ClearReturnSpawn();

        return true;
    }
}