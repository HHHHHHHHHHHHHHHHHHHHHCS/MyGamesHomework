using Unity.Mathematics;
using UnityEngine;

namespace Games_101_1
{
	public class Main_101_1 : MonoBehaviour
	{
		private Mesh mesh;

		private void Awake()
		{
			//camera View
			var camPos = Camera.main.transform.position;
			var camRot = Camera.main.transform.rotation;


			var viewMatrix = GetViewMatrix(camPos, camRot);
			// Camera.main.worldToCameraMatrix =
			// 	GetViewMatrix(camPos, camRot);


			//camera projection
			var camFov = Camera.main.fieldOfView;
			var camAspect = Camera.main.aspect;
			var camZNear = Camera.main.nearClipPlane;
			var camZFar = Camera.main.farClipPlane;

			var projectionMatrix = GetProjectionMatrix(camFov, camAspect, camZNear, camZFar);
			// Camera.main.projectionMatrix =
			// 	GetProjectionMatrix(camFov, camAspect, camZNear, camZFar);


			//model
			float3 p0 = new float3(2.0f, 0.0f, -2.0f);
			float3 p1 = new float3(0.0f, 2.0f, -2.0f);
			float3 p2 = new float3(-2.0f, 0.0f, -2.0f);


			Mesh mesh = new Mesh();
			mesh.SetVertices(new Vector3[] {p0, p1, p2});
			mesh.SetTriangles(new int[] {0, 2, 1}, 0);

			GetComponent<MeshFilter>().mesh = mesh;

			transform.eulerAngles = new Vector3(0, 0, 45);

			var modelMatrix = GetTRSMatrix(transform.position, transform.eulerAngles, transform.lossyScale);

			float4x4 mvp = math.mul(projectionMatrix, math.mul(viewMatrix, modelMatrix));

			float4x4 uMVP = Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix *
			                transform.localToWorldMatrix;

			float4 p0w = math.mul(mvp, new float4(p0, 1.0f));
			float4 p1w = math.mul(mvp, new float4(p1, 1.0f));
			float4 p2w = math.mul(mvp, new float4(p2, 1.0f));

			float4 uP0w = math.mul(uMVP, new float4(p0, 1.0f));
			float4 uP1w = math.mul(uMVP, new float4(p1, 1.0f));
			float4 uP2w = math.mul(uMVP, new float4(p2, 1.0f));

			//这里除以齐次坐标了    正常在vertex阶段是没有除以其次坐标的
			Debug.Log(p0w.xyzw / p0w.w);
			Debug.Log(uP0w.xyzw / uP0w.w);
			Debug.Log(p1w.xyzw / p1w.w);
			Debug.Log(uP1w.xyzw / uP1w.w);
			Debug.Log(p2w.xyzw / p2w.w);
			Debug.Log(uP2w.xyzw / uP2w.w);
		}

		private void OnDisable()
		{
			Destroy(mesh);
			mesh = null;
		}

		private float4x4 GetTransformMatrix(float3 pos)
		{
			return new float4x4(
				1, 0, 0, pos.x,
				0, 1, 0, pos.y,
				0, 0, 1, pos.z,
				0, 0, 0, 1
			);
		}

		private float4x4 GetRotateXMatrix(float zAngle)
		{
			float val = math.radians(zAngle);
			float c = math.cos(val);
			float s = math.sin(val);
			return new float4x4(
				1, 0, 0, 0,
				0, c, -s, 0,
				0, s, c, 0,
				0, 0, 0, 1
			);
		}


		private float4x4 GetRotateYMatrix(float zAngle)
		{
			float val = math.radians(zAngle);
			float c = math.cos(val);
			float s = math.sin(val);
			return new float4x4(
				c, 0, s, 0,
				0, 1, 0, 0,
				-s, 0, c, 0,
				0, 0, 0, 1
			);
		}

		private float4x4 GetRotateZMatrix(float zAngle)
		{
			float val = math.radians(zAngle);
			float c = math.cos(val);
			float s = math.sin(val);
			return new float4x4(
				c, -s, 0, 0,
				s, c, 0, 0,
				0, 0, 1, 0,
				0, 0, 0, 1
			);
		}

