
using UnityEngine;

public class EyePerception : MonsterPerception
{

    public float detectionRange = 17;
    public float detectionAngle = 90f;

    public bool isInAngle, isInRange, isNotHidden;

    public Transform target;

    public override bool TryGetDetection(out Vector3 detectedPosition, out float confidence)
    {
        detectedPosition = Vector3.zero;
        confidence = 0f;

        if (target == null)
            return false;

        if (isInRange && isInAngle && isNotHidden)
        {

            detectedPosition = target.position;

            confidence = 0.5f;

            return true;
        }
        return false;
    }

    public bool CanSee
    {
        get { return isInRange && isInAngle && isNotHidden; }
    }

    private void Update()
    {

        isInAngle = false;
        isInRange = false;
        isNotHidden = false;

        if (target == null)
            return;

        Vector3 dir = target.transform.position - transform.position;

        if (dir.magnitude < detectionRange)
        {
            isInRange = true;


            Vector3 moveDir = new Vector3(dir.x, 0f, dir.z).normalized;

            Vector3 forward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

            float angle = Vector3.Angle(forward, moveDir);

            if (angle < detectionAngle / 2f)
            {
                isInAngle = true;
            }

            if (isInAngle)
            {
                if (Physics.Raycast(transform.position, dir.normalized, out RaycastHit hit, dir.magnitude))
                {
                    if (hit.transform == target)
                    {
                        isNotHidden = true;
                    }
                }
            }
        }


        if (isInRange && isInAngle && isNotHidden)
        {
            Debug.Log("in ai vision");
        }
        else
        {
            Debug.Log("Not in ai vision");
        }

    }

    
}
