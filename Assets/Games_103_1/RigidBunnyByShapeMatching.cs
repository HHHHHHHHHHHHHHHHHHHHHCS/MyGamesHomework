using System;
using UnityEngine;

namespace Games_103_1
{
	public class RigidBunnyByShapeMatching : MonoBehaviour
	{
		private readonly Vector3 G = new Vector3(0.0f, -9.8f, 0.0f);

		public bool launched;

		private Vector3[] X; //world position
		private Vector3[] oriX; //ori world position
		private Vector3[] Y; //temp world position
		private Vector3[] Q; //local coordinates
		private Vector3[] V; //speed

		private Matrix4x4 QQt = Matrix4x4.zero;
		private float linear_decay = 0.999f;
		private float mu_T = 0.5f; //u_T maybe  coefficient of air resistance
		private float mu_N = 5.0f; //u_N maybe coefficient of restitution
		private float timer = 0.0f;

		private Vector3 ground = new Vector3(0f, 0f, 0f);
		private Vector3 groundNormal = new Vector3(0f, 1f, 0f);
		private Vector3 wall = new Vector3(0, 4, -5);
		private Vector3 wallNormal = new Vector3(0f, 0f, 1f);


		private void Start()
		{
			Mesh mesh = GetComponent<MeshFilter>().mesh;
			V = new Vector3[mesh.vertexCount];
			X = mesh.vertices;
			Y = mesh.vertices;
			Q = mesh.vertices;

			//Center Q
			Vector3 c = Vector3.zero;
			foreach (var qp in Q)
			{
				c += qp;
			}

			c /= Q.Length;

			for (int i = 0; i < Q.Length; i++)
			{
				Q[i] -= c;
			}


			//get QQ^t ready
			for (int i = 0; i < Q.Length; i++)
			{
				QQt[0, 0] += Sq(Q[i].x);
				QQt[0, 1] += Sq(Q[i].y);
				QQt[0, 2] += Sq(Q[i].z);
				QQt[1, 0] += Sq(Q[i].x);
				QQt[1, 1] += Sq(Q[i].y);
				QQt[1, 2] += Sq(Q[i].z);
				QQt[2, 0] += Sq(Q[i].x);
				QQt[2, 1] += Sq(Q[i].y);
				QQt[2, 2] += Sq(Q[i].z);
			}

			QQt[3, 3] = 1;

			UpdateMesh(transform.position, Matrix4x4.Rotate(transform.rotation), 0.0f);

			oriX = X;

			transform.position = Vector3.zero;
			transform.rotation = Quaternion.identity;
		}

		private void Update()
		{
			if (Input.GetKey(KeyCode.R))
			{
				X = oriX;
				UpdateMesh(Vector3.zero, Matrix4x4.Rotate(Quaternion.identity), 0.0f);
				launched = false;
			}

			if (Input.GetKey(KeyCode.L))
			{
				for (int i = 0; i < V.Length; i++)
				{
					V[i] = new Vector3(0, 2, -5);
				}

				launched = true;
			}
		}

		private void FixedUpdate()
		{
			if (!launched)
			{
				return;
			}

			UpdateForce(Time.fixedDeltaTime);
		}

		private void UpdateMesh(Vector3 c, Matrix4x4 R, float inv_dt)
		{
			for (int i = 0; i < Q.Length; i++)
			{
				Vector3 x = (Vector3)(R * new Vector4(Q[i].x, Q[i].y, Q[i].z, 0)) + c;

				V[i] = (x - X[i]) * inv_dt;
				X[i] = x;
			}

			GetComponent<MeshFilter>().mesh.vertices = X;
		}

		private void UpdateForce(float dt)
		{
		}

		#region Helper Func

		public static float Sq(float t)
		{
			return t * t;
		}

		#endregion
	}
}