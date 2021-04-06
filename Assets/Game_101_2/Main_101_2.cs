using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Game_101_2
{
	public class Main_101_2 : MonoBehaviour
	{
		private const int blitScreenKernel = 0;

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


		private static readonly float4[] cols =
		{
			new float4(0 / 255f, 238 / 255f, 185 / 255f, 0),
			new float4(217 / 255f, 0 / 255f, 185 / 255f, 0),
			new float4(217 / 255f, 238 / 255f, 0 / 255f, 0),
			new float4(185 / 255f, 217 / 255f, 0 / 255f, 0),
			new float4(185 / 255f, 0 / 255f, 238 / 255f, 0),
			new float4(0 / 255f, 217 / 255f, 238 / 255f, 0),
		};

		public ComputeShader blitScreenCS;

		private Camera mainCamera;
		private int width, height, pixelsCount;

		private NativeArray<float4> colorRT;
		private NativeArray<float> depthRT;

		private NativeArray<float4> pixelsPoses;

		private ClearColorDepthJob clearColorDepthJob;
		private CalcPosJob calcPosJob;
		private CalcAttributeJob calcAttributeJob;

		private int threadX, threadY;
		private ComputeBuffer inputCB;
		private RenderTexture screenRT;
		private CommandBuffer blitCB;

		private void Awake()
		{
			mainCamera = Camera.main;
			width = mainCamera.pixelWidth;
			height = mainCamera.pixelHeight;

			pixelsCount = width * height;

			colorRT = new NativeArray<float4>(pixelsCount, Allocator.Persistent);
			depthRT = new NativeArray<float>(pixelsCount, Allocator.Persistent);
			pixelsPoses = new NativeArray<float4>(indexes.Length * 3, Allocator.Persistent);

			clearColorDepthJob = new ClearColorDepthJob()
			{
				colorRT = colorRT,
				depthRT = depthRT,
			};

			calcPosJob = new CalcPosJob()
			{
				pixelsPoses = pixelsPoses,
			};

			calcAttributeJob = new CalcAttributeJob()
			{
				enableMSAA = false,
				colorRT = colorRT,
				depthRT = depthRT,
				pixelsPoses = pixelsPoses,
				width = width,
				height = height,
			};

			screenRT = new RenderTexture(width, height, 0, GraphicsFormat.R8G8B8A8_UNorm)
			{
				name = "MyScreenRT",
				enableRandomWrite = true,
			};
			inputCB = new ComputeBuffer(colorRT.Length, 4 * sizeof(float));

			blitScreenCS.SetInt("_Width", width);
			blitScreenCS.SetInt("_Height", height);
			blitScreenCS.SetInt("_PixelsCount", pixelsCount);

			threadX = (int) math.ceil(width / 2.0f);
			threadY = (int) math.ceil(height / 2.0f);

			blitScreenCS.SetBuffer(blitScreenKernel, "_Input", inputCB);
			blitScreenCS.SetTexture(blitScreenKernel, "_Result", screenRT);

			blitCB = new CommandBuffer()
			{
				name = "BlitScreen"
			};
			blitCB.Blit(screenRT, BuiltinRenderTextureType.CameraTarget);
		}

		//要活动的话  改成update
		private void Start()
		{
			float4x4 mvps = GetMVPS();
			calcPosJob.mvps = mvps;

			JobHandle jobHandle = new JobHandle();
			jobHandle = clearColorDepthJob.Schedule(pixelsCount, jobHandle);
			jobHandle = calcPosJob.Schedule(indexes.Length, jobHandle);
			jobHandle = calcAttributeJob.Schedule(indexes.Length, jobHandle);
			jobHandle.Complete();

			inputCB.SetData(colorRT);

			blitScreenCS.Dispatch(blitScreenKernel, threadX, threadY, 1);
		}

		private void OnEnable()
		{
			mainCamera.AddCommandBuffer(CameraEvent.AfterEverything, blitCB);
		}

		private void OnDisable()
		{
			mainCamera.RemoveCommandBuffer(CameraEvent.AfterEverything, blitCB);
		}

		private void OnDestroy()
		{
			colorRT.Dispose();
			depthRT.Dispose();
			pixelsPoses.Dispose();
		}

		/*
		private void OnPostRender()
		{
			//因为这个是 srcalpha  不是 one zero   所以不能用这个
			// GL.PushMatrix();
			// // https://answers.unity.com/questions/849712/how-do-i-call-graphicsdrawtexture-correctly.html
			// // GL.LoadOrtho();
			// GL.LoadPixelMatrix(0, width, height,0);
			// Graphics.DrawTexture(new Rect(0, 0, width, height), screenRT);
			// GL.PopMatrix();
		}
		*/

		private float4x4 GetMVPS()
		{
			float4x4 modelMatrix = float4x4.identity;
			float4x4 viewMatrix = mainCamera.worldToCameraMatrix;
			float4x4 projectionMatrix = mainCamera.projectionMatrix;
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
			public NativeArray<float4> colorRT;
			public NativeArray<float> depthRT;

			public void Execute(int index)
			{
				colorRT[index] = float4.zero;
				depthRT[index] = 1;
			}
		}

		//DrawTrianglesJob

		[BurstCompile]
		private struct CalcPosJob : IJobFor
		{
			//https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeDisableParallelForRestrictionAttribute.html
			[WriteOnly, NativeDisableParallelForRestrictionAttribute]
			public NativeArray<float4> pixelsPoses;

			[ReadOnly] public float4x4 mvps;


			public void Execute(int index)
			{
				int i0 = indexes[index].x;
				int i1 = indexes[index].y;
				int i2 = indexes[index].z;

				float4 p0 = new float4(poses[i0], 1.0f);
				float4 p1 = new float4(poses[i1], 1.0f);
				float4 p2 = new float4(poses[i2], 1.0f);

				p0 = math.mul(mvps, p0);
				p1 = math.mul(mvps, p1);
				p2 = math.mul(mvps, p2);

				p0.xyz = p0.xyz / p0.w;
				p1.xyz = p1.xyz / p1.w;
				p2.xyz = p2.xyz / p2.w;

				pixelsPoses[3 * index + 0] = p0;
				pixelsPoses[3 * index + 1] = p1;
				pixelsPoses[3 * index + 2] = p2;
			}
		}

		[BurstCompile]
		private struct CalcAttributeJob : IJobFor
		{
			public bool enableMSAA;

			[WriteOnly, NativeDisableParallelForRestrictionAttribute]
			public NativeArray<float4> colorRT;

			[NativeDisableParallelForRestrictionAttribute]
			public NativeArray<float> depthRT;

			[ReadOnly] public NativeArray<float4> pixelsPoses;


			[ReadOnly] public int width, height;

			public void Execute(int index)
			{
				float4 pos0 = pixelsPoses[3 * index + 0];
				float4 pos1 = pixelsPoses[3 * index + 1];
				float4 pos2 = pixelsPoses[3 * index + 2];


				float4 col0 = cols[3 * index + 0];
				float4 col1 = cols[3 * index + 1];
				float4 col2 = cols[3 * index + 2];

				float2 sp = pos0.xy; //start pos

				float2 max = math.max(pos2.xy, math.max(pos1.xy, sp));
				float2 min = math.min(pos2.xy, math.min(pos1.xy, sp));


				float2 v0 = pos1.xy - sp;
				float2 v1 = pos2.xy - sp;


				for (int x = (int) math.floor(min.x); x < (int) math.ceil(max.x); x++)
				{
					for (int y = (int) math.floor(min.y); y < (int) math.ceil(max.y); y++)
					{
						// y*width+width
						if (x < 0 || x >= width || y < 0 || y >= height)
						{
							continue;
						}

						//if(enableMSAA)

						float2 p3 = new float2(x + 0.5f, y + 0.5f);

						float2 v2 = p3 - sp;

						float d00 = math.dot(v0, v0);
						float d01 = math.dot(v0, v1);
						float d02 = math.dot(v0, v2);
						float d11 = math.dot(v1, v1);
						float d12 = math.dot(v1, v2);

						float invD = 1 / (d00 * d11 - d01 * d01);

						//https://www.cnblogs.com/graphics/archive/2010/08/05/1793393.html
						float u = (d11 * d02 - d01 * d12) * invD;

						if (u < 0 || u > 1)
						{
							continue;
						}

						float v = (d00 * d12 - d01 * d02) * invD;

						if (v < 0 || v > 1)
						{
							continue;
						}

						float w = 1 - u - v;

						if (w < 0 || w > 1)
						{
							continue;
						}

						int indexPos = y * width + x;

						float depth = depthRT[indexPos];

						float newDepth = (pos0 * w + pos1 * u + pos2 * v).z;

						if (newDepth <= depth)
						{
							colorRT[indexPos] = col0 * w + col1 * u + col2 * v;
							depthRT[indexPos] = newDepth;
						}
					}
				}
			}
		}
	}
}