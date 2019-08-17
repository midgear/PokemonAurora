using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClipperLib;
using System.Linq;

namespace BuildingCrafter
{
	public static class BCTilingFloors 
	{
		static Clipper staticClippy;
		static List<List<IntPoint>> staticReturnedPoints = new List<List<IntPoint>>();

		/// <summary>
		/// USED TO GET TILES FOR FLOOR AND ROOF from an outline
		/// </summary>
		/// <returns>The floor tiles.</returns>
		/// <param name="path">Path.</param>
		public static List<Vector3[]> GetSquareTiles(Vector3[] path, List<Vector3[]> cutouts = null, PolyFillType polyfillType = PolyFillType.pftNonZero, float squareSize = 1)
		{
			Bounds bounds = new Bounds(path[0], Vector3.zero);
			for(int i = 0; i < path.Length; i++)
				bounds.Encapsulate(path[i]);

			Vector3 roundedCenter = new Vector3(Mathf.Round(bounds.center.x), 0, Mathf.Round(bounds.center.z));

			bounds.Encapsulate(roundedCenter + new Vector3(Mathf.Ceil(bounds.extents.x), 0, Mathf.Ceil(bounds.extents.z)));
			bounds.Encapsulate(roundedCenter - new Vector3(Mathf.Ceil(bounds.extents.x), 0, Mathf.Ceil(bounds.extents.z)));
			bounds.Encapsulate(roundedCenter + new Vector3(Mathf.Ceil(bounds.extents.x), 0, -1 * Mathf.Ceil(bounds.extents.z)));
			bounds.Encapsulate(roundedCenter - new Vector3(Mathf.Ceil(bounds.extents.x), 0, -1 * Mathf.Ceil(bounds.extents.z)));

			Vector3 lowerStart = bounds.center - bounds.extents;
			int width = Mathf.CeilToInt(bounds.size.x);
			int height = Mathf.CeilToInt(bounds.size.z);

			List<Vector3[]> newPath = new List<Vector3[]>();

			for(float x = 0; x < width; x += squareSize)	
			{
				for(float z = 0; z < height; z+= squareSize)
				{
					Vector3[] squarePath = new Vector3[5];
					squarePath[0] = (lowerStart + new Vector3(x, 0, z));
					squarePath[1] = (lowerStart + new Vector3(x, 0, z + squareSize));
					squarePath[2] = (lowerStart + new Vector3(x + squareSize, 0, z + squareSize));
					squarePath[3] = (lowerStart + new Vector3(x + squareSize, 0, z));
					squarePath[4] = (squarePath[0]);

					if(staticClippy == null)
						staticClippy = new Clipper();
					else
						staticClippy.Clear();

					if(staticClippy == null)
						staticReturnedPoints = new List<List<IntPoint>>();
					else
						staticReturnedPoints.Clear();

					staticClippy.AddPath(BCPaths.GetIntPoints(squarePath.ToArray<Vector3>()), PolyType.ptSubject, true);

					staticClippy.AddPath(BCPaths.GetIntPoints(path), PolyType.ptClip, true);
					if(cutouts != null)
					{
						for(int i = 0; i < cutouts.Count; i++)
							staticClippy.AddPath(BCPaths.GetIntPoints(cutouts[i]), PolyType.ptClip, true);
					}

					if(staticClippy.Execute(ClipType.ctIntersection, staticReturnedPoints, polyfillType))
					{
						for(int clipIndex = 0; clipIndex < staticReturnedPoints.Count; clipIndex++)
						{
							newPath.Add(BCPaths.GetPoints(staticReturnedPoints[clipIndex]));
						}
					}
				}
			} 
			return newPath;
		}

		public static Vector3 FindFirstSquareTileStart(List<Vector3[]> roomTiles, float tileSize = 1)
		{
			if(roomTiles == null || roomTiles.Count == 0 || roomTiles[0] == null || roomTiles[0].Length == 0)
				return Vector3.zero;
			
			for(int i = 0; i < roomTiles.Count; i++)
			{
				if(roomTiles[i].Length != 5)
					continue;
				
				Vector3[] square = roomTiles[i];

				Vector3 testCorner = square[0];
				Vector3 rhs = square[1];
				Vector3 lhs = square[3];

//				Utility.DrawLine(testCorner, rhs, Color.cyan, 4);
//				Utility.DrawLine(testCorner, lhs, Color.blue, 4);

				float rightDistance = (testCorner - rhs).sqrMagnitude;
				float leftDistance = (testCorner - lhs).sqrMagnitude;

				if(Mathf.Abs(rightDistance - leftDistance) < 0.00001f)
				{
					if(rightDistance - tileSize * tileSize < 0.00001f)
						return testCorner;
				}
			}

			// If we can't find a square tile, then we default to the smallest point in the bounds
			float smallestX = float.MaxValue;
			float smallestZ = float.MaxValue;

			// TODO: This is adding 20ms to generation time if the above messes up. Performance should be improved here
			for(int roomTileIndex = 0; roomTileIndex < roomTiles.Count; roomTileIndex++)
			{
				for(int i = 0; i < roomTiles[roomTileIndex].Length; i++)
				{
					if(smallestX > roomTiles[roomTileIndex][i].x)
						smallestX = roomTiles[roomTileIndex][i].x;

					if(smallestZ > roomTiles[roomTileIndex][i].z)
						smallestZ = roomTiles[roomTileIndex][i].z;
				}
			}

			// HACK since the tiles are all built on a 1x1 meter grid, we are rounding the starting point. But this isn't a solution to the problem of UV mapping these tiles.
			smallestX = Mathf.Round(smallestX);
			smallestZ = Mathf.Round(smallestZ);

			return new Vector3(smallestX, 0, smallestZ);
		}

