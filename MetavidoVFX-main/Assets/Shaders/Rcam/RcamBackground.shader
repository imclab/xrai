Shader "Hidden/Rcam4/Background"
{
HLSLINCLUDE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise3D.hlsl"
// Use local shim instead of missing jp.keijiro.rcam4.common package
#include "RcamCommon.hlsl"

// Rcam uniforms
sampler2D _ColorMap;
sampler2D _DepthMap;
float4 _InverseProjection;
float4x4 _InverseView;

// Opacity parameters
float _BackFill, _FrontFill;

// Effect parameters
float4 _EffectColor;

// Scanner effect
float3 ScannerEffect(float3 src, float3 wpos)
{
    // Y axis parameter
    float rep = abs(frac(wpos.y * 100) - 0.5);
    rep = 1 - smoothstep(0, 0.15, rep);

    // Dual noise field
    float n1 = abs(SimplexNoise(wpos * 200));
    float n2 = pow(abs(SimplexNoise(wpos * 50 + 100)), 80) * 200;

    // Source luminance
    float lum = Luminance(src) * 8 + 0.1;

    // Highlight
    float hi = frac(wpos.y - _Time.y * 0.3);
    hi = smoothstep(0.95, 1, hi) * 10;

    // Composition
    float3 scan = lum * (1 + hi) * rep * n1 * (1 + n2);
    return _EffectColor.rgb * scan;
}

void Vertex(uint vertexID : VERTEXID_SEMANTIC,
            out float4 outPosition : SV_Position,
            out float2 outTexCoord : TEXCOORD0)
{
    outPosition = GetFullScreenTriangleVertexPosition(vertexID);
    outTexCoord = GetFullScreenTriangleTexCoord(vertexID);
}

void Fragment(float4 position : SV_Position,
              float2 texCoord : TEXCOORD,
              out float4 outColor : SV_Target,
              out float outDepth : SV_Depth)
{
    // Aspect ratio fix
    float inv_aspect = _InverseProjection.y / _InverseProjection.x;
    float2 uv = texCoord * 2 - 1;
    uv = (uv * float2(1, inv_aspect));
    uv = (uv + 1) / 2;

    // Color/depth samples
    float4 c_in = tex2D(_ColorMap, uv);
    float d_in = tex2D(_DepthMap, uv).x;

    // Source color
    float3 c = c_in.rgb;

    // Human stencil
    float a = smoothstep(0.503, 0.51, c_in.a);

    // World position
    float3 wpos = RcamDistanceToWorldPosition
      (uv, d_in, _InverseProjection, _InverseView);

    // Scanner effect
    float3 scan = ScannerEffect(c, wpos);
    c = lerp(c, scan, a * _EffectColor.a);

    // Stochastic transparency
    float rand = GenerateHashedRandomFloat(position.xy);
    if (rand >= (lerp(_BackFill, _FrontFill, a))) discard;

    // Output
    outColor = float4(c, a);
    outDepth = RcamDistanceToDepth(d_in);
}

ENDHLSL

    SubShader
    {
        Pass
        {
            ZTest LEqual ZWrite On Cull Off Blend Off
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            ENDHLSL
        }
    }

    Fallback Off
}
