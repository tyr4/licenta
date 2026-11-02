using UnityEngine;

[CreateAssetMenu(fileName = "NoiseSettings", menuName = "Scriptable Objects//World Generation/NoiseSettings")]
public class NoiseSettings : ScriptableObject
{
    [Range(0f, 50f)]
    public float noiseStrength;
    
    [Range(0f, 30f)]
    public float noiseScale;

    [Range(0f, 10f)]
    public float softness;
}
