using UnityEditor;

[CustomEditor(typeof(FERM_PE5))]
public class FERM_PE5Editor : FERM_MultiParamAccessEditor {

    SerializedProperty[] sp;

    new FERM_PE5 target { get { return (FERM_PE5)base.target; } }

    private static string[] pre = new string[]{ "f", "i", "r", "t", "q" };

    private void OnEnable() {
        sp = new SerializedProperty[target.n * pre.Length];

        int index = 0;
        foreach(string p in pre) {
            for(int i = 0; i < target.n; i++) {
                string name = p + i;
                sp[index++] = serializedObject.FindProperty(name);
            }
        }
    }

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        serializedObject.ApplyModifiedProperties();
    }

    protected override void ParamGUI(FERM_Parameter parameter, int index) {
        target.AcquireValue(parameter, index);
        SerializedProperty sp = this.sp[GetIndex(parameter.type, index)];
        EditorGUILayout.PropertyField(sp, true);
    }

    private int GetIndex(FERM_Parameter.Type type, int index) {
        int tIndex = GetTIndex(type);
        return tIndex * target.n + index;
    }

    private int GetTIndex(FERM_Parameter.Type type) {
        switch(type) {
        default:
        case FERM_Parameter.Type.Floating:
        return 0;

        case FERM_Parameter.Type.Axis:
        case FERM_Parameter.Type.Integer:
        return 1;

        case FERM_Parameter.Type.Vector:
        return 2;

        case FERM_Parameter.Type.Vec2:
        return 3;

        case FERM_Parameter.Type.Quaternion:
        return 4;
        }
    }
}
