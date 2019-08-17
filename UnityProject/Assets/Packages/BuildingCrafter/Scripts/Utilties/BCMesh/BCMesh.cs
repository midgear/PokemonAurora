using UnityEngine;
using System.Collections;
using LibTessDotNet;
using System.Collections.Generic;
using System.Linq;

using UnityMesh = UnityEngine.Mesh;

namespace BuildingCrafter
{

	public static partial class BCMesh
	{
		public static MeshInfo GenerateGenericMeshInfo(Vector3[] outline, List<Vector3[]> cutouts, float uvTileSize = 1, int submesh = 0)
		{
			Tess meshShape = new Tess();
			if(cutouts != null)
				meshShape = GetVertexContours(outline, cutouts);
			else
				meshShape = GetVertexContours(outline);

			List<Vector3> vertices = new List<Vector3>();
			List<int> triangles = new List<int>();
			List<Vector2> uvs = new List<Vector2>();
			List<Vector4> tangents = new List<Vector4>();
			
			for (int n = 0; n < meshShape.Vertices.Length; n++) 
				vertices.Add (meshShape.Vertices[n].Position.ToVector3());
			for (int n = 0; n < meshShape.Elements.Length; n++) 
				triangles.Add (meshShape.Elements[n]); 

			// Add in the Uvs and Tangents
			uvs.AddRange(BCMesh.CreateUVTiles(vertices.ToArray(), triangles.ToArray(), uvTileSize));

			// Generates junk UV's which are calculated when the mesh is generated
			tangents.AddRange(BCMesh.CreateTangents(vertices.ToArray(), new Vector4(0, 1, 0, 1)));
			
			// Adds all the newly generated verticies to the whole wall
			return new MeshInfo(vertices, triangles, uvs, tangents, submesh);
		}

		public static MeshInfo GenerateGenericMeshInfoWithLowerAnchor(Vector3[] outline, List<Vector3[]> cutouts, float uvTileSize = 1, int submesh = 0)
		{
			Tess meshShape = new Tess();
			if(cutouts != null)
				meshShape = GetVertexContours(outline, cutouts);
			else
				meshShape = GetVertexContours(outline);

			List<Vector3> vertices = new List<Vector3>();
			List<int> triangles = new List<int>();
			List<Vector2> uvs = new List<Vector2>();
			List<Vector4> tangents = new List<Vector4>();

			for (int n = 0; n < meshShape.Vertices.Length; n++) 
				vertices.Add (meshShape.Vertices[n].Position.ToVector3());
			for (int n = 0; n < meshShape.Elements.Length; n++) 
				triangles.Add (meshShape.Elements[n]); 

			// Add in the Uvs and Tangents
			uvs.AddRange(BCMesh.CreateUVTilesFromLowerAnchor(vertices.ToArray(), triangles.ToArray(), uvTileSize));

			// Generates junk UV's which are calculated when the mesh is generated
			tangents.AddRange(BCMesh.CreateTangents(vertices.ToArray(), new Vector4(0, 1, 0, 1)));

			// Adds all the newly generated verticies to the whole wall
			return new MeshInfo(vertices, triangles, uvs, tangents, submesh);
		}

//		/// <summary>
//		/// HACK for generating properly atlasing UV tiles
//		/// </summary>
//		/// <returns>The generic mesh info.</returns>
//		/// <param name="outline">Outline.</param>
//		/// <param name="onlyPositiveUVs">If set to <c>true</c> only positive U vs.</param>
//		/// <param name="uvTileSize">Uv tile size.</param>
//		public static MeshInfo GenerateGenericMeshInfo(Vector3[] outline, bool onlyPositiveUVs, float uvTileSize = 1)
//		{
//			if(onlyPositiveUVs == false)
//				return GenerateGenericMeshInfo(outline, null, uvTileSize);
//
//			MeshInfo meshInfo = GenerateGenericMeshInfo(outline, null, uvTileSize);
//
//			bool uIsNeg = false;
//			bool vIsNeg = false;
//			float smallestU = 0;
//			float smallestV = 0;
//
//			for(int i = 0; i < meshInfo.UVs.Length; i++)
//			{
//				float currentU = meshInfo.UVs[i].x;
//				float currentV = meshInfo.UVs[i].y;
//
//				if(currentU < 0)
//				{
//					uIsNeg = true;
//					if(currentU < smallestU)
//						smallestU = currentU;
//				}
//
//				if(currentV < 0)
//				{
//					vIsNeg = true;
//					if(currentV < smallestV)
//						smallestV = currentV;
//				}
//			}
//
//			if(uIsNeg)
//			{
//				for(int i = 0; i < meshInfo.UVs.Length; i++)
//				{
//					Vector2 Uv =  meshInfo.UVs[i];
////					Uv.x += Mathf.Abs(smallestU);
//					Uv.x += 1;
//					meshInfo.UVs[i] = Uv;
//				}
//			}
//
//			if(vIsNeg)
//			{
//				for(int i = 0; i < meshInfo.UVs.Length; i++)
//				{
//					Vector2 Uv =  meshInfo.UVs[i];
////					Uv.y += Mathf.Abs(smallestV);
//					Uv.y += 1;
//					meshInfo.UVs[i] = Uv;
//				}
//			}
//
////
////			if(uIsNeg || vIsNeg)
////				Debug.Log(smallestU + " " + smallestV);
//
//			return meshInfo;
//		}

		public static MeshInfo GenerateGenericMeshInfo(Vector3[] outline, float uvTileSize = 1, int submesh = 0)
		{
			return GenerateGenericMeshInfo(outline, null, uvTileSize);
		}

		public static MeshInfo GenerateGenericMeshInfo(List<Vector3> outline, float uvTileSize = 1, int submesh = 0)
		{
			return GenerateGenericMeshInfo(outline.ToArray<Vector3>(), null, uvTileSize);
		}

		public static MeshInfo GenerateGenericUVOffsetMeshInfo(Vector3[] outline, float uvSize, Vector3 cross, Vector2 uvOffset, Vector3 vUpDirection)	
		{
			return GenerateGenericUVOffsetMeshInfo(outline, null, uvSize, cross, uvOffset, vUpDirection);
		}

		public static MeshInfo GenerateGenericUVOffsetMeshInfo(Vector3[] outline, List<Vector3[]> cutouts, float uvSize, Vector3 cross, Vector2 uvOffset, Vector3 vUpDirection)	
		{
			Tess meshShape = new Tess();
			if(cutouts != null)
				meshShape = BCMesh.GetVertexContours(outline, cutouts);
			else
				meshShape = BCMesh.GetVertexContours(outline);

			if(meshShape.Vertices.Length == 0)
				return new MeshInfo();

			List<Vector3> vertices = new List<Vector3>();
			List<int> triangles = new List<int>();
			List<Vector2> uvs = new List<Vector2>();
			List<Vector4> tangents = new List<Vector4>();

			for (int n = 0; n < meshShape.Vertices.Length; n++) 
				vertices.Add (meshShape.Vertices[n].Position.ToVector3());
			for (int n = 0; n < meshShape.Elements.Length; n++) 
				triangles.Add (meshShape.Elements[n]); 
			
			// Add in the Uvs and Tangents
			uvs.AddRange(CreateOffsetUVMapping(vertices.ToArray<Vector3>(), triangles.ToArray<int>(), uvSize, cross, uvOffset, vUpDirection).ToArray<Vector2>());
			
			// Generates junk UV's which are calculated when the mesh is generated
			tangents.AddRange(BCMesh.CreateTangents(vertices.ToArray(), new Vector4(0, 1, 0, 1)));
			
			// Adds all the newly generated verticies to the whole wall
			return new MeshInfo(vertices, triangles, uvs, tangents, 0);
		}



		public static Vector2[] CreateOffsetUVMapping(Vector3[] verts, int[] tris, float uvSize, Vector3 cross, Vector2 uvOffset, Vector3 vUpDirection)
		{
			if(verts.Length <= 0)
				return null;

			// Anchor needs work, specifically coming from the angle 
			Vector3 anchorPoint = new Vector3(verts[0].x, verts[0].y, verts[0].z); // This is the point where all other points reference off
			
//			Ensures the anchor point is at the lowest possible y position to ensure the walls are right
			int lowestY = 0;
			for(int i = 0; i < verts.Length; i++)
			{
				if(verts[i].y < verts[lowestY].y)
					lowestY = i;
			}
			anchorPoint = verts[lowestY];
			
			Vector2[] uvs = new Vector2[verts.Length];

			for(int i = 0; i < tris.Length; i += 3)
			{
				// The triangle we are editing
				int t1 = tris[i];
				int t2 = tris[i + 1];
				int t3 = tris[i + 2];
				
				// The verts of the triangle
				Vector3 p1 = verts[t1];
				Vector3 p2 = verts[t2];
				Vector3 p3 = verts[t3];

				Vector2[] nonPlaneUVs = BCMesh.MapNonPlanarUVTile(anchorPoint, p1, p2, p3, uvSize, cross, vUpDirection);
				uvs[t1] = nonPlaneUVs[0] + uvOffset / uvSize;
				uvs[t2] = nonPlaneUVs[1] + uvOffset / uvSize;
				uvs[t3] = nonPlaneUVs[2] + uvOffset / uvSize;
			}
			
			return uvs;
		}

		public static UnityMesh GetMeshFromMeshInfo(MeshInfo meshInfo, Vector3 meshPivot = new Vector3())
		{

			// Offsets the mesh by a certain amount
			if(meshInfo.Vertices == null)
				return new UnityMesh();

			for(int i = 0; i < meshInfo.Vertices.Length; i++)
				meshInfo.Vertices[i] -= meshPivot;

			UnityMesh m = new UnityMesh();
			m.name = "Procedural";
			m.vertices = meshInfo.Vertices.ToArray();
			m.triangles = meshInfo.Triangles.ToArray();
			m.uv = meshInfo.UVs.ToArray();
			m.RecalculateNormals();
			if(meshInfo.Tangents.Length == meshInfo.Vertices.Length)
				m.tangents = meshInfo.Tangents.ToArray();

			if(meshInfo.SubmeshTriangles.Count > 0)
			{
				m.subMeshCount = meshInfo.SubmeshTriangles.Count;
				for(int i = 0; i < meshInfo.SubmeshTriangles.Count; i++)
				{
					m.SetTriangles(meshInfo.SubmeshTriangles[i], i);
				}
			}

			return m;
		}

		public static UnityMesh GetMeshFromMeshInfo(List<MeshInfo> meshInfos, Vector3 meshPivot = new Vector3())
		{
			MeshInfo combinedMeshes = new MeshInfo();

			for(int i = 0; i < meshInfos.Count; i++)
				combinedMeshes = BCMesh.CombineMeshInfos(combinedMeshes, meshInfos[i]);

			return GetMeshFromMeshInfo(combinedMeshes, meshPivot);
		}

		public static GameObject GenerateGameObjectFromVectorOutline(List<Vector3> outline, float UVSize, int submesh, Vector3 objectCentre, string nameOfObject, string nameOfMesh, Material material)
		{
			GameObject newMeshObject = BCMesh.GenerateEmptyGameObject("Create Generic Object", true);

			MeshInfo meshInfo = BCMesh.GenerateGenericMeshInfo(outline, UVSize, submesh);

			meshInfo = UpdateMeshInfoCenter(meshInfo, objectCentre);
			newMeshObject.gameObject.transform.position = objectCentre;
			
			newMeshObject.name = nameOfObject;
			MeshRenderer meshRenderer = newMeshObject.AddComponent<MeshRenderer>();
			MeshFilter meshFilter = newMeshObject.AddComponent<MeshFilter>();
			
			UnityMesh mesh = BCMesh.GetMeshFromMeshInfo(meshInfo);
			mesh.name = nameOfMesh;
			BCMesh.CalculateMeshTangents(mesh);
			
			meshFilter.mesh = mesh;
			meshRenderer.material = material;
			
			return newMeshObject;
		}


