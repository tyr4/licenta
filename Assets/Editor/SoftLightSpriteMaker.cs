using System.IO;
using UnityEditor;
using UnityEngine;

public class SoftLightSpriteWindow : EditorWindow
{
    // Settings
    private int _size = 512;
    private float _innerAngle = 5f;
    private float _outerAngle = 30f;
    private float _innerDist = 0.15f;
    private float _outerDist = 1f;
    private float _falloffPower = 1f;
    private string _savePath = "Assets/Sprites/flashlight_soft.png";

    private Texture2D _preview;
    private Vector2 _scroll;

    [MenuItem("Tools/Soft Light Sprite Generator")]
    static void Open()
    {
        var w = GetWindow<SoftLightSpriteWindow>("Soft Light Sprite");
        w.minSize = new Vector2(320, 520);
        w.RebuildPreview();
    }

    void OnEnable() => RebuildPreview();

    void OnDisable()
    {
        if (_preview != null) DestroyImmediate(_preview);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Cone (points up)", EditorStyles.boldLabel);
        _innerAngle = EditorGUILayout.Slider("Inner Half-Angle", _innerAngle, 0f, 89f);
        _outerAngle = EditorGUILayout.Slider("Outer Half-Angle", _outerAngle, 1f, 90f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Reach", EditorStyles.boldLabel);
        _innerDist = EditorGUILayout.Slider("Inner Distance", _innerDist, 0f, 1.5f);
        _outerDist = EditorGUILayout.Slider("Outer Distance", _outerDist, 0.05f, 1.5f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);
        _falloffPower = EditorGUILayout.Slider("Falloff Power", _falloffPower, 0.2f, 4f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        _size = EditorGUILayout.IntPopup("Size", _size,
            new[] { "128", "256", "512", "1024" },
            new[] { 128, 256, 512, 1024 });
        _savePath = EditorGUILayout.TextField("Save Path", _savePath);

        if (EditorGUI.EndChangeCheck())
        {
            if (_outerAngle <= _innerAngle) _outerAngle = _innerAngle + 1f;
            if (_outerDist <= _innerDist) _outerDist = _innerDist + 0.05f;
            RebuildPreview();
        }

        // Warn about hard cutoffs at the image edges
        if (_outerAngle > 30f && _outerDist > 0.5f)
        {
            EditorGUILayout.HelpBox(
                "Wide angle plus long reach can touch the left/right image edges and " +
                "cause a hard cutoff. Lower the angle or the outer distance.",
                MessageType.Warning);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        
        float previewSize = Mathf.Min(position.width - 24f, 280f);
        Rect r = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.ExpandWidth(false));
        EditorGUI.DrawRect(r, Color.black);
        
        if (_preview != null) GUI.DrawTexture(r, _preview, ScaleMode.ScaleToFit, false);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Sprite", GUILayout.Height(32)))
            Generate();

        EditorGUILayout.EndScrollView();
    }

    // Shader-style smoothstep
    static float Smooth(float e0, float e1, float x)
    {
        if (e1 <= e0) return x >= e1 ? 1f : 0f;
        float t = Mathf.Clamp01((x - e0) / (e1 - e0));
        return t * t * (3f - 2f * t);
    }

    Texture2D BuildTexture(int res)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        var pixels = new Color32[res * res];

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            // Origin at bottom-center, cone points up, square pixels
            float dx = x / (res - 1f) - 0.5f;
            float dy = y / (res - 1f);

            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(Mathf.Abs(dx), dy) * Mathf.Rad2Deg; // 0 = straight up

            float radial = 1f - Smooth(_innerDist, _outerDist, dist);
            float angular = 1f - Smooth(_innerAngle, _outerAngle, angle);
            float v = Mathf.Pow(radial * angular, _falloffPower);

            byte b = (byte)Mathf.RoundToInt(Mathf.Clamp01(v) * 255f);
            pixels[y * res + x] = new Color32(b, b, b, 255);
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    void RebuildPreview()
    {
        if (_preview != null) DestroyImmediate(_preview);
        _preview = BuildTexture(128);
        _preview.wrapMode = TextureWrapMode.Clamp;
        Repaint();
    }

    void Generate()
    {
        string dir = Path.GetDirectoryName(_savePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var tex = BuildTexture(_size);
        File.WriteAllBytes(_savePath, tex.EncodeToPNG());
        DestroyImmediate(tex);

        AssetDatabase.ImportAsset(_savePath, ImportAssetOptions.ForceUpdate);

        var imp = (TextureImporter)AssetImporter.GetAtPath(_savePath);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.filterMode = FilterMode.Bilinear;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency = false;

        var settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0f); // bottom-center
        imp.SetTextureSettings(settings);
        imp.SaveAndReimport();

        var asset = AssetDatabase.LoadAssetAtPath<Object>(_savePath);
        EditorGUIUtility.PingObject(asset);
        Debug.Log($"Soft light sprite saved to {_savePath}");
    }
}