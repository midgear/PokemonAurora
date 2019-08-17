using UnityEngine;
using System.Collections;
using LibTessDotNet;
using System.Collections.Generic;
using System.Linq;

using UnityMesh = UnityEngine.Mesh;

namespace BuildingCrafter
{
	public static class BCTest
	{
		public static void DestroyAllTestBoxes()
		{
			DestroyDis[] allTestBoxes = GameObject.FindObjectsOfType<DestroyDis>();
			for(int i = 0; i < allTestBoxes.Length; i++)
			{
				if(allTestBoxes[i] != null)
				{
#if UNITY_EDITOR
					UnityEditor.Undo.DestroyObjectImmediate(allTestBoxes[i].gameObject);
#else
					GameObject.Destroy(allTestBoxes[i].gameObject);
#endif
				}
			}
		}

		public static void DestroyAllTextBoxesAndMeshes()
		{
			DestroyDis[] allTestBoxes = GameObject.FindObjectsOfType<DestroyDis>();
			for(int i = 0; i < allTestBoxes.Length; i++)
			{
				MeshFilter[] meshFilters = allTestBoxes[i].GetComponentsInChildren<MeshFilter>();

				for(int index = 0; index < meshFilters.Length; index++)
				{
#if UNITY_EDITOR
					UnityEditor.Undo.DestroyObjectImmediate(meshFilters[index].sharedMesh);
#else
					GameObject.Destroy(meshFilters[index].sharedMesh);
#endif
				}

				if(allTestBoxes[i] != null)
				{
#if UNITY_EDITOR
					UnityEditor.Undo.DestroyObjectImmediate(allTestBoxes[i].gameObject);
#else
					GameObject.Destroy(allTestBoxes[i].gameObject);
#endif
				}
			}
		}

		public static GameObject CreateBoundsCube(Bounds bounds)
		{
			GameObject newBoundsCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
			newBoundsCube.transform.localScale = bounds.size;
			newBoundsCube.transform.transform.position = bounds.center;
			newBoundsCube.AddComponent<DestroyDis>();

			return newBoundsCube;
		}

		public static GameObject CreateSuperThinTestPointCube(Vector3 testpoint, float size = 0.2f)
		{
			GameObject newCube = CreateTestPointCube(testpoint, size);
			newCube.transform.localScale = new Vector3(0.01f, size, 0.01f);

			return newCube;
		}


		public static GameObject CreateTestPointCube(Vector3 testPoint, float size = 0.2f)
		{
			return CreateTestPointCube(testPoint, Vector3.up, size);
		}

		public static void CreateTestPointCube(List<Vector3> testPoints, float size = 0.2f)
		{
			foreach (var item in testPoints) 
			{
				CreateTestPointCube(item, Vector3.up, size);
			}
		}
		
		public static GameObject CreateTestPointCube(Vector3 testPoint, Vector3 normal, float size = 0.2f)
		{
			GameObject point = GameObject.CreatePrimitive(PrimitiveType.Cube);
			point.transform.localScale = new Vector3(.075f, size, .075f);
			point.transform.transform.position = testPoint;
			point.transform.rotation = Quaternion.LookRotation(normal) * Quaternion.Euler(0, 90, 90);
			point.AddComponent<DestroyDis>();
			
			return point;
		}

		public static GameObject CreateArrow(Vector3 testPoint, Vector3 normal, float size = 0.2f)
		{
			GameObject point = GameObject.Instantiate(Resources.Load("arrow")) as GameObject;
			point.transform.localScale = new Vector3(size, size, size);
			point.transform.transform.position = testPoint;
			point.transform.rotation = Quaternion.LookRotation(normal);
			point.AddComponent<DestroyDis>();
			
			return point;
		}
		
		public static GameObject CreatePlane(Vector3 planeCenter, Vector3 normal, float size = 1)
		{
			GameObject plane = BCMesh.GenerateEmptyGameObject("Create Broken Window", true);
			plane.transform.position = planeCenter;
			plane.AddComponent<DestroyDis>();
			plane.name = "Test Plane";

			GameObject planeForward = GameObject.CreatePrimitive(PrimitiveType.Quad);
			planeForward.transform.position = planeCenter;
			planeForward.transform.LookAt(planeCenter + normal);

			GameObject planeBackward = GameObject.CreatePrimitive(PrimitiveType.Quad);
			planeBackward.transform.position = planeCenter;
			planeBackward.transform.LookAt(planeCenter - normal);

			planeForward.transform.SetParent(plane.transform);
			planeBackward.transform.SetParent(plane.transform);

			plane.transform.localScale = new Vector3(size, size, size);

			return plane;
		}

		public static void LogAllPointsInAVectorList(Vector3[] vectorList)
		{
			string debugString = "";
			
			for(int i = 0; i < vectorList.Length; i++)
			{
				debugString += vectorList[i] + "\n";
	//			Debug.Log(newOutline[i]);
			}

			Debug.Log(debugString);
		}

		public static void Create3DTriangle(Vector3 p1, Vector3 p2, Vector3 p3, bool doubleSided = true)
		{

			{
				Tess meshShape = new Tess();
				meshShape = BCMesh.GetVertexContours(new Vector3[3] { p1, p2, p3 } );
				
				List<Vector3> vertices = new List<Vector3>();
				List<int> triangles = new List<int>();

				for (int n = 0; n < meshShape.Vertices.Length; n++) 
					vertices.Add (meshShape.Vertices[n].Position.ToVector3());
				for (int n = 0; n < meshShape.Elements.Length; n++) 
					triangles.Add (meshShape.Elements[n]); 

				UnityMesh mesh = new UnityMesh();
				mesh.vertices = vertices.ToArray<Vector3>();
				mesh.triangles = triangles.ToArray<int>();

				GameObject obj = BCMesh.GenerateEmptyGameObject("Create 3D Triangle", true);
				MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
				meshFilter.sharedMesh = mesh;
				mesh.RecalculateNormals();

				MeshRenderer meshRend = obj.AddComponent<MeshRenderer>();
				meshRend.material = Resources.Load("Consumed") as Material;
				obj.AddComponent<DestroyDis>();
			}

			if(doubleSided)
				Create3DTriangle(p3, p2, p1, false); // MUST ALWAYS BE FALSE OR WILL CRASH

		}

		public static void CreateObjectFromUnityMesh(UnityMesh unityMesh, Material material = null, Vector3 position = new Vector3(), Vector3 rotation = new Vector3())
		{
			GameObject newGO = BCMesh.GenerateEmptyGameObject("Create Generate Object From Unity Mesh", true);
			newGO.transform.position = position;
			newGO.transform.rotation = Quaternion.Euler(rotation);


			MeshFilter meshFilter = newGO.AddComponent<MeshFilter>(); 
			meshFilter.mesh = unityMesh;

			if(material == null)
				material = Resources.Load<Material>("FillerGlass");

			Material[] newMaterials = new Material[unityMesh.subMeshCount];
			for(int i = 0; i < newMaterials.Length; i++)
				newMaterials[i] = material;

			MeshRenderer meshRenderer = newGO.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterials = newMaterials;

			newGO.AddComponent<DestroyDis>();
		}
	}
}
