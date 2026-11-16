using UnityEngine;

public abstract class MonsterPerception : MonoBehaviour{
    public abstract bool TryGetDetection(out Vector3 detectedPosition, out float confidence);

}