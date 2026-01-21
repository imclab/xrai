using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class FERM_ParamAccess : MonoBehaviour {

    public ParamAccess target;

    public List<ParamAccess> GetAccess() {
        FERM_Component[] cps = GetComponents<FERM_Component>();
        Dictionary<string, int> sourceNames = new Dictionary<string, int>();
        List<ParamAccess> toReturn = new List<ParamAccess>();

        foreach(FERM_Component cp in cps) {

            string sourceName = cp.GetTypeName();
            int snIndex = 0;
            if(sourceNames.ContainsKey(sourceName))
                snIndex = sourceNames[sourceName]++;
            else
                sourceNames.Add(sourceName, 1);

            if(cp.parameters != null) {
                FERM_Parameter[] subList = cp.parameters;
                for(int i = 0; i < subList.Length; i++) {
                    FERM_Parameter p = subList[i];
                    if(FERM_Util.IsUnityTransformPar(p))
                        continue;

                    string name = sourceName + "_" + snIndex + "_" + p.name;
                    toReturn.Add(new ParamAccess(cp, p, i, toReturn.Count, name));
                }
            }
        }
        return toReturn;
    }

    protected FERM_Parameter.Type GetTargetType() {
        if(target == null)
            return (FERM_Parameter.Type)(-1);
        return target.parameter.type;
    }

    protected void SetTargetValue(object value) {
        if(target == null)
            return;
        target.parameter.SetValue(value);
    }

    protected object GetTargetValue(){
        if(target == null)
            return null;
        return target.parameter.GetValue(); 
    }

    [Serializable]
    public class ParamAccess {
        public FERM_Component component;
        public FERM_Parameter parameter;
        public int cIndex;
        public int pIndex;
        public string name;

        public ParamAccess(FERM_Component component, FERM_Parameter parameter, int cIndex, int pIndex, string name) {
            this.component = component;
            this.parameter = parameter;
            this.cIndex = cIndex;
            this.pIndex = pIndex;
            this.name = name;
        }
    }
}
