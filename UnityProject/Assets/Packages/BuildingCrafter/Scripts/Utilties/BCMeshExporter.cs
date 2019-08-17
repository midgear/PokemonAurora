//using UnityEngine;
//using System.Collections;
//using System.Text;
//using System.Linq;
//
//namespace BuildingCrafter
//{
//	public class BCMeshExporter
//	{
//
//		private static int StartIndex = 0;
//		
//		public static void Start()
//		{
//			StartIndex = 0;
//		}
//		public static void End()
//		{
//			StartIndex = 0;
//		}
//
//		public static string MeshToString(MeshFilter meshFilter, Transform transform) 
//		{	
//			Vector3 scale			= meshFilter.transform.localScale;
//			Vector3 position		= meshFilter.transform.localPosition;
//			Quaternion rotation 	= meshFilter.transform.localRotation;// * Quaternion.Euler(-90, 0, 0);
//			
//			int numVertices = 0;
//			Mesh mesh = meshFilter.sharedMesh;
//			MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
//
//			if (mesh == null || meshRenderer == null)
//				return "####Error####";
//
//			Material[] mats = meshRenderer.sharedMaterials;
//			
//			StringBuilder stringBuilder = new StringBuilder();
//
//			Vector3[] verts = mesh.vertices;
////			verts = verts.Reverse().ToArray<Vector3>();
//
//			for (int i = 0; i < verts.Length; i++)
////			for (int i = verts.Length - 1; i >= 0; i--)
//			{
////				Vector3 vectorPoint = meshFilter.transform.TransformPoint(verts[i]);
////				Vector3 vectorPoint = meshFilter.transform.TransformPoint(mesh.vertices[i]);
//				Vector3 vectorPoint = mesh.vertices[i];
//
//				vectorPoint = new Vector3(vectorPoint.x, vectorPoint.y, vectorPoint.z);
//
//				numVertices++;
//				stringBuilder.Append (string.Format ("v {0} {1} {2}\n", vectorPoint.x, vectorPoint.y, vectorPoint.z));
//			}
//
////			stringBuilder.Append("\n");
////
////			for (int i = 0; i < mesh.normals.Length; i++) 
////			{
////				Vector3 vectorPoint = meshFilter.transform.TransformDirection(mesh.normals[i]);
////
//////				Vector3 vectorPoint = rotation * mesh.normals[i];
//////				Vector3 vectorPoint = meshFilter.transform.TransformDirection(lv);
////
////				vectorPoint = new Vector3(-1 * vectorPoint.x, vectorPoint.y, vectorPoint.z);
////
////
////				// reverse the normals for the interior
////
////				stringBuilder.Append (string.Format ("vn {0} {1} {2}\n", vectorPoint.x, vectorPoint.y, vectorPoint.z));
////			}
//
//			stringBuilder.Append("\n");
//
//			for (int i = 0; i < mesh.uv.Length; i++) 
////			for (int i = mesh.uv.Length - 1; i >= 0; i--)
//			{
//				Vector3 v = mesh.uv[i];
//				stringBuilder.Append (string.Format ("vt {0} {1}\n", v.x, v.y));
//			}
//
//			for (int material = 0; material < mesh.subMeshCount; material ++) 
//			{
//				stringBuilder.Append("\n");
//				stringBuilder.Append("usemtl ").Append(mats[material].name).Append("\n");
//				stringBuilder.Append("usemap ").Append(mats[material].name).Append("\n");
//				
//				int[] triangles = mesh.GetTriangles(material);
//
//				for (int i=0;i<triangles.Length;i+=3) 
////				for (int i = triangles.Length - 4; i > 0; i -= 3) 
//				{
//					stringBuilder.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", 
//					                        triangles[i]+1+StartIndex, triangles[i+1]+1+StartIndex, triangles[i+2]+1+StartIndex));
//				}
//			}
//			
//			StartIndex += numVertices;
//			return stringBuilder.ToString();
//		}
//	}
//}