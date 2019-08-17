using UnityEngine;
using System.Collections;

namespace BuildingCrafter
{
	public static partial class BCMesh
	{	
		private static Vector2[] CreateUVTiles(Vector3[] verts, int[] tris, float size = 1)
		{
			if(verts.Length < 1)
				return new Vector2[0];
			int lowestY = 0;
			
			for(int i = 0; i < verts.Length; i++)
			{
				if(verts[i].y < verts[lowestY].y)
					lowestY = i;
			}
			
			Vector3 anchorPoint = verts[lowestY];
			
			Vector2[] uv = new Vector2[verts.Length];
			
			// Test plain to see where the objects are
			bool xPlain = true;
			bool yPlain = true;
			bool zPlain = true;
			
			for(int i = 0; i < verts.Length; i++)
			{
				if(verts[i].x != verts[0].x)
				{
					xPlain = false;
				}
				if(verts[i].y != verts[0].y)
				{
					yPlain = false;
				}
				if(verts[i].z != verts[0].z)
				{
					zPlain = false;
				}
			}
			
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
				
				Vector3 p1Diff = (p1 - anchorPoint) / size;
				Vector3 p2Diff = (p2 - anchorPoint) / size;
				Vector3 p3Diff = (p3 - anchorPoint) / size;
				
				// Deals with walls facing the x direction
				if(zPlain && !xPlain && !yPlain)
				{
					uv[t1] = new Vector2(p1Diff.x, p1Diff.y);
					uv[t2] = new Vector2(p2Diff.x, p2Diff.y);
					uv[t3] = new Vector2(p3Diff.x, p3Diff.y);
					continue;
				}
				// Deals with walls facing the z direction
				if(xPlain && !zPlain && !yPlain)
				{
					uv[t1] = new Vector2(p1Diff.z, p1Diff.y);
					uv[t2] = new Vector2(p2Diff.z, p2Diff.y);
					uv[t3] = new Vector2(p3Diff.z, p3Diff.y);
					continue;
				}
				// Deals with the roof and floor. Any XZ planes
				if(yPlain && !xPlain && !zPlain)
				{
					uv[t1] = new Vector2(p1Diff.x, p1Diff.z);
					uv[t2] = new Vector2(p2Diff.x, p2Diff.z);
					uv[t3] = new Vector2(p3Diff.x, p3Diff.z);
					continue;
				}
				
				// Here we need to run on the wall the calculation to create the UV panels across the triangles surface
				Vector2[] nonPlaneUVs = MapNonPlanarUVTile(anchorPoint, p1, p2, p3, size);
				
				uv[t1] = nonPlaneUVs[0];
				uv[t2] = nonPlaneUVs[1];
				uv[t3] = nonPlaneUVs[2];
				
			}
			return uv;
		}

