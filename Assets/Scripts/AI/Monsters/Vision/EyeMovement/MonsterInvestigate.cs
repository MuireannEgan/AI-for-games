using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MonsterInvestigate 

{
    private Vector3 center;
    private MonsterMemory memory;

    public float circleRadius = 90f;

    private List<Transform> patrolPoints = new List<Transform>();

    private int pointIndex;

    public bool done
    {
        get { return pointIndex >= patrolPoints.Count; }
    }

    public void create(Vector3 LKP, Transform[] allPatrolPoints)
    {
        if (allPatrolPoints == null)
        {
            return;
        }
        center = LKP;
        patrolPoints.Clear();
        pointIndex = 0;

        foreach (Transform point in allPatrolPoints)
        {
            
            if (point == null)
                continue;

            Vector3 difference = point.position - center;

            difference.y = 0f;

            if (difference.magnitude <= circleRadius)
            {
                patrolPoints.Add(point);
            }
        }
    }

    public Transform search()
    {
        if (done)
            return null;

        return patrolPoints[pointIndex];
    }

    public void MarkChecked()
    {
        if (!done)
       
            pointIndex++;
        
    }

    public void destroy()
    {
        patrolPoints.Clear();
        pointIndex = 0;
    }

}
