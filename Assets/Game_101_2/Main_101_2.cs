using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;

namespace Game_101_2
{
	public class Main_101_2 : MonoBehaviour
	{
		private static readonly int3[] indexes =
		{
			new int3(0, 1, 2),
			new int3(3, 4, 5),
		};

		private static readonly float3[] poses =
		{
			new float3(2, 0, -2),
			new float3(0, 2, -2),
			new float3(-2, 0, -2),
			new float3(3.5f, -1, -5),
			new float3(2.5f, 1.5f, -5),
			new float3(-1, 0.5f, -5),
		};


		private static readonly float3[] cols =
		{
			new float3(0 / 255f, 238 / 255f, 185 / 255f),
			new float3(217 / 255f, 0 / 255f, 185 / 255f),
			new float3(217 / 255f, 238 / 255f, 0 / 255f),
			new float3(185 / 255f, 217 / 255f, 0 / 255f),
			new float3(185 / 255f, 0 / 255f, 238 / 255f),
			new float3(0 / 255f, 217 / 255f, 238 / 255f),
		};

		private Camera mainCamera;
		private int width, height, pixelsCount;

		private NativeArray<float> colorRT, depthRT;

		private ClearColorDepthJob clearColorDepthJob;
		private DrawTrianglesJob drawTrianglesJob;

		private void Awake()
		{
			mainCamera = Camera.main;
			width = mainCamera.pixelWidth;
			height = mainCamera.pixelHeight;

			pixelsCount = width * height;

			colorRT = new NativeArray<float>(pixelsCount, Allocator.Persistent);
			depthRT = new NativeArray<float>(pixelsCount, Allocator.Persistent);

			clearColorDepthJob = new ClearColorDepthJob()
			{
				colorRT = colorRT,
				depthRT = depthRT,
			};


			drawTrianglesJob = new DrawTrianglesJob()
			{
				colorRT = colorRT,
				depthRT = depthRT,
			};
		}

		private void OnDestroy()
		{
			colorRT.Dispose();
			depthRT.Dispose();
		}

		private void Update()
		{
			float4x4 mvps = GetMVPS();
			drawTrianglesJob.mvps = mvps;


			JobHandle jobHandle = new JobHandle();

			jobHandle = clearColorDepthJob.Schedule(pixelsCount, jobHandle);


			jobHandle = drawTrianglesJob.Schedule(indexes.Length, jobHandle);

			jobHandle.Complete();

		}


		private float4x4 GetMVPS()
		{
			float4x4 modelMatrix = float4x4.identity;
			float4x4 viewMatrix = Camera.main.worldToCameraMatrix;
			float4x4 projectionMatrix = Camera.main.projectionMatrix;
			var pixelRect = mainCamera.pixelRect;
			float4x4 screenMatrix = float4x4.identity;
			screenMatrix.c0.x = pixelRect.width / 2f;
			screenMatrix.c3.x = pixelRect.x + pixelRect.width / 2f;
			screenMatrix.c1.y = pixelRect.height / 2f;
			screenMatrix.c3.y = pixelRect.y + pixelRect.height / 2f;

			return math.mul(screenMatrix, math.mul(projectionMatrix, math.mul(viewMatrix, modelMatrix)));
		}

		[BurstCompile]
		private struct ClearColorDepthJob : IJobFor
		{
			public NativeArray<float> colorRT, depthRT;


			public void Execute(int index)
			{
				colorRT[index] = 0;
				depthRT[index] = 1;
				
			}
		}


		[BurstCompile]
		private struct DrawTrianglesJob : IJobFor
		{
			public NativeArray<float> colorRT, depthRT;

			[ReadOnly] public float4x4 mvps;

			public float4 pos0, pos1, pos2;

			public void Execute(int index)
			{
				int i0 = indexes[index].x;
				int i1 = indexes[index].y;
				int i2 = indexes[index].z;

				float4 p0 = new float4(poses[i0], 1.0f);
				float4 p1 = new float4(poses[i1], 1.0f);
				float4 p2 = new float4(poses[i2], 1.0f);

				//需要native传入

				pos0 = p0; //math.mul(mvps, p0);
				pos1 = p1; //math.mul(mvps, p1);
				pos2 = p2; //math.mul(mvps, p2);
			}
		}
	}
}