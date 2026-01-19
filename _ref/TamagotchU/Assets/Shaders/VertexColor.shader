// 7/24/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

Shader "Custom/DebugVertexColor"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR; // Vertex color input
                
                float2 uv : TEXCOORD0;   
                float3 normalOS : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR; // Pass vertex color to fragment

                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 normalWS : NORMAL;
            };

            float4 _BaseColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.color = v.color; // Pass vertex color to fragment
                
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normalWS = normalize(mul((float3x3)unity_ObjectToWorld, v.normalOS));
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // Multiply vertex color with a base color for debugging
                float3 positionWS = i.worldPos;
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - positionWS);
                float3 reflectVector = reflect(viewDir, i.normalWS);                   // reflection direction
                float3 probes = CalculateIrradianceFromReflectionProbes(reflectVector, positionWS, 0, i.uv);

                return i.color * float4(probes, 1) * _BaseColor;
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}