using UnityEngine;


namespace ARFoundationRemoteExamples {
    public class EnvironmentOcclusionExample : MonoBehaviour {
        [SerializeField] Transform plane;


        /// If AROcclusionManager is added at runtime, it will sometimes not work with URP
        /// It works with one order of scripts and not works with other.
        /// Probably, some race conditions involved
        /*void Awake() {
            #if ARFOUNDATION_4_1_OR_NEWER
                gameObject.SetActive(false);
                var manager = gameObject.AddComponent<UnityEngine.XR.ARFoundation.AROcclusionManager>();
                manager.requestedEnvironmentDepthMode = UnityEngine.XR.ARSubsystems.EnvironmentDepthMode.Fastest;
                manager.requestedOcclusionPreferenceMode = UnityEngine.XR.ARSubsystems.OcclusionPreferenceMode.PreferEnvironmentOcclusion;
                gameObject.SetActive(true);
            #endif
        }*/

        void OnGUI() {
            ExamplesUtils.ShowTextAtCenter(getText());
        }

        string getText() {
            if (ExamplesUtils.ARFOUNDATION_4_1_OR_NEWER) {
                return $"Objects further than {plane.localPosition.z}\nmeters away will be clipped." + "\n" + getIOSEnvWarning();
            }
            return "Environment occlusion is only available in AR Foundation >= 4.1";
        }

        static string getIOSEnvWarning() {
            return ExamplesUtils.UNITY_IOS ? "Environment occlusion only available in iOS14." : "";
        }
    }
}
