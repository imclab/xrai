Shader "[QDY]NPR Filter Pro URP/Aquarelle" {
	Properties {}
	SubShader {
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
		Cull Off ZWrite Off ZTest Always
		Pass {
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment frag
			#pragma multi_compile _ NPR_Circle
			#pragma multi_compile _ NPR_Reverse
			#include "Common.hlsl"
			
			float _Joggle, _JoggleColorLerp, _Border, _Whiteness, _BorderSharp, _Stroke;

			float2 hash (float2 p)
			{
				p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
				return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
			}
			float noise (float2 p)
			{
				const float K1 = 0.366025404;
				const float K2 = 0.211324865;

				float2 i = floor(p + (p.x + p.y) * K1);
				float2 a = p - i + (i.x + i.y) * K2;
				float m = step(a.y, a.x); 
				float2 o = float2(m, 1.0 - m);
				float2 b = a - o + K2;
				float2 c = a - 1.0 + 2.0 * K2;
				float3 h = max(0.5 - float3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
				float3 n = h * h * h * h * float3(dot(a, hash(i+0.0)), dot(b, hash(i+o)), dot(c, hash(i+1.0)));
				return dot(n, 70.0);
			}
			float simp (float2 uv)
			{
				uv *= 5.0;
				float2x2 m = float2x2(1.6, 1.2, -1.2, 1.6);
				float f = 0.5 * noise(uv); uv = mul(m, uv);
				f += 0.25 * noise(uv); uv = mul(m, uv);
				f += 0.125 * noise(uv); uv = mul(m, uv);
				f += 0.0625 * noise(uv); uv = mul(m, uv);
				f = 0.2 + 0.8 * f;
				return f;
			}
			float4 offsetFromDepth (float2 uv, float scale)
			{
				float height = simp(uv);
				float2 step = _BlitTexture_TexelSize.xy;
				float2 dxy = height - float2(
					simp(uv + float2(step.x, 0.0)),
					simp(uv + float2(0.0, step.y)));
				return float4(normalize(float3(dxy * scale / step, 1.0)), height);
			}
			float roundedBox (float2 p, float2 b, float4 r)
			{
				r.xy = (p.x > 0.0) ? r.xy : r.zw;
				r.x  = (p.y > 0.0) ? r.x  : r.y;
				float2 q = abs(p) - b + r.x;
				return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
			}
			float4 frag (Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				float2 uv = input.texcoord;

				float4 offset = offsetFromDepth(uv + floor(_Time.y * _Joggle) / _Joggle, _Stroke) / _BorderSharp;
				float a = _JoggleColorLerp;
				float b = 1.0 - _JoggleColorLerp;
				float4 fc = (SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + offset.xy) * a) + (SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv) * b);

				fc += length(offsetFromDepth(uv, 0.1)) * _Whiteness;
				fc += smoothstep(_Border, 0.0, roundedBox((uv + offset.xy) - 0.5, 0.65, 0.25));
				return AreaShape(fc, input);
			}
			ENDHLSL
		}
	}
	FallBack Off
}