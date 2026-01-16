using UnityEngine;


namespace ARFoundationRemoteExamples {
    public class ARKitHumanSegmentationExample : MonoBehaviour {
        [SerializeField] Transform plane;

        
        void Awake() {
            #if !ARFOUNDATION_4_0_OR_NEWER
            Debug.LogError("Human Segmentation is only available in AR Foundation >= 4.0");
            #endif

            if (!ExamplesUtils.UNITY_IOS) {
                Debug.LogError("Human Segmentation is only available on iOS.");
            }
            
            // To prevent a problem similar to EnvironmentOcclusionExample, don't create manager from code
            /*#if ARFOUNDATION_4_0_OR_NEWER
                var manager = gameObject.AddComponent<AROcclusionManager>();
                manager.requestedHumanStencilMode = HumanSegmentationStencilMode.Fastest;
                manager.requestedHumanDepthMode = HumanSegmentationDepthMode.Fastest;
                #if ARFOUNDATION_4_1_OR_NEWER
                    manager.requestedOcclusionPreferenceMode = OcclusionPreferenceMode.PreferHumanOcclusion;
                #endif
            #endif*/
        }
        
        void OnGUI() {
            ExamplesUtils.ShowTextAtCenter(getText());
        }

        string getText() {
            return ExamplesUtils.UNITY_IOS ? $"Only human body that is closer than {plane.localPosition.z}\nmeters away will be visible." :
                "Human segmentation is only supported on iOS";
        }
    }
}
