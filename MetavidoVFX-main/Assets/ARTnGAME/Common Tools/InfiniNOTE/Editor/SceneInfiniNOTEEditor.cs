using UnityEditor;
using UnityEngine;
namespace Artngame.CommonTools
{
    [CustomEditor(typeof(SceneInfiniNOTE))]
public class SceneInfiniNOTEEditor : Editor
{
    private Vector2 scrollPos;
    private GUIStyle textStyle;

    private const string FontSizeKey = "SceneInfiniNOTE_FontSize";
    private const string RichTextKey = "SceneInfiniNOTE_RichText";
    private const string PanelSizeKey = "SceneInfiniNOTE_PanelSize";

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;

        // Load EditorPrefs
        if (EditorPrefs.HasKey(PanelSizeKey))
            ((SceneInfiniNOTE)target).panelSize = EditorPrefs.GetInt(PanelSizeKey);
        if (EditorPrefs.HasKey(FontSizeKey))
        ((SceneInfiniNOTE)target).fontSize = EditorPrefs.GetInt(FontSizeKey);
        if (EditorPrefs.HasKey(RichTextKey))
            ((SceneInfiniNOTE)target).useRichText = EditorPrefs.GetBool(RichTextKey);
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        SceneInfiniNOTE noteTarget = (SceneInfiniNOTE)target;

        EditorGUILayout.Space();

        // Panel size
        int newPanelSize = EditorGUILayout.IntSlider("Panel Size", noteTarget.panelSize, 100, 1400);
        if (newPanelSize != noteTarget.panelSize)
        {
            noteTarget.panelSize = newPanelSize;
            EditorPrefs.SetInt(PanelSizeKey, newPanelSize);
        }

        // Font size
        int newFontSize = EditorGUILayout.IntSlider("Font Size", noteTarget.fontSize, 10, 40);
        if (newFontSize != noteTarget.fontSize)
        {
            noteTarget.fontSize = newFontSize;
            EditorPrefs.SetInt(FontSizeKey, newFontSize);
        }

        // Rich text
        bool newRichText = EditorGUILayout.Toggle("Use Rich Text", noteTarget.useRichText);
        if (newRichText != noteTarget.useRichText)
        {
            noteTarget.useRichText = newRichText;
            EditorPrefs.SetBool(RichTextKey, newRichText);
        }

        // Show in Scene View
        noteTarget.showInSceneView = EditorGUILayout.Toggle("Show in Scene View", noteTarget.showInSceneView);

        EditorGUILayout.Space();

        // Buttons
        if (GUILayout.Button("Auto-Format Tags"))
        {
            noteTarget.note = AutoFormatNote(noteTarget.note);
            GUI.FocusControl(null);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Note as Asset"))
        {
            SaveNoteToAsset(noteTarget);
        }

        if (GUILayout.Button("Load Note from Asset"))
        {
            LoadNoteFromAsset(noteTarget);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Text Editing
        EditorGUILayout.LabelField("Note (Editable)", EditorStyles.boldLabel);

        SetupTextStyle(noteTarget);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(noteTarget.panelSize + 100));
        noteTarget.note = EditorGUILayout.TextArea(noteTarget.note, textStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        // Rich Text Preview
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rich Text Preview", EditorStyles.boldLabel);
        GUIStyle previewStyle = new GUIStyle(EditorStyles.label)
        {
            richText = noteTarget.useRichText,
            fontSize = noteTarget.fontSize,
            wordWrap = true,
            normal = { textColor = Color.white }
        };

        string previewText = ApplyColorTags(noteTarget.note);
        EditorGUILayout.LabelField(previewText, previewStyle, GUILayout.Height(noteTarget.panelSize));

        if (GUI.changed)
            EditorUtility.SetDirty(noteTarget);
    }

    private void SetupTextStyle(SceneInfiniNOTE noteTarget)
    {
        if (textStyle == null)
            textStyle = new GUIStyle(EditorStyles.textArea);

        textStyle.fontSize = noteTarget.fontSize;
        textStyle.wordWrap = true;
        textStyle.richText = noteTarget.useRichText;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        SceneInfiniNOTE noteTarget = (SceneInfiniNOTE)target;
        if (!noteTarget.showInSceneView || string.IsNullOrEmpty(noteTarget.note)) return;

        Handles.BeginGUI();

        Vector3 worldPos = noteTarget.transform.position + Vector3.up * 2.0f;
        Vector3 screenPos = HandleUtility.WorldToGUIPoint(worldPos);

        GUIStyle style = new GUIStyle(EditorStyles.label)
        {
            fontSize = noteTarget.fontSize,
            richText = noteTarget.useRichText,
            normal = { textColor = Color.white },
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };

        string displayText = ApplyColorTags(noteTarget.note);

        GUI.Label(new Rect(screenPos.x - 150, screenPos.y - 50, 300, 100), displayText, style);

        Handles.EndGUI();
    }

    private string ApplyColorTags(string input)
    {
        return input
            .Replace("[red]", "<color=red>").Replace("[/red]", "</color>")
            .Replace("[green]", "<color=green>").Replace("[/green]", "</color>")
            .Replace("[yellow]", "<color=yellow>").Replace("[/yellow]", "</color>")
            .Replace("[blue]", "<color=blue>").Replace("[/blue]", "</color>");
    }

    private string AutoFormatNote(string input)
    {
        return System.Text.RegularExpressions.Regex.Replace(input, @"\b(TODO|FIXME|NOTE)\b", match =>
        {
            switch (match.Value)
            {
                case "TODO": return "[yellow]TODO[/yellow]";
                case "FIXME": return "[red]FIXME[/red]";
                case "NOTE": return "[green]NOTE[/green]";
                default: return match.Value;
            }
        });
    }

    private void SaveNoteToAsset(SceneInfiniNOTE noteTarget)
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Note Asset", "NewNoteData", "asset", "Save your note as a ScriptableObject");
        if (string.IsNullOrEmpty(path)) return;

        var noteAsset = ScriptableObject.CreateInstance<InfiniNoteDataSO>();
        noteAsset.noteText = noteTarget.note;
        noteAsset.fontSize = noteTarget.fontSize;
        noteAsset.richText = noteTarget.useRichText;

        AssetDatabase.CreateAsset(noteAsset, path);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = noteAsset;
    }

    private void LoadNoteFromAsset(SceneInfiniNOTE noteTarget)
    {
        string path = EditorUtility.OpenFilePanel("Load Note Asset", "Assets", "asset");
        if (string.IsNullOrEmpty(path)) return;

        path = FileUtil.GetProjectRelativePath(path);
        var noteAsset = AssetDatabase.LoadAssetAtPath<InfiniNoteDataSO>(path);
        if (noteAsset != null)
        {
            Undo.RecordObject(noteTarget, "Load Note");
            noteTarget.note = noteAsset.noteText;
            noteTarget.fontSize = noteAsset.fontSize;
            noteTarget.useRichText = noteAsset.richText;

            EditorPrefs.SetInt(FontSizeKey, noteAsset.fontSize);
            EditorPrefs.SetBool(RichTextKey, noteAsset.richText);

            EditorUtility.SetDirty(noteTarget);
        }
        else
        {
            EditorUtility.DisplayDialog("Load Failed", "Selected file is not a valid InfiniNoteDataSO asset.", "OK");
        }
    }
}
}