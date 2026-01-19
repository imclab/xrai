Shader "[QDY]NPR Filter Pro URP/SketchDrawing" {
	Properties {
		_NoiseTex ("Noise", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
		Cull Off ZWrite Off ZTest Always Fog { Mode Off }
		Pass {
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment frag
			#pragma multi_compile _ NPR_Circle
			#pragma multi_compile _ NPR_Reverse
			#pragma multi_compile _ USE_GRAYSCALE
			#include "Common.hlsl"

			sampler2D _NoiseTex;
			half _BrushStrength, _Whiteness, _Lines;

			#define PI2 6.28318530717959
			#define RANGE 16.0
			#define STEP 2.0
			#define ANGLENUM 4.0

			float mod (float a, float b)   { return a - b * floor(a / b); }
			float grayscale (float2 pos)
			{
				float4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pos / _ScreenParams.xy);
				return dot(c.xyz, float3(0.2126, 0.7152, 0.0722));
			}
			float2 getGrad (float2 pos, float eps)
			{
				float2 d = float2(eps, 0);
				return float2(
					grayscale(pos + d.xy) - grayscale(pos - d.xy),
					grayscale(pos + d.yx) - grayscale(pos - d.yx)) / eps / 2.0;
			}
			void modify (inout float2 p, float a) { p = cos(a) * p + sin(a) * float2(p.y, -p.x); }

			half4 frag (Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				float2 pos = input.texcoord * _ScreenParams.xy;
				float weight = 1.0;

				for (float j = 0.; j < ANGLENUM; j += 1.0)
				{
					float2 dir = float2(1, 0);
					modify(dir, j * PI2 / (2.0 * ANGLENUM));

					float2 grad = float2(-dir.y, dir.x);

					for (float i = -RANGE; i <= RANGE; i += STEP)
					{
						float2 pos2 = pos + normalize(dir) * i;

						// clamp pixel
						if (pos2.y < 0.0 || pos2.x < 0.0 || pos2.x > _ScreenParams.x || pos2.y > _ScreenParams.y)
							continue;

						float2 g = getGrad(pos2, 1.0);
						if (length(g) < _Lines)
							continue;

						weight -= pow(abs(dot(normalize(grad), normalize(g))), _BrushStrength) / floor((2.0 * RANGE + 1.0) / STEP) / ANGLENUM;
					}
				}

				float4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pos / _ScreenParams.xy);
#if USE_GRAYSCALE
				c = (dot(c.rgb, float3(0.2126, 0.7152, 0.0722))).xxxx;
#endif
				float4 bg = lerp(c, 1.0, _Whiteness);

				float r = length(pos - _ScreenParams.xy * 0.5) / _ScreenParams.x;
				float vign = 1.0 - r * r * r;

				float nis = tex2D(_NoiseTex, input.texcoord).x / 25.0;
				return AreaShape(vign * lerp(0.0, bg, weight) + nis, input);
			}
			ENDHLSL
		}
	}
	FallBack Off
}