		public static GameObject GenerateGameObjectFromMesh(MeshInfo meshInfo, Vector3 objectCentre, string nameOfObject, string nameOfMesh, Material material)
		{
			GameObject newMeshObject = BCMesh.GenerateEmptyGameObject("Generate Generic GameObject", true);
			if(newMeshObject.GetComponent<ProceduralGameObject>() == null)
				newMeshObject.AddComponent<ProceduralGameObject>();

			meshInfo = UpdateMeshInfoCenter(meshInfo, objectCentre);
			newMeshObject.gameObject.transform.position = objectCentre;

			newMeshObject.name = nameOfObject;
			MeshRenderer meshRenderer = newMeshObject.AddComponent<MeshRenderer>();
			MeshFilter meshFilter = newMeshObject.AddComponent<MeshFilter>();

			UnityMesh mesh = BCMesh.GetMeshFromMeshInfo(meshInfo);
			mesh.name = nameOfMesh;
			BCMesh.CalculateMeshTangents(mesh);

			meshFilter.mesh = mesh;
			meshRenderer.material = material;

			return newMeshObject;
		}

		public static GameObject GenerateGameObjectFromMesh(MeshInfo meshInfo, Vector3 objectCentre, string nameOfObject, string nameOfMesh, Material[] materials)
		{
			GameObject newMeshObject = GenerateGameObjectFromMesh(meshInfo, objectCentre, nameOfObject, nameOfMesh, materials[0]);
			if(newMeshObject.GetComponent<ProceduralGameObject>() == null)
				newMeshObject.AddComponent<ProceduralGameObject>();

			MeshRenderer meshRenderer = newMeshObject.GetComponent<MeshRenderer>();
			MeshFilter meshFilter = newMeshObject.GetComponent<MeshFilter>();

			if(meshFilter.sharedMesh.subMeshCount == 1)
				meshRenderer.material = materials[0];
			else
				meshRenderer.materials = materials;

			return newMeshObject;
		}

		public static MeshInfo UpdateMeshInfoCenter(MeshInfo meshInfo, Vector3 newCentre)
		{
			if(meshInfo.Vertices == null)
				return meshInfo;

			for(int i = 0; i < meshInfo.Vertices.Length; i++)
			{
				meshInfo.Vertices[i] -= newCentre;
			}

			return meshInfo;
		}
		
		/// <summary>
		/// Adds the triangles to mesh lists.
		/// </summary>
		/// <returns>Submesh List of Triangles</returns>
		public static int[] AddTrianglesToMeshLists (MeshInfo meshInfo, ref List<Vector3> allVertices, ref List<int> allTriangles, ref List<Vector2> allUVs, ref List<Vector4> allTangents)
		{
			if(meshInfo.Vertices == null || meshInfo.Vertices.Length < 3)
				return new int[0];

			int currentNumVerticies = allVertices.Count;
			List<int> triangles = new List<int>();
			for(int i = 0; i < meshInfo.Triangles.Length; i++)
				triangles.Add(meshInfo.Triangles[i] + currentNumVerticies);
			
			allVertices.AddRange(meshInfo.Vertices);
			allTriangles.AddRange(triangles);
			allUVs.AddRange(meshInfo.UVs);
			allTangents.AddRange(meshInfo.Tangents);
			
			return triangles.ToArray<int>();
		}

		public static Tess GetVertexContours(Vector3[] points, List<Vector3[]> cutouts)
		{
			Tess tessBuilder = new Tess();
			
			// This is where we tell how long the input information is
			int numPoints = points.Length;
			
			ContourVertex[] contour = new ContourVertex[numPoints];
			
			for(int i = 0; i < points.Length; i++)
			{
				contour[i].Position = new Vec3(points[i]);
				// Here the Data Point can add different colours, Leave that for later
			}
			
			tessBuilder.AddContour(contour);
			
			foreach(var cutout in cutouts)
			{
				ContourVertex[] windowContour = new ContourVertex[cutout.Length];
				
				for(int i = 0; i < cutout.Length; i++)
				{
					windowContour[i].Position = new Vec3(cutout[i]);
				}
				tessBuilder.AddContour(windowContour);
			}
			
			tessBuilder.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);
			
			return tessBuilder;
		}

		public static Tess GetVertexContours(Vector3[] points)
		{
			Tess tessBuilder = new Tess();
			
			// This is where we tell how long the input information is
			int numPoints = points.Length;
			
			ContourVertex[] contour = new ContourVertex[numPoints];
			
			for(int i = 0; i < points.Length; i++)
			{
				contour[i].Position = new Vec3(points[i]);
				// Here the Data Point can add different colours, Leave that for later
			}
			
			tessBuilder.AddContour(contour);
			tessBuilder.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);
			
