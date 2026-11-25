using UnityEngine;

public class KeyCollect : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player")) {

            ProgressManager.Instance.ChangeState(ProgressState.KeyCollected);
            Destroy(gameObject);

        } else
        {
            return;
        }
    }
}
