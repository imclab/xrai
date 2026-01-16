using UnityEngine;


namespace ARFoundationRemoteExamples {
    public class ARKitObjectTrackingExample : MonoBehaviour {
        #pragma warning disable 414
        [SerializeField] GameObject trackedObjectPrefab;
        #pragma warning restore 414

        #if ARFOUNDATION_4_0_OR_NEWER
        [SerializeField] public UnityEngine.XR.ARSubsystems.XRReferenceObjectLibrary referenceLibrary;
        
        
        void Awake() {
                if (referenceLibrary == null) {
                    Debug.LogError(
                        $"Please set the ARKitObjectTrackingExample.referenceLibrary in inspector, " +
                        $"add your image library to plugin's 'Assets/Plugins/ARFoundationRemoteInstaller/Resources/ObjectTrackingLibraries', and make a new build of AR Companion app.", this);
                    return;
                }

                gameObject.SetActive(false);
                var manager = gameObject.AddComponent<UnityEngine.XR.ARFoundation.ARTrackedObjectManager>();
                manager.referenceLibrary = referenceLibrary;
                manager.trackedObjectPrefab = trackedObjectPrefab;
                gameObject.SetActive(true);
        }
        #endif

        void Start() {
            if (!ExamplesUtils.ARFOUNDATION_4_0_OR_NEWER) {
                Debug.LogError("ARKit Object Tracking is only available in AR Foundation >= 4.0");
            }
        }
    }
}