		// Takes a closed square and generates a mesh info with two triangles in it
		/// <summary>
		/// ONLY USE FOR SQUARES. Always generates a 0, 0 -> 1, 1 UV tile
		/// </summary>
		/// <returns>The square tiles.</returns>
		/// <param name="outline">Outline.</param>
		public static MeshInfo GenerateSquareTiles(Vector3[] outline, Vector3 startUVPoint, bool isCeiling = false, float tileUVSize = 1)
		{
			// Have to handle the case where we have a tile that is all weirdly shaped.
			// Need to find an "anchor" point for the UV maps
			// Need to find out which way the tile should be positioned
			// Then put it all together and voila, working like a charm

			if(isCeiling == false)
				outline = outline.Reverse().ToArray<Vector3>();

			MeshInfo roofTile = new MeshInfo();

			if(outline.Length < 4 || outline.Length > 5)
				roofTile =  BCMesh.GenerateGenericMeshInfo(outline, tileUVSize);
			else
			{
				List<Vector3> vertices = new List<Vector3>(outline.Length - 1);
				List<int> triangles = new List<int>();
				List<Vector2> uvs = new List<Vector2>();
				List<Vector4> tangents = new List<Vector4>();

				vertices.Add(outline[0]);
				vertices.Add(outline[1]);
				vertices.Add(outline[2]);
				vertices.Add(outline[3]);

				float v1 = (outline[1] - outline[0]).magnitude / tileUVSize;
				float u1 = (outline[3] - outline[0]).magnitude / tileUVSize;

				Vector2 uv00 = new Vector2(0, 0);
				Vector2 uv01 = new Vector2(0, v1);
				Vector2 uv11 = new Vector2(u1, v1);
				Vector2 uv10 = new Vector2(u1, 0);

				uvs.Add(uv00);
				uvs.Add(uv01);
				uvs.Add(uv11);
				uvs.Add(uv10);

				triangles.Add(0);
				triangles.Add(1);
				triangles.Add(2);

				triangles.Add(3);
				triangles.Add(0);
				triangles.Add(2);

				// Generates junk UV's which are calculated when the mesh is generated
				tangents.AddRange(BCMesh.CreateTangents(vertices.ToArray(), new Vector4(0, 1, 0, 1)));

				// Adds all the newly generated verticies to the whole wall
				roofTile = new MeshInfo(vertices, triangles, uvs, tangents);
			}

			// Now we remap all the UVs based on the start UV point. This is important to get everything lined up well. UVs above don't matter
			if(isCeiling == false)
			{
				for(int i = 0; i < roofTile.Vertices.Length; i++)
				{
					// Assumes the verts are flat
					Vector3 vert = roofTile.Vertices[i];
					Vector2 uv = roofTile.UVs[i];

					// Find the UV sizing based on the distance from the start UV
					float uNew = vert.x - startUVPoint.x;
					float vNew = vert.z - startUVPoint.z;

					uNew = uNew / tileUVSize;
					vNew = vNew / tileUVSize;

					uv.x = uNew;
					uv.y = vNew;

					roofTile.UVs[i] = uv;
				}
			}
			else
			{
				for(int i = 0; i < roofTile.Vertices.Length; i++)
				{
					// Assumes the verts are flat
					Vector3 vert = roofTile.Vertices[i];
					Vector2 uv = roofTile.UVs[i];

					// Find the UV sizing based on the distance from the start UV
					float uNew = vert.x - startUVPoint.x;
					float vNew = vert.z - startUVPoint.z;

					uNew = uNew / tileUVSize;
					vNew = vNew / tileUVSize;

					uv.x = 1 - uNew;
					uv.y = vNew;

					roofTile.UVs[i] = uv;
				}
			}


//			return roofTile;

			return BCMesh.CleanUVsToNormalRange(roofTile);
		}
	}
}