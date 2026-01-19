using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace QDY.NprFilterProURP
{
	[RequireComponent(typeof(Camera))]
	public class Mosaic : MonoBehaviour
	{
		public enum EFilter { None = 0, Effect1, Effect2, Effect3, Effect4, Effect5 }
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
			
			public bool HasMat() { return m_Mat != null; }
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
		[System.Serializable] public class Quad : Effect
		{
			[Range(2f, 198f)] public float m_Size = 120f;
			public bool m_UseScreenRatio = true;
			[Range(0.2f, 5f)] public float m_Ratio = 1f;
			[Range(0.2f, 5f)] public float m_ScaleX = 1f;
			[Range(0.2f, 5f)] public float m_ScaleY = 1f;

			public override EFilter Filter() { return EFilter.Effect1; }
			public override void Apply()
			{
				if (m_UseScreenRatio) m_Ratio = (float)Screen.width / (float)Screen.height;
				m_Mat.SetFloat("_Size", 200f - m_Size);
				m_Mat.SetFloat("_Ratio", m_Ratio);
				m_Mat.SetFloat("_ScaleX", m_ScaleX);
				m_Mat.SetFloat("_ScaleY", m_ScaleY);
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class Triangle : Effect
		{
			[Range(2f, 198f)] public float m_Size = 150f;
			public bool m_UseScreenRatio = true;
			[Range(0.2f, 5f)] public float m_Ratio = 1f;
			[Range(0.2f, 5f)] public float m_ScaleX = 1f;
			[Range(0.2f, 5f)] public float m_ScaleY = 1f;

			public override EFilter Filter() { return EFilter.Effect2; }
			public override void Apply()
			{
				if (m_UseScreenRatio) m_Ratio = (float)Screen.width / (float)Screen.height;
				m_Mat.SetFloat("_Size", 200f - m_Size);
				m_Mat.SetFloat("_Ratio", m_Ratio);
				m_Mat.SetFloat("_ScaleX", m_ScaleX);
				m_Mat.SetFloat("_ScaleY", m_ScaleY);
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class Diamond : Effect
		{
			[Range(0.01f, 1f)] public float m_Size = 0.2f;

			public override EFilter Filter() { return EFilter.Effect3; }
			public override void Apply()
			{
				m_Mat.SetFloat("_Size", m_Size);
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class Circle : Effect
		{
			[Range(0.7f, 0.9f)] public float m_Size = 0.85f;
			[Range(0.2f, 1f)] public float m_Radius = 0.45f;
			[Range(0.9f, 5f)] public float m_Interval = 1f;
			public Color m_Background = new Color(0f, 0f, 0f, 1f);

			public override EFilter Filter() { return EFilter.Effect4; }
			public override void Apply()
			{
				float size = (1.01f - m_Size) * 300f;
				Vector4 param = new Vector4(size, m_Interval, m_Radius, 0f);
				m_Mat.SetVector("_Params", param);
				m_Mat.SetColor("_BackgroundColor", m_Background);
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		[System.Serializable] public class Hexagon : Effect
		{
			[Range(0.02f, 0.2f)] public float m_PixelSize = 0.05f;
			[Range(0.01f, 2f)] public float m_GridWidth = 1f;

			public override EFilter Filter() { return EFilter.Effect5; }
			public override void Apply()
			{
				m_Mat.SetFloat("_PixelSize", m_PixelSize);
				m_Mat.SetFloat("_GridWidth", m_GridWidth);
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public Quad m_Quad;
		public Triangle m_Triangle;
		public Diamond m_Diamond;
		public Circle m_Circle;
		public Hexagon m_Hexagon;
		List<Effect> m_Effects;

		void Start()
		{
			if (m_Pipeline != null)
				GraphicsSettings.defaultRenderPipeline = m_Pipeline;
			else
				GraphicsSettings.defaultRenderPipeline = null;

			m_Effects = new List<Effect>{ m_Quad, m_Triangle, m_Diamond, m_Circle, m_Hexagon };

			OnFilterChange();
			m_PrevEnableFilter = m_EnableFilter;
		}
		void Update()
		{
			// when change happen, run it once
			if (m_PrevEnableFilter != m_EnableFilter)
			{
				OnFilterChange();
				m_PrevEnableFilter = m_EnableFilter;
			}
			
			// disable effect, nothing need to do
			if (EFilter.None == m_EnableFilter)
			{
				m_Feature.SetActive(false);
				return;
			}

			// update material params
			AreaParams ap = CreateFrameParams();
			foreach (Effect efc in m_Effects)
			{
				if (efc.HasMat() && efc.Filter() == m_EnableFilter)
				{
					efc.UpdateArea(ap);
					efc.Apply();
				}
			}
		}
		void OnFilterChange()
		{
			foreach (Effect efc in m_Effects)
			{
				if (efc.Filter() == m_EnableFilter)
				{
					efc.SetupFeature(m_Feature);
					return;
				}
			}
		}
	}
}