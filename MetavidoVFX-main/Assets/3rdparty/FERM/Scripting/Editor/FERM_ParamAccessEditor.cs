using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(FERM_ParamAccess))]
public abstract class FERM_ParamAccessEditor : Editor {

    new FERM_ParamAccess target { get { return (FERM_ParamAccess)base.target; } }

    public override void OnInspectorGUI() {
        List<FERM_ParamAccess.ParamAccess> pars = target.GetAccess();

        if(pars.Count == 0) {
            EditorGUILayout.LabelField("Parameter access scripts require a");
            EditorGUILayout.LabelField("FERM component with parameters!");
            target.target = null;
            return;
        }

        string[] parNames = GetNames(pars);
        int targetIndex = ParamAccessPopupGUI(parNames, target.target);
        target.target = targetIndex < 0 ? null : pars[targetIndex];
    }

    protected string[] GetNames(List<FERM_ParamAccess.ParamAccess> pars) {
        int n = pars.Count;
        string[] toReturn = new string[n];
        for(int i = 0; i < n; i++)
            toReturn[i] = pars[i].name;
        return toReturn;
    }

    protected static int ParamAccessPopupGUI(string[] parNames, FERM_ParamAccess.ParamAccess pa) {
        int targetIndex = -1;
        if(pa != null)
            targetIndex = pa.pIndex;
        return EditorGUILayout.Popup(targetIndex, parNames);
    }
}
