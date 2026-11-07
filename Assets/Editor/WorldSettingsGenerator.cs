using System.IO;
using UnityEditor;
using UnityEngine;

public class WorldSettingsGenerator : EditorWindow
{
    private WorldGenerationSettings _settings;

    private string _savePath = "Assets/Scriptable Objects/Generated/World Settings.asset";

    private Texture2D _preview;
    private Vector2 _scroll;

    [MenuItem("Tools/World Generation Settings Generator")]
    private static void Open()
    {
        var w = GetWindow<WorldSettingsGenerator>("Soft Light Sprite");
        w.minSize = new Vector2(320, 520);
        w.RebuildPreview();
    }

    private void OnEnable()
    {
        _settings = CreateInstance<WorldGenerationSettings>();
        RebuildPreview();
    }
    
    private void OnDisable()
    {
        if (_preview != null)
            DestroyImmediate(_preview);
    }

    private void RebuildPreview()
    {
        if (_preview != null) DestroyImmediate(_preview);
        _preview = BuildTexture(_settings.chunkSize);
        _preview.wrapMode = TextureWrapMode.Clamp;
        Repaint();
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Texture scaling", EditorStyles.boldLabel);
        _settings.chunkSize = EditorGUILayout.IntPopup("Size", _settings.chunkSize,
            new[] { "128", "256", "512", "1024" },
            new[] { 128, 256, 512, 1024 });

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Noise Parameters", EditorStyles.boldLabel);
        _settings.seed = EditorGUILayout.IntField("Seed", _settings.seed);
        _settings.octaves = EditorGUILayout.IntSlider("Octaves", _settings.octaves, 0, 25);
        _settings.lacunarity = EditorGUILayout.IntSlider("Lacunarity", _settings.lacunarity, 0, 25);
        _settings.gain = EditorGUILayout.Slider("Gain", _settings.gain, 0, 3f);
        _settings.frequency = EditorGUILayout.Slider("Frequency", _settings.frequency, 0, 1f);
        _settings.tileThreshold = EditorGUILayout.Slider("Tile threshold", _settings.tileThreshold, 0, 1f);
        
        if (EditorGUI.EndChangeCheck())
        {
            RebuildPreview();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        
        Rect avail = GUILayoutUtility.GetRect(
            0, 0,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true));

        float size = Mathf.Min(avail.width, avail.height);
        Rect r = new Rect(
            avail.x + (avail.width - size) * 0.5f,
            avail.y + (avail.height - size) * 0.5f,
            size, size);

        EditorGUI.DrawRect(r, Color.black);

        if (_preview != null)
            GUI.DrawTexture(r, _preview, ScaleMode.ScaleToFit, false);
        
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Save settings", GUILayout.Height(32)))
            SaveSettings();
        
        EditorGUILayout.EndScrollView();
    }

    Texture2D BuildTexture(int res)
    {
        var generator = new BaseGenerator(_settings);
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        var pixels = new Color32[res * res];
        var half = res / 2;

        for (int y = -half; y < half; y++)
        {
            for (int x = -half; x < half; x++)
            {
                var tile = generator.GetBaseTile(x, y);
                
                pixels[(y + half) * res + (x + half)] = GetColor(tile);
            }
        }
        
        tex.filterMode = FilterMode.Point;
        tex.SetPixels32(pixels);
        tex.Apply();
        
        return tex;
    }

    private Color GetColor(TileType tile)
    {
        return tile switch
        {
            TileType.Grass => new Color32(60, 170, 60, 255),
            TileType.Dirt => new Color32(200, 170, 80, 255),
            _ => new Color32(255, 255, 255, 255)
        };
    }

    private void SaveSettings()
    {
        EnsureFolder(Path.GetDirectoryName(_savePath)!.Replace('\\', '/'));
        
        var existing = AssetDatabase.LoadAssetAtPath<WorldGenerationSettings>(_savePath);

        if (existing == null)
        {
            var copy = Instantiate(_settings);
            copy.name = Path.GetFileNameWithoutExtension(_savePath);
            
            AssetDatabase.CreateAsset(copy, _savePath);
        }
        else
        {
            EditorUtility.CopySerialized(_settings, existing);
            EditorUtility.SetDirty(existing);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    
    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string parent = Path.GetDirectoryName(folder)!.Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
