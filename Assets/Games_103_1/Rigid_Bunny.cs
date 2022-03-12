using UnityEngine;

namespace Games_103_1
{
	public class Rigid_Bunny : MonoBehaviour
	{
		private readonly Vector3 G = new Vector3(0.0f, -9.8f, 0.0f);

		public bool launched = false;

		public Vector3 v = new Vector3(0, 0, 0); //velocity
		public Vector3 w = new Vector3(0, 0, 0); //angular vlocity

		public float mass; //mass
		public Matrix4x4 I_ref;

		public float linear_decay = 0.999f; // for velocity decay
		public float angular_decay = 0.98f;
		public float restitution = 0.5f; // for collision

		public float mu_T = 0.5f; // maybe coefficient of air resistance

		private Vector3[] vertices;
		private Vector3 resetPos;
		private Quaternion resetQua;

		private void Start()
		{
			resetPos = transform.position;
			resetQua = transform.rotation;

			Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
			vertices = mesh.vertices;

			float m = 1;
			mass = 0;
			foreach (var localPos in vertices)
			{
				mass += m;
				float diag = m * localPos.sqrMagnitude;
				I_ref[0, 0] += diag;
				I_ref[1, 1] += diag;
				I_ref[2, 2] += diag;
				I_ref[0, 0] -= m * localPos.x * localPos.x;
				I_ref[0, 1] -= m * localPos.x * localPos.y;
				I_ref[0, 2] -= m * localPos.x * localPos.z;
				I_ref[1, 0] -= m * localPos.y * localPos.x;
				I_ref[1, 1] -= m * localPos.y * localPos.y;
				I_ref[1, 2] -= m * localPos.y * localPos.z;
				I_ref[2, 0] -= m * localPos.z * localPos.x;
				I_ref[2, 1] -= m * localPos.z * localPos.y;
				I_ref[2, 2] -= m * localPos.z * localPos.z;
			}

			I_ref[3, 3] = 1;
		}

		private void Update()
		{
			if (Input.GetKey(KeyCode.R))
			{
				transform.position = resetPos;
				restitution = 0.5f;
				launched = false;
			}

			if (Input.GetKey(KeyCode.L))
			{
				v = new Vector3(0, 2, -5);
				launched = true;
			}
		}

		private void FixedUpdate()
		{
			UpdateForce(Time.fixedDeltaTime);
		}

		private void UpdateForce(float dt)
		{
			if (!launched)
			{
				return;
			}

			//1. update force
			v += dt * G;
			v *= linear_decay;
			w *= angular_decay;

			//2. collision impulse
			Collision_Impulse(new Vector3(0, 0, 0), new Vector3(0, 1, 0));
			Collision_Impulse(new Vector3(0, 4, 5), new Vector3(0, 0, 1));


			//3. update position & orientation

			//update lineear status
			Vector3 pos = transform.position;
			pos += dt * v;
			//update angular status
			Quaternion qua = transform.rotation;
			Vector3 dw = 0.5f * dt * w;
			Quaternion qua_rotate = new Quaternion(dw.x, dw.y, dw.z, 0.0f);
			qua = AddTwoQuaternion(qua, qua_rotate * qua);

			//4. update object status
			transform.position = pos;
			transform.rotation = qua;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pos">plane position</param>
		/// <param name="normal">plane normal</param>
		void Collision_Impulse(Vector3 pos, Vector3 normal)
		{
			int[] vid_collision = new int[vertices.Length];
			int num_collision = 0;
			
			
		}

		#region HelpFunc

		private static Quaternion AddTwoQuaternion(Quaternion q0, Quaternion q1)
		{
			Quaternion result = new Quaternion(q0.x + q1.x, q0.y + q1.y, q0.z + q1.z, q0.w + q1.w);
			return result;
		}

		private static Matrix4x4 GetCrossMatrix(Vector3 a)
		{
			//Get the cross product matrix of vector a
			Matrix4x4 matrix = Matrix4x4.zero;
			matrix[0, 0] = 0;
			matrix[0, 1] = -a.z;
			matrix[0, 2] = a.y;
			matrix[1, 0] = a.z;
			matrix[1, 1] = 0;
			matrix[1, 2] = -a.x;
			matrix[2, 0] = -a.y;
			matrix[2, 1] = a.x;
			matrix[2, 2] = 0;
			matrix[3, 3] = 1;
			return matrix;
		}

		private static Matrix4x4 MultiplyScalar(Matrix4x4 a, float b)
		{
			Matrix4x4 rlt = Matrix4x4.zero;
			for (int i = 0; i < 4; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					rlt[i, j] = a[i, j] * b;
				}
			}

			return rlt;
		}

		private static Matrix4x4 AddTwoMatrix(Matrix4x4 a, Matrix4x4 b)
		{
			Matrix4x4 rlt = Matrix4x4.zero;
			for (int i = 0; i < 4; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					rlt[i, j] = a[i, j] + b[i, j];
				}
			}

			return rlt;
		}

		private static Matrix4x4 MinusTwoMatrix(Matrix4x4 a, Matrix4x4 b)
		{
			return AddTwoMatrix(a, MultiplyScalar(b, -1));
		}

		#endregion
	}
}