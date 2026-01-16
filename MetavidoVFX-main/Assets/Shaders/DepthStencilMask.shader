Shader "Hidden/DepthStencilMask"
{
    Properties
    {
        _DepthTex ("Depth Texture", 2D) = "black" {}
        _StencilTex ("Stencil Texture", 2D) = "white" {}
        _StencilThreshold ("Stencil Threshold", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _DepthTex;
            sampler2D _StencilTex;
            float _StencilThreshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float depth = tex2D(_DepthTex, i.uv).r;
                float stencil = tex2D(_StencilTex, i.uv).r;

                // Only output depth where stencil indicates body
                // Set to FAR depth (100m) for non-body pixels so they're rejected by depth range
                // if (stencil < _StencilThreshold)
                //     depth = 100.0;  // Far outside typical 0.1-10m range

                return float4(depth, 0, 0, 1);
            }
            ENDCG
        }
    }
}
