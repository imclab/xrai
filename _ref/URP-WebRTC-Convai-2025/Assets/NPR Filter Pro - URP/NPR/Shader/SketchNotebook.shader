Shader "[QDY]NPR Filter Pro URP/SketchNotebook" {
	Properties {
		_NoiseTex      ("Noise", 2D) = "white" {}
		_Features      ("Features", Vector) = (0.0, 0.0, 0.0, 0.0)
		_BrushStrength ("Brush Strength", Range(0.1, 3.0)) = 1.0
		_BrushExpand   ("Brush Expand", Float) = 400
		_GridSize      ("Grid Size", Float) = 400
		_Colorful      ("Colorful", Float) = 0.2
	}
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

			sampler2D _NoiseTex;
			float4 _NoiseTex_TexelSize, _Features;
			half _BrushStrength, _BrushExpand, _GridSize, _Colorful;

			// effect shader begin ///////////////////////////////////////////////////////////////////////////////////
			#define Res      _BlitTexture_TexelSize.zw
			#define ResNoise _NoiseTex_TexelSize.zw

			#define AngleNum 3
			#define SampNum 16
			#define PI2 6.28318530717959

			float4 random (float2 pos) { return tex2D(_NoiseTex, pos / ResNoise / Res.y * 1080.0); }
//			float4 random (float2 pos) { return tex2D(_NoiseTex, pos / ResNoise*0.2); }
			float4 getCol (float2 pos)
			{
				float2 uv = ((pos - Res.xy * 0.5) / Res.y * Res.y) / Res.xy + 0.5;
				float4 c1 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
				float4 e = smoothstep(-0.05, 0.0, float4(uv, 1.0 - uv));
				c1 = lerp(1.0, c1, e.x * e.y * e.z * e.w);
				float d = saturate(dot(c1.xyz, float3(-0.5, 1.0, -0.5)));
				return min(lerp(c1, 0.7, 1.8 * d), 0.7);
			}
			float4 getColHT (float2 pos) { return smoothstep(0.95, 1.05, getCol(pos) * 0.8 + _Colorful + random(pos)); }
			float grayscale (float2 pos) { return pow(dot(getCol(pos).xyz, 0.333), 1.0) * _BrushStrength; }
			float2 getGrad(float2 pos, float eps)
			{
				float2 d = float2(eps, 0);
				return float2(
					grayscale(pos + d.xy) - grayscale(pos - d.xy),
					grayscale(pos + d.yx) - grayscale(pos - d.yx)) / eps / 2.0;
			}
			float4 fx (in float2 pos)
			{
				float3 col = 0.0;
				float3 col2 = 0.0;
				float sum = 0.0;
				for (int i = 0; i < AngleNum; i++)
				{
					float ang = PI2 / float(AngleNum) * (float(i) + 0.8);
					float2 v = float2(cos(ang), sin(ang));
					for (int j = 0; j < SampNum; j++)
					{
						float2 dpos = v.yx * float2(1, -1) * float(j) * Res.y / _BrushExpand;
						float2 dpos2 = v.xy * float(j * j) / float(SampNum) * 0.5 * Res.y / _BrushExpand;

						for (float s = -1.0; s <= 1.0; s += 2.0)
						{
							float2 pos2 = pos + s * dpos + dpos2;
							float2 pos3 = pos + (s * dpos + dpos2).yx * float2(1, -1) * 2.0;
							float2 g = getGrad(pos2, 0.4);
							float fact = dot(g, v) - 0.5 * abs(dot(g, v.yx * float2(1, -1)));
							float fact2 = dot(normalize(g + 0.0001), v.yx * float2(1, -1));

							fact = clamp(fact, 0.0, 0.05);
							fact2 = abs(fact2);

							fact *= 1.0 - float(j) / float(SampNum);
							col += fact;
							col2 += fact2 * getColHT(pos3).xyz;
							sum += fact2;
						}
					}
				}
				col /= float(SampNum * AngleNum) * 0.75 / sqrt(Res.y);
				col2 /= sum;
				col.x *= (0.6 + 0.8 * random(pos * 0.7).x);
				col.x = 1.0 - col.x;
				col.x *= col.x * col.x;

				float3 karo = 1.0;
				if (_Features.x > 0.5)
				{
					float2 s = sin(pos.xy * 0.1 / sqrt(Res.y / _GridSize));
					karo -= 0.5 * float3(0.25, 0.1, 0.1) * dot(exp(-s * s * 80.0), 1.0);
				}

				float vign = 1.0;
				float r = length(pos - Res.xy * 0.5) / Res.x;
				vign = max(0.0, vign - _Features.y * r * r * r);
				return float4(float3(col.x * col2 * karo * vign), 1.0);
			}
			// effect shader end ///////////////////////////////////////////////////////////////////////////////////
			half4 frag (Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				float2 uv = input.texcoord;
				return AreaShape(fx(uv * Res), input);
			}
			ENDHLSL
		}
	}
	FallBack Off
}
