Shader "Hidden/Rcam4/Frame Decoder"
{
HLSLINCLUDE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
// Use local shim instead of missing jp.keijiro.rcam4.common package
#include "RcamCommon.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_TexelSize;

TEXTURE3D(_LutTexture);
SAMPLER(sampler_LutTexture);

float2 _DepthRange;

void Vertex(uint vertexID : VERTEXID_SEMANTIC,
            out float4 outPosition : SV_Position,
            out float2 outTexCoord : TEXCOORD0)
{
    outPosition = GetFullScreenTriangleVertexPosition(vertexID);
    outTexCoord = GetFullScreenTriangleTexCoord(vertexID);
}

float4 FragmentColor(float4 position : SV_Position,
                     float2 texCoord : TEXCOORD) : SV_Target
{
    UnityTexture2D src = UnityBuildTexture2DStructNoScale(_MainTex);
    float3 rgb  = src.Load(RcamUV2TC(src, texCoord, float2(0.5, 1), 0)).xyz;
    float2 mask = src.Load(RcamUV2TC(src, texCoord, 0.5, float2(0.5, 0))).xy;
    float alpha = 0.5 + 0.5 * (mask.x < 0.5 ? -1 : 1) * mask.y;
    rgb = _LutTexture.Sample(sampler_LutTexture, LinearToSRGB(rgb)).rgb;
    return float4(SRGBToLinear(rgb), alpha);
}

float FragmentDepth(float4 position : SV_Position,
                    float2 texCoord : TEXCOORD) : SV_Target
{
    UnityTexture2D src = UnityBuildTexture2DStructNoScale(_MainTex);
    float3 rgb = src.Load(RcamUV2TC(src, texCoord, 0.5, 0.5)).xyz;
    return RcamDecodeDepth(LinearToSRGB(rgb), _DepthRange);
}

ENDHLSL

    SubShader
    {
        Pass
        {
            ZTest Always ZWrite Off Cull Off Blend Off
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentColor
            ENDHLSL
        }
        Pass
        {
            ZTest Always ZWrite Off Cull Off Blend Off
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentDepth
            ENDHLSL
        }
    }

    Fallback Off
}
