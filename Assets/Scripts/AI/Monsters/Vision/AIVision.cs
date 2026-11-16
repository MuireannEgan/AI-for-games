using UnityEngine;

public class AIVision : MonoBehaviour
{
    public float DetectionRange = 10;
    public float DetectionAngle = 90f;

    public bool isInAngle, isInRange;

    public GameObject Player;

    private void Update()
    {
        
        isInAngle = false;
        isInRange = false;

      
        if(Vector3.Distance(transform.position, Player.transform.position) < DetectionRange)
        {
            isInRange = true;

            Vector3 directionToPlayer = (Player.transform.position - transform.position);
            directionToPlayer.y = 0f;
            directionToPlayer = directionToPlayer.normalized;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward = forward.normalized;

            float angle = Vector3.Angle(forward, directionToPlayer);

            if (angle < DetectionAngle / 2f)
            {
                isInAngle = true;
            }
            if(isInRange && isInAngle)
            {
                Debug.Log("in ai vision");
            }
            
        }

    }
}
