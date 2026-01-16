#if AR_FOUNDATION_5_0_OR_NEWER
    using ARSessionOrigin = Unity.XR.CoreUtils.XROrigin;
#else
    using UnityEngine.XR.ARFoundation;
#endif
using UnityEngine;


namespace ARFoundationRemoteExamples {
    public static class ExamplesUtils {
        public static void ShowTextAtCenter(string text) {
            GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(text, new GUIStyle {fontSize = 30, normal = new GUIStyleState {textColor = Color.white}});
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
        }

        public static bool UNITY_IOS {
            get {
                return
                #if UNITY_IOS
                    true;
                #else
                    false;
                #endif
            }
        }

        public static bool UNITY_ANDROID {
            get {
                return
                #if UNITY_ANDROID
                    true;
                #else
                    false;
                #endif
            }
        }

        public static bool ARFOUNDATION_4_0_OR_NEWER {
            get {
                return
                #if ARFOUNDATION_4_0_OR_NEWER
                    true;
                #else
                    false;
                #endif
            }
        }

        public static bool isLegacyInputManagerEnabled {
            get {
                #if ENABLE_LEGACY_INPUT_MANAGER || UNITY_2019_2
                return true;
                #else
                return false;
                #endif
            }
        }
        
        public static Camera GetCamera(this ARSessionOrigin origin) {
            return origin.
                #if AR_FOUNDATION_5_0_OR_NEWER
                Camera;
                #else
                camera;
                #endif
        }
        
        public static bool AR_FOUNDATION_5_0_OR_NEWER {
            get {
                return
                #if AR_FOUNDATION_5_0_OR_NEWER
                    true;
                #else
                    false;
                #endif
            }
        }
        
        public static bool ARFOUNDATION_4_1_OR_NEWER {
            get {
                return
                #if ARFOUNDATION_4_1_OR_NEWER
                    true;
                #else
                    false;
                #endif
            }
        }
    }
}
