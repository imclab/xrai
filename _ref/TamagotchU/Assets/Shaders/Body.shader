// 7/23/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

Shader "Custom/Body"
{
    Properties
    {
        _EnableFade("Enable Fade", int) = 1
        _FadeStart ("Fade Start (World Y)", Float) = 1.0
        _FadeEnd ("Fade End (World Y)", Float) = 0.0
        _Speed ("Fall Speed", Range(0, 1)) = 1.0
        _EndColor ("Start Color", Color) = (0, 0, 0, 1)
        _StartColor ("End Color", Color) = (1, 1, 1, 1)
        _BellyColor ("Belly Color", Color) = (1, 1, 1, 1)
        _LCDScale ("LCD Scale", Float) = 100.0
        _LEDScale ("LED Scale", Float) = 5.0
        _VoronoiScale ("Voronoi Scale", Float) = 5.0
        _GlowIntensity ("Glow Intensity", Float) = 2.0
        _Transparency ("Transparency", Range(0, 1)) = 0.8
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Include Unity's shader libraries
            #include "UnityCG.cginc"

            // Shader properties
            int _EnableFade;
            float _FadeStart;
            float _FadeEnd;
            float4 _EndColor;
            float4 _StartColor;
            float4 _BellyColor;
            float _LCDScale;
            float _Speed;
            float _GlowIntensity;
            float _LEDScale;
            float _VoronoiScale;
            float _Transparency;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            // Vertex shader
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz; // Get world position
                o.uv = v.uv; // Apply tiling to UVs
                return o;
            }
            
            // Hash function for Voronoi
            float2 hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            // Voronoi noise function
            float voronoi(float2 uv, out float glow)
            {
                float2 p = floor(uv);
                float2 f = frac(uv);
                float res = 8.0;
                glow = 0.0;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
                        float2 pt = hash22(p + neighbor);
                        pt = 0.5 + 0.5 * sin(_Time.y + 6.2831 * pt);
                        float2 diff = neighbor + pt - f;
                        float d = dot(diff, diff);
                        glow += exp(-10.0 * d);
                        res = min(res, d);
                    }
                }

                glow = saturate(glow);
                return sqrt(res);
            }

            half4 frag (v2f i) : SV_Target
            {
                // Simulate a screen distortion normal (procedural or texture)
                float2 bumpUV = i.uv * 5 + _Time.y * 0.5;

                // Procedural distortion (can replace with a normal map sample)
                float2 normalOffset;
                normalOffset.x = sin(bumpUV.y * 20.0 + sin(bumpUV.x * 10.0)) * 0.01;
                normalOffset.y = cos(bumpUV.x * 20.0 + cos(bumpUV.y * 10.0)) * 0.01;

                // Refraction strength scaling
                normalOffset *= 0.05;

                // Offset UV for refracted LED color
                float2 refractedUV = i.uv + normalOffset;

                float2 ledUV = refractedUV;
                float2 voronoiUV = refractedUV;
                voronoiUV.y += _Time.y * _Speed;
                ledUV *= _LEDScale;

                // Local UV inside the LED cell
                float2 localUV = frac(ledUV * _LEDScale) - 0.5;

                // LED radius
                float dist = length(localUV);
                float ledMask = smoothstep(0.65, 0.25, dist); // Soft circular mask

                // LED color from noise or voronoi
                float glow; // Can be randomized
                float voronoiValue = voronoi(voronoiUV * _VoronoiScale, glow);

                float3 voronoiColor = lerp(_StartColor.rgb, _EndColor.rgb, glow);
                float3 ledColor = voronoiColor * ledMask; // Apply circular mask

                // Optional: glow halo around LED
                float glowMask = smoothstep(0.4, 0.0, dist);
                float3 glowColor = ledColor * glowMask * 0.2;

                // Combine LED + Glow
                float3 skinColor = ledColor + glowColor;

                // Fade out top/bottom using world Y
                float fadeFactor = saturate((i.worldPos.y - _FadeEnd) / (_FadeStart - _FadeEnd));
                float alpha = _Transparency * fadeFactor;

                // Chrome effect: RGB shifting bands
                float2 chromeUV = refractedUV * _LCDScale * 0.5 + _Time.y * 0.25;
                float shift = sin(chromeUV.x * 15.0 + chromeUV.y * 5.0);

                // RGB stripe shimmer
                float3 chromeColor = float3(
                    0.5 + 0.5 * sin(chromeUV.x * 30.0 + shift),
                    0.5 + 0.5 * sin(chromeUV.x * 30.0 + shift + 2.0),
                    0.5 + 0.5 * sin(chromeUV.x * 30.0 + shift + 4.0)
                );

                // Make chrome subtle and position dependent
                chromeColor *= 0.5; // adjust intensity and fade

                // Blend into LED color
                skinColor = lerp(skinColor, skinColor + chromeColor, 0.4) * _GlowIntensity;
                float skinAlpha = saturate(pow(alpha + glow * alpha, 1));
                float4 sc = float4(skinColor, skinAlpha);
                float bellyFadeFactor = saturate((_FadeEnd - i.worldPos.y + voronoiValue) / (_FadeStart - _FadeEnd)) * voronoiValue;
                bellyFadeFactor = pow(bellyFadeFactor, 0.9);
                float4 bc = _BellyColor * bellyFadeFactor;

                float4 finalColor = _EnableFade ? lerp(bc, sc, skinAlpha) : float4(skinColor, 1);
                return finalColor;
                //return float4(refractedUV, 0, 1);
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}