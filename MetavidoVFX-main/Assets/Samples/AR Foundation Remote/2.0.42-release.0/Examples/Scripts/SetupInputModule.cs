using UnityEngine;
using UnityEngine.EventSystems;


namespace ARFoundationRemoteExamples {
    [RequireComponent(typeof(EventSystem))]
    public class SetupInputModule : MonoBehaviour {
        void Awake() {
            if (isNewInputSystemEnabled && !ExamplesUtils.isLegacyInputManagerEnabled) {
                gameObject.SetActive(false);
                #if INPUT_SYSTEM_INSTALLED
                var inputModule = gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                inputModule.pointerBehavior = UnityEngine.InputSystem.UI.UIPointerBehavior.AllPointersAsIs;
                #endif
                gameObject.SetActive(true);
            } else {
                gameObject.AddComponent<StandaloneInputModule>();
            }
        }
        
        static bool isNewInputSystemEnabled {
            get {
                #if ENABLE_INPUT_SYSTEM
                return true;
                #else
                return false;
                #endif
            }
        }
    }
}
