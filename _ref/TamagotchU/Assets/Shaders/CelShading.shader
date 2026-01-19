Shader "Custom/ToonCartoonCel"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Base Color", Color) = (1,1,1,1)
        _ShadowColor("Shadow Color", Color) = (0.1,0.1,0.1,1)
        _LightThreshold("Light Threshold", Range(0,1)) = 0.5
        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimIntensity("Rim Intensity", Range(0,1)) = 0.4
        _RimPower("Rim Power", Range(1, 10)) = 4
        _LitDirection("Light Direction", Vector) = (0.5, 0.5, 0, 0)
        _SpecColor("Highlight Color", Color) = (1,1,1,1)
        _SpecPower("Highlight Sharpness", Range(1, 100)) = 50
        _SpecThreshold("Highlight Threshold", Range(0, 1)) = 0.95
        _Emission("Emission Intensity", Range(1, 20)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Color;
            float4 _ShadowColor;
            float _LightThreshold;
            float4 _RimColor;
            float _RimIntensity;
            float _RimPower;
            float4 _LitDirection;

            float4 _SpecColor;
            float _SpecPower;
            float _SpecThreshold;
            float _Emission;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.worldNormal);
                float3 L = normalize(_LitDirection.xyz);
                float3 V = normalize(i.viewDir);

                float NdotL = dot(N, L);

                // Flat cel shading step
                float3 baseColor = (NdotL > _LightThreshold) ? _Color.rgb : _ShadowColor.rgb;

                // Rim light
                float rim = 1.0 - saturate(dot(N, V));
                rim = pow(rim, _RimPower);
                float3 rimColor = rim * _RimColor.rgb * _RimIntensity;

                // Specular highlight (hard circle)
                float3 H = normalize(L + V); // Half vector
                float spec = pow(saturate(dot(N, H)), _SpecPower);
                spec = step(_SpecThreshold, spec); // Hard cutoff
                float3 specColor = _SpecColor.rgb * spec;

                float3 texColor = tex2D(_MainTex, i.uv).rgb;
                float3 finalColor = (baseColor + rimColor + specColor) * texColor;

                return float4(finalColor * _Emission, _Color.a);
            }
            ENDCG
        }
    }

    FallBack "Unlit/Texture"
}
