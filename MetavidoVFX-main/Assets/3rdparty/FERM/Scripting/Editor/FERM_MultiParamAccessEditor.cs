using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(FERM_MultiParamAccess))]
public abstract class FERM_MultiParamAccessEditor : FERM_ParamAccessEditor {

    new FERM_MultiParamAccess target { get { return (FERM_MultiParamAccess)base.target; } }

    public override void OnInspectorGUI() {
        List<FERM_ParamAccess.ParamAccess> pars = target.GetAccess();

        if(pars.Count == 0) {
            EditorGUILayout.LabelField("Parameter access scripts require a");
            EditorGUILayout.LabelField("FERM component with parameters!");
            for(int i = 0; i < target.n; i++)
                target.pas[i] = null;
            return;
        }

        string[] parNames = GetNames(pars);
        for(int i = 0; i < target.n; i++) {
            int targetIndex = ParamAccessPopupGUI(parNames, target.pas[i]);
            FERM_ParamAccess.ParamAccess pa = targetIndex < 0 ? null : pars[targetIndex];
            target.pas[i] = pa;
            if(pa != null)
                ParamGUI(pa.parameter, i);
            
        }
    }

    protected abstract void ParamGUI(FERM_Parameter parameter, int i);
}
