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
		private const string k_CalcPoints = "CalcPoints";
		private const string k_BlitScreen = "BlitScreen";

		private static readonly int Result_ID = Shader.PropertyToID("_ResultTex");
		private static readonly int Width_ID = Shader.PropertyToID("_Width");
		private static readonly int Height_ID = Shader.PropertyToID("_Height");


		public Transform pointsParent;
		public int tCount = 101;
		public ComputeShader bezierCS;


		private Camera mainCamera;
		private Vector3[] points;

		private CommandBuffer cmd;
		private RenderTexture screenRT;
		private ComputeBuffer inputPoints_cb, outPoints_cb;


		private void OnEnable()
		{
			InitCmd();

			if (cmd != null)
			{
				Graphics.ExecuteCommandBuffer(cmd);
				Vector3[] ps = new Vector3[tCount];
				outPoints_cb.GetData(ps);
				foreach (var p in ps)
				{
					var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					go.transform.localScale = Vector3.one * 0.1f;
					go.transform.position = GetMVPS().inverse * new Vector4(p.x, p.y, p.z, 1.0f);
				}
			}
		}

		private void OnDisable()
		{
			cmd?.Dispose();
			cmd = null;
			Destroy(screenRT);
			inputPoints_cb?.Dispose();
			inputPoints_cb = null;
			outPoints_cb?.Dispose();
			outPoints_cb = null;
		}

		// private void Update()
		// {
		// }

		private void InitCmd()
		{
			mainCamera = Camera.main;
			points = pointsParent.Cast<Transform>().Select(x => x.position).ToArray();

			int width = mainCamera.scaledPixelWidth;
			int height = mainCamera.scaledPixelHeight;
			screenRT = new RenderTexture(width, height, 0, GraphicsFormat.R8G8B8A8_UNorm)
			{
				name = "MyScreenRT",
				enableRandomWrite = true,
			};

			GetComponent<BlitMonoCtrl>().rt = screenRT;

			if (tCount < 2)
			{
				return;
			}

			int calcPoints_kernel = bezierCS.FindKernel(k_CalcPoints);
			bezierCS.SetInt("_PointsCount", points.Length);
			bezierCS.SetFloat("_TStep", 1.0f / (tCount - 1));
			inputPoints_cb = new ComputeBuffer(points.Length, 3 * sizeof(float), ComputeBufferType.Structured);
			outPoints_cb = new ComputeBuffer(tCount, 3 * sizeof(float), ComputeBufferType.Structured);
			inputPoints_cb.SetData(points);
			bezierCS.SetBuffer(calcPoints_kernel, "_Points", inputPoints_cb);
			bezierCS.SetBuffer(calcPoints_kernel, "_BezierPoints", outPoints_cb);

			bezierCS.SetMatrix("_VPSMatrix", GetMVPS());

			int blitScreen_kernel = bezierCS.FindKernel(k_BlitScreen);
			bezierCS.SetInt(Width_ID, width);
			bezierCS.SetInt(Height_ID, height);
			bezierCS.SetTexture(blitScreen_kernel, Result_ID, screenRT);
			bezierCS.SetBuffer(blitScreen_kernel, "_BezierPoints", outPoints_cb);
			bezierCS.SetInt("_TCount", tCount);


			cmd = new CommandBuffer {name = "Bezier"};
			cmd.DispatchCompute(bezierCS, calcPoints_kernel, tCount, 1, 1);
			cmd.DispatchCompute(bezierCS, blitScreen_kernel, Mathf.CeilToInt(width / 8.0f),
				Mathf.CeilToInt(height / 8.0f), 1);
		}


		private Matrix4x4 GetMVPS()
		{
			var pixelRect = mainCamera.pixelRect;
			Matrix4x4 screenMatrix = Matrix4x4.identity;
			screenMatrix.m00 = pixelRect.width / 2f;
			screenMatrix.m03 = pixelRect.x + pixelRect.width / 2f;
			screenMatrix.m11 = pixelRect.height / 2f;
			screenMatrix.m13 = pixelRect.y + pixelRect.height / 2f;
			return screenMatrix * (mainCamera.projectionMatrix * mainCamera.worldToCameraMatrix);
		}
	}
}