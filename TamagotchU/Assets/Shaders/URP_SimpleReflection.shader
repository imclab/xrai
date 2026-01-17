Shader "Custom/URP_SimpleReflection"
{
     Properties
    {

    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        Cull Front

        Pass
        {
            HLSLPROGRAM 
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;   
                float3 normalOS : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 normalWS : NORMAL;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normalWS = normalize(mul((float3x3)unity_ObjectToWorld, v.normalOS));
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Use the skybox direction vector projected to 2D for pattern generation
                float2 uv = i.uv;

                float3 positionWS = i.worldPos;
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - positionWS);

                float3 reflectVector = reflect(viewDir, i.normalWS);                   // reflection direction
                //fixed4 reflColor = UNITY_SAMPLE_TEXCUBE(_SpecGlossMap, reflDir);

                float3 probes = CalculateIrradianceFromReflectionProbes(reflectVector, positionWS, 0, frac(uv));

                return float4(probes,1);
            }
            ENDHLSL 
        }
    }
    FallBack Off
    }
