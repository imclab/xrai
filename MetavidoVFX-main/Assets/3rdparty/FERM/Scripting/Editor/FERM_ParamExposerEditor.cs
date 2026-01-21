using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FERM_ParamExposer))]
public class FERM_ParamExposerEditor : FERM_ParamAccessEditor {

    SerializedProperty value_float, value_int, value_vec, value_vec2, value_quat;

    new FERM_ParamExposer target { get { return (FERM_ParamExposer)base.target; } }

    private void OnEnable() {
        value_float = serializedObject.FindProperty("value_float");
        value_int = serializedObject.FindProperty("value_int");
        value_vec = serializedObject.FindProperty("value_vec");
        value_vec2 = serializedObject.FindProperty("value_vec2");
        value_quat = serializedObject.FindProperty("value_quat"); 
    }

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        if(target.target != null) {
            target.AcquireValue();
            ExposeValueParameter(target.target.parameter.type);
        }
    }

    private void ExposeValueParameter(FERM_Parameter.Type type) {
        switch(type) {
        case FERM_Parameter.Type.Floating:
            EditorGUILayout.PropertyField(value_float);
            break;
        case FERM_Parameter.Type.Integer:
            EditorGUILayout.PropertyField(value_int);
            break;
        case FERM_Parameter.Type.Vector:
            EditorGUILayout.PropertyField(value_vec);
            break;
        case FERM_Parameter.Type.Vec2:
            EditorGUILayout.PropertyField(value_vec2);
            break;
        case FERM_Parameter.Type.Quaternion:
            EditorGUILayout.PropertyField(value_quat, true);
            break;
        default:
            Debug.LogError("Unkown target type: " + type);
            break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