			return tessBuilder;
		}

		public static MeshInfo GetDoorFrame(DoorInfo door)
		{
			Vector3[] doorOutline = DoorOutline(Vector3.zero, door.End - door.Start, door, Vector3.zero);

			Vector3 startBottom = doorOutline[0];
			Vector3 endBottom = doorOutline[3];
			Vector3 startTop = doorOutline[1];

			MeshInfo returnMeshInfo = new MeshInfo();

			Vector3 direction = (door.End - door.Start).normalized;
			Vector3 rightAngleDirection = direction;
			if(direction.x == 0)
				rightAngleDirection = new Vector3(1, 0, 0);
			if(direction.z == 0)
				rightAngleDirection = new Vector3(0, 0, 1);

			Vector3 inset = Vector3.zero;

			Vector3 pStartLeft = Vector3.zero;
			Vector3 pStartRight = Vector3.zero;
			Vector3 pEndLeft = Vector3.zero;
			Vector3 pEndRight = Vector3.zero;

			// Need to draw the outlines differently depending on the direction
			if(direction.x < 0 || direction.z > 0)
			{
				pStartLeft = startBottom + rightAngleDirection * 0.1f + inset;
				pStartRight = startBottom - rightAngleDirection * 0.1f + inset;
				pEndLeft = endBottom + rightAngleDirection * 0.1f - inset;
				pEndRight = endBottom - rightAngleDirection * 0.1f - inset;
			}
			else
			{	
				pStartLeft = endBottom + rightAngleDirection * 0.1f - inset;
				pStartRight = endBottom - rightAngleDirection * 0.1f - inset;
				pEndLeft = startBottom + rightAngleDirection * 0.1f + inset;
				pEndRight = startBottom - rightAngleDirection * 0.1f + inset;
			}

			Vector3 t = startTop.y * Vector3.up;


			// Build one side
			Vector3[] startOutline = new Vector3[5]
			{
				pStartLeft, pStartLeft + t, pStartRight + t, pStartRight, pStartLeft
			};

			returnMeshInfo = BCMesh.CombineMeshInfos(returnMeshInfo, BCMesh.GenerateGenericMeshInfo(startOutline, 3));

			Vector3[] topOutline = new Vector3[5]
			{
				pStartLeft + t, pEndLeft + t, pEndRight + t,  pStartRight + t, pStartLeft + t,
			};

			returnMeshInfo = BCMesh.CombineMeshInfos(returnMeshInfo, BCMesh.GenerateGenericMeshInfo(topOutline, 3));

			Vector3[] endOutline = new Vector3[5]
			{
				pEndRight, pEndRight + t, pEndLeft + t, pEndLeft, pEndRight 
			};

			returnMeshInfo = BCMesh.CombineMeshInfos(returnMeshInfo, BCMesh.GenerateGenericMeshInfo(endOutline, 3));



			return returnMeshInfo;
		}

		public static MeshInfo CombineMeshInfoList(List<MeshInfo> allMeshInfos)
		{
			MeshInfo meshInfo = new MeshInfo();
			for(int i = 0; i < allMeshInfos.Count; i++)
			{
				meshInfo = BCMesh.CombineMeshInfos(meshInfo, allMeshInfos[i]);
			}

			return meshInfo;
		}

		public static MeshInfo CombineMeshInfos(MeshInfo meshInfo, MeshInfo addMeshInfo)
		{
			List<Vector3> allVertices = new List<Vector3>();
			List<int> allTriangles = new List<int>();
			List<Vector2> allUVs = new List<Vector2>();
			List<Vector4> allTangents = new List<Vector4>();
			List<List<int>> allSubmeshTris = new List<List<int>>();

			// Add the first mesh info
			if(meshInfo.Vertices != null)
			{
				allVertices.AddRange(meshInfo.Vertices);
				allTriangles.AddRange(meshInfo.Triangles);
				allUVs.AddRange(meshInfo.UVs);
				allTangents.AddRange(meshInfo.Tangents);
				for(int i = 0; i < meshInfo.SubmeshTriangles.Count; i++)
				{
					int[] subMesh = meshInfo.SubmeshTriangles[i];
					allSubmeshTris.Add(subMesh.ToList<int>());
				}
			}

			int currentVerticies = allVertices.Count;

			// Add the second mesh info
			if(addMeshInfo.Vertices != null)
			{
				allVertices.AddRange(addMeshInfo.Vertices);
				for(int i = 0; i < addMeshInfo.Triangles.Length; i++)
					allTriangles.Add(addMeshInfo.Triangles[i] + currentVerticies);
				allUVs.AddRange(addMeshInfo.UVs);
				allTangents.AddRange(addMeshInfo.Tangents);

				int breaker = 0;

				while(breaker < 16384 && allSubmeshTris.Count < addMeshInfo.SubmeshTriangles.Count)
				{
					breaker++;
					allSubmeshTris.Add(new List<int>());
				}

				for(int i = 0; i < addMeshInfo.SubmeshTriangles.Count; i++)
				{
					// Adds the submeshes with the proper vertices
					for(int j = 0; j < addMeshInfo.SubmeshTriangles[i].Length; j++)
						allSubmeshTris[i].Add(addMeshInfo.SubmeshTriangles[i][j] + currentVerticies);
				}
			}

			// Convert the submesh list list to list array
			List<int[]> submeshIntList = new List<int[]>();
			for(int i = 0; i < allSubmeshTris.Count; i++)
			{
				submeshIntList.Add(allSubmeshTris[i].ToArray<int>());
			}

			return new MeshInfo(allVertices, allTriangles, allUVs, allTangents, submeshIntList);
		}

		public static GameObject[] GenerateDoorFrames (int floorIndex, BuildingBlueprint buildingBp, Transform parent)
		{
			FloorBlueprint floorBp = buildingBp.Floors[floorIndex];
			BuildingStyle buildingStyle = buildingBp.BuildingStyle;

			GameObject[] doorOutlines = new GameObject[floorBp.Doors.Count];
			
			for (int i = 0; i < floorBp.Doors.Count; i++) 
			{
				DoorInfo door = floorBp.Doors[i];
				
				MeshInfo newMesh = BCMesh.GetDoorFrame(door);
				
				GameObject newDoorOutline = BCMesh.GenerateEmptyGameObject("Create Door Frame", true);
				newDoorOutline.AddComponent<DoorHolder>().Index = -2;
				newDoorOutline.name = "Door Frame " + i;
				newDoorOutline.transform.position = door.Start;
				newDoorOutline.transform.SetParent(parent);
				
				MeshRenderer meshRenderer = newDoorOutline.AddComponent<MeshRenderer>();
				MeshFilter meshFilter = newDoorOutline.AddComponent<MeshFilter>();
				
				meshRenderer.material = buildingStyle.DoorWindowFrames as Material;
				
				UnityMesh mesh = new UnityMesh();
				mesh.name = "Procedural Door Frame";
				mesh.vertices = newMesh.Vertices.ToArray();
				mesh.triangles = newMesh.Triangles.ToArray();
				mesh.uv = newMesh.UVs;
				mesh.tangents = newMesh.Tangents;

				mesh.RecalculateNormals();
				BCMesh.CalculateMeshTangents(mesh);

				meshFilter.mesh = mesh;
				
				doorOutlines[i] = newDoorOutline;
//				newDoorOutline.transform.position += Vector3.up * floorHeight;
			}
			
			return doorOutlines;
		}
		
		public static MeshInfo GetWindowFrame(WindowInfo window)
		{
			Vector3[] windowOutline = LocalWindowOutline(window);

			Vector3 startBottom = windowOutline[0];
			Vector3 endBottom = windowOutline[1];
	//		Vector3 endTop = windowOutline[2];
			Vector3 startTop = windowOutline[3];

			MeshInfo returnMeshInfo = new MeshInfo();

			Vector3 direction = (window.End - window.Start).normalized;
			Vector3 rightAngleDirection = direction;
			if(direction.x == 0)
				rightAngleDirection = new Vector3(1, 0, 0);
			if(direction.z == 0)
				rightAngleDirection = new Vector3(0, 0, 1);
			
			Vector3 inset = Vector3.zero;

			Vector3 pStartLeft = Vector3.zero;
			Vector3 pStartRight = Vector3.zero;
			Vector3 pEndLeft = Vector3.zero;
			Vector3 pEndRight = Vector3.zero;
			
			// Need to draw the outlines differently depending on the direction
			if(direction.x < 0 || direction.z > 0)
			{
				pStartLeft = startBottom + rightAngleDirection * 0.1f + inset;
				pStartRight = startBottom - rightAngleDirection * 0.1f + inset;
				pEndLeft = endBottom + rightAngleDirection * 0.1f - inset;
				pEndRight = endBottom - rightAngleDirection * 0.1f - inset;
			}
			else
			{	
				pStartLeft = endBottom + rightAngleDirection * 0.1f - inset;
				pStartRight = endBottom - rightAngleDirection * 0.1f - inset;
				pEndLeft = startBottom + rightAngleDirection * 0.1f + inset;
				pEndRight = startBottom - rightAngleDirection * 0.1f + inset;
			}

			// TODO: Use BCUtils Window Generation Alg. to figure this out

			Vector3 t = startTop.y * Vector3.up - startBottom.y * Vector3.up; // Removes the offset created by the bottom of the system
			
			// Build one side
			Vector3[] startOutline = new Vector3[5]
			{
				pStartLeft, pStartLeft + t, pStartRight + t, pStartRight, pStartLeft
			};
			
			returnMeshInfo = AddMeshInfo(startOutline, returnMeshInfo);
			
			Vector3[] topOutline = new Vector3[5]
			{
				pStartLeft + t, pEndLeft + t, pEndRight + t,  pStartRight + t, pStartLeft + t,
			};
			
			returnMeshInfo = AddMeshInfo(topOutline, returnMeshInfo);
			
			Vector3[] endOutline = new Vector3[5]
			{
				pEndRight, pEndRight + t, pEndLeft + t, pEndLeft, pEndRight
			};

			returnMeshInfo = AddMeshInfo(endOutline, returnMeshInfo);

			Vector3[] bottomOutline = new Vector3[5]
			{
				pStartLeft, pStartRight, pEndRight, pEndLeft, pStartLeft, 
			};

			returnMeshInfo = AddMeshInfo(bottomOutline, returnMeshInfo);
			
			return returnMeshInfo;
		}

		public static MeshInfo GetFrameFourSide(Vector3[] outline, 
		                                        Vector3 startLocation, 
		                                        bool generateBottomOfFrame = true, 
		                                        float frameCenter = 0.0f, 
		                                        float frameExtendsBy = 0.1f)
		{
			if(outline.Length != 4)
			{
				Debug.LogError("Foursided frame has too many outline points " + outline.Length);
				return new MeshInfo();
			}

			Vector3 direction = outline[1] - outline[0];
			Vector3 cross = Vector3.Cross(direction, Vector3.up);
			cross.Normalize();

			float bottomOfWindow = outline[0].y;
			float topOfWindow = outline[2].y;

			cross *= -1;

			if(frameExtendsBy > 0)
			{
				outline = new Vector3[] { outline[1], outline[0], outline[3], outline[2] };
			}
			
			MeshInfo frame = new MeshInfo();

			Vector3 sillCross = Vector3.zero;
			Vector3 upCross = Vector3.up;
			// First the flooring section
			if(generateBottomOfFrame)
			{
				Vector3[] bottomSill = new Vector3[]
				{
					outline[1] + cross * frameCenter, 
					outline[0] + cross * frameCenter,
					outline[0] + cross * frameExtendsBy,
					outline[1] + cross * frameExtendsBy
				};

				sillCross = Vector3.Cross(bottomSill[0] - bottomSill[1], bottomSill[1] - bottomSill[2]);
				upCross = Vector3.Cross(sillCross, (bottomSill[3] - bottomSill[2]).normalized);
				frame = BCMesh.CombineMeshInfos(frame, BCMesh.GenerateGenericUVOffsetMeshInfo(bottomSill, 3f, sillCross, new Vector2(0, bottomOfWindow), upCross));
			}

			Vector3[] farSill = new Vector3[]
			{
				outline[2] + cross * frameCenter, 
				outline[1] + cross * frameCenter,
				outline[1] + cross * frameExtendsBy,
				outline[2] + cross * frameExtendsBy,
			};

			sillCross = Vector3.Cross(farSill[0] - farSill[1], farSill[1] - farSill[2]);
			upCross = Vector3.Cross(sillCross, (farSill[3] - farSill[2]).normalized);
			frame = BCMesh.CombineMeshInfos(frame, BCMesh.GenerateGenericUVOffsetMeshInfo(farSill, 3f, sillCross, new Vector2(0, bottomOfWindow), Vector3.up));

			Vector3[] topSill = new Vector3[]
			{
				outline[3] + cross * frameCenter, 
				outline[2] + cross * frameCenter,
				outline[2] + cross * frameExtendsBy,
				outline[3] + cross * frameExtendsBy
			};

			sillCross = Vector3.Cross(topSill[0] - topSill[1], topSill[1] - topSill[2]);
			upCross = Vector3.Cross(sillCross, (topSill[3] - topSill[2]).normalized);
			frame = BCMesh.CombineMeshInfos(frame, BCMesh.GenerateGenericUVOffsetMeshInfo(topSill, 3f, sillCross, new Vector2(0, 3 - topOfWindow), upCross * -1));

			Vector3[] closeSill = new Vector3[]
			{
				outline[0] + cross * frameCenter, 
				outline[3] + cross * frameCenter,
				outline[3] + cross * frameExtendsBy,
				outline[0] + cross * frameExtendsBy
			};
			sillCross = Vector3.Cross(closeSill[0] - closeSill[1], closeSill[1] - closeSill[2]);
			upCross = Vector3.Cross(sillCross, (closeSill[3] - closeSill[2]).normalized);
			frame = BCMesh.CombineMeshInfos(frame, BCMesh.GenerateGenericUVOffsetMeshInfo(closeSill, 3f, sillCross, new Vector2(0, bottomOfWindow), Vector3.up));

			// Moves the window to where it should be in the world space
			frame.MoveEntireMesh(startLocation);
			
			// Combine this into all the frames
			return frame;
		}

		public static MeshInfo AddMeshInfo(Vector3[] newOutline, MeshInfo meshInfo)
		{
			List<Vector3> vertices = new List<Vector3>();
			List<int> triangles = new List<int>();
			List<Vector2> uvs = new List<Vector2>();
			List<Vector4> tangents = new List<Vector4>();

			int currentTriangles = 0;
			if(meshInfo.Vertices != null)
			{
				currentTriangles = meshInfo.Vertices.Length;
				vertices.AddRange(meshInfo.Vertices);
			}
			if(meshInfo.Triangles != null)
				triangles.AddRange(meshInfo.Triangles);
			if(meshInfo.UVs != null)
				uvs.AddRange(meshInfo.UVs);
			if(meshInfo.Tangents != null)
				tangents.AddRange(meshInfo.Tangents);

			List<Vector3> newVertices = new List<Vector3>();
			List<int> newTriangles = new List<int>();

			Tess tessBuilder = new Tess();
			ContourVertex[] contour = new ContourVertex[5];

			for(int i = 0; i < newOutline.Length; i++)
				contour[i].Position = new Vec3(newOutline[i]);

			tessBuilder.AddContour(contour.ToArray<ContourVertex>());
			
			tessBuilder.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

			for(int i = 0; i < tessBuilder.Vertices.Length; i++)
				newVertices.Add(tessBuilder.Vertices[i].Position.ToVector3());

			for(int i = 0; i < tessBuilder.Elements.Length; i++)
				newTriangles.Add(tessBuilder.Elements[i]);

			vertices.AddRange(newVertices);
			for(int i = 0; i < newTriangles.Count; i++)
				triangles.Add(tessBuilder.Elements[i] + currentTriangles);

			uvs.AddRange(CreateUVTiles(newVertices.ToArray(), newTriangles.ToArray()));
			tangents.AddRange(CreateTangents(newVertices.ToArray(), new Vector4(1, 0, 0, 1)));


			return new MeshInfo(vertices, triangles, uvs, tangents);
		}


		public static Vector4[] CreateTangents(Vector3[] verts, Vector4 direction)
		{
			Vector4[] tangents = new Vector4[verts.Length];
			for(int i = 0; i < tangents.Length; i++)
				tangents[i] = direction;
			
			return tangents;
		}

		public static Vector3[] DoorOutline(DoorInfo door, Vector3 offset)
		{
			return DoorOutline(door.Start, door.End, door, offset);
		}

		public static Vector3[] DoorOutline(Vector3 start, Vector3 end, DoorInfo door, Vector3 offset)
		{
			float frameInset = 0.0f;
			Vector3 direction = (start - end).normalized;

			float height = 2;
			if(door.DoorType == DoorTypeEnum.TallOpen)
			{
				height = 2.5f;
				frameInset = 0.1f;
			}
			if(door.DoorType == DoorTypeEnum.SkinnyOpen)
			{
				height = 2.0f;
				frameInset = 0.1f;
			}
			if(door.DoorType == DoorTypeEnum.Closet)
			{
				height = 2.0f;
				frameInset = 0.1f;
			}

			if(door.DoorType == DoorTypeEnum.DoorToRoof)
			{
				height = 2.5f;
			}

			return new Vector3[4] { start + offset - direction * frameInset, 
				start + Vector3.up * height + offset - direction * frameInset , 
				end + Vector3.up * height + offset + direction * frameInset, 
				end + offset + direction * frameInset };
		}

		
		public static Vector3[] WindowOutline (Vector3 startPoint, Vector3 endPoint, WindowInfo windowInfo, Vector3 wallOffset)
		{
			return WindowOutline(startPoint, endPoint, windowInfo, wallOffset, 0.1f);
		}
		
		public static Vector3[] WindowOutline (Vector3 startPoint, Vector3 endPoint, WindowInfo windowInfo, Vector3 wallOffset, float inset)
		{
			float bottomHeight = windowInfo.BottomHeight;
			float topHeight = windowInfo.TopHeight;

			// Figure out the inside inset of the window
			float frameInset = inset;
			Vector3 direction = (endPoint - startPoint).normalized;
			
			// NOTE: These windows must travel in a counter clockwise direction to be tesselated correctly
			return new Vector3[5] { 
				startPoint + direction * frameInset + wallOffset + Vector3.up * bottomHeight, 
				endPoint - direction * frameInset + wallOffset + Vector3.up * bottomHeight, 
				endPoint -  direction * frameInset + wallOffset + Vector3.up * topHeight, 
				startPoint + direction * frameInset + wallOffset + Vector3.up * topHeight,
				startPoint + direction * frameInset + wallOffset + Vector3.up * bottomHeight
			};
		}

		public static Vector3[] WindowOutline (WindowInfo window, Vector3 offset)
		{
			return WindowOutline(window.Start, window.End, window, offset);
		}
		
		public static Vector3[] LocalWindowOutline(WindowInfo window)
		{
			float length = (window.End - window.Start).magnitude;
			Vector3 direction = (window.End - window.Start).normalized;
			
			Vector3[] outline = WindowOutline(Vector3.zero, direction * length, window, Vector3.zero);
			return new Vector3[4] { outline[0], outline[1], outline[2], outline[3] };
		}

		public static Vector3[] GlassPaneOutline(WindowInfo window, float glassThicknessOffset)
		{
			float length = (window.End - window.Start).magnitude;

			return WindowOutline(Vector3.zero, new Vector3(length, 0, 0), window, new Vector3(0, 0, glassThicknessOffset), 0);
		}

		public static Vector3[] BrokenGlassCutout(WindowInfo window, float glassThicknessOffset)
		{
			// Create a randomized smashed angle along the bottom

			List<Vector3> newOutline = new List<Vector3>();
			float windowLength = (window.Start - window.End).magnitude;

			float bottomHeight = window.BottomHeight;
			float topHeight = window.TopHeight;

			// Generates the bottom line
			newOutline.Add(new Vector3(0.2f, bottomHeight + 0.1f, glassThicknessOffset));

			for(float f = 0.2f; f < windowLength - 0.2f; f += Random.Range(0.1f, 0.2f))
			{
				float jaggedYRange = Random.Range(0.01f, 0.15f);
				newOutline.Add(new Vector3(0.2f + f, bottomHeight + jaggedYRange, glassThicknessOffset));
			}

			// Generates up the side of the window
			newOutline.Add(new Vector3(windowLength - 0.2f, bottomHeight + 0.1f, glassThicknessOffset));
			for(float f = bottomHeight + 0.1f; f < topHeight - 0.2f; f += Random.Range(0.1f, 0.2f))
			{
				float jagged = Random.Range(0.01f, 0.1f);
				newOutline.Add(new Vector3(windowLength - 0.1f - jagged, f, glassThicknessOffset));
			}

			// Generates the top line
			newOutline.Add(new Vector3(windowLength - 0.2f, topHeight - 0.1f, glassThicknessOffset));
			for(float f = windowLength - 0.3f; f > 0.3 ; f -= Random.Range(0.1f, 0.2f))
			{
				float jaggedYRange = Random.Range(0.01f, 0.15f);
				newOutline.Add(new Vector3(0.2f + f, topHeight - jaggedYRange, glassThicknessOffset));
			}

			// Down the other side
			newOutline.Add(new Vector3(0.2f, topHeight - 0.1f, glassThicknessOffset));
			for(float f = topHeight - 0.1f; f > bottomHeight + 0.2f; f -= Random.Range(0.1f, 0.2f))
			{
				float jagged = Random.Range(0.01f, 0.1f);
				newOutline.Add(new Vector3(0.1f + jagged, f, glassThicknessOffset));
			}

			return newOutline.ToArray<Vector3>();

		}

		public static List<Vector3> GetOrderedDoorOutlines(FloorBlueprint floorBp, int floor, Vector3 p1, Vector3 p2, float plane, Vector3 wallOffset)
		{
			List<Vector3> doorOutlines = new List<Vector3>();
			List<DoorInfo> unorderedDoors = new List<DoorInfo>();
			
			List<DoorInfo> doors = floorBp.Doors;

			if(p1.x == p2.x)
			{
				for(int i = 0; i < doors.Count; i++)
				{
					if(doors[i].Start.x != plane && doors[i].End.x != plane)
						continue;
					
					if(BCUtils.TestBetweenTwoPoints(doors[i].Start, p1, p2) == false
					   || BCUtils.TestBetweenTwoPoints(doors[i].End, p1, p2) == false)
						continue;
					
					unorderedDoors.Add(doors[i]);
				}
			}

			if(p1.z == p2.z)
			{
				for(int i = 0; i < doors.Count; i++)
				{
					if(doors[i].Start.z != plane && doors[i].End.z != plane)
						continue;
					
					if(BCUtils.TestBetweenTwoPoints(doors[i].Start, p1, p2) == false
					   || BCUtils.TestBetweenTwoPoints(doors[i].End, p1, p2) == false)
						continue;
					
					unorderedDoors.Add(doors[i]);
				}
			}

			
			if(unorderedDoors.Count < 1)
				return doorOutlines;
			
			if(unorderedDoors.Count > 0)
			{
				// For dealing with walls along the X axis
				if(p1.z == p2.z) 
				{
					// Deals with the wall going in a positive X direction
					if(p1.x > p2.x)
					{
						List<DoorInfo> orderedDoors = unorderedDoors.OrderBy(d => (d.Start.x + d.End.x)).ToList();
						
						foreach(var door in orderedDoors)
						{
							if(door.End.x < door.Start.x)
								doorOutlines.AddRange(DoorOutline(door.End, door.Start, door, wallOffset));
							else
								doorOutlines.AddRange(DoorOutline(door.Start, door.End, door, wallOffset));
						}
						
					}
					// Deals with the wall going in the negative direction
					else if(p1.x < p2.x)
					{
						List<DoorInfo> orderedDoors = unorderedDoors.OrderByDescending(d => (d.Start.x + d.End.x)).ToList();
						
						foreach(var door in orderedDoors)
						{
							if(door.End.x > door.Start.x)
								doorOutlines.AddRange(DoorOutline(door.End, door.Start, door, wallOffset));
							else
								doorOutlines.AddRange(DoorOutline(door.Start, door.End, door, wallOffset));
						}
					}
				}
				
				// For dealing with walls along the X axis
				if(p1.x == p2.x) 
				{
					// Deals with the wall going in a positive X direction
					if(p1.z > p2.z)
					{
						List<DoorInfo> orderedDoors = unorderedDoors.OrderBy(d => (d.Start.z + d.End.z)).ToList();
						
						foreach(var door in orderedDoors)
						{
							if(door.End.z < door.Start.z)
								doorOutlines.AddRange(DoorOutline(door.End, door.Start, door, wallOffset));
							else
								doorOutlines.AddRange(DoorOutline(door.Start, door.End, door, wallOffset));
						}
						
					}
					// Deals with the wall going in the negative direction
					else if(p1.z < p2.z)
					{
						List<DoorInfo> orderedDoors = unorderedDoors.OrderByDescending(d => (d.Start.z + d.End.z)).ToList();
						
						foreach(var door in orderedDoors)
						{
							if(door.End.z > door.Start.z)
								doorOutlines.AddRange(DoorOutline(door.End, door.Start, door, wallOffset));
							else
								doorOutlines.AddRange(DoorOutline(door.Start, door.End, door, wallOffset));
						}
					}
				}
			}
			
			for(int j = 0; j < doorOutlines.Count; j++)
			{
				doorOutlines[j] += Vector3.up * 3f * floor; 
			}
			
			return doorOutlines;
		}

		public static List<Vector3[]> GetWindowOutsideWallCutouts(FloorBlueprint floorBp, int floorLevel, Vector3 p1, Vector3 p2, Vector3 wallOffset)
		{
			List<Vector3[]> cutouts = new List<Vector3[]>();
			
			Vector3 floorHeight = floorLevel * Vector3.up * 3;
			
			List<WindowInfo> unorderedWindows = new List<WindowInfo>();
			
			List<WindowInfo> windows = floorBp.Windows;

			for(int i = 0; i < windows.Count; i++)
			{
				if(BCUtils.TestBetweenTwoPoints(windows[i].Start, p1, p2) == false
				   || BCUtils.TestBetweenTwoPoints(windows[i].End, p1, p2) == false)
					continue;
				
				unorderedWindows.Add(windows[i]);
			}

			for(int i = 0; i < unorderedWindows.Count; i++)
			{
				Vector3[] newWindow = BCMesh.WindowOutline(unorderedWindows[i], wallOffset);
				for(int n = 0; n < newWindow.Length; n++)
					newWindow[n] += floorHeight;
				cutouts.Add(newWindow);
			}
			
			return cutouts;
		}

		/// <summary>
		/// Takes in two points and figures out if between these two points there should be a filler wall
		/// </summary>
		/// <returns>The filler wall mesh.</returns>
		/// <param name="p1">P1.</param>
		/// <param name="p2">P2.</param>
		public static MeshInfo GenerateFillerWallMesh(Vector3 p1, Vector3 p2, Vector3 p1Outset, Vector3 p2Outset, FloorBlueprint floorBp, Vector3[] floorOutline, BuildingBlueprint buildingBp)
		{
			// Is the midpoint between the two of these guys on a party wall
			if(p1.x == p2.x && buildingBp.XPartyWalls.Contains(p1.x))
				return new MeshInfo();

			if(p1.z == p2.z && buildingBp.ZPartyWalls.Contains(p1.z))
				return new MeshInfo();

			// Is this wall is entirely within the floor outline, return false
			if(BCUtils.IsPointOnlyInsideARoom(p1, floorOutline) == true && BCUtils.IsPointOnlyInsideARoom(p2, floorOutline) == true)
				return new MeshInfo();

			// TODO: Analyze if I should still have this removed
			// Removed because edge cases were stopping filler placement
			// If all is on an edge and then 0.5 meters in it is inside a building, then do not produce a filler wall
//			{
//				Vector3 direction = (p2 - p1).normalized;
//
//				if(BCUtils.IsPointAlongAWall(p1, floorOutline) && BCUtils.IsPointOnlyInsideARoom(p1 + direction * 0.5f, floorOutline))
//					return new MeshInfo();
//
//				if(BCUtils.IsPointAlongAWall(p2, floorOutline) && BCUtils.IsPointOnlyInsideARoom(p2 - direction * 0.5f, floorOutline))
//					return new MeshInfo();
//			}

			bool foundOpening = false;

			// Are there no windows or doors between theset two vectors, then return
			for(int i = 0; i < floorBp.Doors.Count; i++)
			{
				DoorInfo doorInfo = floorBp.Doors[i];

				if(BCUtils.TestBetweenTwoPoints((doorInfo.Start + doorInfo.End) / 2, p1, p2))
				{
					foundOpening = true;
					break;
				}
			}
			
			for(int i = 0; i < floorBp.Windows.Count; i++)
			{
				WindowInfo windowInfo = floorBp.Windows[i];
				
				if(BCUtils.TestBetweenTwoPoints((windowInfo.Start + windowInfo.End) / 2, p1, p2))
				{
					foundOpening = true;
					break;
				}
			}

			// If there is no opening found, then no filler is needed
			if(foundOpening == false)
				return new MeshInfo();

			List<Vector3> tempLines = new List<Vector3>();
			
			float wallHeight = 3f;

			tempLines.AddRange( new Vector3[4] 
			{   
				p2Outset,
				p2Outset + Vector3.up * wallHeight, 
				p1Outset + Vector3.up * wallHeight, 
				p1Outset
			} );
			
			tempLines.Add(tempLines[0]);
			
			return BCMesh.GenerateGenericMeshInfo(tempLines.ToArray<Vector3>());
		}

		/// <summary>
		/// Takes in two points and figures out if between these two points there should be a filler wall
		/// </summary>
		/// <returns>The filler wall mesh.</returns>
		/// <param name="p1">P1.</param>
		/// <param name="p2">P2.</param>
		public static MeshInfo GenerateFillerWallMesh(Vector3 p1, Vector3 p2, FloorBlueprint floorBp, Vector3[] floorOutline, BuildingBlueprint buildingBp)
		{
			// Is the midpoint between the two of these guys on a party wall
			if(p1.x == p2.x && buildingBp.XPartyWalls.Contains(p1.x))
				return new MeshInfo();

			if(p1.z == p2.z && buildingBp.ZPartyWalls.Contains(p1.z))
				return new MeshInfo();

			// Is this wall is entirely within the floor outline, return false
			if(BCUtils.IsPointOnlyInsideARoom(p1, floorOutline) == true && BCUtils.IsPointOnlyInsideARoom(p2, floorOutline) == true)
				return new MeshInfo();

			// TODO: Analyze if I should still have this removed
			// Removed because edge cases were stopping filler placement
			// If all is on an edge and then 0.5 meters in it is inside a building, then do not produce a filler wall
			//			{
			//				Vector3 direction = (p2 - p1).normalized;
			//
			//				if(BCUtils.IsPointAlongAWall(p1, floorOutline) && BCUtils.IsPointOnlyInsideARoom(p1 + direction * 0.5f, floorOutline))
			//					return new MeshInfo();
			//
			//				if(BCUtils.IsPointAlongAWall(p2, floorOutline) && BCUtils.IsPointOnlyInsideARoom(p2 - direction * 0.5f, floorOutline))
			//					return new MeshInfo();
			//			}

			bool foundOpening = false;

			// Are there no windows or doors between theset two vectors, then return
			for(int i = 0; i < floorBp.Doors.Count; i++)
			{
				DoorInfo doorInfo = floorBp.Doors[i];

				if(BCUtils.TestBetweenTwoPoints((doorInfo.Start + doorInfo.End) / 2, p1, p2))
				{
					foundOpening = true;
					break;
				}
			}

			for(int i = 0; i < floorBp.Windows.Count; i++)
			{
				WindowInfo windowInfo = floorBp.Windows[i];

				if(BCUtils.TestBetweenTwoPoints((windowInfo.Start + windowInfo.End) / 2, p1, p2))
				{
					foundOpening = true;
					break;
				}
			}

			// If there is no opening found, then no filler is needed
			if(foundOpening == false)
				return new MeshInfo();

			List<Vector3> tempLines = new List<Vector3>();

			float wallHeight = 3f;

			tempLines.AddRange( new Vector3[4] 
			{   
				p1,
				p1 + Vector3.up * wallHeight, 
				p2 + Vector3.up * wallHeight, 
				p2 
			} );

			tempLines.Add(tempLines[0]);

			return BCMesh.GenerateGenericMeshInfo(tempLines.ToArray<Vector3>());
		}

		public static MeshInfo GenerateRoofWallMesh(Vector3 p1, Vector3 p2, Vector3 firstOffset, Vector3 secondOffset, FloorBlueprint floorBp, float roofExtraHeight, int roofFloor)
		{
			// The outside outline of the wall
			List<Vector3> tempLines = new List<Vector3>();

			tempLines.AddRange( new Vector3[4] 
			{   
				p1 + firstOffset * 0.1f,
				p1 + Vector3.up * roofExtraHeight + firstOffset * 0.1f, 
				p2 + Vector3.up * roofExtraHeight + secondOffset * 0.1f, 
				p2 + secondOffset * 0.1f 
			} );

			// Adds the doors in the proper order

			tempLines.Add(tempLines[0]);

			for(int i = 0; i < tempLines.Count; i++)
			{
				tempLines[i] += Vector3.up * 3f + Vector3.up * 3f * roofFloor;
			}

			// From the wall outline for this wall, build filled in triangles and add them to the wall triangles and vertices
			return BCMesh.GenerateGenericMeshInfo(tempLines.ToArray(), 3f);
		}

		/// <summary>
		/// Will find the outline of the floor. If currently floor has no rooms, it will generate an outline of the floor below with rooms
		/// </summary>
		/// <returns>The outline floor.</returns>
		/// <param name="buildingBp">Building bp.</param>
		/// <param name="floorIndex">Floor index.</param>
		public static Vector3[] GenerateOutlineFloor(BuildingBlueprint buildingBp, int floorIndex)
		{
			if(floorIndex < 0 || floorIndex >= buildingBp.Floors.Count)
			{
				Debug.LogError("The outline that is trying to be generated on building " + buildingBp.name + " and floor " + (floorIndex + 1) + "is not within the building");
				return null;
			}

			return GenerateOutlineFloor(buildingBp.Floors[floorIndex]);
		}

