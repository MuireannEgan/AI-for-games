using UnityEngine;

[System.Serializable]
public class ScentPoint{

    public Vector3 Position{
        get; private set;
    }

    public float InitialStrength{
        get; private set;
    }

    public float CreationTime{
        get; private set;
    }

    public float Lifetime{
        get; private set;
    }

    public ScentPoint(Vector3 position, float strength, float lifetime){
        Position = position;
        InitialStrength = strength;
        Lifetime = lifetime;
        CreationTime = Time.time;
    }

    public float GetCurrentStrength(){
        if(Lifetime <= 0f)
            return 0f;

        float age = Time.time - CreationTime;
        float remainingStrength = 1f - Mathf.Clamp01(age/Lifetime);


        return InitialStrength * remainingStrength;

    }

    public bool HasExpired(){
        return Time.time - CreationTime >= Lifetime;
    }

}