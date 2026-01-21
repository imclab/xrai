using System.Reflection;
using UnityEngine;

[ExecuteInEditMode]
public class FERM_PE5 : FERM_MultiParamAccess{

    public override int n {
        get {
            return 5;
        }
    }

    public float f0, f1, f2, f3, f4;
    public int i0, i1, i2, i3, i4;
    public Vector3 r0, r1, r2, r3, r4;
    public Vector2 t0, t1, t2, t3, t4;
    public Quaternion q0, q1, q2, q3, q4;

    private void LateUpdate() {
        ApplyValue();
    }

    public void ApplyValue() {
        for(int i = 0; i < n; i++) {
            ParamAccess pa = pas[i];
            if(pa != null) {
                FERM_Parameter p = pa.parameter;
                p.SetValue(GetValue(p.type, i));
            }
        }
    }

    private object GetValue(FERM_Parameter.Type type, int i) {
        return GetField(type, i).GetValue(this);
    }

    private void SetValue(FERM_Parameter.Type type, int i, object value) {
        GetField(type, i).SetValue(this, value);
    }

    private FieldInfo GetField(FERM_Parameter.Type type, int i) {
        string fieldName = GetPre(type) + i;
        BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        return GetType().GetField(fieldName, flags);
    }

    public static string GetPre(FERM_Parameter.Type type) {
        switch(type) {

        case FERM_Parameter.Type.Floating:
        return "f";

        case FERM_Parameter.Type.Integer:
        case FERM_Parameter.Type.Axis:
        return "i";

        case FERM_Parameter.Type.Vector:
        return "r";

        case FERM_Parameter.Type.Vec2:
        return "t";

        case FERM_Parameter.Type.Quaternion:
        return "q";
        }
        return "";
    }

    public void AcquireValue(FERM_Parameter p, int i) {
        SetValue(p.type, i, p.GetValue());
    }
}
