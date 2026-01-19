Shader "[QDY]NPR Filter Pro URP/Mosaic/Hexagon" {
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

			float _PixelSize, _GridWidth;

			float HexDist (float2 a, float2 b)
			{
				float2 p = abs(b - a);
				float s = 0.5;
				float c = 0.8660254;

				float dist = s * p.x + c * p.y;
				return max(dist, p.x) / c;
			}
			float2 NearestHex (float s, float2 st)
			{
				float h = 0.5 * s;
				float r = 0.8660254 * s;
				float b = s + 2.0 * h;
				float a = 2.0 * r;
				float m = h / r;

				float2 sect = st / float2(2.0 * r, h + s);
				float2 sectPxl = fmod(st, float2(2.0 * r, h + s));

				float2 coord = floor(sect);
				float section = fmod(floor(sect.y), 2.0);
				if (section > 0.0)
				{
					if (sectPxl.y < (h - sectPxl.x * m))
						coord -= 1.0;
					else if (sectPxl.y < (-h + sectPxl.x * m))
						coord.y -= 1.0;
				}
				else
				{
					if (sectPxl.x > r)
					{
						if (sectPxl.y < (2.0 * h - sectPxl.x * m))
							coord.y -= 1.0;
					}
					else
					{
						if (sectPxl.y < (sectPxl.x * m))
							coord.y -= 1.0;
						else
							coord.x -= 1.0;
					}
				}
				float xoff = fmod(coord.y, 2.0) * r;
				return float2(coord.x * 2.0 * r - xoff, coord.y * (h + s)) + float2(r * 2.0, s);
			}
			float4 frag (Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float pixelSize = _PixelSize * _ScreenParams.x * 0.2;
				float2 nearest = NearestHex(pixelSize, input.texcoord * _ScreenParams.xy);

				float4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, nearest / _ScreenParams.xy);

				float dist = HexDist(input.texcoord * _ScreenParams.xy, nearest);
				float interior = 1.0 - smoothstep(pixelSize - 0.8, pixelSize, dist * _GridWidth);
				return AreaShape(float4(c.rgb * interior, 1.0), input);
			}
			ENDHLSL
		}
	}
	FallBack Off
}