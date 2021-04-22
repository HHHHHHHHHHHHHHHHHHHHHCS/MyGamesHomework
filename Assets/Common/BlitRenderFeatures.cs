using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Common
{
	public class BlitRenderFeatures : ScriptableRendererFeature
	{
		public Shader blitShader;

		private BlitRenderPass blitRenderPass;

		private Material blitMaterial;

		public override void Create()
		{
			if (blitMaterial != null && blitMaterial.shader != blitShader)
			{
				DestroyImmediate(blitMaterial);
			}

			if (blitShader == null)
			{
				Debug.LogError("Blit Shader is null!");
				return;
			}

			blitMaterial = CoreUtils.CreateEngineMaterial(blitShader);

			blitRenderPass = new BlitRenderPass()
			{
				renderPassEvent = RenderPassEvent.AfterRendering
			};
			blitRenderPass.Init(blitMaterial);
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (blitRenderPass == null)
			{
				return;
			}

			var blitCtrl = renderingData.cameraData.camera.GetComponent<BlitMonoCtrl>();
			if (blitCtrl != null && blitCtrl.enableBlit)
			{
				blitRenderPass.Setup(blitCtrl.rtID, blitCtrl.rt);
				renderer.EnqueuePass(blitRenderPass);
			}
		}
	}


	public class BlitRenderPass : ScriptableRenderPass
	{
		private const string k_BlitPass = "BlitPass";
		private readonly ProfilingSampler profilingSampler = new ProfilingSampler(k_BlitPass);

		private static RenderTargetIdentifier CameraColorTexture_RTI = Shader.PropertyToID("_CameraColorTexture");

		private static int SrcTex_ID = Shader.PropertyToID("_SrcTex");

		public Material blitMaterial;

		private int blitRT_ID;
		private RenderTexture blitRT;


		public void Init(Material blitMat)
		{
			blitMaterial = blitMat;
		}

		public void Setup(int rtID, RenderTexture rt)
		{
			blitRT_ID = rtID;
			blitRT = rt;
		}

		public override void FrameCleanup(CommandBuffer cmd)
		{
			blitRT_ID = -1;
			blitRT = null;
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (!(blitRT_ID >= 0 || blitRT != null))
			{
				return;
			}

			CommandBuffer cmd = CommandBufferPool.Get(k_BlitPass);
			using (new ProfilingScope(cmd, profilingSampler))
			{
				if (blitRT_ID >= 0)
				{
					cmd.SetGlobalTexture(SrcTex_ID, blitRT_ID);
				}
				else //if (blitRT!= null)
				{
					cmd.SetGlobalTexture(SrcTex_ID, blitRT);
				}

				cmd.SetRenderTarget(CameraColorTexture_RTI);
				CoreUtils.DrawFullScreen(cmd, blitMaterial, null, 0);

				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();
			}

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}
	}
}