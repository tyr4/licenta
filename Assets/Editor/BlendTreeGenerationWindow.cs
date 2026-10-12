using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class BlendTreeGeneratorWindow : EditorWindow
{
    private enum Rotation { Clockwise, CounterClockwise }
    private enum SaveMode { InsideAnimatorController, StandaloneAsset }

    private static readonly string[] StartLabels =
    {
        "Up (12 o'clock)", "Up-Right (1:30)", "Right (3 o'clock)", "Down-Right (4:30)",
        "Down (6 o'clock)", "Down-Left (7:30)", "Left (9 o'clock)", "Up-Left (10:30)", "Custom angle"
    };
    // Atan2 convention: 0 = right, 90 = up, counterclockwise positive
    private static readonly float[] StartAngles = { 90f, 45f, 0f, -45f, -90f, -135f, 180f, 135f, 0f };

    [SerializeField] private List<AnimationClip> _clips = new();

    private SerializedObject _so;
    private Vector2 _scroll;
    private int _startIndex;
    private float _customAngle = 90f;
    private Rotation _rotation = Rotation.CounterClockwise;
    private SaveMode _saveMode = SaveMode.StandaloneAsset;
    private AnimatorController _controller;
    private string _stateName = "DEFAULT";
    private string _paramX = "dirX";
    private string _paramY = "dirY";

    [MenuItem("Tools/Directional Blend Tree Generator")]
    private static void Open() => GetWindow<BlendTreeGeneratorWindow>("Blend Tree Generator");

    private void OnEnable() => _so = new SerializedObject(this);

    private void OnGUI()
    {
        _so.Update();

        DrawDropZone();

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(260));
        EditorGUILayout.PropertyField(_so.FindProperty("_clips"), new GUIContent("Clips (in rotation order)"), true);
        EditorGUILayout.EndScrollView();
        _so.ApplyModifiedProperties();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Sort by name"))
                _clips = _clips.Where(c => c != null)
                               .OrderBy(c => c.name, Comparer<string>.Create(EditorUtility.NaturalCompare))
                               .ToList();
            if (GUILayout.Button("Clear")) _clips.Clear();
        }

        int n = _clips.Count(c => c != null);
        EditorGUILayout.HelpBox(
            n == 0 ? "Add clips to see the angle spacing."
                   : $"{n} clips → {360f / n:0.##}° apart", MessageType.None);

        EditorGUILayout.Space();
        _startIndex = EditorGUILayout.Popup("First clip faces", _startIndex, StartLabels);
        if (_startIndex == StartLabels.Length - 1)
            _customAngle = EditorGUILayout.FloatField("Custom angle (°)", _customAngle);
        _rotation = (Rotation)EditorGUILayout.EnumPopup("Each next clip rotates", _rotation);

        EditorGUILayout.Space();
        _saveMode = (SaveMode)EditorGUILayout.EnumPopup("Save as", _saveMode);
        if (_saveMode == SaveMode.InsideAnimatorController)
        {
            _controller = (AnimatorController)EditorGUILayout.ObjectField(
                "Animator Controller", _controller, typeof(AnimatorController), false);
            _stateName = EditorGUILayout.TextField("State name", _stateName);
        }
        else
        {
            _stateName = EditorGUILayout.TextField("Asset name", _stateName);
        }
        _paramX = EditorGUILayout.TextField("Param X", _paramX);
        _paramY = EditorGUILayout.TextField("Param Y", _paramY);

        EditorGUILayout.Space();
        bool ready = n > 0 && (_saveMode == SaveMode.StandaloneAsset || _controller != null);
        using (new EditorGUI.DisabledScope(!ready))
            if (GUILayout.Button("Generate Blend Tree", GUILayout.Height(30)))
                Generate();
    }

    private void DrawDropZone()
    {
        var rect = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(rect, "Drop clips (or models containing clips) here");

        var e = Event.current;
        if (!rect.Contains(e.mousePosition)) return;

        if (e.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            e.Use();
        }
        else if (e.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            var dropped = new List<AnimationClip>();
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is AnimationClip c) { dropped.Add(c); continue; }
                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(obj)))
                    if (sub is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                        dropped.Add(clip);
            }
            // natural name order within one drop, appended after existing entries
            dropped.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));
            foreach (var c in dropped)
                if (!_clips.Contains(c)) _clips.Add(c);
            e.Use();
        }
    }

    private void Generate()
    {
        var clips = _clips.Where(c => c != null).ToList();

        BlendTree tree = _saveMode == SaveMode.InsideAnimatorController
            ? GetOrCreateTreeInController()
            : GetOrCreateStandaloneTree(clips[0]);

        tree.blendType = BlendTreeType.SimpleDirectional2D;
        tree.blendParameter = _paramX;
        tree.blendParameterY = _paramY;
        tree.children = new ChildMotion[0];

        float startAngle = _startIndex == StartLabels.Length - 1 ? _customAngle : StartAngles[_startIndex];
        float step = 360f / clips.Count * (_rotation == Rotation.CounterClockwise ? 1f : -1f);

        for (int i = 0; i < clips.Count; i++)
        {
            float a = (startAngle + step * i) * Mathf.Deg2Rad;
            tree.AddChild(clips[i], new Vector2(Mathf.Cos(a), Mathf.Sin(a)));
        }

        EditorUtility.SetDirty(tree);
        if (_controller != null) EditorUtility.SetDirty(_controller);
        AssetDatabase.SaveAssets();
        Debug.Log($"Built blend tree with {clips.Count} clips ({360f / clips.Count:0.##}° apart).");
    }

    private BlendTree GetOrCreateTreeInController()
    {
        AddParam(_paramX);
        AddParam(_paramY);

        var sm = _controller.layers[0].stateMachine;
        var state = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == _stateName)
                    ?? sm.AddState(_stateName);

        if (state.motion is BlendTree existing) return existing;

        var tree = new BlendTree { name = _stateName + " Tree" };
        AssetDatabase.AddObjectToAsset(tree, _controller);
        state.motion = tree;
        return tree;
    }

    // saved next to the first clip
    private BlendTree GetOrCreateStandaloneTree(AnimationClip firstClip)
    {
        string folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(firstClip));
        string path = $"{folder}/{_stateName}.asset".Replace('\\', '/');

        var existing = AssetDatabase.LoadAssetAtPath<BlendTree>(path);
        if (existing != null) return existing;

        var tree = new BlendTree { name = _stateName };
        AssetDatabase.CreateAsset(tree, path);
        return tree;
    }

    private void AddParam(string name)
    {
        if (_controller.parameters.Any(p => p.name == name)) return;
        _controller.AddParameter(name, AnimatorControllerParameterType.Float);
    }
}