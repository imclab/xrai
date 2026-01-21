using UnityEngine;
using UnityEditor;
namespace Artngame.CommonTools
{
    public class InfiniNOTE : EditorWindow
    {
        private string text = "Write your text here...";
        private int fontSize = 24;
        //private int panelSize = 100;
        private Vector2 scrollPosition;
        private GUIStyle textStyle;

        [MenuItem("Window/ARTnGAME/InfiniNOTE")]
        public static void ShowWindow()
        {
            window = GetWindow<InfiniNOTE>("InfiniNOTE Text Editor");
            window.minSize = new Vector2(300, 200); // Optional: Set a minimum size
        }
        static InfiniNOTE window;

        private void OnGUI()
        {
           
            // Font size slider
            EditorGUILayout.LabelField("Font Size:", EditorStyles.boldLabel);
            fontSize = EditorGUILayout.IntSlider(fontSize, 10, 40);
            //panelSize = EditorGUILayout.IntSlider(panelSize, 100, 1400);

            // Set up text style
            if (textStyle == null)
            {
                textStyle = new GUIStyle(EditorStyles.textArea);
            }

            textStyle.fontSize = fontSize;
            textStyle.wordWrap = true;

            // Scrollable text area
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            text = EditorGUILayout.TextArea(text, textStyle, GUILayout.ExpandHeight(true));

           

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Save Note"))
            {
                //InfiniNoteDataSO noteTarget = new InfiniNoteDataSO();
                //noteTarget.noteText = text;
                //noteTarget.fontSize = fontSize;
                
                SaveNoteToAsset(text, fontSize);
            }
            if (GUILayout.Button("Load Note"))
            {
                LoadNoteFromAsset();
                //this.Repaint();
            }
            //this.Repaint();
        }


        private void SaveNoteToAsset(string noteText, int fontSize)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Note Asset", "NewNoteData", "asset", "Save your note as a ScriptableObject");
            if (string.IsNullOrEmpty(path)) return;

            var noteAsset = ScriptableObject.CreateInstance<InfiniNoteDataSO>();
            noteAsset.noteText = noteText;
            noteAsset.fontSize = fontSize;
           // noteAsset.richText = noteTarget.richText;

            AssetDatabase.CreateAsset(noteAsset, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = noteAsset;
        }

        private void LoadNoteFromAsset()
        {
            string path = EditorUtility.OpenFilePanel("Load Note Asset", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) return;

            path = FileUtil.GetProjectRelativePath(path);
            AssetDatabase.ImportAsset(path);
            var noteAsset = AssetDatabase.LoadAssetAtPath<InfiniNoteDataSO>(path);
            if (noteAsset != null)
            {
                //Undo.RecordObject(noteTarget, "Load Note");
                text = noteAsset.noteText;
                fontSize = noteAsset.fontSize;
                // noteTarget.richText = noteAsset.richText;

                //EditorPrefs.SetInt(FontSizeKey, noteAsset.fontSize);
                //EditorPrefs.SetBool(RichTextKey, noteAsset.richText);

                //EditorUtility.SetDirty(noteAsset);
                //AssetDatabase.SaveAssets();

                //EditorUtility.SetDirty(noteTarget);
                GUI.FocusControl(null);
                //Repaint();
                //AssetDatabase.SaveAssets();
                //Selection.activeObject = noteAsset;
                //window = GetWindow<InfiniNOTE>("InfiniNOTE Text Editor");
                //EditorUtility.SetDirty(window);
                //if (window != null)
                //    window.Repaint();
            }
            else
            {
                EditorUtility.DisplayDialog("Load Failed", "Selected file is not a valid InfiniNoteDataSO asset.", "OK");
            }
        }


    }
}