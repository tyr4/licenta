using UnityEngine;

[CreateAssetMenu(
    fileName = "World Generation Settings", 
    menuName = "Scriptable Objects//World Generation/World Generation Settings")]
public class WorldGenerationSettings : ScriptableObject
{
    public int seed = 1;
    public int chunkSize = 512;
    public int octaves = 4;
    public int lacunarity = 2;
    public float gain = 0.5f;
    public float frequency = 0.01f;
    public float tileThreshold = 0.5f;
}
