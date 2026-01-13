using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.VFX;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(ARCameraBackground))]
[RequireComponent(typeof(AROcclusionManager))]
public class PeopleOcclusionVFXManager : MonoBehaviour
{
    struct ThreadSize
    {
        public int x;
        public int y;
        public int z;

        public ThreadSize(uint x, uint y, uint z)
        {
            this.x = (int)x;
            this.y = (int)y;
            this.z = (int)z;
        }
    }

    [SerializeField]
    VisualEffect m_VfxPrefab;
    [SerializeField]
    ComputeShader m_ComputeShader;

    ARCameraBackground m_CameraBackground;
    AROcclusionManager m_OcclusionManager;
    Camera m_Camera;
    RenderTexture m_CaptureTexture, m_PositionTexture;
    VisualEffect m_VfxInstance;
    int m_Kernel;
    ThreadSize m_ThreadSize;

    void Awake()
    {
        m_Camera = GetComponent<Camera>();
        m_CameraBackground = GetComponent<ARCameraBackground>();
        m_OcclusionManager = GetComponent<AROcclusionManager>();
    }

    void OnEnable()
    {
        m_VfxInstance = Instantiate(m_VfxPrefab, transform.parent);
        m_CaptureTexture = new RenderTexture(Screen.width, Screen.height, 16);
        m_CaptureTexture.Create();
        SetupComputeShader();
    }

    void OnDisable()
    {
        Destroy(m_VfxInstance);
        m_CaptureTexture.Release();
        m_PositionTexture.Release();
    }

    void Update()
    {
        Texture2D stencilTexture = m_OcclusionManager.humanStencilTexture;
        Texture2D depthTexture = m_OcclusionManager.humanDepthTexture;

        if (stencilTexture == null || depthTexture == null)
        {
            return;
        }


        //var viewPosition = m_Camera.WorldToViewportPoint(transform.TransformPoint(facePosition));
        //if (Input.deviceOrientation == DeviceOrientation.Portrait)
        //{
        //    viewPosition = new Vector3(1.0f - viewPosition.y, viewPosition.x, viewPosition.z);
        //}
        //else if (Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown)
        //{
        //    viewPosition = new Vector3(viewPosition.y, 1.0f - viewPosition.x, viewPosition.z);
        //}

        Matrix4x4 invVPMatrix = (m_Camera.projectionMatrix * m_Camera.transform.worldToLocalMatrix).inverse;

        m_ComputeShader.SetTexture(m_Kernel, "DepthTexture", depthTexture);
        m_ComputeShader.SetMatrix("InvVPMatrix", invVPMatrix);
        m_ComputeShader.SetMatrix("ProjectionMatrix", m_Camera.projectionMatrix);
        m_ComputeShader.Dispatch(m_Kernel, Mathf.CeilToInt(m_PositionTexture.width / m_ThreadSize.x), Mathf.CeilToInt(m_PositionTexture.height / m_ThreadSize.y), 1);

        m_VfxInstance.SetTexture("Color Map", m_CaptureTexture);
        m_VfxInstance.SetTexture("Stencil Map", stencilTexture);
    }

    void SetupComputeShader()
    {
        m_PositionTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
        m_PositionTexture.enableRandomWrite = true;
        m_PositionTexture.Create();

        m_Kernel = m_ComputeShader.FindKernel("GeneratePositionTexture");
        uint threadSizeX, threadSizeY, threadSizeZ;
        m_ComputeShader.GetKernelThreadGroupSizes(m_Kernel, out threadSizeX, out threadSizeY, out threadSizeZ);
        m_ThreadSize = new ThreadSize(threadSizeX, threadSizeY, threadSizeZ);

        m_ComputeShader.SetTexture(m_Kernel, "PositionTexture", m_PositionTexture);
        m_VfxInstance.SetTexture("Position Map", m_PositionTexture);
    }

    void LateUpdate()
    {
        if (m_CameraBackground.material != null)
        {
            Graphics.Blit(null, m_CaptureTexture, m_CameraBackground.material);
        }
    }
}