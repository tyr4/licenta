using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways]
public class GroundZones : MonoBehaviour
{
    [System.Serializable]
    public class Zone
    {
        public int paletteIndex;

        [Header("Rectangle (world coordinates")]
        public Vector2 min, max;
    
        [Header("Blend")]
        public NoiseSettings noiseSettings;
    }
    
    [Header("Texture quality")]
    [SerializeField] private int textureSize = 512;
    
    [Header("Zone Data")]
    [SerializeField] private List<Palette> palettes = new();
    [SerializeField] private List<Zone> zones = new();

    private const int MaxZones = 256;
    private const int MaxPalettes = 16;
    
    private Texture2DArray _textures;
    private int _signature;

    private void OnEnable()
    {
        Apply(true);
    }

    private void OnValidate()
    {
        Apply(false);
    }

    private void Update()
    {
        if (!Application.isPlaying)
            Apply(false);
    }

    private void OnDisable()
    {
        Shader.SetGlobalFloat("_ZoneCount", 0);
    }

    private void OnDestroy()
    {
        Kill();
    }

    private void Kill()
    {
        if (_textures == null) return;
        
        if (Application.isPlaying) Destroy(_textures); 
        else DestroyImmediate(_textures);
        
        _textures = null;
    }
    
    private void Apply(bool forceRebuild)
    {
        if (zones.Count == 0 || palettes.Count == 0)
        {
            Shader.SetGlobalFloat("_ZoneCount", 0);
            return;
        }
        
        int sig = 17;
        
        var zoneList = zones.FindAll(z => z != null && z.paletteIndex >= 0 && z.paletteIndex < palettes.Count && z.noiseSettings);        
        var paletteList = palettes.FindAll(p => p != null && p.matR && p.matG && p.matB);
        
        // handle exit cases
        if (paletteList.Count == 0 || zones.Count == 0)
        {
            Shader.SetGlobalFloat("_ZoneCount", 0);
            return;
        }

        if (paletteList.Count > MaxPalettes)
        {
            paletteList = paletteList.GetRange(0, MaxPalettes);
        }
        
        // recompute the signature and decide updating the texture
        foreach (var zone in zoneList)
        {
            var palette = paletteList[zone.paletteIndex];
            
            sig = sig * 31 + palette.matR.GetInstanceID() * 7 + palette.matG.GetInstanceID() * 13 +  palette.matB.GetInstanceID();
        }
        
        if (forceRebuild || _textures == null || sig != _signature)
        {
            Rebuild(paletteList);
            _signature = sig;
        }

        if (!_textures) return;

        // rectangles and styles are cheap, so they're uploaded every time
        var rect = new Vector4[MaxZones];
        var style = new Vector4[MaxZones];
        for (int i = 0; i < zoneList.Count; i++)
        {
            var zone = zoneList[i];
            if (i >= MaxZones) continue;

            var noise = zone.noiseSettings;
            rect[i] = new Vector4(zone.min.x, zone.min.y, zone.max.x, zone.max.y);
            style[i] = new Vector4(noise.noiseStrength, noise.noiseScale, zone.paletteIndex, noise.softness);
        }
        
        var info = new Vector4[MaxPalettes];
        for (int i = 0; i < palettes.Count; i++)
        {
            info[i] = new Vector4(palettes[i].textureScale, 0, 0, 0);
        }

        Shader.SetGlobalTexture("_ZoneTex", _textures);
        Shader.SetGlobalFloat("_ZoneCount", zoneList.Count);
        Shader.SetGlobalFloat("_PaletteCount", palettes.Count);
        Shader.SetGlobalVectorArray("_ZoneRect", rect);
        Shader.SetGlobalVectorArray("_ZoneStyle", style);
        Shader.SetGlobalVectorArray("_PaletteInfo", info);
    }

    private void Rebuild(List<Palette> paletteList)
    {
        Kill();

        // fixed size + fixed format, so any source texture fits
        _textures = new Texture2DArray(textureSize, textureSize, paletteList.Count * 3,
            TextureFormat.RGBA32, false, false);   // last false = sRGB
        _textures.hideFlags = HideFlags.HideAndDontSave;
        _textures.wrapMode = TextureWrapMode.Repeat;
        _textures.filterMode = FilterMode.Bilinear;

        var rt = RenderTexture.GetTemporary(textureSize, textureSize, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var readback = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false, false);
        var prev = RenderTexture.active;

        for (int i = 0; i < paletteList.Count; i++)
        {
            var palette = paletteList[i];
            
            Upload(palette.matR, i * 3 + 0, rt, readback);
            Upload(palette.matG, i * 3 + 1, rt, readback);
            Upload(palette.matB, i * 3 + 2, rt, readback);
        }

        _textures.Apply(false);

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        if (Application.isPlaying) Destroy(readback); else DestroyImmediate(readback);
    }

    private void Upload(Texture2D src, int slice, RenderTexture rt, Texture2D readback)
    {
        Graphics.Blit(src, rt);                       // resizes and converts format
        RenderTexture.active = rt;
        readback.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
        readback.Apply(false);
        _textures.SetPixels32(readback.GetPixels32(), slice);
    }

    private void OnDrawGizmosSelected()
    {
        foreach (var z in zones)
        {
            if (z == null) continue;
            Vector2 c = (z.min + z.max) * 0.5f, s = z.max - z.min;
            Gizmos.color = new Color(0.6f, 0.4f, 0.2f, 1f);
            Gizmos.DrawWireCube(c, s);
            Gizmos.color = new Color(0.6f, 0.4f, 0.2f, 0.4f);

            if (!z.noiseSettings) continue;
            Gizmos.DrawWireCube(c, s - Vector2.one * 2f * z.noiseSettings.softness);   // where the fade ends
        }
    }
}