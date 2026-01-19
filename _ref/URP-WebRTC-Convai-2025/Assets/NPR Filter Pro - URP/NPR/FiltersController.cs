using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace QDY.NprFilterProURP
{
	public class FiltersController : MonoBehaviour
	{
		public enum EFilter {
			None = 0, Effect1, Effect2, Effect3, Effect4, Effect5, Effect6, Effect7, Effect8,
			Effect9, Effect10, Effect11, Effect12, Effect13, Effect14, Effect15
		}
		public EFilter m_EnableFilter = EFilter.None;
		EFilter m_PrevEnableFilter = EFilter.None;
		public RenderPipelineAsset m_Pipeline;
		public NprFeature m_Feature;
		////////////////////////////////////////////////////////////////////////////////////////////////////
		float m_MouseX = 0.5f;
		float m_MouseY = 0.5f;
		bool m_TraceMouse = false;
		public enum EArea { EA_Fullscreen = 0, EA_Circle };
		public EArea m_Area = EArea.EA_Fullscreen;
		public bool m_Reverse = false;
		[Range(0f, 1f)] public float m_CircleRadius = 0.2f;
		[Range(0f, 0.1f)] public float m_CircleFading = 0.05f;
		// used for params pass.
		public class AreaParams
		{
			public float mouseX;
			public float mouseY;
			public EArea area;
			public bool reverse;
			public float circleRadius;
			public float circleFading;
		}
		public AreaParams CreateFrameParams()
		{
			if (Input.GetMouseButtonDown(0))
			{
				m_TraceMouse = true;
			}
			else if (Input.GetMouseButtonUp(0))
			{
				m_TraceMouse = false;
			}
			else if (Input.GetMouseButton(0))
			{
				if (m_TraceMouse)
				{
					m_MouseX = Input.mousePosition.x / Screen.width;
					m_MouseY = Input.mousePosition.y / Screen.height;
				}
			}
			AreaParams ap = new AreaParams();
			ap.mouseX = m_MouseX;
			ap.mouseY = m_MouseY;
			ap.area = m_Area;
			ap.reverse = m_Reverse;
			ap.circleRadius = m_CircleRadius;
			ap.circleFading = m_CircleFading;
			return ap;
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public class Effect
		{
			public Material m_Mat;
			
			public void UpdateArea(AreaParams ap)
			{
				if (ap.area == EArea.EA_Fullscreen)
				{
					m_Mat.DisableKeyword("NPR_Circle");
				}
				else if (ap.area == EArea.EA_Circle)
				{
					m_Mat.EnableKeyword("NPR_Circle");
					m_Mat.SetVector("_CircleXYWH", new Vector4(ap.mouseX, ap.mouseY, ap.circleRadius, ap.circleFading));
				}
				if (ap.reverse) m_Mat.EnableKeyword("NPR_Reverse");
				else m_Mat.DisableKeyword("NPR_Reverse");
			}
			public virtual EFilter Filter() { return EFilter.None; }
			public virtual void Apply() {}
			public virtual void SetupFeature(NprFeature ft)
			{
				ft.SetActive(true);
				ft.m_UsePass = 0;
				ft.m_Mat = m_Mat;
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class Brick : Effect
		{
			[Range(0.01f, 0.2f)] public float m_Size = 0.07f;
			[Range(1f, 6f)] public float m_GridShadow = 2f;
			[Range(1.5f, 3.5f)] public float m_CircleSize = 2f;
			[Range(0.2f, 1f)] public float m_CircleHard = 0.9f;

			public override EFilter Filter() { return EFilter.Effect1; }
			public override void Apply()
			{
				m_Mat.SetFloat("_Size", m_Size);
				m_Mat.SetFloat("_GridShadow", m_GridShadow);
				m_Mat.SetFloat("_CircleSize", m_CircleSize);
				m_Mat.SetFloat("_CircleHard", m_CircleHard);
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class WaterColor : Effect
		{
			public Texture2D m_WobbleTex;
			public float m_WobbleScale = 1f;
			[Range(0.001f, 0.006f)] public float m_WobblePower = 0.003f;
			[Range(0f, 2f)] public float m_EdgeSize = 1f;
			[Range(-3f, 3f)] public float m_EdgePower = 3f;
			public Texture2D m_Paper1;
			public float m_Paper1Power = 1f;
			public Texture2D m_Paper2;
			public float m_Paper2Power = 3f;

			public override EFilter Filter() { return EFilter.Effect2; }
			public override void Apply()
			{
				m_Mat.SetTexture("_WobbleTex", m_WobbleTex);
				m_Mat.SetFloat("_WobbleScale", m_WobbleScale);
				m_Mat.SetFloat("_WobblePower", m_WobblePower);
				m_Mat.SetFloat("_EdgeSize", m_EdgeSize);
				m_Mat.SetFloat("_EdgePower", m_EdgePower);
			}
			public override void SetupFeature(NprFeature ft)
			{
				ft.SetActive(true);
				ft.m_UsePass = 1;
				ft.m_Mat = m_Mat;
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class SketchDrawing : Effect
		{
			public bool m_GrayScale = false;
			[Range(1f, 16f)] public float m_BrushStrength = 10f;
			[Range(0f, 1f)] public float m_Whiteness = 0.5f;
			[Range(0.001f, 0.1f)] public float m_Lines = 0.01f;

			public override EFilter Filter() { return EFilter.Effect3; }
			public override void Apply()
			{
				if (m_GrayScale)
					m_Mat.EnableKeyword("USE_GRAYSCALE");
				else
					m_Mat.DisableKeyword("USE_GRAYSCALE");
				m_Mat.SetFloat("_BrushStrength", m_BrushStrength);
				m_Mat.SetFloat("_Whiteness", m_Whiteness);
				m_Mat.SetFloat("_Lines", m_Lines);
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class Knitwear : Effect
		{
			[Range(-2f, 2f)] public float m_Shear = 1f;
			[Range(1f, 200f)] public float m_Division = 50f;
			[Range(0.2f, 5f)] public float m_Aspect = 1f;
			public Texture2D m_Tex;

			public override EFilter Filter() { return EFilter.Effect4; }
			public override void Apply()
			{
				m_Mat.SetFloat("_KnitwearShear", m_Shear);
				m_Mat.SetFloat("_KnitwearDivision", m_Division);
				m_Mat.SetFloat("_KnitwearAspect", m_Aspect);
				m_Mat.SetTexture("_KnitwearTex", m_Tex);
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class DotDrawing : Effect
		{
			[Range(1f, 64f)] public float m_DotSize = 28f;
			[Range(1f, 64f)] public float m_Darkness = 6f;

			public override EFilter Filter() { return EFilter.Effect5; }
			public override void Apply()
			{
				m_Mat.SetFloat("_DotSize", m_DotSize);
				m_Mat.SetFloat("_Darkness", m_Darkness);
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class OneBit : Effect
		{
			public bool m_UseOrigColor = false;
			[Range(16f, 255f)] public float m_ColorBit = 255f;
			[Range(1f, 8f)] public float m_Scale = 2f;
			[Range(1f, 16f)] public float m_DitherSize = 10f;
			[Range(1f, 120f)] public float m_Threshold = 60f;
			public Color m_ColorA = new Color(0f, 0f, 0f, 1f);
			public Color m_ColorB = new Color(1f, 1f, 1f, 1f);
			[Range(1, 8)] public int m_ColorDepth = 2;

			public override EFilter Filter() { return EFilter.Effect6; }
			public override void Apply()
			{
				if (m_UseOrigColor)
					m_Mat.EnableKeyword("NFP_OrigColor");
				else
					m_Mat.DisableKeyword("NFP_OrigColor");
				m_Mat.SetFloat("_DitherSize", m_DitherSize);
				m_Mat.SetFloat("_Threshold", m_Threshold);
				m_Mat.SetInt("_ColorDepth", m_ColorDepth);
				m_Mat.SetFloat("_Scale", m_Scale);
				m_Mat.SetColor("_ColorA", m_ColorA);
				m_Mat.SetColor("_ColorB", m_ColorB);
				m_Mat.SetFloat("_ColorBit", m_ColorBit);
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class PolygonColor : Effect
		{
			[Range(0f, 1f)] public float m_Strength = 1f;
			[Range(1f, 32f)] public float m_Size = 8f;
			[Range(1f, 4f)] public float m_Blur = 2f;
			public enum EType { Custom = 0, Blur, Clean };
			public EType m_Fx = EType.Blur;

			public override EFilter Filter() { return EFilter.Effect7; }
			public override void Apply()
			{
				if (m_Fx == EType.Blur)
				{
					m_Strength = 1f;
					m_Size = 8f;
					m_Blur = 2f;
				}
				else if (m_Fx == EType.Clean)
				{
					m_Strength = 1f;
					m_Size = 32f;
					m_Blur = 1f;
				}
				m_Mat.SetFloat("_Strength", m_Strength);
				m_Mat.SetFloat("_Size", m_Size);
				m_Mat.SetFloat("_Blur", m_Blur);
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class Tiles : Effect
		{
			[Range(1f, 60f)] public float m_NumTiles = 48f;
			[Range(0f, 1f)] public float m_Threshhold = 0.2f;
			public Color m_EdgeColor = new Color(0.7f, 0.7f, 0.7f, 1f);

			public override EFilter Filter() { return EFilter.Effect8; }
			public override void Apply()
			{
				m_Mat.SetFloat("_NumTiles", m_NumTiles);
				m_Mat.SetFloat("_Threshhold", m_Threshhold);
				m_Mat.SetColor("_EdgeColor", m_EdgeColor);
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class OilPaint : Effect
		{
			[Range(0.1f, 3f)] public float m_Resolution = 0.9f;
			public float m_EdgeSize = 1f;
			public float m_EdgePower = 3f;

			public override EFilter Filter() { return EFilter.Effect9; }
			public override void Apply()
			{
				m_Mat.SetFloat("_Resolution", m_Resolution);
				m_Mat.SetFloat("_EdgeSize", m_EdgeSize);
				m_Mat.SetFloat("_EdgePower", m_EdgePower);
			}
			public override void SetupFeature(NprFeature ft)
			{
				ft.SetActive(true);
				ft.m_UsePass = 2;
				ft.m_Mat = m_Mat;
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class BayerDither : Effect
		{
			public override EFilter Filter() { return EFilter.Effect10; }
			public override void Apply() {}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class SketchNotebook : Effect
		{
			[Range(0.01f, 3f)] public float m_BrushStrength = 1f;
			[Range(90f, 1500f)] public float m_BrushExpand = 400f;
			[Range(0.01f, 6f)] public float m_Vignette = 1f;
			public bool m_BackgroundGrid = true;
			[Range(1f, 600f)] public float m_GridSize = 400f;
			[Range(0.01f, 0.3f)] public float m_Colorful = 0.3f;

			public override EFilter Filter() { return EFilter.Effect11; }
			public override void Apply()
			{
				m_Mat.SetVector("_Features", new Vector4(m_BackgroundGrid ? 1f : 0f, m_Vignette, 0f, 0f));
				m_Mat.SetFloat("_BrushStrength", m_BrushStrength);
				m_Mat.SetFloat("_BrushExpand", m_BrushExpand);
				m_Mat.SetFloat("_GridSize", m_GridSize);
				m_Mat.SetFloat("_Colorful", m_Colorful);
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class Aquarelle : Effect
		{
			[Range(1f, 16f)] public float m_Joggle = 4f;
			[Range(0f, 1f)] public float m_JoggleColorLerp = 0.3f;
			[Range(-0.6f, -0.1f)] public float m_Border = -0.35f;
			[Range(0f, 0.5f)] public float m_Whiteness = 0.1f;
			[Range(10f, 90f)] public float m_BorderSharp = 80f;
			[Range(0.01f, 1f)] public float m_Stroke = 0.1f;

			public override EFilter Filter() { return EFilter.Effect12; }
			public override void Apply()
			{
				m_Mat.SetFloat("_Joggle", m_Joggle);
				m_Mat.SetFloat("_JoggleColorLerp", m_JoggleColorLerp);
				m_Mat.SetFloat("_Border", m_Border);
				m_Mat.SetFloat("_Whiteness", m_Whiteness);
				m_Mat.SetFloat("_BorderSharp", m_BorderSharp);
				m_Mat.SetFloat("_Stroke", m_Stroke);
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class Kuwahara : Effect
		{
			[Range(2, 20)] public int m_KernelSize = 15;
			[Range(2, 8)] public int m_Loop = 8;
			[Range(2f, 18f)] public float m_Sharpness = 18f;
			[Range(1f, 100f)] public float m_Hardness = 100;
			[Range(0.5f, 2f)] public float m_ZeroCrossing = 0.58f;
			public bool m_UseZeta = false;
			[Range(0.01f, 2f)] public float m_Zeta = 1f;
			[Range(1, 4)] public int m_Passes = 2;

			public override EFilter Filter() { return EFilter.Effect13; }
			public override void Apply()
			{
				m_Mat.SetInt("_KernelSize", m_KernelSize);
				m_Mat.SetInt("_Loop", m_Loop);
				m_Mat.SetFloat("_Sharpness", m_Sharpness);
				m_Mat.SetFloat("_Hardness", m_Hardness);
				m_Mat.SetFloat("_ZeroCrossing", m_ZeroCrossing);
				m_Mat.SetFloat("_Zeta", m_UseZeta ? m_Zeta : 2f / (m_KernelSize / 2f));
			}
			public override void SetupFeature(NprFeature ft)
			{
				ft.SetActive(true);
				ft.m_UsePass = 3;
				ft.m_Mat = m_Mat;
				ft.m_KuwaharaNumPass = m_Passes;  // bugfix: this code should call every frame.
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class Weave : Effect
		{
			[Range(4f, 24f)] public float m_PixelSize = 12f;
			[Range(0.4f, 1f)] public float m_StripeBright = 0.6f;
			[Range(1f, 3f)] public float m_AspectRatio = 1.8f;
			[Range(0f, 0.25f)] public float m_NoiseAmount = 0.18f;
			[Range(0f, 2f)] public float m_HueShift = 0.08f;

			public override EFilter Filter() { return EFilter.Effect14; }
			public override void Apply()
			{
				m_Mat.SetFloat("_PixelSize", m_PixelSize);
				m_Mat.SetFloat("_StripeBright", m_StripeBright);
				m_Mat.SetFloat("_AspectRatio", m_AspectRatio);
				m_Mat.SetFloat("_NoiseAmount", m_NoiseAmount);
				m_Mat.SetFloat("_HueShift", m_HueShift);
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class BlackWhiteScary : Effect
		{
			[Range(2f, 32f)] public float m_Speed = 16f;
			[Range(0.01f, 0.05f)] public float m_Zigzag = 0.015f;
			[Range(0f, 0.1f)] public float m_Noisy = 0.01f;

			public override EFilter Filter() { return EFilter.Effect15; }
			public override void Apply()
			{
				m_Mat.SetFloat("_Speed", m_Speed);
				m_Mat.SetFloat("_Zigzag", m_Zigzag);
				m_Mat.SetFloat("_Noisy", m_Noisy);
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public Brick m_Brick;
		public WaterColor m_WaterColor;
		public SketchDrawing m_SketchDrawing;
		public Knitwear m_Knitwear;
		public OilPaint m_OilPaint;
		public DotDrawing m_DotDrawing;
		public OneBit m_OneBit;
		public Tiles m_Tiles;
		public PolygonColor m_PolygonColor;
		public BayerDither m_BayerDither;
		public SketchNotebook m_SketchNotebook;
		public Aquarelle m_Aquarelle;
		public Kuwahara m_Kuwahara;
		public Weave m_Weave;
		public BlackWhiteScary m_BlackWhiteScary;
		List<Effect> m_AllEffects;

		void Start()
		{
			if (m_Pipeline != null)
				GraphicsSettings.defaultRenderPipeline = m_Pipeline;
			else
				GraphicsSettings.defaultRenderPipeline = null;

			m_AllEffects = new List<Effect>{ m_Brick, m_WaterColor, m_SketchDrawing, m_Knitwear, m_OilPaint, m_DotDrawing, m_OneBit, m_PolygonColor,
				m_Tiles, m_BayerDither, m_SketchNotebook, m_Aquarelle, m_Kuwahara, m_Weave, m_BlackWhiteScary };

			ApplyFilter();
		}
		void Update()
		{
			AreaParams ap = CreateFrameParams();
			foreach (Effect efc in m_AllEffects)
			{
				if (efc.Filter() == m_EnableFilter)
				{
					efc.UpdateArea(ap);
					efc.Apply();
				}
			}

			// realtime material params update for watercolor
			if (EFilter.Effect2 == m_EnableFilter)
			{
				m_Feature.m_Paper1 = m_WaterColor.m_Paper1;
				m_Feature.m_Paper1Power = m_WaterColor.m_Paper1Power;
				m_Feature.m_Paper2 = m_WaterColor.m_Paper2;
				m_Feature.m_Paper2Power = m_WaterColor.m_Paper2Power;
			}

			if (m_PrevEnableFilter != m_EnableFilter)
			{
				ApplyFilter();
				m_PrevEnableFilter = m_EnableFilter;
			}
		}
		void ApplyFilter()
		{
			if (EFilter.None == m_EnableFilter)
			{
				m_Feature.SetActive(false);
				return;
			}

			foreach (Effect efc in m_AllEffects)
			{
				if (efc.Filter() == m_EnableFilter)
				{
					efc.SetupFeature(m_Feature);
					return;
				}
			}
			m_Feature.SetActive(false);
		}
	}
}