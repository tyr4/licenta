using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    private static readonly int Seed = Shader.PropertyToID("_Seed");
    
    // !! the object needs the Levitate material for this to work !!
    private void Start()
    {
        var mpb = new MaterialPropertyBlock();
        var sr = GetComponent<SpriteRenderer>();
        
        sr.GetPropertyBlock(mpb);
        mpb.SetFloat(Seed, Random.Range(0f, 1.53f));
        sr.SetPropertyBlock(mpb);
    }
}