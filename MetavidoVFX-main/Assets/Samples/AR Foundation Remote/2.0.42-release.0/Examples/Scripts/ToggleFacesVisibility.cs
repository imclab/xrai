using UnityEngine;
using UnityEngine.XR.ARFoundation;


namespace ARFoundationRemote.Runtime {
    public class ToggleFacesVisibility : MonoBehaviour {
        [SerializeField] ARFaceManager manager = null;
        
        bool isEnabled;


        void Awake() {
            isEnabled = manager.enabled;
        }

        void Update() {
            var newIsEnabled = manager.enabled;
            if (isEnabled != newIsEnabled) {
                isEnabled = newIsEnabled;
                
                foreach (var trackable in manager.trackables) {
                    trackable.gameObject.SetActive(newIsEnabled);
                }
            }
        }
    }
}
