using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Metavido {

public class ARCameraTextureProvider : MonoBehaviour
{
    [SerializeField] ARCameraBackground _cameraBackground;
    [SerializeField] RenderTexture _renderTexture;

    public Texture Texture => _renderTexture;

    void Start()
    {
        if (_cameraBackground == null)
            _cameraBackground = GetComponent<ARCameraBackground>();

        if (_renderTexture == null)
        {
            _renderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _renderTexture.Create();
        }
    }

    void Update()
    {
        if (_cameraBackground != null && _cameraBackground.material != null)
        {
            // Blit the AR background material (which handles YCbCr -> RGB) to our RT
            Graphics.Blit(null, _renderTexture, _cameraBackground.material);
        }
    }

    void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            if (Application.isEditor)
                DestroyImmediate(_renderTexture);
            else
                Destroy(_renderTexture);
        }
    }
}

} // namespace Metavido