//		/// <summary>
//		/// RETURNS NULL: if the floor index is not valid
//		/// </summary>
//		/// <returns>The outlines for floor.</returns>
//		/// <param name="buildingBp">Building bp.</param>
//		/// <param name="floorIndex">Floor index.</param>
//		public static List<Vector3[]> GenerateOutlinesForFloor(BuildingBlueprint buildingBp, int floorIndex)
//		{
//			if(floorIndex < 0 || floorIndex >= buildingBp.Floors.Count)
//				return null;
//
//			return BCPaths.GetFloorOutline(buildingBp.Floors[floorIndex]);
//		}
//
//		public static List<Vector3[]> GenerateOutlineFloor(FloorBlueprint floorBp, bool newGen)
//		{
//			List<WallInformation[]> allWallInfos = new List<WallInformation[]>();
//			for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
//			{
//				allWallInfos.Add(floorBp.RoomBlueprints[i].GetWallInfos());
//			}
//
//			return BCFastWalls.UnitePaths(allWallInfos);
//		}

//		public static List<Vector3[]> GenerateOutlineFloor(FloorBlueprint floorBp, bool newGen)
//		{
//			List<WallInformation[]> allWallInfos = new List<WallInformation[]>();
//			for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
//			{
//				allWallInfos.Add(floorBp.RoomBlueprints[i].GetWallInfos());
//			}
//
//			return BCFastWalls.UnitePaths(allWallInfos);
//		}

		public static Vector3[] GenerateOutlineFloor(FloorBlueprint floorBp)
		{
			if(floorBp.RoomBlueprints.Count < 1)
				return null;

			// NOTE: This was the old ordinal way of combining all the floor outlines. Now do it fancy like.

			// Find a starting point that doesn't overlap with any other rooms
			
			int breaker = 0;
			int testRoomId = 0;
			int wallIndexId = -1;
			
			// Finds a starting point for the wall to enclose everything
			while(breaker < 100)
			{
				breaker++;
				
				// Take the current roomId and keep testing until we find a collision
				// Test this wall to see if it has any free edges
				
				// The room being tested again
				if(testRoomId >= floorBp.RoomBlueprints.Count || floorBp.RoomBlueprints.Count == 1)
				{
					wallIndexId = 0;
					break;
				}
				
				
				RoomBlueprint roomBp = floorBp.RoomBlueprints[testRoomId];
				
				for(int i = 0; i < roomBp.PerimeterWalls.Count; i++)
				{
					
					bool foundEmptyPoint = true;

					// returns the index of the first wall point that isn't touching another wall
					for(int testRoom = 0; testRoom < floorBp.RoomBlueprints.Count; testRoom++)
					{
						if(testRoom == testRoomId)
						{
							continue;
						}
						
						if(BCUtils.IsPointInARoom(roomBp.PerimeterWalls[i], floorBp.RoomBlueprints[testRoom]) == true)
						{
							foundEmptyPoint = false;
						}
					}
					
					if(foundEmptyPoint == true)
					{
						wallIndexId = i;
						break;
					}
					
				}
				
				if(wallIndexId > -1)
					break;
				
				testRoomId++;
			}
			
			
			// Start the new wall with a single point
			List<Vector3> newOutline = new List<Vector3>();

			if(testRoomId < 0)
				return new Vector3[0];

			newOutline.Add(floorBp.RoomBlueprints[testRoomId].PerimeterWalls[wallIndexId]);
			
			breaker = 0;
			float nextFloatPoint = 1;
			// Points are added when a condition is met
			
			while(breaker < 150)
			{
				breaker++;
				
				RoomBlueprint testRoom = floorBp.RoomBlueprints[testRoomId];
				
				for(int i = 0; i < testRoom.PerimeterWalls.Count - 1; i++)
				{
					bool breakout = false;
					
					// Make sure we go right around the entire thing
					int startIndex = wallIndexId + i;
					int nextIndex = wallIndexId + i + 1;
					
					if(startIndex == testRoom.PerimeterWalls.Count - 1)
					{
						startIndex = 0;
						nextIndex = 1;
					}
					else if(startIndex >= testRoom.PerimeterWalls.Count - 1)
					{
						startIndex = wallIndexId + i - testRoom.PerimeterWalls.Count + 1;
						nextIndex = wallIndexId + i + 1 - testRoom.PerimeterWalls.Count + 1;
					}
									
					float length = (testRoom.PerimeterWalls[nextIndex] - testRoom.PerimeterWalls[startIndex]).magnitude;
					Vector3 direction = (testRoom.PerimeterWalls[nextIndex] - testRoom.PerimeterWalls[startIndex]).normalized;

					for(float f = nextFloatPoint; f <= length; f += 1.0f)
					{
						Vector3 testPoint = testRoom.PerimeterWalls[startIndex] + f * direction;
						
						List<int> newRoomIndexes = BCUtils.RoomOverlapIndexes(testPoint, floorBp);
						int totalOverlapping = newRoomIndexes.Count;
						
						// At each stage, check for a few things
						// 1) Check to see if we are overlapping with any other rooms
						if(totalOverlapping < 1 || totalOverlapping > 3)
							Debug.LogError("Searching for walls has a major error");
						
						if(totalOverlapping == 1)
						{
							
							if(testPoint == testRoom.PerimeterWalls[nextIndex])
							{
								nextFloatPoint = 1;
								newOutline.Add(testPoint);
								if(newOutline[0] == newOutline[newOutline.Count - 1])
									breakout = true;
							}
							
							continue;
						}
						
						if(totalOverlapping == 2)
						{
							
							if(newRoomIndexes[0] != testRoomId)
								testRoomId = newRoomIndexes[0];
							else
								testRoomId = newRoomIndexes[1];
							
							newOutline.Add(testPoint);
							// We have to figure out what wall index we should start from
							wallIndexId = BCUtils.GetIndexOfWall(testPoint, floorBp.RoomBlueprints[testRoomId]);
							
							// Must now calculate the distance to the next positive point from the last point
							//						float newLength = floorBp.RoomBlueprints[testRoomId].PerimeterWalls[wallIndexId + 1]
							
							nextFloatPoint = BCUtils.FindDistanceToNextPoint(testPoint, wallIndexId, floorBp.RoomBlueprints[testRoomId]) + 1;
							
							breakout = true;
							break;
						}
						if(totalOverlapping == 3)
						{
							//						newOutline.Add(testPoint);`
							// Need to look in four directions and figure out which way is the proper way to go
							// Here we will fuck around a bit and check by half a meter
							// Check in all directions for indexes
							List<int> left = BCUtils.RoomOverlapIndexes(testPoint + Vector3.left * 0.5f, floorBp);
							List<int> right =  BCUtils.RoomOverlapIndexes(testPoint + Vector3.right * 0.5f, floorBp);
							List<int> forward = BCUtils.RoomOverlapIndexes(testPoint + Vector3.forward * 0.5f, floorBp);
							List<int> back = BCUtils.RoomOverlapIndexes(testPoint + Vector3.back * 0.5f, floorBp);

							if(left.Count == 1 && left[0] != testRoomId)
								testRoomId = left[0];
							else if(right.Count == 1 && right[0] != testRoomId)
							{
								testRoomId = right[0];
							}	
							
							
							else if(back.Count == 1 && back[0] != testRoomId)
								testRoomId = back[0];
							else if(forward.Count == 1 && forward[0] != testRoomId)
								testRoomId = forward[0];
							
							newOutline.Add(testPoint);
							nextFloatPoint = 1;
							wallIndexId = BCUtils.GetIndexOfWall(testPoint, floorBp.RoomBlueprints[testRoomId]);

							breakout =true;
							break;
							
						}
						if(breakout) break;
					}
					if(breakout) break;
				}
				
				if(newOutline[0] == newOutline[newOutline.Count - 1])
					break;
			}

			BCUtils.CollapseWallLines(newOutline);

			return newOutline.ToArray();
		}

		public static MeshInfo GenerateRoofMesh(BuildingBlueprint buildingBp, int floor, out Vector3[] output)
		{
			// Skip if the bottom floor since there should be no roof generated there
			List<Vector3> outputVectors = new List<Vector3>();

			Vector3[] floorBelowOutline = BCMesh.GenerateOutlineFloor(buildingBp.Floors[floor]);
			Vector3[] floorOutline = new Vector3[0];

			if(floor < buildingBp.Floors.Count - 1)
				floorOutline = BCMesh.GenerateOutlineFloor(buildingBp.Floors[floor + 1]);

			List<Vector3[]> roofVectors = BCMesh.GenerateDifferentVectors(floorBelowOutline, floorOutline);
			output = null;

			if(roofVectors == null)
				return new MeshInfo();

			for(int shape = 0; shape < roofVectors.Count; shape++)
			{
				Vector3[] roofPart = roofVectors[shape];

				for(int i = roofPart.Length - 1; i >= 0; i--)
				{
					Vector3 offset = BCUtils.GetOutsetFromManyRooms(roofPart[i], buildingBp.Floors[floor]);

					int prevPoint = i - 1;
					if(prevPoint < 0)
						prevPoint = roofPart.Length - 2;

					Vector3 firstPartyWallOffset;
					Vector3 secondPartyWallOffset;

					BCUtils.GetPartyWallOffset(buildingBp, buildingBp.Floors[floor], roofPart, i, out firstPartyWallOffset, out secondPartyWallOffset);

					roofPart[i] += (offset - firstPartyWallOffset) * 0.1f + Vector3.up * floor * 3  + Vector3.up * 3.5f; // 3.5 is the amount offset from this floor that it goes up

					outputVectors.Add(roofPart[i]);
				}
			}

			MeshInfo meshInfo = new MeshInfo();

			List<Vector3[]> cutout = new List<Vector3[]>();
			cutout.Add(floorOutline);
			if(roofVectors.Count == 1 && IsOutlineInsideAnotherOutline(floorOutline, floorBelowOutline) == true) // For cutouts that are entirely inside the roof
			{

				for(int i = 0; i < cutout[0].Length; i++)
				{
					cutout[0][i] += Vector3.up * floor * 3 + Vector3.up * 3.5f;
				}

				meshInfo = BCMesh.CombineMeshInfos(meshInfo, BCMesh.GenerateGenericMeshInfo(roofVectors[0], cutout));
			}
			else
			{
				for(int i = 0; i < roofVectors.Count; i++)
				{
					meshInfo = BCMesh.CombineMeshInfos(meshInfo, BCMesh.GenerateGenericMeshInfo(roofVectors[i]));
				}
			}

			output = outputVectors.ToArray<Vector3>();

			return meshInfo;
		}

		public static MeshInfo GenerateRoofMeshOld(BuildingBlueprint buildingBp, int floor, out Vector3[] output)
		{
			// Skip if the bottom floor since there should be no roof generated there
			List<Vector3> outputVectors = new List<Vector3>();

			Vector3[] floorBelowOutline = BCMesh.GenerateOutlineFloor(buildingBp.Floors[floor]);
			Vector3[] floorOutline = new Vector3[0];

			if(floor < buildingBp.Floors.Count - 1)
				floorOutline = BCMesh.GenerateOutlineFloor(buildingBp.Floors[floor + 1]);

			List<Vector3[]> roofVectors = BCMesh.GenerateDifferentVectors(floorBelowOutline, floorOutline);
			output = null;

			if(roofVectors == null)
				return new MeshInfo();

			for(int shape = 0; shape < roofVectors.Count; shape++)
			{
				Vector3[] roofPart = roofVectors[shape];
				
				for(int i = roofPart.Length - 1; i >= 0; i--)
				{
					Vector3 offset = BCUtils.GetOutsetFromManyRooms(roofPart[i], buildingBp.Floors[floor]);

					int prevPoint = i - 1;
					if(prevPoint < 0)
						prevPoint = roofPart.Length - 2;

					Vector3 firstPartyWallOffset;
					Vector3 secondPartyWallOffset;
					
					BCUtils.GetPartyWallOffset(buildingBp, buildingBp.Floors[floor], roofPart, i, out firstPartyWallOffset, out secondPartyWallOffset);

					roofPart[i] += (offset - firstPartyWallOffset) * 0.1f + Vector3.up * floor * 3  + Vector3.up * 3.5f; // 3.5 is the amount offset from this floor that it goes up

					outputVectors.Add(roofPart[i]);
				}
			}

			MeshInfo meshInfo = new MeshInfo();

			List<Vector3[]> cutout = new List<Vector3[]>();
			cutout.Add(floorOutline);
			if(roofVectors.Count == 1 && IsOutlineInsideAnotherOutline(floorOutline, floorBelowOutline) == true) // For cutouts that are entirely inside the roof
			{

				for(int i = 0; i < cutout[0].Length; i++)
				{
					cutout[0][i] += Vector3.up * floor * 3 + Vector3.up * 3.5f;
				}

				meshInfo = BCMesh.CombineMeshInfos(meshInfo, BCMesh.GenerateGenericMeshInfo(roofVectors[0], cutout));
			}
			else
			{
				for(int i = 0; i < roofVectors.Count; i++)
				{
					meshInfo = BCMesh.CombineMeshInfos(meshInfo, BCMesh.GenerateGenericMeshInfo(roofVectors[i]));
				}
			}

			output = outputVectors.ToArray<Vector3>();
					
			return meshInfo;
		}

		/// <summary>
		/// Generates the difference between the current floor and the floor below
		/// </summary>
		public static List<Vector3> GenerateRoofCutout(List<Vector3> thisFloor, Vector3[] lowerFloor)
		{
			List<Vector3> newRoofOutline = new List<Vector3>();

			bool insideFloorBelow = true;
			float startPoint = 0.5f;
			int wallIndexId = 2;
			
			// Go through this floor to find a start point within the lower floor
			for(int i = 0; i < thisFloor.Count; i++)
			{
				Vector3 testPoint = thisFloor[i];
				
				if(BCUtils.IsPointInARoom(testPoint, lowerFloor.ToList()))
					wallIndexId = i;
			}
			
			if(wallIndexId == -1)
				Debug.LogError("You don't have a starting point on the floor below");
			
			bool breakCompletely = false;
			
			int breaker = 0;
			
			while(breaker < 20)
			{
				breaker++;
				
				if(insideFloorBelow)
				{
					for(int i = 0; i < thisFloor.Count - 1; i++)
					{
						if(newRoofOutline.Count > 2 && newRoofOutline[0] == newRoofOutline[newRoofOutline.Count - 1])
						{
							breakCompletely = true;
							break;
						}
						
						int startIndex = wallIndexId + i;
						int nextIndex = wallIndexId + i + 1;
						
						if(startIndex == thisFloor.Count - 1)
						{
							startIndex = 0;
							nextIndex = 1;
						}
						else if(startIndex >= thisFloor.Count - 1)
						{
							startIndex = wallIndexId + i - thisFloor.Count + 1;
							nextIndex = wallIndexId + i + 1 - thisFloor.Count + 1;
						}
						
						Vector3 direction = (thisFloor[nextIndex] - thisFloor[startIndex]).normalized;
						float length = (thisFloor[nextIndex] - thisFloor[startIndex]).magnitude;
						
						for(float f = startPoint; f <= length; f += 0.5f)
						{
							if(newRoofOutline.Count > 2 && newRoofOutline[0] == newRoofOutline[newRoofOutline.Count - 1])
							{
								breakCompletely = true;
								break;
							}
							
							Vector3 testPoint = thisFloor[startIndex] + direction * f;
							
							// Resets the start point for the next go
							
							
							if(BCUtils.IsPointInARoom(testPoint, lowerFloor.ToList<Vector3>()) == false)
							{
								insideFloorBelow = false;
								// Figure out what point we crossed over
								Vector3 crossOverPoint = thisFloor[startIndex] + direction * (f - 0.5f);
								newRoofOutline.Add(crossOverPoint);
								wallIndexId = BCUtils.GetIndexOfWall(crossOverPoint, lowerFloor.ToList<Vector3>());
								startPoint = (crossOverPoint - lowerFloor[wallIndexId]).magnitude + 1;
								
								break;
							}
							
							if(f == length)
							{
								newRoofOutline.Add(testPoint);
								startPoint = 0.5f;
							}
							
							if(insideFloorBelow == false || breakCompletely) break;
						}
						if(insideFloorBelow == false || breakCompletely) break;
					}
				}
				
				if(breakCompletely) break;
				
				if(insideFloorBelow == false)
				{
					List<Vector3> firstFloorList = lowerFloor.ToList();
					
					for(int i = 0; i < firstFloorList.Count - 1; i++)
					{
						if(newRoofOutline.Count > 2 && newRoofOutline[0] == newRoofOutline[newRoofOutline.Count - 1])
						{
							breakCompletely = true;
							break;
						}
						
						int startIndex = wallIndexId + i;
						int nextIndex = wallIndexId + i + 1;
						
						if(startIndex == firstFloorList.Count - 1)
						{
							startIndex = 0;
							nextIndex = 1;
						}
						else if(startIndex >= firstFloorList.Count - 1)
						{
							startIndex = wallIndexId + i - firstFloorList.Count + 1;
							nextIndex = wallIndexId + i + 1 - firstFloorList.Count + 1;
						}
						
						Vector3 direction = (firstFloorList[nextIndex] - firstFloorList[startIndex]).normalized;
						float length = (firstFloorList[nextIndex] - firstFloorList[startIndex]).magnitude;
						
						for(float f = startPoint; f <= length; f += 0.5f)
						{
							if(newRoofOutline.Count > 2 && newRoofOutline[0] == newRoofOutline[newRoofOutline.Count - 1])
							{
								breakCompletely = true;
								break;
							}
							
							Vector3 testPoint = firstFloorList[startIndex] + direction * f;
							
							if(BCUtils.IsPointAlongAWall(testPoint, thisFloor) == true)
							{
								
								insideFloorBelow = true;
								
								Vector3 crossOverPoint = firstFloorList[startIndex] + direction * (f);
								newRoofOutline.Add(crossOverPoint);
								
								wallIndexId = BCUtils.GetIndexOfWall(crossOverPoint, thisFloor.ToList<Vector3>());
								startPoint = (crossOverPoint - thisFloor[wallIndexId]).magnitude + 0.5f;
								newRoofOutline.Add(testPoint);
								
								break;
								
							}
							
							if(f == length)
							{
								newRoofOutline.Add(testPoint);
								startPoint = 0.5f;
							}
							
							if(insideFloorBelow == true) break;
						}
						
						if(insideFloorBelow == true) break;
					}
				}
				if(breakCompletely) break;
			}

			return newRoofOutline;
		}

		public static GameObject GenerateRoomFiller(RoomBlueprint roomBp, FloorBlueprint floorBp, int floorIndex, GameObject parentRoom, BuildingBlueprint buildingBp)
		{
			// TODO - All this is on the ONLY ordinal generation system. We need to replace this with the new system

			Vector3[] floorOutline = BCMesh.GenerateOutlineFloor(floorBp);

			MeshInfo fillerMeshInfo = new MeshInfo();

			WallInformation[] wallInfos = BCGenerator.GenerateWallInfosForMeshGeneration(roomBp.PerimeterWalls.ToArray<Vector3>(), null, floorBp, -0.1f);

			for(int i = wallInfos.Length - 1; i >= 0; i--)
			{
				fillerMeshInfo = BCMesh.CombineMeshInfos(fillerMeshInfo, BCMesh.GenerateFillerWallMesh(wallInfos[i].Start, wallInfos[i].End, 
					wallInfos[i].StartOffset, wallInfos[i].EndOffset,
					floorBp, floorOutline, buildingBp));
			}

			// Do NOT make a LOD GO if there is no valid mesh to be had
			if(fillerMeshInfo.IsValid == false)
				return null;

			Vector3 zerodParentPos = parentRoom.transform.position;
			zerodParentPos.y = 0;

			UnityEngine.Mesh fillerMesh = BCMesh.GetMeshFromMeshInfo(fillerMeshInfo, zerodParentPos);
			fillerMesh.name = "ProceduralFillerMesh";

			GameObject roomFiller = BCMesh.GenerateEmptyGameObject("Create Building Filler", true);
			roomFiller.name = "BuildingFiller";

			MeshFilter meshFilter = roomFiller.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = roomFiller.AddComponent<MeshRenderer>();
			meshRenderer.material = Resources.Load("FillerGlass") as Material;

			meshFilter.mesh = fillerMesh;

			roomFiller.transform.SetParent(parentRoom.transform);
			roomFiller.transform.localPosition = Vector3.zero;

			return roomFiller;
			// TODO: Add a seperate door mesh that is generated so the doors don't disappear
		}


		public static bool IsOutlineInsideAnotherOutline(Vector3[] insideOutline, Vector3[] outsideLine, float accuracy = 0.5f)
		{
			if(accuracy > 0.75)
				accuracy = 0.5f;

			if(insideOutline == null || outsideLine == null)
				return false;

			// Go through every point in the inside poly and test if it is inside
			for(int i = 0; i < insideOutline.Length - 1; i++)
			{
				int thisPoint = i;
				int nextPoint = i + 1;

				Vector3 pointStart = insideOutline[thisPoint];
				float length = (insideOutline[thisPoint] - insideOutline[nextPoint]).magnitude;
				Vector3 direction = (insideOutline[nextPoint] - insideOutline[thisPoint]).normalized;

				for(float f = 0.0f; f <= length; f += accuracy)
				{
					f = (float)System.Math.Round(f, 3); // Corrects any rounding floating point problems
					Vector3 testPoint = pointStart + direction * f; // The test point along the line

					if(BCUtils.IsPointOnlyInsideARoom(testPoint, outsideLine) == false)
						return false;
				}

			}

			return true;
		}

		// <summary>
		// Generates new cutouts of the difference between the two vectors
		// </summary>
		public static List<Vector3[]> GenerateDifferentVectors(Vector3[] subject, Vector3[] clipper)
		{
			// TODO - Need to refactor this to generate difference vectors without cardinal lines.
			if(subject == null || subject.Length == 0)
				return null;

			List<Vector3[]> vectorCutouts = new List<Vector3[]>();

			// If the clipper is empty, return just the subject
			if(clipper == null || clipper.Length == 0)
			{
				vectorCutouts.Add(subject);
				return vectorCutouts;
			}

			// Check if the clipper is entirely inside of the other object

			// Three steps that keep getting repeated
			// 1. Find a spot where the subject hasn't been tested yet AND it isn't inside the clipper
			// 2. Go along subject until it colliders
			// 3. Go counterclockwise along the clipper till we find the subject again.
			// 4. Go along the subject till the are is closed

			int breakerIndex = 0;

			while(breakerIndex < 20)
			{
				int subjectIndex = -1; // Used to tell what index the point is on
				float subjectTestPoint = 0; // Used for the distance from the subject index
				
				int clipperIndex = -1; // Where on the clipper point is this on
				float clipperTestPoint = 0; // Where along the line is this clipper point on

				breakerIndex++;

				// A single cutout
				List<Vector3> cutoutPoly = new List<Vector3>();

				bool breakStartFinder = false;

				// First test for the first open point against the clipper
				for(int pointIndex = 0; pointIndex < subject.Length - 1 ; pointIndex++)
				{
					Vector3 pointStart = subject[pointIndex];

					// First find if the starting point is outside the clipper, then we don't need to go searching
					if(BCUtils.IsPointInARoom(pointStart, clipper) == false && IsPointWithinListOfWalls(pointStart, vectorCutouts) == false)
					{
						subjectIndex = pointIndex;
						subjectTestPoint = 0;
						cutoutPoly.Add(pointStart);
						breakStartFinder = true;
						break;
					}

					int nextPoint = pointIndex + 1;

					float length = (subject[pointIndex] - subject[nextPoint]).magnitude;
					Vector3 direction = (subject[nextPoint] - subject[pointIndex]).normalized;

					for(float f = 0.0f; f < length; f += 0.5f)
					{
						Vector3 testPoint = pointStart + direction * f; // The test point along the line

						if(BCUtils.IsPointInARoom(testPoint, clipper) == false && IsPointWithinListOfWalls(testPoint, vectorCutouts) == false)
						{
							// We now break out of everything because we found a point that is not inside the clipper wall
							// AND hasn't been used up when testing this wall

							subjectIndex = pointIndex;
							subjectTestPoint = f;
							breakStartFinder = true;

							Vector3 successPoint = testPoint;
							if(f > 0) // Ensures that the start point is on a corner
								successPoint -= direction * 0.5f;

							cutoutPoly.Add(successPoint); // Adds the start point
							break;
						}
					}


					if(breakStartFinder) break;
				}

				if(breakStartFinder == false) // If no start points are found, this is done and break out of the search
					break;

				int subBreakerIndex = 0;

				// This while loop is to get the full shape of the cutout
				while(subBreakerIndex < 20)
				{
					subBreakerIndex++;
					bool completedLoop = false;
					bool breakMidFinder = false;

					// Next we get into the meat of it, and start testing to find the cross point between the subject and the clipper
					for(int pointIndex = 0; pointIndex < subject.Length - 1; pointIndex++)
					{
						// Have to do some screwy things here so that the subject can go past its start point
						int startIndex = -1;
						int nextIndex = -1;

						// Grabs this point and next point depending on the start point
						BCUtils.GetNextPointsInWall(pointIndex + subjectIndex, subject, out startIndex, out nextIndex);

						// Now we can get onto testing each point
						float length = (subject[startIndex] - subject[nextIndex]).magnitude; // This is the distance to the next length
						Vector3 direction = (subject[nextIndex] - subject[startIndex]).normalized;
						Vector3 pointStart = subject[startIndex];

						// If we find a cross over, then we create a point as a corner and travel backwards on the clipper path
						for(float f = subjectTestPoint + 0.5f; f <= length; f += 0.5f)
						{
							Vector3 testPoint = pointStart + direction * f; // The test point along the line
	//						CreateTestPointCube(testPoint, 2f);

							if(testPoint == cutoutPoly[0] && cutoutPoly.Count > 2)
							{
								cutoutPoly.Add(testPoint);
								completedLoop = true;
								break;
							}

							if(BCUtils.IsPointInARoom(testPoint, clipper) == true)
							{
								// We now break out of everything because we found a point that IS inside the clipper wall
								// AND hasn't been used up when testing this wall

	//							CreateTestPointCube(testPoint, 1f);

								subjectIndex = startIndex;
								subjectTestPoint = f;
								breakMidFinder = true;

								// find where and what point the collision happened on the clipper
								// A bit complex since we are going counterclockwise on the clipper
								clipperIndex = BCUtils.GetIndexOfWall(testPoint, clipper);
								// Finds the distance between the test point and the clipper point
								clipperTestPoint = (testPoint - clipper[clipperIndex]).magnitude;
								if(clipperTestPoint < 0)
									clipperTestPoint = 0;
								cutoutPoly.Add(testPoint);

								break;
							}

							// At the end of a line, we have to do some special checks
							if(f == length)
							{
								cutoutPoly.Add(testPoint);
								subjectTestPoint = 0;
							}

							// If we find a corner without running into the clipper, then we add a corner point and reset everything

						}

						if(breakMidFinder || completedLoop) 
						{
							break;
						}
					}

					if(completedLoop == true)
						break;


					bool breakClipperFinder = false;


					// At this point, we now have to go backwards on the cutout to find where it collides with the subject
					for(int pointIndex = clipper.Length - 1; pointIndex >= 0; pointIndex--) // going counterclockwise here
					{
						int startIndex = -1;
						int nextIndex = -1;
						
						// Grabs this point and next point depending on the start point
						BCUtils.GetNextPointsInWall(pointIndex + clipperIndex, clipper, out startIndex, out nextIndex);

						Vector3 direction = (clipper[nextIndex] - clipper[startIndex]).normalized;
						Vector3 pointStart = clipper[startIndex];

						// Decreasing away to go the other way
						for(float f = clipperTestPoint; f >= 0; f -= 0.5f)
						{
							Vector3 testPoint = pointStart + direction * f; // The test point along the line

							if(testPoint == cutoutPoly[0] && cutoutPoly.Count > 2)
							{
								cutoutPoly.Add(testPoint);
								completedLoop = true;
								break;
							}

							// If we find the end of a line, we may have to set up the right subject
							if(BCUtils.IsPointInARoom(testPoint, subject) == false)
							{
								Vector3 crossPoint = testPoint + direction * 0.5f;

								clipperIndex = -1;
								clipperTestPoint = 0;
								
								subjectIndex = BCUtils.GetIndexOfWall(crossPoint, subject); // Finds where the collision was
								subjectTestPoint = (crossPoint - subject[subjectIndex]).magnitude;
								
								cutoutPoly.Add(crossPoint);
								breakClipperFinder = true;
								break;
							}

							// EDGE CASE TRAVELLING ALONG A SURFACE WALL
							// CHECK in front of the current point by the direction etc
							// and then see if it is still following along. If so, break out
							if(BCUtils.IsPointAlongAWall(testPoint, subject) && BCUtils.IsPointAlongAWall(testPoint, clipper))
							{
								Vector3 pointahead = testPoint - direction * 0.5f; // The next point in the wall to be tested for
								if(BCUtils.IsPointAlongAWall(pointahead, subject) && BCUtils.IsPointAlongAWall(pointahead, clipper))
								{
									clipperIndex = -1;
									clipperTestPoint = 0;

									subjectIndex = BCUtils.GetIndexOfWall(testPoint, subject);
									subjectTestPoint = (testPoint - subject[subjectIndex]).magnitude;

									cutoutPoly.Add(testPoint);

									breakClipperFinder = true;
									break;
								}
							}

							// THE END OF A CLIPPER LINE HAS LOTS OF EDGE CASES
							// 1. If the clipper ends on a T with both left and right having the subject
							if(f == 0)
							{
								Vector3 left = (pointStart + direction * f) - (new Vector3(direction.z, direction.y, direction.x)) * 0.1f;
								Vector3 right = (pointStart + direction * f) - (new Vector3(-direction.z, direction.y, -direction.x)) * 0.1f;

								// If both the left and right ways are on the subject, then do something
								if(BCUtils.IsPointAlongAWall(left, subject) && BCUtils.IsPointAlongAWall(right, subject))
								{
									// If we find either side has a collision we flip back onto the subject
									if(BCUtils.IsPointAlongAWall(left, clipper) || BCUtils.IsPointAlongAWall(right, clipper))
									{

										clipperIndex = -1;
										clipperTestPoint = 0;
										
										subjectIndex = BCUtils.GetIndexOfWall(testPoint, subject); // Finds where the collision was
										subjectTestPoint = (testPoint - subject[subjectIndex]).magnitude;

										cutoutPoly.Add(testPoint);
										breakClipperFinder = true;
										break;
									}
								}

								int i = -1;
								int iPrev = -1;

								cutoutPoly.Add(testPoint);
								BCUtils.GetPreviousPointsInWall(startIndex, clipper, out i, out iPrev);
								clipperTestPoint = (clipper[iPrev] - clipper[i]).magnitude; // Resets the clipper test point to be the full length

							}


						}

						if(breakClipperFinder || completedLoop) 
						{
							break;
						}
					}

					if(completedLoop == true)
						break;
				}

				// Collapse any midpoints in the provided thing
				BCUtils.CollapseWallLines(cutoutPoly);

				vectorCutouts.Add(cutoutPoly.ToArray<Vector3>());
			}
			return vectorCutouts;

		}


		public static bool IsPointWithinListOfWalls(Vector3 point, List<Vector3[]> walls)
		{
			// Check this point start to see if it is in any of the cutouts
			for(int i = 0; i < walls.Count; i++)
			{
				if(BCUtils.IsPointInARoom(point, walls[i]) == true)
				{
					return true;
				}
			}
			return false;
		}


		/// <summary>
		/// Modifies the lowerfloor to be bigger or smaller depending on the offset
		/// </summary>
		/// <param name="lowerFloor">Lower floor.</param>
		/// <param name="offset">Offset.</param>
		public static Vector3[] ExpandPointsIn (Vector3[] points, float offsetAmount)
		{
			Vector3[] newPoints = new Vector3[points.Length];

			for(int i = 0; i < points.Length - 1; i++)
			{
				Vector3 newOffset = BCUtils.Get8InsetDirection(points[i], points[i + 1], points);
				newPoints[i] = points[i] + newOffset * offsetAmount;
			}

			// Ensure the who loop is good
			newPoints[newPoints.Length - 1] = newPoints[0];

			return newPoints;
		}

		public static MeshInfo GenerateOverhangs(BuildingBlueprint buildingBp, int overhangFloor)
		{
			Vector3[] floorOverhang = BCMesh.GenerateOutlineFloor(buildingBp.Floors[overhangFloor]);
			Vector3[] underFloor = BCMesh.GenerateOutlineFloor(buildingBp.Floors[overhangFloor - 1]);

			// Have to ensure that the outlines are going the opposite way
			if(BCUtils.IsClockwisePolygon(floorOverhang) == false)
			{
				Debug.Log("Major problem here");
				floorOverhang = floorOverhang.Reverse().ToArray<Vector3>();
			}

			if(BCUtils.IsClockwisePolygon(underFloor) == false)
				Debug.Log("Major problem here");


			List<Vector3[]> overHangPolys = BCMesh.GenerateDifferentVectors(floorOverhang, underFloor);

			for(int shape = 0; shape < overHangPolys.Count; shape++)
			{
				Vector3[] overhang = overHangPolys[shape];

				for(int i = overhang.Length - 1; i >= 0; i--)
				{
					Vector3 offset = BCUtils.GetOutsetFromManyRooms(overhang[i], buildingBp.Floors[overhangFloor]);
					
					int prevPoint = i - 1;
					if(prevPoint < 0)
						prevPoint = overhang.Length - 2;
					
					Vector3 firstPartyWallOffset;
					Vector3 secondPartyWallOffset;
					
					BCUtils.GetPartyWallOffset(buildingBp, buildingBp.Floors[overhangFloor], overhang, i, out firstPartyWallOffset, out secondPartyWallOffset);
					
					overhang[i] += (offset - firstPartyWallOffset) * 0.1f + Vector3.up * overhangFloor * 3;
				}
			}

			MeshInfo meshInfo = new MeshInfo();

			List<Vector3[]> cutouts = new List<Vector3[]>();
			Vector3[] newUnderfloor = underFloor.ToArray<Vector3>();
			for(int i = 0; i < newUnderfloor.Length; i++)
				newUnderfloor[i] += Vector3.up * overhangFloor * 3f;

			if(overHangPolys.Count == 1 && BCUtils.IsPolygonEntirelyInsideOtherPolygoneXZ(underFloor, floorOverhang)) // If we have just one overhang entirely surrounding the floor below, we do something special
			{			
				Vector3[] reversedOverhangs = overHangPolys[0].Reverse().ToArray<Vector3>();

				cutouts.Add(newUnderfloor);

				MeshInfo overhangMesh = BCMesh.GenerateGenericMeshInfo(reversedOverhangs, new List<Vector3[]>()
				                                                       { newUnderfloor } );

				meshInfo = BCMesh.CombineMeshInfos(meshInfo, overhangMesh);
			}

			for(int i = 0; i < overHangPolys.Count; i++)
			{
				// Must reverse to get the right direction
				Vector3[] reversedOverhangs = overHangPolys[i].Reverse().ToArray<Vector3>();

				meshInfo = BCMesh.CombineMeshInfos(meshInfo, BCMesh.GenerateGenericMeshInfo(reversedOverhangs, cutouts));
			}
			return meshInfo;
		}

		public static MeshInfo ChangeSubmeshIndex (MeshInfo meshInfo, int submesh)
		{
			if(meshInfo.IsValid == false)
				return meshInfo;

			return new MeshInfo(meshInfo.Vertices.ToList<Vector3>(), meshInfo.Triangles.ToList<int>(), meshInfo.UVs.ToList<Vector2>(), meshInfo.Tangents.ToList<Vector4>(), submesh);
		}

		/// <summary>
		/// Recalculates the tangents based on mesh information
		/// </summary>
		/// <param name="mesh">Reference Mesh, class object will be updated</param>
		public static void CalculateMeshTangents(UnityEngine.Mesh mesh)
		{
			if(mesh == null || mesh.vertexCount == 0 || mesh.triangles == null || mesh.triangles.Length < 1)
				return;

			//speed up math by copying the mesh arrays
			int[] triangles = mesh.triangles;
			Vector3[] vertices = mesh.vertices;
			Vector2[] uv = mesh.uv;
			Vector3[] normals = mesh.normals;
			
			//variable definitions
			int triangleCount = triangles.Length;
			int vertexCount = vertices.Length;
			
			Vector3[] tan1 = new Vector3[vertexCount];
			Vector3[] tan2 = new Vector3[vertexCount];
			
			Vector4[] tangents = new Vector4[vertexCount];
			
			for (long a = 0; a < triangleCount; a += 3)
			{
				long i1 = triangles[a + 0];
				long i2 = triangles[a + 1];
				long i3 = triangles[a + 2];
				
				Vector3 v1 = vertices[i1];
				Vector3 v2 = vertices[i2];
				Vector3 v3 = vertices[i3];

				Vector2 w1 = uv[i1];
				Vector2 w2 = uv[i2];
				Vector2 w3 = uv[i3];
				
				float x1 = v2.x - v1.x;
				float x2 = v3.x - v1.x;
				float y1 = v2.y - v1.y;
				float y2 = v3.y - v1.y;
				float z1 = v2.z - v1.z;
				float z2 = v3.z - v1.z;
				
				float s1 = w2.x - w1.x;
				float s2 = w3.x - w1.x;
				float t1 = w2.y - w1.y;
				float t2 = w3.y - w1.y;
				
				float r = 1.0f / (s1 * t2 - s2 * t1);
				
				Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
				Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);
				
				tan1[i1] += sdir;
				tan1[i2] += sdir;
				tan1[i3] += sdir;
				
				tan2[i1] += tdir;
				tan2[i2] += tdir;
				tan2[i3] += tdir;
			}
			
			
			for (long a = 0; a < vertexCount; ++a)
			{
				Vector3 n = normals[a];
				Vector3 t = tan1[a];
				
	//			Vector3 tmp = (t - n * Vector3.Dot(n, t)).normalized;
	//			tangents[a] = new Vector4(tmp.x, tmp.y, tmp.z);
				Vector3.OrthoNormalize(ref n, ref t);
				tangents[a].x = t.x;
				tangents[a].y = t.y;
				tangents[a].z = t.z;
				
				tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
	//			tangents[a].w = -1f;
			}
			
			mesh.tangents = tangents;
		}

		public static GameObject BuildWindowGameObject(WindowInfo windowInfo, Material glassPaneMaterial, bool generateBrokenGlass, GameObject windowPaneHolder = null)
		{
			GameObject windowGameObject;
			
			// Creates a window from the template provided
			if(windowPaneHolder != null)
			{

				windowGameObject = GameObject.Instantiate(windowPaneHolder);
#if UNITY_EDITOR
				UnityEditor.Undo.RegisterCreatedObjectUndo(windowGameObject, "Create Window Pane Holder");
#endif
			}
			else
				windowGameObject = BCMesh.GenerateEmptyGameObject("Create Window Pane Holder, false");
			
			windowGameObject.name = "WindowPane";
			windowGameObject.transform.position = Vector3.zero;
			
			if(generateBrokenGlass)
			{
				List<MeshInfo> meshInfos = new List<MeshInfo>();
				
				// Returns a window outline without the regular inset it usually has
				Vector3[] windowOutline = BCMesh.GlassPaneOutline(windowInfo, 0.02f);
				List<Vector3[]> brokenCutout = new List<Vector3[]>();
				brokenCutout.Add(BCMesh.BrokenGlassCutout(windowInfo, 0.02f));
				meshInfos.Add(BCMesh.GenerateGenericMeshInfo(windowOutline, brokenCutout));
				
				Vector3[] windowOutlineBack = BCMesh.GlassPaneOutline(windowInfo, -0.02f);
				windowOutlineBack = windowOutlineBack.Reverse().ToArray<Vector3>();
				for(int n = 0; n < brokenCutout[0].Length; n++) // offsets the back part of the glass to the correct distance
					brokenCutout[0][n] -= new Vector3(0, 0, 0.04f);
				meshInfos.Add(BCMesh.GenerateGenericMeshInfo(windowOutlineBack, brokenCutout));
				
				GameObject brokenWindow = BCMesh.GenerateEmptyGameObject("Create Broken Window", true);
				brokenWindow.name = "brokenWindowMesh";
				brokenWindow.transform.SetParent(windowGameObject.transform);
				MeshRenderer meshRenderer = brokenWindow.AddComponent<MeshRenderer>();
				MeshFilter meshFilter = brokenWindow.AddComponent<MeshFilter>();
				
				UnityMesh mesh = BCMesh.GetMeshFromMeshInfo(meshInfos);
				mesh.name = "Procedural Broken Glass Window";
				BCMesh.CalculateMeshTangents(mesh);
				
				meshFilter.mesh = mesh;
				meshRenderer.material = glassPaneMaterial;
				
				brokenWindow.SetActive(false);
			}
			
			{
				List<MeshInfo> meshInfos = new List<MeshInfo>();
				
				// Returns a window outline without the regular inset it usually has
				Vector3[] windowOutline = BCMesh.GlassPaneOutline(windowInfo, 0.02f);
				meshInfos.Add(BCMesh.GenerateGenericMeshInfo(windowOutline));
				
				Vector3[] windowOutlineBack = BCMesh.GlassPaneOutline(windowInfo, -0.02f);
				windowOutlineBack = windowOutlineBack.Reverse().ToArray<Vector3>();
				meshInfos.Add(BCMesh.GenerateGenericMeshInfo(windowOutlineBack));
				
				GameObject fullWindow = BCMesh.GenerateEmptyGameObject("Create Broken Window", true);
				fullWindow.name = "FullWindowMesh";
				fullWindow.transform.SetParent(windowGameObject.transform);
				MeshRenderer meshRenderer = fullWindow.AddComponent<MeshRenderer>();
				MeshFilter meshFilter = fullWindow.AddComponent<MeshFilter>();
				
				UnityMesh mesh = BCMesh.GetMeshFromMeshInfo(meshInfos);
				mesh.name = "Procedural Glass Window";
				BCMesh.CalculateMeshTangents(mesh);
				
				meshFilter.mesh = mesh;
				meshRenderer.material = glassPaneMaterial;
			}

			return windowGameObject;
		}

		#region Newly Organized Stuff

		/// <summary>
		/// Will generate any flat wall on any angle
		/// </summary>
		/// <returns>The wall mesh.</returns>
		/// <param name="p1">P1.</param>
		/// <param name="p2">P2.</param>
		/// <param name="firstOffset">First offset.</param>
		/// <param name="secondOffset">Second offset.</param>
		/// <param name="floorHeight">Floor height.</param>
		/// <param name="wallHeight">Wall height.</param>
		/// <param name="wallInset">Wall inset.</param>
		/// <param name="submesh">Submesh.</param>
		/// <param name="doorCutouts">Door cutouts.</param>
		/// <param name="windowCutouts">Window cutouts.</param>
		/// <param name="reverseWallDirection">If set to <c>true</c> reverse wall direction.</param>
		public static MeshInfo GeneratePreviewWall(
			Vector3 p1, 
			Vector3 p2, 
			Vector3 firstOffset, 
			Vector3 secondOffset, 
			float floorHeight,
			float wallHeight = 3f,
			float wallInset = 0.1f,
			int submesh = 0,
			bool reverseWallDirection = false
		)
		{
			// Create the outline for the wall for the ends and top
			List<Vector3> tempLines = new List<Vector3>();

			Vector3 directionOfWall = (p2 - p1).normalized;
			float lengthOfWall = (p1 - p2).magnitude;
			//			Vector3 crossProduct = Vector3.Cross(directionOfWall * -1, Vector3.up);

			tempLines.Add(p1 + firstOffset * wallInset);

			// Add the full wall outline that goes along the top
			tempLines.AddRange( new Vector3[4] 
			{
				p2 + secondOffset * wallInset,
				p2 + Vector3.up * wallHeight + secondOffset * wallInset, 
				p1 + Vector3.up * wallHeight + firstOffset * wallInset, 
				p1 + firstOffset * wallInset
			} );

			// Reversed to get the right direction for the Mesh info
			// TOOD - Check the direction to ensure the wall have normals facing the right way
			if(reverseWallDirection == false)
				tempLines.Reverse();

			List<Vector3[]> tempCutouts = null;

			return BCMesh.GenerateGenericMeshInfo(tempLines.ToArray<Vector3>(), tempCutouts, 3f, submesh);
		}

		public static MeshInfo FixOutOfBoundsUVs(MeshInfo meshInfo)
		{
			Vector2[] newUVs = meshInfo.UVs.ToArray<Vector2>();

			float uMax = float.MinValue;
			float uMin = float.MaxValue;

			float vMax = float.MinValue;
			float vMin = float.MaxValue;

			// Test to see if the UVs are out of bounds
			for(int i = 0; i < newUVs.Length; i++)
			{
				Vector2 currentUv = newUVs[i];

				if(currentUv.x > uMax)
					uMax = currentUv.x;
				if(currentUv.x < uMin)
					uMin = currentUv.x;
				
				if(currentUv.y > vMax)
					vMax = currentUv.y;
				if(currentUv.y < vMin)
					vMin = currentUv.y;
			}

			if(uMin < 0)
			{
				float uRange = Mathf.Abs(uMax - uMin);
				for(int i = 0; i < newUVs.Length; i++)
				{
					Vector2 newVector = newUVs[i];
					newVector.x += uRange;
					newUVs[i] = newVector;
				}
			}

			meshInfo.UVs = newUVs;

			return meshInfo;
		}

		#endregion
	}
}
