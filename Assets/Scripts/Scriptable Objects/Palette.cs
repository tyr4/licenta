using UnityEngine;

[CreateAssetMenu(fileName = "Palette", menuName = "Scriptable Objects/World Generation/Palette")]
public class Palette : ScriptableObject
{
    public string paletteName;
    public Texture2D matR, matG, matB;

    [Range(0f, 1f)]
    public float textureScale;
}