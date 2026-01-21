
using UnityEngine;

public abstract class FERM_MultiParamAccess : FERM_ParamAccess {

    public ParamAccess[] pas { get {
            if(_pas == null || _pas.Length != n)
                _pas = new ParamAccess[n];
            return _pas;
        } }
    
    public ParamAccess[] _pas;

    public abstract int n { get; }
    
}
