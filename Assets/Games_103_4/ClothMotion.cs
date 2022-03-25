using UnityEngine;

namespace Games_103_4
{
	public class ClothMotion : MonoBehaviour
	{
		private float damping = 0.99f;
		private int[] E;
		private float[] L;
		private Vector3[] V;


		// Use this for initialization
		private void Start()
		{
			Mesh mesh = GetComponent<MeshFilter>().mesh;

			//Resize the mesh.
			int n = 21;
			Vector3[] X = new Vector3[n * n];
			Vector2[] UV = new Vector2[n * n];
			int[] T = new int[(n - 1) * (n - 1) * 6];
			for (int j = 0; j < n; j++)
			for (int i = 0; i < n; i++)
			{
				X[j * n + i] = new Vector3(5 - 10.0f * i / (n - 1), 0, 5 - 10.0f * j / (n - 1));
				UV[j * n + i] = new Vector3(i / (n - 1.0f), j / (n - 1.0f));
			}

			int t = 0;
			for (int j = 0; j < n - 1; j++)
			for (int i = 0; i < n - 1; i++)
			{
				T[t * 6 + 0] = j * n + i;
				T[t * 6 + 1] = j * n + i + 1;
				T[t * 6 + 2] = (j + 1) * n + i + 1;
				T[t * 6 + 3] = j * n + i;
				T[t * 6 + 4] = (j + 1) * n + i + 1;
				T[t * 6 + 5] = (j + 1) * n + i;
				t++;
			}

			mesh.vertices = X;
			mesh.triangles = T;
			mesh.uv = UV;
			mesh.RecalculateNormals();

			//Construct the original edge list
			int[] _E = new int[T.Length * 2];
			for (int i = 0; i < T.Length; i += 3)
			{
				_E[i * 2 + 0] = T[i + 0];
				_E[i * 2 + 1] = T[i + 1];
				_E[i * 2 + 2] = T[i + 1];
				_E[i * 2 + 3] = T[i + 2];
				_E[i * 2 + 4] = T[i + 2];
				_E[i * 2 + 5] = T[i + 0];
			}

			//Reorder the original edge list
			for (int i = 0; i < _E.Length; i += 2)
				if (_E[i] > _E[i + 1])
					Swap(ref _E[i], ref _E[i + 1]);
			//Sort the original edge list using quicksort
			QuickSort(ref _E, 0, _E.Length / 2 - 1);

			int e_number = 0;
			for (int i = 0; i < _E.Length; i += 2)
				if (i == 0 || _E[i + 0] != _E[i - 2] || _E[i + 1] != _E[i - 1])
					e_number++;

			E = new int[e_number * 2];
			for (int i = 0, e = 0; i < _E.Length; i += 2)
				if (i == 0 || _E[i + 0] != _E[i - 2] || _E[i + 1] != _E[i - 1])
				{
					E[e * 2 + 0] = _E[i + 0];
					E[e * 2 + 1] = _E[i + 1];
					e++;
				}

			L = new float[E.Length / 2];
			for (int e = 0; e < E.Length / 2; e++)
			{
				int i = E[e * 2 + 0];
				int j = E[e * 2 + 1];
				L[e] = (X[i] - X[j]).magnitude;
			}

			V = new Vector3[X.Length];
			for (int i = 0; i < X.Length; i++)
				V[i] = new Vector3(0, 0, 0);
		}

		// Update is called once per frame
		private void FixedUpdate()
		{
			float dt = Time.fixedDeltaTime;
			
			Mesh mesh = GetComponent<MeshFilter>().mesh;
			Vector3[] X = mesh.vertices;

			for (int i = 0; i < X.Length; i++)
			{
				if (i == 0 || i == 20) continue;
				V[i] += dt * new Vector3(0, -9.8f, 0);
				V[i] *= damping;
				X[i] += dt * V[i];
			}

			mesh.vertices = X;

			for (int l = 0; l < 32; l++)
			{
				StrainLimiting(dt);
			}
			

			CollisionHandling(dt);

			mesh.RecalculateNormals();
		}
		
		private static void QuickSort(ref int[] a, int l, int r)
		{
			int j;
			if (l < r)
			{
				j = QuickSortPartition(ref a, l, r);
				QuickSort(ref a, l, j - 1);
				QuickSort(ref a, j + 1, r);
			}
		}

		private static int QuickSortPartition(ref int[] a, int l, int r)
		{
			int pivot_0, pivot_1, i, j;
			pivot_0 = a[l * 2 + 0];
			pivot_1 = a[l * 2 + 1];
			i = l;
			j = r + 1;
			while (true)
			{
				do ++i;
				while (i <= r && (a[i * 2] < pivot_0 || a[i * 2] == pivot_0 && a[i * 2 + 1] <= pivot_1));
				do --j;
				while (a[j * 2] > pivot_0 || a[j * 2] == pivot_0 && a[j * 2 + 1] > pivot_1);
				if (i >= j) break;
				Swap(ref a[i * 2], ref a[j * 2]);
				Swap(ref a[i * 2 + 1], ref a[j * 2 + 1]);
			}

			Swap(ref a[l * 2 + 0], ref a[j * 2 + 0]);
			Swap(ref a[l * 2 + 1], ref a[j * 2 + 1]);
			return j;
		}

		private static void Swap(ref int a, ref int b)
		{
			(a, b) = (b, a);
		}

		void StrainLimiting(float dt)
		{
			Mesh mesh = GetComponent<MeshFilter>().mesh;
			Vector3[] vertices = mesh.vertices;

			Vector3[] temp_x = new Vector3[vertices.Length];
			float[] temp_n = new float[vertices.Length];

			for (int i = 0; i < vertices.Length; i++)
			{
				temp_x[i] = vertices[i] * 0.2f;
				temp_n[i] = 0.2f;
			}

			for (int e = 0; e < E.Length / 2; e++)
			{
				int v0 = E[e * 2 + 0];
				int v1 = E[e * 2 + 1];
				Vector3 D = L[e] * 0.5f * (vertices[v0] - vertices[v1]).normalized;
				Vector3 C = (vertices[v0] + vertices[v1]) * 0.5f;

				temp_x[v0] += C + D;
				temp_x[v1] += C - D;
				temp_n[v0] += 1;
				temp_n[v1] += 1;
			}

			for (int i = 0; i < vertices.Length; i++)
			{
				if (i == 0 || i == 20) continue;
				V[i] += (temp_x[i] / temp_n[i] - vertices[i]) / dt;
				vertices[i] = temp_x[i] / temp_n[i];
			}

			mesh.vertices = vertices;
		}

		void CollisionHandling(float dt)
		{
			Mesh mesh = GetComponent<MeshFilter>().mesh;
			Vector3[] X = mesh.vertices;
			GameObject sphere = GameObject.Find("Sphere");

			Vector3 center = sphere.transform.TransformPoint(new Vector3(0, 0, 0));
			float radius = sphere.transform.lossyScale.x * 0.5f;
			for (int i = 0; i < X.Length; i++)
			{
				if (i == 0 || i == 20) continue;

				Vector3 d = X[i] - center;
				if (d.magnitude < radius)
				{
					V[i] += (center + radius * d.normalized - X[i]) / dt;
					X[i] = center + radius * d.normalized;
				}
			}

			mesh.vertices = X;
		}

		
	}
}