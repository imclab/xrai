using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FERM_Renderer))]
public class FERM_RendererEditor : Editor {

    private FERM_Renderer rend;
    private MaterialEditor _materialEditor;
    private int guiUpdateCount = 0;

    void OnEnable() {
        rend = (FERM_Renderer)target;
        if(rend.material != null)
            _materialEditor = (MaterialEditor)CreateEditor(rend.material);
    }

    public void OnSceneGUI() {
        /* focus kick requires the selection to change briefly.
         * After 2 gui updates, the Renderer should be updated 
         * properly, so we can swap selection back to where it 
         * came from.
         */

        if(rend.selectionRef != null) {

            if(rend.selectionRef == rend.gameObject) {
                rend.selectionRef = null;
                return;
            }

            if(guiUpdateCount++ >= 2)
                guiUpdateCount = 0;
            else
                return;

            
            Selection.activeGameObject = rend.selectionRef;
            rend.selectionRef = null;
            guiUpdateCount = 0;
            SceneView.RepaintAll();
        }
    }

    public override void OnInspectorGUI() {
        EditorGUI.BeginChangeCheck();
        bool requireRebuild = false;

        Material m = (Material)EditorGUILayout.ObjectField("FERM Material", rend.material, typeof(Material), false);
        if(m != rend.material) {
            Undo.RecordObject(target, "Changed FERM material");
            rend.shader.SetMaterial(m);
            rend.SetShader(rend.shader);
            requireRebuild |= m;

        }
        if(rend.material == null) {
            EditorGUILayout.LabelField("Please assign a generated FERM Material");
            EditorGUILayout.LabelField("to this renderer or generate a new renderer.");
            return;
        }

        EditorGUILayout.ObjectField("FERM Shader",  rend.material.shader, typeof(Shader), false);

        bool autoCompile = EditorGUILayout.Toggle("Auto Compile", rend.autoCompile);
        if(autoCompile != rend.autoCompile) {
            Undo.RecordObject(target, "Toggled FERM AutoCompile");
            rend.autoCompile = autoCompile;
        }
        bool showTips = EditorGUILayout.Toggle("Show tips", rend.showTips);
        if(showTips != rend.showTips) {
            Undo.RecordObject(target, "Toggled FERM ShowTips");
            rend.showTips = showTips;
        }
        FERM_Renderer.Mode mode = (FERM_Renderer.Mode)
            EditorGUILayout.EnumPopup("Rendering mode", rend.renderingMode);
        if(mode != rend.renderingMode) {
            Undo.RecordObject(target, "Changed FERM rendering mode");
            rend.renderingMode = mode;
            requireRebuild = true;
        }

        GUIStyle s = FERM_EditorUtil.Style.Make(new Color(.6f, .4f, 0f));
        GUILayout.Label("WARNING: When changing the supersampling to a new value for the first time, Unity might require several minutes of compile time. Note that supersampling is extremely slow and not recommended for real time rendering.", s);
        FERM_Renderer.SuperSampling sampling = (FERM_Renderer.SuperSampling)
            EditorGUILayout.EnumPopup("Supersampling", rend.superSampling);
        if(sampling != rend.superSampling) {
            Undo.RecordObject(target, "Changed FERM supersampling amount");
            rend.superSampling = sampling;
            requireRebuild = true;
        }

        if(GUILayout.Button("Delete renderer")) {
            if(EditorUtility.DisplayDialog("Confirm deletion",
                "Are you sure you want to permanently delete this renderer and its associated shader assets?",
                "Delete",
                "Cancel")) {
                rend.shader.Delete();
                DestroyImmediate(rend.gameObject);
            }
        }
        if(requireRebuild || GUILayout.Button("Recompile shader"))
            rend.BuildShader();

        if(EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(target, "Changed FERM material properties");
            serializedObject.ApplyModifiedProperties();
            RefreshMaterialEditor();
        }

        if(_materialEditor != null && rend.material != null)
            DrawMaterialInspector();
    }

    private void RefreshMaterialEditor() {
        if(_materialEditor != null)
            DestroyImmediate(_materialEditor);

        if(rend.material != null)
            _materialEditor = (MaterialEditor)CreateEditor(rend.material);
    }

    private void DrawMaterialInspector() {
        _materialEditor.DrawHeader();
        bool isDefaultMaterial = !AssetDatabase.GetAssetPath(rend.material).StartsWith("Assets");
        using(new EditorGUI.DisabledGroupScope(isDefaultMaterial))
            _materialEditor.OnInspectorGUI();
    }

    void OnDisable() {
        if(_materialEditor != null)
            DestroyImmediate(_materialEditor);
    }
}