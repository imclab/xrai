Shader "Custom/ProceduralWavyLines"
{
    Properties
    {
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _Transparency ("Transparency", Range(0, 1)) = 0.5
        _Tiling ("Tiling", Vector) = (1, 1, 0, 0)
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _DotSize ("Dot Size", Range(0, 1)) = 0.5
        _EmissiveIntensity("Emission Intensity", Range(1, 10)) = 2.5
        _NoiseStrength("Noise Strength", Range(0, 10)) = 2
        _PixelDensity ("Pixel Density", Float) = 100.0

    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Include Unity's shader libraries
            #include "UnityCG.cginc"
            
            // Include Keijiro's Simplex Noise
            #include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise2D.hlsl"

            // Shader properties
            float _WaveSpeed;
            float _Transparency;
            float4 _Tiling;
            float4 _BaseColor; 
            float _DotSize;
            float _EmissiveIntensity;
            float _NoiseStrength;
            float _PixelDensity;

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

            // Vertex shader
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _Tiling.xy; // Apply tiling to UVs
                return o;
            }

            // Procedural pattern fragment shader
            half4 frag (v2f i) : SV_Target
            {
                // Create a time-based wave
                float2 wave = sin(i.uv + _Time.y * _WaveSpeed);
                float noise = SimplexNoise(wave);

                // Create soft dots using a Voronoi-like effect
                float2 uvOffset = wave  + noise * _NoiseStrength; // Offset UVs by the wave
                float2 grid = frac(uvOffset); // Get fractional part of UVs
                float2 dist = abs(grid - 0.5); // Distance from center of each cell
                //float softness = step(length(dist), _DotSize);// Soft dots
                float softness = 1.0 - smoothstep(0.0, _DotSize, length(dist)); // Soft dots

                // Combine wave and dots
                float pattern = softness;
                float3 color = float3(pattern, pattern, pattern);

                // Chrome effect: RGB shifting bands
                float2 chromeUV = wave * _PixelDensity * 0.5 + _Time.y * 0.05;
                float shift = sin(chromeUV.x * 25.0 + chromeUV.y * 5.0);

                // RGB stripe shimmer
                float3 chromeColor = float3(
                    0.5 + 0.5 * sin(chromeUV.x * 30.0 + shift),
                    0.5 + 0.5 * sin(chromeUV.x * 30.0 + shift + 2.0),
                    0.5 + 0.5 * sin(chromeUV.x * 30.0 + shift + 4.0)
                );
                color += 10*chromeColor;
                // Output color with transparency
                return fixed4(color * _EmissiveIntensity * _BaseColor.rgb, pattern * _Transparency + _BaseColor.w);
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}