		public static float4x4 GetRotateDirMatrix(float3 axis, float angle)
		{
			float sina, cosa;
			math.sincos(angle, out sina, out cosa);

			float4 u = new float4(axis, 0.0f);
			// float4 u_yzx = u.yzxx;
			// float4 u_zxy = u.zxyx;
			float4 u_inv_cosa = u - u * cosa; // u * (1.0f - cosa);
			float4 t = new float4(u.xyz * sina, cosa);

			uint4 ppnp = new uint4(0x00000000, 0x00000000, 0x80000000, 0x00000000);
			uint4 nppp = new uint4(0x80000000, 0x00000000, 0x00000000, 0x00000000);
			uint4 pnpp = new uint4(0x00000000, 0x80000000, 0x00000000, 0x00000000);
			uint4 mask = new uint4(0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0x00000000);

			return new float4x4(
				u.x * u_inv_cosa + math.asfloat((math.asuint(t.wzyx) ^ ppnp) & mask),
				u.y * u_inv_cosa + math.asfloat((math.asuint(t.zwxx) ^ nppp) & mask),
				u.z * u_inv_cosa + math.asfloat((math.asuint(t.yxwx) ^ pnpp) & mask),
				new float4(0.0f, 0.0f, 0.0f, 1.0f)
			);
		}

		private float4x4 GetScaleMatrix(float3 scale)
		{
			return new float4x4(
				scale.x, 0, 0, 0,
				0, scale.y, 0, 0,
				0, 0, scale.z, 0,
				0, 0, 0, 1
			);
		}

		private float4x4 GetTRSMatrix(float3 pos, float3 rot, float3 size)
		{
			float4x4 mat = float4x4.identity;
			mat = math.mul(GetScaleMatrix(size), mat);
			mat = math.mul(GetRotateYMatrix(rot.y), mat);
			mat = math.mul(GetRotateXMatrix(rot.x), mat);
			mat = math.mul(GetRotateZMatrix(rot.z), mat);
			mat = math.mul(GetTransformMatrix(pos), mat);
			return mat;
		}


		private float4x4 GetViewMatrix(float3 pos, float3 rot)
		{
			var viewMatrix = float4x4.identity;
			viewMatrix = math.mul(viewMatrix, GetTransformMatrix(pos));
			viewMatrix = math.mul(viewMatrix, GetRotateYMatrix(rot.y));
			viewMatrix = math.mul(viewMatrix, GetRotateXMatrix(rot.x));
			viewMatrix = math.mul(viewMatrix, GetRotateZMatrix(rot.z));

			viewMatrix = math.inverse(viewMatrix);

			//unity 左右手坐标系相反
			if(SystemInfo.usesReversedZBuffer)
			{
				viewMatrix[0].z = -viewMatrix[0].z;
				viewMatrix[1].z = -viewMatrix[1].z;
				viewMatrix[2].z = -viewMatrix[2].z;
				viewMatrix[3].z = -viewMatrix[3].z;
			}

			return viewMatrix;
		}

		private float4x4 GetViewMatrix(float3 pos, quaternion rot)
		{
			var viewMatrix = float4x4.TRS(pos, rot, new float3(1, 1, 1));

			viewMatrix = math.inverse(viewMatrix);

			//unity 左右手坐标系相反
			if(SystemInfo.usesReversedZBuffer)
			{
				viewMatrix[0].z = -viewMatrix[0].z;
				viewMatrix[1].z = -viewMatrix[1].z;
				viewMatrix[2].z = -viewMatrix[2].z;
				viewMatrix[3].z = -viewMatrix[3].z;
			}
			


			return viewMatrix;
		}

		private float4x4 GetProjectionMatrix(float fov, float aspect
			, float zNear, float zFar)
		{
			float cotangent = 1.0f / math.tan(math.radians(fov) * 0.5f);
			float rcpdz = 1.0f / (zNear - zFar);

			return new float4x4(
				cotangent / aspect, 0.0f, 0.0f, 0.0f,
				0.0f, cotangent, 0.0f, 0.0f,
				0.0f, 0.0f, (zFar + zNear) * rcpdz, 2.0f * zNear * zFar * rcpdz,
				0.0f, 0.0f, -1.0f, 0.0f
			);
		}
	}
}
