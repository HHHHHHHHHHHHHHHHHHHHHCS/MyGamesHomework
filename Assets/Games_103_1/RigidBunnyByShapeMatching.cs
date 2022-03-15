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
			oriX = X;
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
				QQt[0, 0] += Q[i].x * Q[i].x;
				QQt[0, 1] += Q[i].x * Q[i].y;
				QQt[0, 2] += Q[i].x * Q[i].z;
				QQt[1, 0] += Q[i].y * Q[i].x;
				QQt[1, 1] += Q[i].y * Q[i].y;
				QQt[1, 2] += Q[i].y * Q[i].z;
				QQt[2, 0] += Q[i].z * Q[i].x;
				QQt[2, 1] += Q[i].z * Q[i].y;
				QQt[2, 2] += Q[i].z * Q[i].z;
			}

			QQt[3, 3] = 1;

			UpdateMesh(transform.position, Matrix4x4.Rotate(transform.rotation), 0.0f);


			transform.position = Vector3.zero;
			transform.rotation = Quaternion.identity;
		}

		private void Update()
		{
			if (Input.GetKey(KeyCode.R))
			{
				X = oriX;

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

				UpdateMesh(new Vector3(0, 1.5f, -2f), Matrix4x4.Rotate(Quaternion.Euler(80.0f, 0f, 0f)), 0.0f);
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
			// GetComponent<MeshFilter>().mesh.RecalculateNormals();
		}

		private void UpdateForce(float dt)
		{
			//1. run a simple particle system
			for (int i = 0; i < V.Length; i++)
			{
				V[i] += G * dt;
				V[i] *= linear_decay;
			}

			//2. perform simple particle collision
			DoCollision(dt == 0 ? 0 : 1 / dt);

			//3. Use shape matching to get new translation c and 
			// new rotation R. Update the mesh by c and R.
			//Shape Matching (translation)
			for (int i = 0; i < V.Length; i++)
			{
				Y[i] = X[i] + V[i] * dt;
			}

			//calc c
			Vector3 c = Vector3.zero;
			for (int i = 0; i < Y.Length; i++)
			{
				c += Y[i];
			}

			c = c / Y.Length;

			//calc A
			Matrix4x4 A = Matrix4x4.zero;
			for (int i = 0; i < Y.Length; i++)
			{
				Matrix4x4 o = Vector3x1DotVector1x3(Y[i] - c, Q[i]);
				A[0, 0] += o[0, 0];
				A[0, 1] += o[0, 1];
				A[0, 2] += o[0, 2];
				A[1, 0] += o[1, 0];
				A[1, 1] += o[1, 1];
				A[1, 2] += o[1, 2];
				A[2, 0] += o[2, 0];
				A[2, 1] += o[2, 1];
				A[2, 2] += o[2, 2];
			}

			A[3, 3] = 1.0f;

			A = A * QQt.inverse;

			//Shape Matching (rotation)
			// calc R
			Matrix4x4 R = Matrix4x4.zero;
			R = GetRotation(A);

			UpdateMesh(c, R, dt == 0 ? 0 : 1.0f / dt);
		}

		private void DoCollision(float invDt)
		{
			for (int i = 0; i < X.Length; i++)
			{
				if (Vector3.Dot(X[i] - ground, groundNormal) < 0 &&
				    Vector3.Dot(V[i], groundNormal) < 0) // collision with ground
				{
					Vector3 VN = Vector3.Dot(V[i], groundNormal) * groundNormal;
					Vector3 VT = V[i] - VN;
					float a = Mathf.Max(0, 1.0f - mu_T * (1.0f + mu_N)) * Vector3.Magnitude(VN) / Vector3.Magnitude(VT);
					V[i] = -1.0f * mu_N * VN + 2.0f * a * VT;
				}
				else if (Vector3.Dot(X[i] - wall, wallNormal) < 0 &&
				         Vector3.Dot(V[i], wallNormal) < 0) // collision with wall
				{
					Vector3 VN = Vector3.Dot(V[i], wallNormal) * wallNormal;
					Vector3 VT = V[i] - VN;
					float a = Mathf.Max(0, 1.0f - mu_T * (1.0f + mu_N)) * Vector3.Magnitude(VN) / Vector3.Magnitude(VT);
					V[i] = -1.0f * mu_N * VN + 2.0f * a * VT;
				}
			}
		}

		// Polar Decomposition that returns the rotation from F.
		Matrix4x4 GetRotation(Matrix4x4 F)
		{
			Matrix4x4 C = Matrix4x4.zero;
			for (int ii = 0; ii < 3; ii++)
			for (int jj = 0; jj < 3; jj++)
			for (int kk = 0; kk < 3; kk++)
				C[ii, jj] += F[kk, ii] * F[kk, jj];

			Matrix4x4 C2 = Matrix4x4.zero;
			for (int ii = 0; ii < 3; ii++)
			for (int jj = 0; jj < 3; jj++)
			for (int kk = 0; kk < 3; kk++)
				C2[ii, jj] += C[ii, kk] * C[jj, kk];

			float det = F[0, 0] * F[1, 1] * F[2, 2] +
			            F[0, 1] * F[1, 2] * F[2, 0] +
			            F[1, 0] * F[2, 1] * F[0, 2] -
			            F[0, 2] * F[1, 1] * F[2, 0] -
			            F[0, 1] * F[1, 0] * F[2, 2] -
			            F[0, 0] * F[1, 2] * F[2, 1];

			float I_c = C[0, 0] + C[1, 1] + C[2, 2];
			float I_c2 = I_c * I_c;
			float II_c = 0.5f * (I_c2 - C2[0, 0] - C2[1, 1] - C2[2, 2]);
			float III_c = det * det;
			float k = I_c2 - 3 * II_c;

			Matrix4x4 inv_U = Matrix4x4.zero;
			if (k < 1e-10f)
			{
				float inv_lambda = 1 / Mathf.Sqrt(I_c / 3);
				inv_U[0, 0] = inv_lambda;
				inv_U[1, 1] = inv_lambda;
				inv_U[2, 2] = inv_lambda;
			}
			else
			{
				float l = I_c * (I_c * I_c - 4.5f * II_c) + 13.5f * III_c;
				float k_root = Mathf.Sqrt(k);
				float value = l / (k * k_root);
				if (value < -1.0f) value = -1.0f;
				if (value > 1.0f) value = 1.0f;
				float phi = Mathf.Acos(value);
				float lambda2 = (I_c + 2 * k_root * Mathf.Cos(phi / 3)) / 3.0f;
				float lambda = Mathf.Sqrt(lambda2);

				float III_u = Mathf.Sqrt(III_c);
				if (det < 0) III_u = -III_u;
				float I_u = lambda + Mathf.Sqrt(-lambda2 + I_c + 2 * III_u / lambda);
				float II_u = (I_u * I_u - I_c) * 0.5f;


				float inv_rate, factor;
				inv_rate = 1 / (I_u * II_u - III_u);
				factor = I_u * III_u * inv_rate;

				Matrix4x4 U = Matrix4x4.zero;
				U[0, 0] = factor;
				U[1, 1] = factor;
				U[2, 2] = factor;

				factor = (I_u * I_u - II_u) * inv_rate;
				for (int i = 0; i < 3; i++)
				for (int j = 0; j < 3; j++)
					U[i, j] += factor * C[i, j] - inv_rate * C2[i, j];

				inv_rate = 1 / III_u;
				factor = II_u * inv_rate;
				inv_U[0, 0] = factor;
				inv_U[1, 1] = factor;
				inv_U[2, 2] = factor;

				factor = -I_u * inv_rate;
				for (int i = 0; i < 3; i++)
				for (int j = 0; j < 3; j++)
					inv_U[i, j] += factor * U[i, j] + inv_rate * C[i, j];
			}

			Matrix4x4 R = Matrix4x4.zero;
			for (int ii = 0; ii < 3; ii++)
			for (int jj = 0; jj < 3; jj++)
			for (int kk = 0; kk < 3; kk++)
				R[ii, jj] += F[ii, kk] * inv_U[kk, jj];
			R[3, 3] = 1;
			return R;
		}

		#region Helper Func

		public static Matrix4x4 Vector3x1DotVector1x3(Vector3 A, Vector3 B)
		{
			Matrix4x4 rlt = Matrix4x4.zero;

			rlt[0, 0] = A.x * B.x;
			rlt[0, 1] = A.x * B.y;
			rlt[0, 2] = A.x * B.z;

			rlt[1, 0] = A.y * B.x;
			rlt[1, 1] = A.y * B.y;
			rlt[1, 2] = A.y * B.z;

			rlt[2, 0] = A.z * B.x;
			rlt[2, 1] = A.z * B.y;
			rlt[2, 2] = A.z * B.z;

			rlt[3, 3] = 1.0f;

			return rlt;
		}

		#endregion
	}
}