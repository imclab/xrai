using UnityEngine;


namespace ARFoundationRemoteExamples {
    public class CheckUguiInstalled : MonoBehaviour {
        void Awake() {
            #if !UGUI_INSTALLED
            Debug.LogError($"Please install Unity UI (\"com.unity.ugui\") via Package Manager to run this example");
            #endif
        }
    }
}
