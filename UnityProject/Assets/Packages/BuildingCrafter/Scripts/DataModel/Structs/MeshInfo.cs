using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{
	public struct MeshInfo
	{
		public Vector3[] Vertices;
		public int[] Triangles;
		public Vector2[] UVs;
		public Vector4[] Tangents;

		// Holds a list of submesh Triangles. NOTE: The index in the list is the submesh number
		public List<int[]> SubmeshTriangles;

		public MeshInfo(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Vector4> tangents, int subMesh = 0)
		{
			Vertices = vertices.ToArray<Vector3>();
			Triangles = triangles.ToArray<int>();
			if(uvs != null)
				UVs = uvs.ToArray<Vector2>();
			else
				UVs = new Vector2[0];
			if(tangents != null)
				Tangents = tangents.ToArray<Vector4>();
			else
				Tangents = new Vector4[0];
			SubmeshTriangles = new List<int[]>();

			// Adds all the required submeshes to get the right index
			for(int i = 0; i <= subMesh; i++)
				SubmeshTriangles.Add(new int[0]);

			// Set this submesh to the correct position
			SubmeshTriangles[subMesh] = Triangles.ToArray<int>();
		}

		public MeshInfo(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Vector4> tangents, List<int[]> submeshTriangles)
		{
			Vertices = vertices.ToArray<Vector3>();
			Triangles = triangles.ToArray<int>();
			UVs = uvs.ToArray<Vector2>();
			Tangents = tangents.ToArray<Vector4>();

			SubmeshTriangles = submeshTriangles;
		}

		public MeshInfo(Mesh mesh, int subMesh = 0)
		{
			Vertices = mesh.vertices;
			Triangles = mesh.triangles;
			UVs = mesh.uv;
			Tangents = mesh.tangents;

			SubmeshTriangles = new List<int[]>();
			// Adds all the required submeshes to get the right index
			for(int i = 0; i <= subMesh; i++)
				SubmeshTriangles.Add(new int[0]);

			// Set this submesh to the correct position
			SubmeshTriangles[subMesh] = Triangles.ToArray<int>();
		}

		public bool IsValid
		{
			get { return Vertices != null && Vertices.Length > 0; }
		}

		public void MoveEntireMesh(Vector3 offsetMove)
		{
			if(this.IsValid == false)
				return;

			for(int i = 0; i < Vertices.Length; i++)
			{
				Vertices[i] += offsetMove;
			}
		}

	}
}