using System;
using System.Linq;
using Common;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Games_101_4
{
	public class Main_101_4 : MonoBehaviour
	{
		private const string k_CSMain = "CSMain";

		private static readonly int BlitTex_ID = Shader.PropertyToID("_BlitTex");
		private static readonly int Result_ID = Shader.PropertyToID("_ResultTex");
		private static readonly int Width_ID = Shader.PropertyToID("_Width");
		private static readonly int Height_ID = Shader.PropertyToID("_Height");


		public Transform pointsParent;

		public ComputeShader bezierCS;

		private Transform[] points;
		private CommandBuffer cmd;
		private Camera mainCamera;

		private RenderTexture screenRT;


		private void OnEnable()
		{
			mainCamera = Camera.main;
			points = pointsParent.Cast<Transform>().ToArray();

			InitCmd();
		}

		private void OnDisable()
		{
			cmd?.Dispose();
			cmd = null;
			Destroy(screenRT);
		}

		private void Update()
		{
			if (cmd != null)
			{
				Graphics.ExecuteCommandBuffer(cmd);
			}
		}

		private void InitCmd()
		{
			int width = mainCamera.scaledPixelWidth;
			int height = mainCamera.scaledPixelHeight;
			screenRT = new RenderTexture(width, height, 0, GraphicsFormat.R8G8B8A8_UNorm)
			{
				name = "MyScreenRT",
				enableRandomWrite = true,
			};
			
			GetComponent<BlitMonoCtrl>().rt = screenRT;

			int mainKernel = bezierCS.FindKernel(k_CSMain);
			bezierCS.SetInt(Width_ID, width);
			bezierCS.SetInt(Height_ID, height);
			bezierCS.SetTexture(mainKernel, Result_ID, screenRT);

			cmd = new CommandBuffer {name = "Bezier"};
			cmd.DispatchCompute(bezierCS, mainKernel, Mathf.CeilToInt(width / 8.0f), Mathf.CeilToInt(height / 8.0f), 1);
		}
	}
}