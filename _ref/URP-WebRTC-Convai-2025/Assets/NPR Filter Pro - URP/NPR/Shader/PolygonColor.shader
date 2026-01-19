Shader "[QDY]NPR Filter Pro URP/PolygonColor" {
	Properties {}
	SubShader {
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
		Cull Off ZWrite Off ZTest Always Fog { Mode Off }
		Pass {
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment frag
			#pragma multi_compile _ NPR_Circle
			#pragma multi_compile _ NPR_Reverse
			#include "Common.hlsl"

			float _Strength, _Size, _Blur;

			float2 hash2 (float2 p)
			{
				return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
			}
			float2 voronoi (float2 x)
			{
				float2 n = floor(x);
				float2 f = frac(x);

				float2 mr = 0;
				float md = _Strength;
				for (int j = -1; j <= 1; j++)
				{
					for (int i = -1; i <= 1; i++)
					{
						float2 g = float2(float(i), float(j));
						float2 o = hash2(n + g);
						float2 r = g + o - f;
						float d = dot(r, r);
						if (d < md)
						{
							md = d;
							mr = r;
						}
					}
				}
				return mr;
			}
			float3 voronoiColor (float steps, float2 uv)
			{
				float2 c = voronoi(steps * uv);
				float2 uv1 = uv;
				uv1.x += c.x / steps;
				uv1.y += c.y / steps * _ScreenParams.x / _ScreenParams.y;
				return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv1).xyz;
			}
			float4 frag (Varyings input) : SV_Target
			{
				float2 uv = input.texcoord;
				float3 c = 0.0;
				for (int i = 0; i < 4; i++)
				{
					float steps = _Size * pow(_Blur, i);
					c += voronoiColor(steps, uv);
				}
				return AreaShape(float4(c * 0.25, 1.0), input);
			}
			ENDHLSL
		}
	}
	FallBack Off
}