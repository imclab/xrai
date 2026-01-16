namespace UnityEngine.XR.ARFoundation.Samples
{
    public class ToggleMeshClassificationFixed : MonoBehaviour
    {
        /// <summary>
        /// The mesh manager for the scene.
        /// </summary>
        [SerializeField]
        ARMeshManager m_MeshManager;

        /// <summary>
        /// Whether mesh classification should be enabled.
        /// </summary>
        [SerializeField]
        bool m_ClassificationEnabled = false;

        /// <summary>
        /// The mesh manager for the scene.
        /// </summary>
        public ARMeshManager meshManager { get => m_MeshManager; set => m_MeshManager = value; }

        /// <summary>
        /// Whether mesh classification should be enabled.
        /// </summary>
        public bool classificationEnabled
        {
            get => m_ClassificationEnabled;
            set
            {
                m_ClassificationEnabled = value;
                UpdateMeshSubsystem();
            }
        }

        /// <summary>
        /// On enable, update the mesh subsystem with the classification enabled setting.
        /// </summary>
        void OnEnable()
        {
            UpdateMeshSubsystem();
        }

        /// <summary>
        /// Update the mesh subsystem with the classiication enabled setting.
        /// </summary>
        void UpdateMeshSubsystem()
        {
            #if (UNITY_IOS || UNITY_EDITOR) && ARKIT_INSTALLED && ARFOUNDATION_4_0_2_OR_NEWER
            Debug.Assert(m_MeshManager != null, "mesh manager cannot be null");
            var meshSubsystem = m_MeshManager.subsystem; // compilation error if ARF is embedded and native lib is not present. But native library is available for all editor platforms
            if ((m_MeshManager != null) && (meshSubsystem != null)) {
                SetMeshClassificationEnabled(m_ClassificationEnabled);
            }
            #endif
        }

        #if (UNITY_IOS || UNITY_EDITOR) && ARKIT_INSTALLED && ARFOUNDATION_4_0_2_OR_NEWER
        static void SetMeshClassificationEnabled(bool classificationEnabled) 
        {
            #if UNITY_EDITOR && AR_FOUNDATION_REMOTE_INSTALLED
            if (Application.isEditor) {
                ARFoundationRemote.Runtime.ARKitMeshingExtensions.SetClassificationEnabled(classificationEnabled);
                return;
            }
            #endif
            ARKit.ARKitMeshSubsystemExtensions.SetClassificationEnabled(null, classificationEnabled);
        }
        #endif
    }
}
