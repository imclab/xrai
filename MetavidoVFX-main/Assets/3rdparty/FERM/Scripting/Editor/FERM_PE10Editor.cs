using UnityEditor;

[CustomEditor(typeof(FERM_PE10))]
public class FERM_PE10Editor : FERM_PE5Editor {

    SerializedProperty[] sp;

    new FERM_PE10 target { get { return (FERM_PE10)base.target; } }
}
