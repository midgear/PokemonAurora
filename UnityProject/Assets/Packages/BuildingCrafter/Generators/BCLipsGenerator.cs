using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace BuildingCrafter
{
	public static class BCLipsGenerator 
	{

		/// <summary>
		/// Generates the building floor lip.
		/// </summary>
		/// <returns>The building floor lip.</returns>
		/// <param name="lineSegment">Line segment.</param>
		/// <param name="floorBp">Floor bp.</param>
		/// <param name="floor">Floor.</param>
		public static MeshInfo GenerateBuildingFloorLip(Vector3[] lineSegment, Vector2[] crossSection)
		{
			if(crossSection.Length == 0)
				return new MeshInfo();

			MeshInfo meshInfo = new MeshInfo();
			// First go through ALL the line segements and create the slanted connector for each corner.
			// This will then be the working system for the edge
			// For each corner, project the previous corner and then take the intersections as the corner

			bool fullLoop = false;

			if(BCUtils.ArePointsCloseEnough(lineSegment[lineSegment.Length - 1], lineSegment[0]))
				fullLoop = true;

			List<Vector3[]> intersectedPoints = new List<Vector3[]>();
			for(int n = 0; n < lineSegment.Length - 1; n++)
			{
				int nC = n + 1;
				int tC = n;
				int pC = n - 1;

				if(nC >= lineSegment.Length)
					nC = 0;

				if(pC < 0)
					pC = lineSegment.Length - 2;

				Vector3 nextCorner = lineSegment[nC];
				Vector3 thisCorner = lineSegment[tC];
				Vector3 prevCorner = lineSegment[pC];

				Vector3 directionNext = (thisCorner - nextCorner).normalized;
				Vector3 directionPrevious = (thisCorner - prevCorner).normalized;
				Vector3 directionThis = (thisCorner - nextCorner).normalized;

				Vector3 offsetNextDirection = Vector3.Cross(directionNext, Vector3.down);
				Vector3 offsetPrevDirection = Vector3.Cross(directionPrevious, Vector3.up);
				Vector3 offsetThisDirection = Vector3.Cross(directionThis, Vector3.down);

				Vector3[] newPoints = new Vector3[crossSection.Length];

				// For each point, project it to the correct plane and then find the intersection
				for(int i = 0; i < crossSection.Length; i++)
				{
					Vector2 thisPoint = crossSection[i];

					Quaternion qi = Quaternion.Inverse(Quaternion.FromToRotation(offsetNextDirection, Vector3.right));
					Vector3 rotatedNextPoint = qi * (new Vector3(thisPoint.x, thisPoint.y));

					Quaternion qi2 = Quaternion.Inverse(Quaternion.FromToRotation(offsetPrevDirection, Vector3.right));
					Vector3 rotatedPrevPoint = qi2 * (new Vector3(thisPoint.x, thisPoint.y));

					rotatedNextPoint += nextCorner;
					rotatedPrevPoint += prevCorner;

					Vector3 intersection = new Vector3();

					if(fullLoop == false && tC == 0)
					{
						Quaternion qi3 = Quaternion.Inverse(Quaternion.FromToRotation(offsetThisDirection, Vector3.right));
						Vector3 rotatedThisPoint = qi3 * (new Vector3(thisPoint.x, thisPoint.y));
						rotatedThisPoint += thisCorner;

						newPoints[i] = rotatedThisPoint;
					}
					else if(BCUtils.FindIntersectOfTwoInfinityLinesXZ(rotatedPrevPoint, rotatedPrevPoint + directionPrevious, rotatedNextPoint, rotatedNextPoint + directionNext, out intersection))
					{
						newPoints[i] = intersection;
					}
				}

				intersectedPoints.Add(newPoints);
			}

			if(intersectedPoints.Count == 0)
				return new MeshInfo();

			// Adds the final corner if this lip is not a loop
			if(fullLoop == false)
			{
				Vector3 finalSegment = lineSegment[lineSegment.Length - 1];
				Vector3 secondLastSegment = lineSegment[lineSegment.Length - 2];

				Vector3 directionThis = (finalSegment - secondLastSegment).normalized;
				Vector3 offsetThisDirection = Vector3.Cross(directionThis, Vector3.up);

				Vector3[] newPoints = new Vector3[crossSection.Length];

				for(int i = 0; i < crossSection.Length; i++)
				{
					Vector2 thisPoint = crossSection[i];

					Quaternion qi = Quaternion.Inverse(Quaternion.FromToRotation(offsetThisDirection, Vector3.right));
					Vector3 rotatedNextPoint = qi * (new Vector3(thisPoint.x, thisPoint.y));

					rotatedNextPoint += finalSegment;

					newPoints[i] = rotatedNextPoint;
				}

				intersectedPoints.Add(newPoints);
			}

			// 1. We need to figure out the starting "plane" for each intersected point
			for(int lineIndex = 0; lineIndex < intersectedPoints.Count - 1; lineIndex++)
			{
				Vector3[] startIntersection = intersectedPoints[lineIndex];
				Vector3[] endIntersection = intersectedPoints[lineIndex + 1];

				// Need to find the start point for the UV section
				float vDistanceTravelled = 0;

				for(int i = 0; i < startIntersection.Length - 1; i++)
				{
					float newVDistance = 0;

					MeshInfo newMeshInfo = GenerateWallSectionFromFlatSection( 
						startIntersection[i], endIntersection[i], 
						startIntersection[i + 1], endIntersection[i + 1],
						vDistanceTravelled, out newVDistance);
					
					meshInfo = BCMesh.CombineMeshInfos(meshInfo, newMeshInfo);

					vDistanceTravelled += newVDistance;
				}
			}

			if(fullLoop == true && intersectedPoints.Count > 1)
			{
				Vector3[] startIntersection = intersectedPoints[intersectedPoints.Count - 1];
				Vector3[] endIntersection = intersectedPoints[0];

				float vDistanceTravelled = 0;

				for(int i = 0; i < startIntersection.Length - 1; i++)
				{
					float newVDistance;

					meshInfo = BCMesh.CombineMeshInfos(meshInfo, 
						GenerateWallSectionFromFlatSection( 
							startIntersection[i], endIntersection[i], 
							startIntersection[i + 1], endIntersection[i + 1],
							vDistanceTravelled, out newVDistance));

					vDistanceTravelled += newVDistance;
				}
			}
			else
			{
				// TODO - The end caps should be generated with an efficient system instead of using the tesselator, which can lead to weird UVS for big areas

				List<Vector3> endCap = intersectedPoints[0].ToList<Vector3>();
				endCap.Add(lineSegment[0]);
				endCap.Reverse();
				MeshInfo startCapMesh = BCMesh.GenerateGenericMeshInfo(endCap, 3);

				startCapMesh = BCMesh.FixOutOfBoundsUVs(startCapMesh);

				endCap = intersectedPoints[intersectedPoints.Count - 1].ToList<Vector3>();
				endCap.Add(lineSegment[lineSegment.Length - 1]);
				MeshInfo endCapMesh = BCMesh.GenerateGenericMeshInfo(endCap, 3);

				endCapMesh = BCMesh.FixOutOfBoundsUVs(endCapMesh);

				meshInfo = BCMesh.CombineMeshInfos(meshInfo, startCapMesh);
				meshInfo = BCMesh.CombineMeshInfos(meshInfo, endCapMesh);
			}

			return meshInfo;
		}

		private static List<Vector3> vertices = new List<Vector3>();
		private static List<int> indices = new List<int>();
		private static List<Vector2> uvs = new List<Vector2>();

		public static MeshInfo GenerateWallSectionFromFlatSection(
			Vector3 bottomStart, Vector3 bottomEnd, Vector3 topStart, Vector3 topEnd,
			float vStartDistance,
			out float vertDistanceTravelled, float startUDistance = 0, float maxWallDistance = 3)
		{
			// TODO : Deal with situations where the start of the section is after the end of the section
			vertices.Clear();
			indices.Clear();
			uvs.Clear();

			vertDistanceTravelled = 0;

			Vector3 wallVector = bottomEnd - bottomStart;
			float totalWallDistance = wallVector.magnitude;
			Vector3 wallDirection = wallVector.normalized;

			Plane uvStartPlane = new Plane(wallDirection, bottomStart);
			Vector3 uvStartPoint = bottomStart;
			if(uvStartPlane.GetDistanceToPoint(topStart) < 0)
			{
				uvStartPlane = new Plane(wallDirection, topStart);
				uvStartPoint = topStart;
			}

			// First find the number of sections that will be needed to complete this wall

			// This will be the end section points and start section points
			float fartestDistance = Mathf.Max(uvStartPlane.GetDistanceToPoint(topEnd), uvStartPlane.GetDistanceToPoint(bottomEnd));
//			float closestDistance = Mathf.Min(uvStartPlane.GetDistanceToPoint(topStart), uvStartPlane.GetDistanceToPoint(bottomStart));

			int sectionsNeeded = Mathf.CeilToInt(fartestDistance / maxWallDistance);// - Mathf.FloorToInt(closestDistance / maxWallDistance);

			Vector3 bottomStartOnPlane =  FindClosestPointOnPlane(bottomStart, uvStartPlane);
			Vector3 topStartOnPlane = FindClosestPointOnPlane(topStart, uvStartPlane);
			float topEndDistance =  uvStartPlane.GetDistanceToPoint(topEnd);
			float bottomEndDistance = uvStartPlane.GetDistanceToPoint(bottomEnd);

			Vector3 vDirection = (topStartOnPlane - bottomStartOnPlane).normalized;
			Plane vPlane = new Plane(vDirection, bottomStartOnPlane);

			for(int i = 0; i < sectionsNeeded; i++)
			{
				Vector3 thisBottom = bottomStart;
				Vector3 thisTop = topStart;
				Vector3 nextBottom = bottomEnd;
				Vector3 nextTop = topEnd;

				bool bottomIsTooClose = false;
				bool topIsTooClose = false;

				if(sectionsNeeded > 1 && i > 0)
				{
					thisBottom = bottomStartOnPlane + wallDirection * maxWallDistance * i;
					thisTop = topStartOnPlane + wallDirection * maxWallDistance * i;
				}

				if(sectionsNeeded > 1 && i < sectionsNeeded - 1)
				{
					nextBottom = bottomStartOnPlane + wallDirection * maxWallDistance * (i + 1);
					nextTop = topStartOnPlane + wallDirection * maxWallDistance * (i + 1);
				}

				if(uvStartPlane.GetDistanceToPoint(nextTop) > topEndDistance)
					topIsTooClose = true;
				if(uvStartPlane.GetDistanceToPoint(nextBottom) > bottomEndDistance)
					bottomIsTooClose = true;

				float vRealDistance = vPlane.GetDistanceToPoint(topStart);
				vertDistanceTravelled = vRealDistance;

				if(topIsTooClose == false && bottomIsTooClose == false)
				{
					float distanceToThisBottom = uvStartPlane.GetDistanceToPoint(thisBottom);
					float distanceToThisTop = uvStartPlane.GetDistanceToPoint(thisTop);
					Plane thisStartPlane = new Plane(wallDirection, thisBottom);
					if(distanceToThisBottom > distanceToThisTop)
						thisStartPlane = new Plane(wallDirection, thisTop);

					float localDistanceToThisBottom = thisStartPlane.GetDistanceToPoint(thisBottom);
					float localDistanceToThisTop = thisStartPlane.GetDistanceToPoint(thisTop);
					float localDistanceToNextBottom = thisStartPlane.GetDistanceToPoint(nextBottom);
					float localDistanceToNextTop = thisStartPlane.GetDistanceToPoint(nextTop);

					float thisBottomRatio = localDistanceToThisBottom / maxWallDistance;
					float thisTopRatio = localDistanceToThisTop / maxWallDistance;
					float nextBottomRatio = localDistanceToNextBottom / maxWallDistance;
					float nextTopRatio = localDistanceToNextTop / maxWallDistance;

					float vBottom = vStartDistance / maxWallDistance;
					float vTop = (vStartDistance + vRealDistance) / maxWallDistance;

					thisBottomRatio = (float)System.Math.Round(thisBottomRatio, 5);
					thisTopRatio = (float)System.Math.Round(thisTopRatio, 5);
					nextBottomRatio = (float)System.Math.Round(nextBottomRatio, 5);
					nextTopRatio = (float)System.Math.Round(nextTopRatio, 5);

					Vector2 thisBottomUV = new Vector2(thisBottomRatio, vBottom);
					Vector2 thisTopUV = new Vector2(thisTopRatio, vTop);
					Vector2 nextBottomUV = new Vector2(nextBottomRatio, vBottom);
					Vector2 nextTopUv = new Vector2(nextTopRatio, vTop);

					BuildMeshTriangle(ref vertices, ref indices, ref uvs, thisBottom, thisTop, nextTop, thisBottomUV, thisTopUV, nextTopUv);
//					meshInfo = BCMesh.CombineMeshInfos(meshInfo, firstTri);

					BuildMeshTriangle(ref vertices, ref indices, ref uvs, nextTop, nextBottom, thisBottom, nextTopUv, nextBottomUV, thisBottomUV);
//					meshInfo = BCMesh.CombineMeshInfos(meshInfo, secondTri);

				}
				else if(topIsTooClose || bottomIsTooClose)
				{
					// Find the position along either the top line or the bottom line
					Plane plane = new Plane(wallDirection, uvStartPoint + wallDirection * maxWallDistance * (i + 1));

					Ray ray = new Ray(bottomEnd, (topEnd - bottomEnd).normalized);
					float distance = 0;

					Vector3 topClose = nextTop;
					Vector3 bottomClose = nextBottom;
					Vector3 cutPoint = nextTop;

					if(plane.Raycast(ray, out distance))
					{
						Vector3 position = ray.origin + ray.direction * distance;
						cutPoint = position;
					}

					if(topIsTooClose)
						topClose = topEnd;
					if(bottomIsTooClose)
						bottomClose = bottomEnd;

					// build the first section
					float distanceToThisBottom = uvStartPlane.GetDistanceToPoint(thisBottom);
					float distanceToThisTop = uvStartPlane.GetDistanceToPoint(thisTop);
					Plane thisStartPlane = new Plane(wallDirection, thisBottom);
					if(distanceToThisBottom > distanceToThisTop)
						thisStartPlane = new Plane(wallDirection, thisTop);

					float localDistanceToThisBottom = thisStartPlane.GetDistanceToPoint(thisBottom);
					float localDistanceToThisTop = thisStartPlane.GetDistanceToPoint(thisTop);
					float localDistanceToCloseBottom = thisStartPlane.GetDistanceToPoint(bottomClose);
					float localDistanceToCloseTop = thisStartPlane.GetDistanceToPoint(topClose);

					float thisBottomRatio = localDistanceToThisBottom / maxWallDistance;
					float thisTopRatio = localDistanceToThisTop / maxWallDistance;
					float closeBottomRatio = localDistanceToCloseBottom / maxWallDistance;
					float closeTopRatio = localDistanceToCloseTop / maxWallDistance;

					float vBottom = vStartDistance / maxWallDistance;
					float vTop = (vStartDistance + vRealDistance) / maxWallDistance;

					float localDistanceToStartOfV = vPlane.GetDistanceToPoint(cutPoint);
					float cutPointV = (vStartDistance + localDistanceToStartOfV) / maxWallDistance;

					thisBottomRatio = (float)System.Math.Round(thisBottomRatio, 5);
					thisTopRatio = (float)System.Math.Round(thisTopRatio, 5);
					closeBottomRatio = (float)System.Math.Round(closeBottomRatio, 5);
					closeTopRatio = (float)System.Math.Round(closeTopRatio, 5);
					cutPointV = (float)System.Math.Round(cutPointV, 5);

					Vector2 thisBottomUV = new Vector2(thisBottomRatio, vBottom);
					Vector2 thisTopUV = new Vector2(thisTopRatio, vTop);
					Vector2 closeBottomUV = new Vector2(closeBottomRatio, vBottom);
					Vector2 closeTopUv = new Vector2(closeTopRatio, vTop);
					Vector2 cutPointUv = new Vector2(1, cutPointV);

					BuildMeshTriangle(ref vertices, ref indices, ref uvs, thisTop, topClose, thisBottom, thisTopUV, closeTopUv, thisBottomUV);
					BuildMeshTriangle(ref vertices, ref indices, ref uvs, topClose, cutPoint, thisBottom, closeTopUv, cutPointUv, thisBottomUV);
					BuildMeshTriangle(ref vertices, ref indices, ref uvs, thisBottom, cutPoint, bottomClose, thisBottomUV, cutPointUv, closeBottomUV);

					// build the final section
					if(bottomIsTooClose)
					{
						float finalDistance = thisStartPlane.GetDistanceToPoint(topEnd) - maxWallDistance;
						Vector2 finalPoint = new Vector2(finalDistance / maxWallDistance, vTop);
						BuildMeshTriangle(ref vertices, ref indices, ref uvs, cutPoint, topClose, topEnd, new Vector2(0, cutPointV), new Vector2(0, vTop), finalPoint);
					}
					if(topIsTooClose)
					{
						float finalDistance = thisStartPlane.GetDistanceToPoint(bottomEnd) - maxWallDistance;
						Vector2 finalPoint = new Vector2(finalDistance / maxWallDistance, vBottom);
						BuildMeshTriangle(ref vertices, ref indices, ref uvs, cutPoint, bottomEnd, bottomClose, new Vector2(0, cutPointV), finalPoint, new Vector2(0, vBottom));
					}

					break;
				}
			}

			return new MeshInfo(vertices, indices, uvs, null);
		}

		public static Vector3 FindClosestPointOnPlane(Vector3 point, Plane plane)
		{
			Vector3 towardsPlane = plane.normal;
			if(plane.GetSide(point) == true)
				towardsPlane *= -1;

			Ray newRay = new Ray(point, towardsPlane);

			Vector3 positionOnPlane = point;
			float distanceToPlane = 0;
			if(plane.Raycast(newRay, out distanceToPlane))
				positionOnPlane = point + distanceToPlane * towardsPlane;

			return positionOnPlane;
		}

		private static void BuildMeshTriangle(
			ref List<Vector3> vertices, ref List<int> indices, ref List<Vector2> uvs, 
			Vector3 point1, Vector3 point2, Vector3 point3, 
			Vector2 uv1, Vector2 uv2, Vector2 uv3)
		{
			int vertLength = vertices.Count;

			vertices.Add(point1);
			vertices.Add(point2);
			vertices.Add(point3);

			indices.Add(vertLength);
			indices.Add(vertLength + 1);
			indices.Add(vertLength + 2);

			uvs.Add(uv1);
			uvs.Add(uv2);
			uvs.Add(uv3);
		}

//		public static void DrawMesh(MeshInfo meshInfo, Color color, float duration = 0)
//		{
//			DrawMesh(meshInfo, Vector3.zero, 0, color, duration);
//		}
//
//		public static void DrawMesh(MeshInfo meshInfo, Vector3 offsetDirection, float offsetDistance, float duraction = 0)
//		{
//			DrawMesh(meshInfo, offsetDirection, offsetDistance, Color.white, duraction);
//		}
//
//		public static void DrawMesh(MeshInfo meshInfo, Vector3 offsetDirection, float offsetDistance, Color color, float duraction = 0)
//		{
//			if(meshInfo.IsValid == false)
//				return;
//
//			for(int i = 0; i < meshInfo.Triangles.Length; i += 3)
//			{
//				int triIndex1 = meshInfo.Triangles[i];
//				int triIndex2 = meshInfo.Triangles[i + 1];
//				int triIndex3 = meshInfo.Triangles[i + 2];
//
//				Vector3 tri1 = meshInfo.Vertices[triIndex1] + offsetDirection * offsetDistance;
//				Vector3 tri2 = meshInfo.Vertices[triIndex2] + offsetDirection * offsetDistance;
//				Vector3 tri3 = meshInfo.Vertices[triIndex3] + offsetDirection * offsetDistance;
//
//				Utility.DrawTriangle(tri1, tri2, tri3, color, duraction);
//
////				if(IsUVOutOfBounds(meshInfo.UVs[triIndex1]) || IsUVOutOfBounds(meshInfo.UVs[triIndex2]) || IsUVOutOfBounds(meshInfo.UVs[triIndex3]))
////				{
//////					Debug.Log("tri 1 " + meshInfo.UVs[triIndex1].x + ", " + "tri 2 " + meshInfo.UVs[triIndex2].x + ", " + "tri 3 " + meshInfo.UVs[triIndex3].x);
////					Utility.DrawTriangle(tri1, tri2, tri3, Color.red, duraction);
////				}
//			}
//		}

		public static Vector3[] RotateCrossToDirection(Vector3 direction, Vector3 pos, Vector2[] crossSection)
		{
			Vector3[] newCross = new Vector3[crossSection.Length];
			Vector3 offsetThisDirection = Vector3.Cross(direction, Vector3.up);

			for(int i = 0; i < crossSection.Length; i++)
			{
				Vector2 thisPoint = crossSection[i];

				Quaternion qi = Quaternion.Inverse(Quaternion.FromToRotation(offsetThisDirection, Vector3.right));
				Vector3 rotatedThisPoint = qi * (new Vector3(thisPoint.x, thisPoint.y));
				rotatedThisPoint += pos;
				newCross[i] = rotatedThisPoint;
			}

			return newCross;
		}

		/// <summary>
		/// Given two equal paths, find out which one is closest to its corresponding point
		/// </summary>
		/// <returns>The closest index of grouping.</returns>
		/// <param name="firstpath">Firstpath.</param>
		/// <param name="secondPath">Second path.</param>
		public static int FindClosestIndexOfGrouping(Vector3[] firstpath, Vector3[] secondPath)
		{
			if(firstpath.Length != secondPath.Length)
				return -1;

			float maxDistance = float.MaxValue;
			int index = -1;

			for(int i = 0; i < firstpath.Length; i++)
			{
				float distance = (firstpath[i] - secondPath[i]).sqrMagnitude;
				if(distance < maxDistance)
				{
					index = i;
					maxDistance = distance;
				}
			}

			return index;
		}

		public static Vector3 FindStartPointForUvs(Vector3[] firstPath, Vector3[] secondPath)
		{
			if(firstPath.Length != secondPath.Length)
				return Vector3.zero;

			if(firstPath.Length < 1)
				return Vector3.zero;

			Vector3 normal = secondPath[0] - firstPath[0];

			Plane testPlane = new Plane(normal.normalized, firstPath[0]);
			int index = 0;
			float distance = 0;

			for(int i = 1; i < firstPath.Length; i++)
			{
				float distanceToPlane = testPlane.GetDistanceToPoint(firstPath[i]);

				if(distanceToPlane < distance)
				{
					distance = distanceToPlane;
					index = i;
				}
			}

			return firstPath[index];
		}
	}
}