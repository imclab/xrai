Shader "[QDY]NPR Filter Pro URP/Weave" {
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

			sampler2D _KnitwearTex;
			float _PixelSize, _StripeBright, _AspectRatio, _NoiseAmount, _HueShift;

			float random (float2 v) { return frac(sin(dot(v, float2(12.9898, 78.233))) * 43758.5453123); }
			float noise (float2 st)
			{
				float2 i = floor(st);
				float2 f = frac(st);

				float a = random(i);
				float b = random(i + float2(1.0, 0.0));
				float c = random(i + float2(0.0, 1.0));
				float d = random(i + float2(1.0, 1.0));

				float2 u = f * f * (3.0 - 2.0 * f);

				return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
			}
			float3 rgbToHsv (float3 c)
			{
				float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
				float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
				float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
				float d = q.x - min(q.w, q.y);
				float e = 1.0e-10;
				return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
			}
			float3 hsvToRgb (float3 c)
			{
				float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
				float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
				return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
			}
			float4 frag (Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				float2 uv = input.texcoord;

				float2 normalizedPixelSize = _PixelSize / _BlitTexture_TexelSize.zw;
				float2 uvPixel = normalizedPixelSize * floor(uv / normalizedPixelSize);
				float4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvPixel);
				
				float2 cellPosition = floor(uv / normalizedPixelSize);
				float2 cellUV = frac(uv / normalizedPixelSize);
				
				float rowOffset = sin((random(float2(0.0, uvPixel.y)) - 0.5) * 0.25);
				cellUV.x += rowOffset; 
				float2 centered = cellUV - 0.5;
//return AreaShape(float4(centered, 0, 1), input);

				float2 noisyCenter = centered + (float2(
					random(cellPosition + centered ),
					random(cellPosition + centered)
					) - 0.5) * _NoiseAmount;

				float isAlternate = fmod(cellPosition.x, 2.0);
				float angle = isAlternate == 0.0 ? radians(-65.0) : radians(65.0);
    
				float2 rotated = float2(
					noisyCenter.x * cos(angle) - noisyCenter.y * sin(angle),
					noisyCenter.x * sin(angle) + noisyCenter.y * cos(angle));

				float ellipse = length(float2(rotated.x, rotated.y * _AspectRatio - 0.075));
				color.rgb *= smoothstep(0.2, 1.0, 1.0 - ellipse);
				
				float stripeNoise = noise(float2(centered.x, centered.y * 100.0)); 
				color.rgb *= stripeNoise + _StripeBright;

float hueShift = (random(cellPosition) - 0.5) * _HueShift;
float3 hsv = rgbToHsv(color.rgb);
hsv.x += hueShift;
color.rgb = hsvToRgb(hsv);
				return AreaShape(color, input);
			}
			ENDHLSL
		}
	}
	FallBack Off
}