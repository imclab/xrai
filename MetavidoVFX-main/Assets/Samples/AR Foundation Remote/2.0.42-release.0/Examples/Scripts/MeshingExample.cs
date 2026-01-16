using UnityEngine;
using UnityEngine.XR.ARFoundation;


namespace ARFoundationRemoteExamples {
    public class MeshingExample : MonoBehaviour {
        [SerializeField] ARMeshManager manager = null;
        [SerializeField] bool showMeshDestructionButton = false;
        
        
        void Awake() {
            if (ExamplesUtils.UNITY_IOS && !(arKitInstalled && ExamplesUtils.ARFOUNDATION_4_0_OR_NEWER)) {
                Debug.LogError($"Please install ARKit XR Plugin >= 4.0");
            }

            if (ExamplesUtils.UNITY_ANDROID) {
                Debug.LogError($"Meshing is not supported by ARCore");
            }
        }
        
        static bool arKitInstalled {
            get {
                #if ARKIT_INSTALLED
                return true;
                #else
                return false;
                #endif
            }
        }
        
        void OnGUI() {
            if (showMeshDestructionButton && GUI.Button(new Rect(0,0,400,200), "DestroyAllMeshes")) {
                manager.DestroyAllMeshes();
            }
        }
    }
}
