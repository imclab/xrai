Shader "Hidden/Rcam4/Recolor"
{
HLSLINCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

// Shader properties
TEXTURE2D(_SourceTexture);
TEXTURE2D(_AlphaTexture);
float4x4 _BgColors, _FgColors;
float _BackFill, _FrontFill;
float _Dithering;

// Bayer matrix for dithering
static const float BayerArray[] =
{
    0.000000, 0.531250, 0.132812, 0.664062,
    0.796875, 0.265625, 0.929688, 0.398438,
    0.199219, 0.730469, 0.066406, 0.597656,
    0.996094, 0.464844, 0.863281, 0.332031,
};

static float GetDither(uint2 tc)
{
    return BayerArray[(tc.x & 3) + (tc.y & 3) * 4];
}

// Applying a palette stored in a float4x4 matrix
float3 ApplyPalette(float4x4 palette, float lum)
{
    float3 c = palette[0].rgb;
    c = lum > palette[1].w ? palette[1].rgb : c;
    c = lum > palette[2].w ? palette[2].rgb : c;
    c = lum > palette[3].w ? palette[3].rgb : c;
    return c;
}

// Fragment shader: Capture pass (alpha copy)
float4 FragCapture(float4 position : SV_Position) : SV_Target0
{
    return LOAD_TEXTURE2D(_BlitTexture, position.xy).a;
}

// Fragment shader: Recolor pass
float4 FragRecolor(float4 position : SV_Position) : SV_Target0
{
    // Color/alpha samples
    float3 c = LOAD_TEXTURE2D(_SourceTexture, position.xy).rgb;
    float a = LOAD_TEXTURE2D(_AlphaTexture, position.xy).r; 

    // Luminance with dithering
    float l = Luminance(LinearToSRGB(c));
    l += (GetDither(position.xy) - 0.5) * _Dithering;

    // Composition
    float3 bg = lerp(c, ApplyPalette(_BgColors, l), _BackFill);
    float3 fg = lerp(c, ApplyPalette(_FgColors, l), _FrontFill);
    return float4(lerp(bg, fg, a), 1);
}

ENDHLSL

    SubShader
    {
        ZTest Off ZWrite Off Cull Off Blend Off
        Pass
        {
            Name "Recolor Capture"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragCapture
            ENDHLSL
        }
        Pass
        {
            Name "Recolor"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragRecolor
            ENDHLSL
        }
    }
}
