Shader "Custom/BrainSurface"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1,0.9,0.8,1)
        _GrooveColor ("Groove Color", Color) = (0.15,0.12,0.1,1)
        _FresnelColor ("Fresnel Color", Color) = (0.15,0.12,0.1,1)
        _Scale ("Pattern Scale", Float) = 3.0
        _Detail ("Detail Level", Float) = 2.0
        _WarpStrength ("Warp Strength", Float) = 0.25

        _LightDir ("Light Direction", Vector) = (0.5, 0.8, 0.6, 0)
        _Ambient ("Ambient Intensity", Float) = 0.3
        _Specular ("Specular Intensity", Float) = 0.5
        _Shininess ("Shininess", Float) = 20

        _Smoothness ("Smoothness", Range(0,1)) = 0.7
        _FresnelPower ("Fresnel Power", Float) = 3.0
        _FresnelIntensity ("Fresnel Intensity", Float) = 0.7
        _WaveSpeed ("Wave Speed", Float) = 0.2
        _WaveAmplitude ("Wave Amplitude", Float) = 0.15

        _RidgeIntensity ("Ridge Intensity", Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        Cull Front

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _MainColor;
            fixed4 _GrooveColor;
            fixed4 _FresnelColor;
            float _Scale;
            float _Detail;
            float _WarpStrength;

            float4 _LightDir;
            float _Ambient;
            float _Specular;
            float _Shininess;

            float _Smoothness;
            float _FresnelPower;
            float _FresnelIntensity;
            float _WaveSpeed;
            float _WaveAmplitude;
            float _RidgeIntensity;

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

            float noise(float2 p)
            {
                return frac(sin(dot(p ,float2(127.1,311.7))) * 43758.5453);
            }

            float smoothNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));

                float2 u = 0.5 - 0.5 * cos(f * 3.14159265);

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;

                for (int i = 0; i < 5; i++)
                {
                    value += amplitude * smoothNoise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            float ridgeSoft(float h, float intensity)
            {
                h = 1.0 - abs(h);
                return pow(h, intensity);
            }

            float mazeRidgePattern(float2 p, float ridgeIntensity)
            {
                float2 q = p + float2(fbm(p + float2(0.0, 0.0)), fbm(p + float2(5.2, 1.3))) * 0.4;
                float2 r = p + float2(fbm(q + float2(1.7, 9.2)), fbm(q + float2(8.3, 2.8))) * 0.4;

                float val = fbm(r);
                float centered = val * 2.0 - 1.0;

                return ridgeSoft(centered, ridgeIntensity);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv * _Scale;

                // Wave motion
                float t = _Time.y * _WaveSpeed;
                float waveX = mazeRidgePattern(uv + float2(t, 0), _RidgeIntensity) * _WaveAmplitude;
                float waveY = mazeRidgePattern(uv + float2(0, t), _RidgeIntensity) * _WaveAmplitude;
                float2 warpedUV = uv + float2(waveX, waveY);

                // Sulcus pattern
                float n = mazeRidgePattern(warpedUV * _Detail, _RidgeIntensity);

                // Groove isolation
                float groove = smoothstep(0.4, 0.55, n) - smoothstep(0.55, 0.65, n);
                groove = saturate(groove * 4.0);

                // Fake normals from height gradient
                float eps = 0.001;
                float hx = mazeRidgePattern(warpedUV * _Detail + float2(eps, 0), _RidgeIntensity);
                float hy = mazeRidgePattern(warpedUV * _Detail + float2(0, eps), _RidgeIntensity);
                float3 normal = normalize(float3(n - hx, n - hy, eps));

                float3 lightDir = normalize(_LightDir.xyz);
                float3 viewDir = float3(0, 0, 1);
                float3 halfDir = normalize(lightDir + viewDir);

                float diff = saturate(dot(normal, lightDir)) + _Ambient;
                float spec = pow(saturate(dot(normal, halfDir)), lerp(1.0, _Shininess, _Smoothness)) * _Specular * _Smoothness;

                float fresnel = pow(1.0 - saturate(dot(viewDir, normal)), _FresnelPower) * _FresnelIntensity;

                fixed3 baseCol = lerp(_MainColor.rgb, _GrooveColor.rgb, groove);
                fixed3 finalCol = baseCol * diff + spec + fresnel * _FresnelColor;

                return fixed4(saturate(finalCol), 1);
            }
            ENDCG
        }
    }
}