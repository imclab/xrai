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

            Varyings vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Calculate distance from center (UV space)
                half distFromCenter = abs(input.uv.x - 0.5) * 2.0;
                half falloff = pow(1.0 - saturate(distFromCenter), _FalloffPower);

                // Core color
                half4 coreColor = _Color * _CoreBrightness;
                if (_UseVertexColor > 0.5)
                {
                    coreColor *= input.color;
                }

                // Glow color
                half4 glowColor = _GlowColor * _GlowStrength;

                // Audio reactive modulation
                half audioMod = 1.0;
                if (_AudioReactive > 0.5)
                {
                    audioMod = lerp(0.3, 1.5, _AudioLevel);
                }

                // Combine core and glow
                half4 color = lerp(glowColor, coreColor, falloff) * texColor * audioMod;
                color.a = texColor.a * input.color.a;

                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
