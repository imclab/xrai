// BrushStrokeParticle.shader - URP shader for particle brushes
// Part of Spec 011: OpenBrush Integration
//
// Billboard particle shader for brushes like Bubbles, Embers, Snow, Stars.
// Supports soft particles, emission, and audio reactivity.

Shader "XRRAI/BrushStrokeParticle"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _EmissionColor ("Emission", Color) = (0,0,0,0)
        _EmissionStrength ("Emission Strength", Range(0, 5)) = 1
        [Toggle] _UseVertexColor ("Use Vertex Color", Float) = 1
        [Toggle] _SoftParticles ("Soft Particles", Float) = 1
        _SoftParticlesFade ("Soft Particles Fade", Range(0.01, 3)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10

        // Audio reactive properties (set from C#)
        _AudioPulse ("Audio Pulse", Range(0, 1)) = 0
        _AudioScale ("Audio Scale Multiplier", Range(0.5, 3)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "BrushParticle"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _SOFT_PARTICLES

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 customData : TEXCOORD1; // x=size, y=rotation, z=lifetime, w=random
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float fogFactor : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float customData : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _EmissionColor;
                half _EmissionStrength;
                half _UseVertexColor;
                half _SoftParticles;
                half _SoftParticlesFade;
                half _AudioPulse;
                half _AudioScale;
            CBUFFER_END

            // sRGB to Linear conversion (OpenBrush TbVertToNative pattern)
            half3 SrgbToLinear(half3 srgb)
            {
                return srgb * (srgb * (srgb * 0.305306011h + 0.682171111h) + 0.012522878h);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Apply audio-reactive scale
                float3 scaledPos = input.positionOS.xyz;
                scaledPos *= lerp(1.0, _AudioScale, _AudioPulse);

                output.positionCS = TransformObjectToHClip(scaledPos);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                // Color space conversion: sRGB vertex colors to Linear (URP requirement)
                #if defined(UNITY_COLORSPACE_GAMMA)
                    output.color = input.color;
                #else
                    output.color.rgb = SrgbToLinear(input.color.rgb);
                    output.color.a = input.color.a;
                #endif

                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.customData = input.customData.z; // Pass lifetime

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Base color
                half4 color = texColor * _Color;

                // Apply vertex color if enabled
                if (_UseVertexColor > 0.5)
                {
                    color *= input.color;
                }

                // Add emission with audio modulation
                half emissionMod = 1.0 + _AudioPulse * _EmissionStrength;
                half3 emission = _EmissionColor.rgb * emissionMod;
                color.rgb += emission;

                // Soft particles
                #if defined(_SOFT_PARTICLES)
                if (_SoftParticles > 0.5)
                {
                    float2 screenUV = input.screenPos.xy / input.screenPos.w;
                    float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                    float particleDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                    float fade = saturate((sceneDepth - particleDepth) / _SoftParticlesFade);
                    color.a *= fade;
                }
                #endif

                // Apply fog
                color.rgb = MixFog(color.rgb, input.fogFactor);

                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Particles/Unlit"
}
