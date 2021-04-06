using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Main_101_2_Test : MonoBehaviour
{
	private static readonly int[] indexes =
	{
		0, 2, 1,
		3, 5, 4,
	};

	private static readonly Vector3[] poses =
	{
		new Vector3(2, 0, -2),
		new Vector3(0, 2, -2),
		new Vector3(-2, 0, -2),
		new Vector3(3.5f, -1, -5),
		new Vector3(2.5f, 1.5f, -5),
		new Vector3(-1, 0.5f, -5),
	};


	private static readonly Color[] cols =
	{
		new Color(0 / 255f, 238 / 255f, 185 / 255f, 0),
		new Color(217 / 255f, 0 / 255f, 185 / 255f, 0),
		new Color(217 / 255f, 238 / 255f, 0 / 255f, 0),
		new Color(185 / 255f, 217 / 255f, 0 / 255f, 0),
		new Color(185 / 255f, 0 / 255f, 238 / 255f, 0),
		new Color(0 / 255f, 217 / 255f, 238 / 255f, 0),
	};

	public MeshFilter meshFilter;
	
	private Mesh mesh;

	public void Awake()
	{
		mesh = new Mesh()
		{
			name = "MyMesh"
		};
		mesh.SetVertices(poses);
		mesh.SetColors(cols);
		mesh.SetIndices(indexes, MeshTopology.Triangles, 0);
		meshFilter.mesh = mesh;
	}

}