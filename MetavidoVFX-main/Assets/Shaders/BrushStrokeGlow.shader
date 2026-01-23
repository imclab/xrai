// BrushStrokeGlow.shader - URP brush stroke with glow effect
// Part of Spec 011: OpenBrush Integration
//
// Additive glow shader for light-style brushes.
// Audio reactive via emission strength.

Shader "XRRAI/BrushStrokeGlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowStrength ("Glow Strength", Range(0, 5)) = 1
        _CoreBrightness ("Core Brightness", Range(0, 3)) = 1
        _FalloffPower ("Falloff Power", Range(0.5, 4)) = 2
        [Toggle] _UseVertexColor ("Use Vertex Color", Float) = 1
        [Toggle] _AudioReactive ("Audio Reactive", Float) = 0
        _AudioLevel ("Audio Level", Range(0, 1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "BrushStrokeGlow"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One  // Additive
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _GlowColor;
                half _GlowStrength;
                half _CoreBrightness;
                half _FalloffPower;
                half _UseVertexColor;
                half _AudioReactive;
                half _AudioLevel;
            CBUFFER_END

            // sRGB to Linear conversion (OpenBrush TbVertToNative pattern)
            half3 SrgbToLinear(half3 srgb)
            {
                return srgb * (srgb * (srgb * 0.305306011h + 0.682171111h) + 0.012522878h);
            }

            // Bloom color function from Tilt Brush - creates HDR glow effect
            // gain: 0-1 maps to 2x-180x brightness multiplier
            half4 BloomColor(half4 color, half gain)
            {
                // Guarantee minimum of all channels (prevents saturated colors clipping to secondary)
                half cmin = length(color.rgb) * 0.05h;
                color.rgb = max(color.rgb, half3(cmin, cmin, cmin));

                // Gamma correction for proper HDR blending
                color = pow(color, 2.2h);

                // Emission gain: gain 0-1 maps to exp(0)-exp(10) = 1x-22026x, scaled by 2
                color.rgb *= 2.0h * exp(gain * 10.0h);

                return color;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                // Color space conversion: sRGB vertex colors to Linear (URP requirement)
                #if defined(UNITY_COLORSPACE_GAMMA)
                    output.color = input.color;
                #else
                    output.color.rgb = SrgbToLinear(input.color.rgb);
                    output.color.a = input.color.a;
                #endif

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Calculate distance from center (UV space) for core/edge gradient
                half distFromCenter = abs(input.uv.x - 0.5) * 2.0;
                half falloff = pow(1.0 - saturate(distFromCenter), _FalloffPower);

                // Base color from vertex color or _Color
                half4 baseColor = _Color;
                if (_UseVertexColor > 0.5)
                {
                    baseColor *= input.color;
                }

                // Audio reactive modulation - affects both brightness and emission
                half audioMod = 1.0;
                half emissionBoost = 0.0;
                if (_AudioReactive > 0.5)
                {
                    audioMod = lerp(0.5, 2.0, _AudioLevel);
                    emissionBoost = _AudioLevel * 0.3;  // Extra glow on beat
                }

                // Apply Tilt Brush-style bloom for proper HDR glow
                // _GlowStrength normalized to 0-1 range for bloomColor gain parameter
                half normalizedGain = saturate(_GlowStrength * 0.2);  // 0-5 → 0-1
                half4 bloomedColor = BloomColor(baseColor, normalizedGain + emissionBoost);

                // Core is brighter, edges have glow falloff
                half4 coreColor = bloomedColor * _CoreBrightness;
                half4 glowColor = bloomedColor * _GlowColor;

                // Combine core and glow based on distance from center
                half4 color = lerp(glowColor, coreColor, falloff) * texColor * audioMod;

                // Alpha for additive blending (usually 1.0)
                color.a = 1.0;

                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
