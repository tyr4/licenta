using System.IO;
using UnityEditor;
using UnityEngine;

public class SoftLightSpriteWindow : EditorWindow
{
    // Settings
    int size = 512;
    float innerAngle = 5f;
    float outerAngle = 30f;
    float innerDist = 0.15f;
    float outerDist = 1f;
    float falloffPower = 1f;
    string savePath = "Assets/Sprites/flashlight_soft.png";

    Texture2D preview;
    Vector2 scroll;

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
        if (preview != null) DestroyImmediate(preview);
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Cone (points up)", EditorStyles.boldLabel);
        innerAngle = EditorGUILayout.Slider("Inner Half-Angle", innerAngle, 0f, 89f);
        outerAngle = EditorGUILayout.Slider("Outer Half-Angle", outerAngle, 1f, 90f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Reach", EditorStyles.boldLabel);
        innerDist = EditorGUILayout.Slider("Inner Distance", innerDist, 0f, 1.5f);
        outerDist = EditorGUILayout.Slider("Outer Distance", outerDist, 0.05f, 1.5f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);
        falloffPower = EditorGUILayout.Slider("Falloff Power", falloffPower, 0.2f, 4f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        size = EditorGUILayout.IntPopup("Size", size,
            new[] { "128", "256", "512", "1024" },
            new[] { 128, 256, 512, 1024 });
        savePath = EditorGUILayout.TextField("Save Path", savePath);

        if (EditorGUI.EndChangeCheck())
        {
            if (outerAngle <= innerAngle) outerAngle = innerAngle + 1f;
            if (outerDist <= innerDist) outerDist = innerDist + 0.05f;
            RebuildPreview();
        }

        // Warn about hard cutoffs at the image edges
        if (outerAngle > 30f && outerDist > 0.5f)
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
        
        if (preview != null) GUI.DrawTexture(r, preview, ScaleMode.ScaleToFit, false);

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

            float radial = 1f - Smooth(innerDist, outerDist, dist);
            float angular = 1f - Smooth(innerAngle, outerAngle, angle);
            float v = Mathf.Pow(radial * angular, falloffPower);

            byte b = (byte)Mathf.RoundToInt(Mathf.Clamp01(v) * 255f);
            pixels[y * res + x] = new Color32(b, b, b, 255);
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    void RebuildPreview()
    {
        if (preview != null) DestroyImmediate(preview);
        preview = BuildTexture(128);
        preview.wrapMode = TextureWrapMode.Clamp;
        Repaint();
    }

    void Generate()
    {
        string dir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var tex = BuildTexture(size);
        File.WriteAllBytes(savePath, tex.EncodeToPNG());
        DestroyImmediate(tex);

        AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);

        var imp = (TextureImporter)AssetImporter.GetAtPath(savePath);
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

        var asset = AssetDatabase.LoadAssetAtPath<Object>(savePath);
        EditorGUIUtility.PingObject(asset);
        Debug.Log($"Soft light sprite saved to {savePath}");
    }
}