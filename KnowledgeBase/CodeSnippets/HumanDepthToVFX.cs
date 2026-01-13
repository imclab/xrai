using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.VFX;
using Unity.Collections;

[RequireComponent(typeof(AROcclusionManager))]
public class HumanDepthToVFX : MonoBehaviour
{
    [Header("VFX Setup")]
    public VisualEffect vfxGraph;
    public int textureSize = 256;
    
    [Header("Depth Processing")]
    public float depthScale = 10f;
    public float depthThreshold = 0.1f;
    
    private AROcclusionManager occlusionManager;
    private Texture2D positionMap;
    private Texture2D colorMap;
    private ARCameraManager cameraManager;
    
    void Start()
    {
        occlusionManager = GetComponent<AROcclusionManager>();
        cameraManager = GetComponentInParent<ARCameraManager>();
        
        // Initialize textures for VFX Graph
        positionMap = new Texture2D(textureSize, textureSize, TextureFormat.RGBAFloat, false);
        colorMap = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        
        // Enable human segmentation
        occlusionManager.requestedHumanStencilMode = HumanSegmentationStencilMode.Fastest;
        occlusionManager.requestedHumanDepthMode = HumanSegmentationDepthMode.Fastest;
    }
    
    void Update()
    {
        // Get human stencil and depth textures
        var humanStencil = occlusionManager.humanStencilTexture;
        var humanDepth = occlusionManager.humanDepthTexture;
        var cameraTexture = cameraManager.GetComponent<ARCameraBackground>()?.material?.mainTexture;
        
        if (humanStencil != null && humanDepth != null && cameraTexture != null)
        {
            UpdateVFXMaps(humanStencil, humanDepth, cameraTexture);
        }
    }
    
    void UpdateVFXMaps(Texture stencil, Texture depth, Texture color)
    {
        // Create temporary render textures for processing
        var tempRT = RenderTexture.GetTemporary(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        
        // Process position map (world positions from depth)
        Graphics.Blit(depth, tempRT, GetDepthToPositionMaterial());
        
        // Read back to CPU (normally you'd want to avoid this, but for simplicity...)
        RenderTexture.active = tempRT;
        positionMap.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
        positionMap.Apply();
        
        // Process color map (masked by stencil)
        Graphics.Blit(color, tempRT, GetStencilMaskMaterial(stencil));
        colorMap.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
        colorMap.Apply();
        
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(tempRT);
        
        // Update VFX Graph
        vfxGraph.SetTexture("PositionMap", positionMap);
        vfxGraph.SetTexture("ColorMap", colorMap);
        vfxGraph.SetInt("PointCount", textureSize * textureSize);
        vfxGraph.SendEvent("OnUpdateMaps");
    }
    
    // Shader for converting depth to world position
    Material GetDepthToPositionMaterial()
    {
        if (_depthMaterial == null)
        {
            var shader = Shader.Find("Hidden/DepthToPosition");
            if (shader == null)
            {
                shader = CreateDepthShader();
            }
            _depthMaterial = new Material(shader);
        }
        
        // Set camera matrices
        var cam = cameraManager.GetComponent<Camera>();
        _depthMaterial.SetMatrix("_InverseProjection", cam.projectionMatrix.inverse);
        _depthMaterial.SetMatrix("_CameraToWorld", cam.cameraToWorldMatrix);
        _depthMaterial.SetFloat("_DepthScale", depthScale);
        
        return _depthMaterial;
    }
    
    // Shader for masking color by stencil
    Material GetStencilMaskMaterial(Texture stencil)
    {
        if (_stencilMaterial == null)
        {
            var shader = Shader.Find("Hidden/StencilMask");
            if (shader == null)
            {
                shader = CreateStencilShader();
            }
            _stencilMaterial = new Material(shader);
        }
        
        _stencilMaterial.SetTexture("_StencilTex", stencil);
        return _stencilMaterial;
    }
    
    private Material _depthMaterial;
    private Material _stencilMaterial;
    
    // Create simple depth-to-position shader
    Shader CreateDepthShader()
    {
        var shaderText = @"
            Shader ""Hidden/DepthToPosition"" {
                Properties {
                    _MainTex (""Texture"", 2D) = ""white"" {}
                }
                SubShader {
                    Pass {
                        CGPROGRAM
                        #pragma vertex vert
                        #pragma fragment frag
                        #include ""UnityCG.cginc""
                        
                        struct appdata {
                            float4 vertex : POSITION;
                            float2 uv : TEXCOORD0;
                        };
                        
                        struct v2f {
                            float2 uv : TEXCOORD0;
                            float4 vertex : SV_POSITION;
                        };
                        
                        sampler2D _MainTex;
                        float4x4 _InverseProjection;
                        float4x4 _CameraToWorld;
                        float _DepthScale;
                        
                        v2f vert (appdata v) {
                            v2f o;
                            o.vertex = UnityObjectToClipPos(v.vertex);
                            o.uv = v.uv;
                            return o;
                        }
                        
                        float4 frag (v2f i) : SV_Target {
                            float depth = tex2D(_MainTex, i.uv).r * _DepthScale;
                            
                            // Convert UV to clip space
                            float4 clipPos = float4(i.uv * 2.0 - 1.0, depth, 1.0);
                            clipPos.y = -clipPos.y;
                            
                            // Transform to world space
                            float4 viewPos = mul(_InverseProjection, clipPos);
                            viewPos /= viewPos.w;
                            float4 worldPos = mul(_CameraToWorld, viewPos);
                            
                            return float4(worldPos.xyz, 1.0);
                        }
                        ENDCG
                    }
                }
            }";
        return Shader.Create(shaderText);
    }
    
    // Create simple stencil mask shader
    Shader CreateStencilShader()
    {
        var shaderText = @"
            Shader ""Hidden/StencilMask"" {
                Properties {
                    _MainTex (""Texture"", 2D) = ""white"" {}
                    _StencilTex (""Stencil"", 2D) = ""white"" {}
                }
                SubShader {
                    Pass {
                        CGPROGRAM
                        #pragma vertex vert
                        #pragma fragment frag
                        #include ""UnityCG.cginc""
                        
                        struct appdata {
                            float4 vertex : POSITION;
                            float2 uv : TEXCOORD0;
                        };
                        
                        struct v2f {
                            float2 uv : TEXCOORD0;
                            float4 vertex : SV_POSITION;
                        };
                        
                        sampler2D _MainTex;
                        sampler2D _StencilTex;
                        
                        v2f vert (appdata v) {
                            v2f o;
                            o.vertex = UnityObjectToClipPos(v.vertex);
                            o.uv = v.uv;
                            return o;
                        }
                        
                        float4 frag (v2f i) : SV_Target {
                            float4 color = tex2D(_MainTex, i.uv);
                            float stencil = tex2D(_StencilTex, i.uv).r;
                            
                            // Only keep pixels where human is detected
                            color.a *= step(0.5, stencil);
                            return color;
                        }
                        ENDCG
                    }
                }
            }";
        return Shader.Create(shaderText);
    }
}