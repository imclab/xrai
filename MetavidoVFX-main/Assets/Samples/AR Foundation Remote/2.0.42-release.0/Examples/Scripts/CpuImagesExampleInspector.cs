#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


namespace ARFoundationRemoteExamples {
    [CustomEditor(typeof(CpuImagesExample))]
    public class CpuImagesExampleInspector : Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            serializedObject.Update();
            GUILayout.Space(8);
            var mode = (CpuImageMode) serializedObject.FindProperty("mode").enumValueIndex;
            switch (mode) {
                case CpuImageMode.ConvertToRGB: 
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("conversionType"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("textureScale"));
                    break;
                case CpuImageMode.RawImagePlanes:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rawImagePlaneIndex"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("debugString"));
                    if (ExamplesUtils.UNITY_ANDROID) {
                        var imageType = (CpuImageType) serializedObject.FindProperty("cpuImageType").enumValueIndex;
                        if (imageType == CpuImageType.RawEnvironmentDepth || imageType == CpuImageType.SmoothedEnvironmentDepth) {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("androidDepthTextureMultiplier"));
                        }    
                    }
                    break;
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
