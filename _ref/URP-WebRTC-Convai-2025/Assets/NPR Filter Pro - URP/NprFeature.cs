using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

namespace QDY.NprFilterProURP
{
	public class NprFeature : ScriptableRendererFeature
	{
		internal static RenderTextureDescriptor GetCompatibleDescriptor(RenderTextureDescriptor desc, int width, int height, GraphicsFormat format)
		{
			desc.depthBufferBits = (int)DepthBits.None;
			desc.msaaSamples = 1;
			desc.width = width;
			desc.height = height;
			desc.graphicsFormat = format;
			return desc;
		}
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public class SinglePass : ScriptableRenderPass
		{
			Material m_Mat;
			RTHandle m_CameraColorRT;
			RenderTextureDescriptor m_Descriptor;
			RTHandle m_TempRT;

			public SinglePass()
			{
				this.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
			}
			public void Setup(RTHandle colorHandle, RenderTextureDescriptor sourceDescriptor, Material mat)
			{
				m_CameraColorRT = colorHandle;
				m_Descriptor = sourceDescriptor;
				m_Mat = mat;
			}
			RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, GraphicsFormat format)
				=> NprFeature.GetCompatibleDescriptor(m_Descriptor, width, height, format);
			public override void Execute(ScriptableRenderContext context, ref RenderingData data)
			{
				var cameraData = data.cameraData;
				if (cameraData.camera.cameraType != CameraType.Game)
					return;
				if (m_Mat == null)
					return;

				var desc = GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, GraphicsFormat.R8G8B8A8_UNorm);
				RenderingUtils.ReAllocateIfNeeded(ref m_TempRT, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_tempRT");

				CommandBuffer cmd = CommandBufferPool.Get("NprFeature");
				Blitter.BlitCameraTexture(cmd, m_CameraColorRT, m_TempRT);
				Blitter.BlitCameraTexture(cmd, m_TempRT, m_CameraColorRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_Mat, 0);

				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();
				CommandBufferPool.Release(cmd);
			}
		}
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public class WaterColorPass : ScriptableRenderPass
		{
			Material m_Mat;
			RTHandle m_CameraColorRT;
			RenderTextureDescriptor m_Descriptor;
			RTHandle m_RT1, m_RT2, m_RT3;
			
			// extra material params inside pass
			public Texture2D m_Paper1;
			public float m_Paper1Power = 1f;
			public Texture2D m_Paper2;
			public float m_Paper2Power = 3f;

			public WaterColorPass()
			{
				this.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
			}
			public void Setup(RTHandle colorHandle, RenderTextureDescriptor sourceDescriptor, Material mat)
			{
				m_CameraColorRT = colorHandle;
				m_Descriptor = sourceDescriptor;
				m_Mat = mat;
			}
			RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, GraphicsFormat format)
				=> NprFeature.GetCompatibleDescriptor(m_Descriptor, width, height, format);
			public override void Execute(ScriptableRenderContext context, ref RenderingData data)
			{
				var cameraData = data.cameraData;
				if (cameraData.camera.cameraType != CameraType.Game)
					return;
				if (m_Mat == null)
					return;

				var desc = GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, GraphicsFormat.R8G8B8A8_UNorm);
				RenderingUtils.ReAllocateIfNeeded(ref m_RT1, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_RT1");
				RenderingUtils.ReAllocateIfNeeded(ref m_RT2, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_RT2");
				RenderingUtils.ReAllocateIfNeeded(ref m_RT3, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_RT3");

				CommandBuffer cmd = CommandBufferPool.Get("WaterColorFeature");
				
				// build orig scene rt
				Blitter.BlitCameraTexture(cmd, m_CameraColorRT, m_RT3);
				cmd.SetGlobalTexture("_Global_OrigScene", m_RT3);

				// do water color pass
				Blitter.BlitCameraTexture(cmd, m_CameraColorRT, m_RT1, m_Mat, 0);

				m_Mat.SetTexture("_PaperTex", m_Paper1);
				m_Mat.SetFloat("_PaperPower", m_Paper1Power);
				Blitter.BlitCameraTexture(cmd, m_RT1, m_RT2, m_Mat, 1);
				Blitter.BlitCameraTexture(cmd, m_RT2, m_RT1, m_Mat, 2);

				m_Mat.SetTexture("_PaperTex", m_Paper2);
				m_Mat.SetFloat("_PaperPower", m_Paper2Power);
				Blitter.BlitCameraTexture(cmd, m_RT1, m_RT2, m_Mat, 1);
				Blitter.BlitCameraTexture(cmd, m_RT2, m_CameraColorRT, m_Mat, 2);

				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();
				CommandBufferPool.Release(cmd);
			}
		}
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public class OilPaintPass : ScriptableRenderPass
		{
			Material m_Mat;
			RTHandle m_CameraColorRT;
			RenderTextureDescriptor m_Descriptor;
			RTHandle m_RT1, m_RT2, m_RT3;

			public OilPaintPass()
			{
				this.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
			}
			public void Setup(RTHandle colorHandle, RenderTextureDescriptor sourceDescriptor, Material mat)
			{
				m_CameraColorRT = colorHandle;
				m_Descriptor = sourceDescriptor;
				m_Mat = mat;
			}
			RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, GraphicsFormat format)
				=> NprFeature.GetCompatibleDescriptor(m_Descriptor, width, height, format);
			public override void Execute(ScriptableRenderContext context, ref RenderingData data)
			{
				var cameraData = data.cameraData;
				if (cameraData.camera.cameraType != CameraType.Game)
					return;
				if (m_Mat == null)
					return;

				var desc = GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, GraphicsFormat.R8G8B8A8_UNorm);
				RenderingUtils.ReAllocateIfNeeded(ref m_RT1, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_RT1");
				RenderingUtils.ReAllocateIfNeeded(ref m_RT2, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_RT2");
				RenderingUtils.ReAllocateIfNeeded(ref m_RT3, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_RT3");

				CommandBuffer cmd = CommandBufferPool.Get("OilPaintFeature");
				
				// build orig scene rt
				Blitter.BlitCameraTexture(cmd, m_CameraColorRT, m_RT3);
				cmd.SetGlobalTexture("_Global_OrigScene", m_RT3);

				// run oil paint pass
				Blitter.BlitCameraTexture(cmd, m_CameraColorRT, m_RT1, m_Mat, 0);
				RTHandle rtResult = m_RT1;
				for (int i = 1; i < 3; i++)
				{
					if (i % 2 == 1)   // the odd num pass
					{
						Blitter.BlitCameraTexture(cmd, m_RT1, m_RT2, m_Mat, 0);
						rtResult = m_RT2;
					}
					else   // the even num pass
					{
						Blitter.BlitCameraTexture(cmd, m_RT2, m_RT1, m_Mat, 0);
						rtResult = m_RT1;
					}
				}
				Blitter.BlitCameraTexture(cmd, rtResult, m_CameraColorRT, m_Mat, 1);

				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();
				CommandBufferPool.Release(cmd);
			}
		}
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public class KuwaharaPass : ScriptableRenderPass
		{
			Material m_Mat;
			RTHandle m_CameraColorRT;
			RenderTextureDescriptor m_Descriptor;
			int m_NumPass = 0;
			RTHandle m_RT;

			public KuwaharaPass()
			{
				this.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
			}
			public void Setup(RTHandle colorHandle, RenderTextureDescriptor srcDesc, Material mat, int numPass)
			{
				m_CameraColorRT = colorHandle;
				m_Descriptor = srcDesc;
				m_Mat = mat;
				m_NumPass = numPass;
			}
			RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, GraphicsFormat format)
				=> NprFeature.GetCompatibleDescriptor(m_Descriptor, width, height, format);
			public override void Execute(ScriptableRenderContext context, ref RenderingData data)
			{
				var cameraData = data.cameraData;
				if (cameraData.camera.cameraType != CameraType.Game)
					return;
				if (m_Mat == null)
					return;
				
				if (m_NumPass <= 0)
					return;

				RTHandle[] rts = new RTHandle[m_NumPass];
				var desc = GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, GraphicsFormat.R8G8B8A8_UNorm);
				
				for (int i = 0; i < m_NumPass; i++)
					RenderingUtils.ReAllocateIfNeeded(ref rts[i], desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_tmpRT"+i.ToString());

				CommandBuffer cmd = CommandBufferPool.Get("KuwaharaPass");
				
				// build orig scene rt
				RenderingUtils.ReAllocateIfNeeded(ref m_RT, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_RT");
				Blitter.BlitCameraTexture(cmd, m_CameraColorRT, m_RT);
				cmd.SetGlobalTexture("_Global_OrigScene", m_RT);

				// run kuwahara pass
				Blitter.BlitCameraTexture(cmd, m_CameraColorRT, rts[0], m_Mat, 0);
				for (int i = 1; i < m_NumPass; ++i)
					Blitter.BlitCameraTexture(cmd, rts[i - 1], rts[i], m_Mat, 0);
				Blitter.BlitCameraTexture(cmd, rts[m_NumPass - 1], m_CameraColorRT);

				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();
				CommandBufferPool.Release(cmd);
				
				for (int i = 0; i < m_NumPass; i++)
					rts[i].Release();
			}
		}
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		[HideInInspector] public Material m_Mat;
		[HideInInspector] public int m_UsePass = 0;
		SinglePass m_Pass;

		// water color
		WaterColorPass m_WcPass;
		[HideInInspector] public Texture2D m_Paper1;
		[HideInInspector] public float m_Paper1Power = 1f;
		[HideInInspector] public Texture2D m_Paper2;
		[HideInInspector] public float m_Paper2Power = 3f;
		
		// oil paint
		OilPaintPass m_OpPass;
		
		// kuwahara
		KuwaharaPass m_KhPass;
		[HideInInspector] public int m_KuwaharaNumPass = 0;

		public override void Create()
		{
			m_Pass = new SinglePass();
			m_WcPass = new WaterColorPass();
			m_OpPass = new OilPaintPass();
			m_KhPass = new KuwaharaPass();
		}
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData data)
		{
			if (data.cameraData.cameraType != CameraType.Game)
				return;

			if (0 == m_UsePass)
				renderer.EnqueuePass(m_Pass);
			if (1 == m_UsePass)
				renderer.EnqueuePass(m_WcPass);
			if (2 == m_UsePass)
				renderer.EnqueuePass(m_OpPass);
			if (3 == m_UsePass)
				renderer.EnqueuePass(m_KhPass);
		}
		public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData data)
		{
			if (data.cameraData.cameraType != CameraType.Game)
				return;

			if (0 == m_UsePass)
			{
				m_Pass.ConfigureInput(ScriptableRenderPassInput.Color);
				m_Pass.Setup(renderer.cameraColorTargetHandle, data.cameraData.cameraTargetDescriptor, m_Mat);
			}
			if (1 == m_UsePass)
			{
				m_WcPass.ConfigureInput(ScriptableRenderPassInput.Color);
				m_WcPass.Setup(renderer.cameraColorTargetHandle, data.cameraData.cameraTargetDescriptor, m_Mat);
				m_WcPass.m_Paper1 = m_Paper1;
				m_WcPass.m_Paper1Power = m_Paper1Power;
				m_WcPass.m_Paper2 = m_Paper2;
				m_WcPass.m_Paper2Power = m_Paper2Power;
			}
			if (2 == m_UsePass)
			{
				m_OpPass.ConfigureInput(ScriptableRenderPassInput.Color);
				m_OpPass.Setup(renderer.cameraColorTargetHandle, data.cameraData.cameraTargetDescriptor, m_Mat);
			}
			if (3 == m_UsePass)
			{
				m_KhPass.ConfigureInput(ScriptableRenderPassInput.Color);
				m_KhPass.Setup(renderer.cameraColorTargetHandle, data.cameraData.cameraTargetDescriptor, m_Mat, m_KuwaharaNumPass);
			}
		}
	}
}