		private static Vector2[] CreateUVTilesFromLowerAnchor(Vector3[] verts, int[] tris, float size = 1)
		{
			if(verts.Length < 1)
				return new Vector2[0];

			Vector3 lowestAnchorPoint = Vector3.one * float.MaxValue;

			for(int i = 0; i < verts.Length; i++)
			{
				if(verts[i].x < lowestAnchorPoint.x)
					lowestAnchorPoint.x = verts[i].x;
				if(verts[i].y < lowestAnchorPoint.y)
					lowestAnchorPoint.y = verts[i].y;
				if(verts[i].z < lowestAnchorPoint.z)
					lowestAnchorPoint.z = verts[i].z;
			}

//			int lowestY = 0;
//
//			for(int i = 0; i < verts.Length; i++)
//			{
//				if(verts[i].y < verts[lowestY].y)
//					lowestY = i;
//			}
//			Vector3 anchorPoint = verts[lowestY];
			Vector3 anchorPoint = lowestAnchorPoint;

			Vector2[] uv = new Vector2[verts.Length];

			// Test plain to see where the objects are
			bool xPlain = true;
			bool yPlain = true;
			bool zPlain = true;

			for(int i = 0; i < verts.Length; i++)
			{
				if(verts[i].x != verts[0].x)
				{
					xPlain = false;
				}
				if(verts[i].y != verts[0].y)
				{
					yPlain = false;
				}
				if(verts[i].z != verts[0].z)
				{
					zPlain = false;
				}
			}

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

				Vector3 p1Diff = (p1 - anchorPoint) / size;
				Vector3 p2Diff = (p2 - anchorPoint) / size;
				Vector3 p3Diff = (p3 - anchorPoint) / size;

				// Deals with walls facing the x direction
				if(zPlain && !xPlain && !yPlain)
				{
					uv[t1] = new Vector2(p1Diff.x, p1Diff.y);
					uv[t2] = new Vector2(p2Diff.x, p2Diff.y);
					uv[t3] = new Vector2(p3Diff.x, p3Diff.y);
					continue;
				}
				// Deals with walls facing the z direction
				if(xPlain && !zPlain && !yPlain)
				{
					uv[t1] = new Vector2(p1Diff.z, p1Diff.y);
					uv[t2] = new Vector2(p2Diff.z, p2Diff.y);
					uv[t3] = new Vector2(p3Diff.z, p3Diff.y);
					continue;
				}
				// Deals with the roof and floor. Any XZ planes
				if(yPlain && !xPlain && !zPlain)
				{
					uv[t1] = new Vector2(p1Diff.x, p1Diff.z);
					uv[t2] = new Vector2(p2Diff.x, p2Diff.z);
					uv[t3] = new Vector2(p3Diff.x, p3Diff.z);
					continue;
				}

				// Here we need to run on the wall the calculation to create the UV panels across the triangles surface
				Vector2[] nonPlaneUVs = MapNonPlanarUVTile(anchorPoint, p1, p2, p3, size);

				uv[t1] = nonPlaneUVs[0];
				uv[t2] = nonPlaneUVs[1];
				uv[t3] = nonPlaneUVs[2];

			}
			return uv;
		}




		public static Vector2[] MapNonPlanarUVTile(Vector3 anchorPoint, Vector3 p1, Vector3 p2, Vector3 p3, float size)
		{
			Vector3 cross = Vector3.Cross((p3 - p1).normalized, (p2 - p1).normalized);
			
			return MapNonPlanarUVTile(anchorPoint, p1, p2, p3, size, cross, Vector3.up);
		}

		public static Vector2[] MapNonPlanarUVTile(Vector3 anchorPoint, Vector3 p1, Vector3 p2, Vector3 p3, float size, Vector3 cross, Vector3 vUpDirection)
		{
			Vector3 p1Plane  = ProjectPointOnPlane(cross, anchorPoint, p1);
			Vector3 p2Plane  = ProjectPointOnPlane(cross, anchorPoint, p2);
			Vector3 p3Plane  = ProjectPointOnPlane(cross, anchorPoint, p3);

			Vector2 p1Diff = GetOffsets(cross, p1Plane, anchorPoint, vUpDirection) / size;
			Vector2 p2Diff = GetOffsets(cross, p2Plane, anchorPoint, vUpDirection) / size;
			Vector2 p3Diff = GetOffsets(cross, p3Plane, anchorPoint, vUpDirection) / size;
			
			return new Vector2[3] { p1Diff, p2Diff, p3Diff };
		}

		public static Vector2 GetOffsets(Vector3 cross, Vector3 planePoint, Vector3 anchor, Vector3 vUpDirection)
		{
			Vector3 crossX= Vector3.Cross(cross, vUpDirection);
			Vector3 crossY = Vector3.Cross(crossX, cross);

			Vector3 collidePointX = ProjectPointOnPlane(crossX, planePoint, anchor);
			Vector3 collidePointY = ProjectPointOnPlane(crossY, planePoint, anchor);

			// Figuring out the side of X the point is on
			Vector3 crossXNormal = crossX.normalized;
			Vector3 collideXDir = (collidePointX - anchor).normalized;
			float xDirection = (crossXNormal - collideXDir).sqrMagnitude;

			// Figuring out the side of Y the point is on
			Vector3 crossYNormal = crossY.normalized;
			Vector3 collideYDir = (collidePointY - anchor).normalized;
			float yDirection = (crossYNormal - collideYDir).sqrMagnitude;

			int xMod = 1;
			int yMod = 1;

			// a value bigger than 1 means that the directions do not match
			if(yDirection > 1.5) 
				yMod = -1;

			if(xDirection > 1.5)
				xMod = -1;

			float xDistance = xMod * (anchor - collidePointX).magnitude;
			float yDistance = yMod * (anchor - collidePointY).magnitude;

			// Return the layout
			return new Vector2(xDistance, yDistance);
		}

		private static Vector3 ProjectPointOnPlane(Vector3 planeNorm, Vector3 planePoint, Vector3 point)
		{
			Vector3 planeNormal = planeNorm.normalized;
			float distance = -Vector3.Dot(planeNormal, (point - planePoint));
			return point + planeNormal * distance;
		}

		public static MeshInfo CleanUVsToNormalRange(MeshInfo meshInfo)
		{
			// TODO - insert a thing to confirm that the UV range is between 0, 1. This will cause problems if it is outside that range

			// Find the smallest corner of the map
			Vector2 anchorPoint = SmallestUVCorner(meshInfo);
			float uClamp = Mathf.Floor(anchorPoint.x);
			float vClamp = Mathf.Floor(anchorPoint.y);

			for(int i = 0; i < meshInfo.UVs.Length; i++)
			{
				Vector2 uv = meshInfo.UVs[i];
				uv.x = uv.x - uClamp;
				uv.y = uv.y - vClamp;

				meshInfo.UVs[i] = uv;
			}

			return meshInfo;
		}

		public static Vector2 SmallestUVCorner(MeshInfo meshInfo)
		{
			Vector2 smallestU = new Vector2(float.MaxValue, float.MaxValue);
			Vector2 smallestV = new Vector2(float.MaxValue, float.MaxValue);

			for(int i = 0; i < meshInfo.UVs.Length; i++)
			{
				Vector2 currentUV = meshInfo.UVs[i];
				if(currentUV.x < smallestU.x)
				{
					smallestU = currentUV;
				}

				if(currentUV.y < smallestV.y)
				{
					smallestV = currentUV;
				}
			}

			return new Vector2(smallestU.x, smallestV.y);
		}
	}
}
