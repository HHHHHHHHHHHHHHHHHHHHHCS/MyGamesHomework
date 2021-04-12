using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Games_101_3
{
	public class Main_101_3 : MonoBehaviour
	{
		private const int blitScreenKernel = 0;

		public Mesh mesh;

		public ComputeShader blitScreenCS;

		private Camera mainCamera;
		private int width, height, pixelsCount;

		private NativeArray<float4> colorRT;
		private NativeArray<float> depthRT;
		private NativeArray<float4> pixelsPoses;

		private NativeArray<int3> indices;
		private NativeArray<Vector3> a2v_pos;
		private NativeArray<Vector3> a2v_normal;
		private NativeArray<Vector4> a2v_tangent;
		private NativeArray<Vector2> a2v_uv0;


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


			using (Mesh.MeshDataArray mda = Mesh.AcquireReadOnlyMeshData(mesh))
			{
				Mesh.MeshData md = mda[0];

				int[] _indices = mesh.GetIndices(0);

				indices = new NativeArray<int3>(_indices.Length / 3, Allocator.Persistent);

				for (int i = 0; i < indices.Length; i++)
				{
					indices[i] = new int3(_indices[3 * i + 0], _indices[3 * i + 1], _indices[3 * i + 2]);
				}

				pixelsPoses = new NativeArray<float4>(_indices.Length, Allocator.Persistent);

				int len = md.vertexCount;

				a2v_pos = new NativeArray<Vector3>(len, Allocator.Persistent);
				md.GetVertices(a2v_pos);

				a2v_normal = new NativeArray<Vector3>(len, Allocator.Persistent);
				md.GetNormals(a2v_normal);

				a2v_tangent = new NativeArray<Vector4>(len, Allocator.Persistent);
				md.GetTangents(a2v_tangent);

				a2v_uv0 = new NativeArray<Vector2>(len, Allocator.Persistent);
				md.GetUVs(0, a2v_uv0);
			}

			clearColorDepthJob = new ClearColorDepthJob()
			{
				colorRT = colorRT,
				depthRT = depthRT,
			};

			calcPosJob = new CalcPosJob()
			{
				pixelsPoses = pixelsPoses,
				indicesArray = indices,
				a2v_pos = a2v_pos
			};

			calcAttributeJob = new CalcAttributeJob()
			{
				colorRT = colorRT,
				depthRT = depthRT,
				pixelsPoses = pixelsPoses,
				a2v_normal = a2v_normal,
				a2v_tangent = a2v_tangent,
				a2v_uv0 = a2v_uv0,
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
		private void Update()
		{
			float4x4 mvps = GetMVPS();
			calcPosJob.mvps = mvps;

			JobHandle jobHandle = new JobHandle();
			jobHandle = clearColorDepthJob.Schedule(pixelsCount, jobHandle);
			jobHandle = calcPosJob.Schedule(indices.Length, jobHandle);
			jobHandle = calcAttributeJob.Schedule(indices.Length, jobHandle);
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
			inputCB.Dispose();
			colorRT.Dispose();
			depthRT.Dispose();
			pixelsPoses.Dispose();

			indices.Dispose();
			a2v_pos.Dispose();
			a2v_normal.Dispose();
			a2v_tangent.Dispose();
			a2v_uv0.Dispose();
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
			[WriteOnly, NativeDisableParallelForRestriction]
			public NativeArray<float4> pixelsPoses;

			[ReadOnly] public NativeArray<int3> indicesArray;

			[ReadOnly, NativeDisableParallelForRestriction]
			public NativeArray<Vector3> a2v_pos;

			[ReadOnly] public float4x4 mvps;


			public void Execute(int index)
			{
				int3 indices = indicesArray[index];

				float4 p0 = new float4(a2v_pos[indices.x], 1.0f);
				float4 p1 = new float4(a2v_pos[indices.y], 1.0f);
				float4 p2 = new float4(a2v_pos[indices.z], 1.0f);

				p0 = math.mul(mvps, p0);
				p1 = math.mul(mvps, p1);
				p2 = math.mul(mvps, p2);

				p0.xyz = p0.xyz / p0.w;
				p1.xyz = p1.xyz / p1.w;
				p2.xyz = p2.xyz / p2.w;

				pixelsPoses[indices.x] = p0;
				pixelsPoses[indices.y] = p1;
				pixelsPoses[indices.z] = p2;
			}
		}

		[BurstCompile]
		private static bool PointInTriangle(float2 v0, float2 v1, float2 v2, out float3 uvw)
		{
			uvw = float3.zero;

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
				return false;
			}

			float v = (d00 * d12 - d01 * d02) * invD;

			if (v < 0 || v > 1)
			{
				return false;
			}

			float w = 1 - u - v;

			if (w < 0 || w > 1)
			{
				return false;
			}

			uvw = new float3(u, v, w);
			return true;
		}


		//todo:indices

		[BurstCompile]
		private struct CalcAttributeJob : IJobFor
		{
			[WriteOnly, NativeDisableParallelForRestriction]
			public NativeArray<float4> colorRT;

			[NativeDisableParallelForRestriction] public NativeArray<float> depthRT;

			[ReadOnly, NativeDisableParallelForRestriction]
			public NativeArray<float4> pixelsPoses;

			[ReadOnly, NativeDisableParallelForRestriction]
			public NativeArray<Vector3> a2v_normal;

			[ReadOnly, NativeDisableParallelForRestriction]
			public NativeArray<Vector4> a2v_tangent;

			[ReadOnly, NativeDisableParallelForRestriction]
			public NativeArray<Vector2> a2v_uv0;


			[ReadOnly] public int width, height;

			public void Execute(int index)
			{
				float4 pos0 = pixelsPoses[3 * index + 0];
				float4 pos1 = pixelsPoses[3 * index + 1];
				float4 pos2 = pixelsPoses[3 * index + 2];

				float3 normal0 = a2v_normal[3 * index + 0];
				float3 normal1 = a2v_normal[3 * index + 1];
				float3 normal2 = a2v_normal[3 * index + 2];

				// float4 tangent0 = a2v_tangent[3 * index + 0];
				// float4 tangent1 = a2v_tangent[3 * index + 1];
				// float4 tangent2 = a2v_tangent[3 * index + 2];
				//
				// float2 uv0 = a2v_uv0[3 * index + 0];
				// float2 uv1 = a2v_uv0[3 * index + 1];
				// float2 uv2 = a2v_uv0[3 * index + 2];
				//
				// float2 sp = pos0.xy; //start pos
				//
				// float2 max = math.max(pos2.xy, math.max(pos1.xy, sp));
				// float2 min = math.min(pos2.xy, math.min(pos1.xy, sp));
				//
				//
				// float2 v0 = pos1.xy - sp;
				// float2 v1 = pos2.xy - sp;
				//
				//
				// for (int x = (int) math.floor(min.x); x < (int) math.ceil(max.x); x++)
				// {
				// 	for (int y = (int) math.floor(min.y); y < (int) math.ceil(max.y); y++)
				// 	{
				// 		// y*width+width
				// 		if (x < 0 || x >= width || y < 0 || y >= height)
				// 		{
				// 			continue;
				// 		}
				//
				//
				// 		float2 p3 = new float2(x + 0.5f, y + 0.5f);
				//
				// 		float2 v2 = p3 - sp;
				//
				// 		if (!PointInTriangle(v0, v1, v2, out var uvw))
				// 		{
				// 			continue;
				// 		}
				//
				// 		int indexPos = y * width + x;
				//
				// 		float depth = depthRT[indexPos];
				//
				// 		float newDepth = (pos0 * uvw.z + pos1 * uvw.x + pos2 * uvw.y).z;
				//
				// 		if (newDepth <= depth)
				// 		{
				// 			depthRT[indexPos] = newDepth;
				//
				// 			float3 normal = normal0 * uvw.z + normal1 * uvw.x + normal2 * uvw.y;
				// 			float4 tangent = tangent0 * uvw.z + tangent1 * uvw.x + tangent2 * uvw.y;
				// 			float2 uv = uv0 * uvw.z + uv1 * uvw.x + uv2 * uvw.y;
				//
				// 			colorRT[indexPos] = new float4(uv, 0, 1);
				// 		}
				// 	}
				// }
			}
		}
	}
}