using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{
	public struct VertsAndTris
	{
		public bool IsValid;
		public Vector3 Vert;
		public int TriIndex;

		public VertsAndTris(Vector3 vert, int triIndex)
		{
			this.Vert = vert;
			this.TriIndex = triIndex;
			this.IsValid = true;
		}
	}

	public static partial class BCTiledWall
	{
		static List<Vector3> vertices = new List<Vector3>();
		static List<int> triangles = new List<int>();
		static List<Vector2> uvs = new List<Vector2>();

		public static MeshInfo CreateSingleWall(WallInformation wallInfo, float wallHeight, float VResolution, bool renderExterior = false)
		{
			float distance = 0;
			return CreateSingleWall(wallInfo, wallHeight, VResolution, distance, out distance, renderExterior);
		}

		public static MeshInfo CreateSingleWall(WallInformation wallInfo, float wallHeight, float VResolution, float startedDistance, out float endDistance, bool renderExterior = false)
		{
			vertices.Clear();
			triangles.Clear();
			uvs.Clear();

			if(wallInfo.ReadyForGeneration == false)
				Debug.LogError("A wall info has not been preped for generation, may generate wrong");

			float maxUDistance = VResolution; // ensures that no section are greater than the height of the wall
			float totalUDistance = startedDistance; // Used to tell how far along the wall the U part of the UV has been used
			endDistance = startedDistance;

			// FIND BASIC WALL INFO
			WallInformation currentWallInfo = wallInfo;
			Vector3 intersectionStart = wallInfo.StartOffset;
			Vector3 intersectionEnd = wallInfo.EndOffset;

			Vector3 thisOutsetVector = intersectionEnd - intersectionStart;
			float thisOutsetDistance = thisOutsetVector.magnitude;
			Vector3 thisOutsetDirection = thisOutsetVector.normalized;

			float cornerDistance = wallInfo.GetStartOutsetSpacing();

			// Need to figure out if the start of the wall is inset..
			Vector3 noJoinStartOffset = wallInfo.Start + wallInfo.OutsetDirection * wallInfo.Outset;
			Vector3 noJoinEndOffset = wallInfo.End + wallInfo.OutsetDirection * wallInfo.Outset;
			if((noJoinStartOffset - noJoinEndOffset).magnitude > (intersectionStart - noJoinEndOffset).magnitude)
				cornerDistance *= -1;

			float outsetDistance = (currentWallInfo.StartOffset - currentWallInfo.EndOffset).magnitude;

			// Last Verts and Tris
			VertsAndTris[] lastVertsAndTris = new VertsAndTris[0];

			// info to help in building the wall along the outset
			float wallDrawn = 0;
			int subIndex = 0;

			// TODO - May want to update totalUDistance if is an external wall
			// and we want all the external wall UVs to match up

			// ======== OPENINGS =========
			Opening nextOpening = new Opening();
			Opening prevOpening = new Opening();
			Opening currentOpening = new Opening();

			// Find if the wall STARTS on a window or door opening
			int firstIndex = GetNextOpening(0, currentWallInfo.Openings);
			if(firstIndex > -1)
			{
				Vector3 openingStart = currentWallInfo.Openings[firstIndex].GetStartPositionOutset(wallInfo, 0);
				if(BCUtils.ArePointsCloseEnough(openingStart, wallInfo.StartOffset))
					nextOpening = currentWallInfo.Openings[firstIndex];
			}

			int breaker = 0;
			while(wallDrawn < outsetDistance && breaker < 128)
			{
//				if(breaker > wallAmount)
//					break;
				breaker++;

				// This is how much of the distance the subSection will use
				// It will be cut down as we run into issues
				float subDistance = maxUDistance;

				float localUDistance = GetLocalUDistance(totalUDistance, maxUDistance); // SO, this is how far we are along the line of this section
				subDistance -= localUDistance;

				Vector3 subSectionStart = intersectionStart + thisOutsetDirection * wallDrawn;
				subSectionStart = RoundVector(subSectionStart);

				// Find where the distance is on the wall for testing for openings
				float onWallStartDist = wallDrawn - cornerDistance;

				if(nextOpening.IsValid)
				{
					currentOpening = nextOpening;

					if(onWallStartDist + subDistance > currentOpening.EndDistance)
					{
						nextOpening.IsValid = false;
						prevOpening = currentOpening;
						subDistance = currentOpening.EndDistance - onWallStartDist;
					}
				}

				// Need to find the opening in this wall
				if(currentOpening.IsValid == false && nextOpening.IsValid == false 
					&& currentWallInfo.Openings != null && currentWallInfo.Openings.Length > 0)
				{
					int nextIndex = GetNextOpening(onWallStartDist, currentWallInfo.Openings);
					if(nextIndex > -1)
					{
						if(onWallStartDist + subDistance > currentWallInfo.Openings[nextIndex].StartDistance)
						{
							nextOpening = currentWallInfo.Openings[nextIndex];
							subDistance = nextOpening.StartDistance - onWallStartDist;
						}
					}
				}

				if(subDistance + wallDrawn > thisOutsetDistance)
					subDistance = thisOutsetDistance - wallDrawn;

				Vector3 subSectionEnd = subSectionStart + thisOutsetDirection * subDistance;
				subSectionEnd = RoundVector(subSectionEnd);
				wallDrawn += subDistance;
				totalUDistance += subDistance;

				// The four corners of this section
				Vector3 startBottom = subSectionStart;
				Vector3 endTop = subSectionEnd + Vector3.up * wallHeight;
				Vector3 endBottom = endTop; endBottom.y = startBottom.y;
				Vector3 startTop = startBottom; startTop.y = endTop.y;

				// Now 
				List<Vector3> startVerts = new List<Vector3>();
				List<Vector3> endVerts = new List<Vector3>();

				// Figure out all the spacing for this
				// ALWAYS add the top and bottom points
				bool currentTop = true;
				bool currentBottom = true;
				bool prevTop = true;
				bool prevBottom = true;
				bool nextTop = true;
				bool nextBottom = true;

				if(currentOpening.IsValid && currentOpening.HasTop(wallHeight) == false)
					currentTop = false;
				if(currentOpening.IsValid && currentOpening.HasBottom() == false)
					currentBottom = false;

				if(prevOpening.IsValid && prevOpening.HasTop(wallHeight) == false)
					prevTop = false;
				if(prevOpening.IsValid && prevOpening.HasBottom() == false)
					prevBottom = false;

				if(nextOpening.IsValid && nextOpening.HasTop(wallHeight) == false)
					nextTop = false;
				if(nextOpening.IsValid && nextOpening.HasBottom() == false)
					nextBottom = false;

				if(currentTop)
				{
					startVerts.Add(startTop);
					endVerts.Add(endTop);
				}
				if(currentBottom)
				{
					startVerts.Add(startBottom);
					endVerts.Add(endBottom);
				}

				// add the current opening, no matter what we always want this
				if(currentOpening.IsValid)
				{
					if(currentTop)
					{
						startVerts.Add(startBottom + Vector3.up * currentOpening.Top);
						endVerts.Add(endBottom + Vector3.up * currentOpening.Top);
					}

					if(currentBottom)
					{
						startVerts.Add(startBottom + Vector3.up * currentOpening.Bottom);
						endVerts.Add(endBottom + Vector3.up * currentOpening.Bottom);
					}
				}

				// Now we do the edge cases
				if(currentOpening.IsValid == false)
				{
					if(nextOpening.IsValid)
					{
						if(nextTop)
							endVerts.Add(endBottom + Vector3.up * nextOpening.Top);
						if(nextBottom)
							endVerts.Add(endBottom + Vector3.up * nextOpening.Bottom);
					}
					if(prevOpening.IsValid)
					{
						if(prevTop)
							startVerts.Add(startBottom + Vector3.up * prevOpening.Top);
						if(prevBottom)
							startVerts.Add(startBottom + Vector3.up * prevOpening.Bottom);
					}
				}

				startVerts = startVerts.OrderByDescending(height => height.y).ToList<Vector3>();
				endVerts = endVerts.OrderByDescending(height => height.y).ToList<Vector3>();

				float startLocalDistance = GetLocalUDistance(totalUDistance - subDistance, maxUDistance);
				float endLocalDistance = GetLocalUDistance(totalUDistance, maxUDistance);

				float startPercentage = startLocalDistance / maxUDistance;
				float endLocalPercentage = endLocalDistance / maxUDistance;

				startPercentage = (float)System.Math.Round(startPercentage, 5);
				endLocalPercentage = (float)System.Math.Round(endLocalPercentage, 5);

				float startUDistance = startPercentage;
				float endUDistance = endLocalPercentage;

				if(subDistance == maxUDistance 
					|| (localUDistance + subDistance) / maxUDistance == 1)
				{
					endUDistance = 1;
				}

				float wallWorldLength = (startBottom - endBottom).magnitude;

				// Special case where the windows weren't getting stretched right - HACK
				if(startUDistance >= endUDistance && wallWorldLength != 0f)
				{
					if(startUDistance == 1)
						startUDistance = 0;
					if(endUDistance == 0)
						endUDistance = 1;
				}

				// Starts a new wall section if we get to the end of a tiled section
				if(startUDistance == 0)
				{
					lastVertsAndTris = new VertsAndTris[0];
				}

				if(renderExterior == true)
				{
					startUDistance = 1 - startUDistance;
					endUDistance = 1 - endUDistance;
				}

				if(wallWorldLength != 0f) // Ensures no zero triangles are created which screw up height maps
				{
//					lastVertsAndTris = GetVertIndexesAndPoints(startVerts, endVerts, currentOpening.IsValid, renderExterior, 
//						lastVertsAndTris, startUDistance, endUDistance, VResolution,
//						ref vertices, ref triangles, ref uvs);

					lastVertsAndTris = BuildMeshSection(startVerts, endVerts, lastVertsAndTris, currentOpening.IsValid, renderExterior,
						startUDistance, endUDistance, VResolution, ref vertices, ref triangles, ref uvs);
				}

				if(currentOpening.IsValid == false && prevOpening.IsValid)
					prevOpening.IsValid = false;

				if(prevOpening == currentOpening)
					currentOpening.IsValid = false;

				if(breaker == 128) 
					Debug.Log("Broke it, can't have a wall longer than (128 * 3) meters");
				
				subIndex++;
			}

			MeshInfo meshInfo = new MeshInfo(vertices, triangles, uvs, null, 0);
			return meshInfo;
		}

		static Vector3 RoundVector(Vector3 vector, int decimals = 5)
		{
			return new Vector3((float)System.Math.Round(vector.x, decimals), (float)System.Math.Round(vector.y, decimals), (float)System.Math.Round(vector.z, decimals));
		}

		public static VertsAndTris[] BuildMeshSection(List<Vector3> startPoints, List<Vector3> endPoints, VertsAndTris[] lastVertsAndTris, 
			bool hasOpening, bool isExterior,
			float startUDistance, float endUDistance, float maxWallHeight,
			ref List<Vector3> verticies, ref List<int> triangles, ref List<Vector2> uvs,
			int fillIn = -1)
		{
			// TODO - make sure no zero area triangles appear. If two start points or end points are on top of each other, delete them. This could cause some problems...
			startPoints = startPoints.OrderByDescending(x => x.y).ToList<Vector3>();
			endPoints = endPoints.OrderByDescending(x => x.y).ToList<Vector3>();

			VertsAndTris[] startVT = new VertsAndTris[startPoints.Count + lastVertsAndTris.Length];
			VertsAndTris[] endVT = new VertsAndTris[endPoints.Count];

			// Adds in new vertexes if they are needed
			for(int i = 0; i < startPoints.Count; i++)
			{
				int foundIndex = GetIndex(startPoints[i].y, lastVertsAndTris);

				if(foundIndex < 0) // A new index point!
				{
					startVT[i] = new VertsAndTris(startPoints[i], verticies.Count);
					verticies.Add(startPoints[i]);
					uvs.Add(new Vector2(startUDistance, startVT[i].Vert.y / maxWallHeight));

				}
				else // index point has been found
				{
					startVT[i] = new VertsAndTris(startPoints[i], foundIndex);
				}
			}

			for(int i = 0; i < endPoints.Count; i++)
			{
				endVT[i] = new VertsAndTris(endPoints[i], verticies.Count);
				uvs.Add(new Vector2(endUDistance, endVT[i].Vert.y / maxWallHeight));
				verticies.Add(endPoints[i]);
			}

			if(hasOpening == false)
			{
				for(int i = 0; i < endVT.Length - 1; i++)
				{
					int tri1 = startVT[0].TriIndex;
					int tri2 = endVT[i + 1].TriIndex;
					int tri3 = endVT[i].TriIndex;

					if(isExterior)
					{
						triangles.Add(tri1);
						triangles.Add(tri2);
						triangles.Add(tri3);
					}
					else
					{
						triangles.Add(tri1);
						triangles.Add(tri3);
						triangles.Add(tri2);
					}
				}

				for(int i = 0; i < startVT.Length - 1; i++)
				{
					if(startVT[i].IsValid == false || startVT[i + 1].IsValid == false)
						break;

					int tri1 = endVT[endVT.Length - 1].TriIndex;
					int tri2 = startVT[i].TriIndex;
					int tri3 = startVT[i + 1].TriIndex;

					if(isExterior == true)
					{
						triangles.Add(tri1);
						triangles.Add(tri2);
						triangles.Add(tri3);
					}
					else
					{
						triangles.Add(tri1);
						triangles.Add(tri3);
						triangles.Add(tri2);
					}
				}
			}
			else
			{
				// HACK adds only above and below window areas

				if(isExterior)
				{
					triangles.Add(startVT[0].TriIndex);
					triangles.Add(endVT[1].TriIndex);
					triangles.Add(endVT[0].TriIndex);

					triangles.Add(startVT[0].TriIndex);
					triangles.Add(startVT[1].TriIndex);
					triangles.Add(endVT[1].TriIndex);

					if(startVT.Length > 2 && endVT.Length > 2)
					{
						triangles.Add(startVT[2].TriIndex);
						triangles.Add(endVT[3].TriIndex);
						triangles.Add(endVT[2].TriIndex);

						triangles.Add(startVT[2].TriIndex);
						triangles.Add(startVT[3].TriIndex);
						triangles.Add(endVT[3].TriIndex);
					}
				}
				else
				{
					triangles.Add(startVT[0].TriIndex);
					triangles.Add(endVT[0].TriIndex);
					triangles.Add(endVT[1].TriIndex);

					triangles.Add(startVT[0].TriIndex);
					triangles.Add(endVT[1].TriIndex);
					triangles.Add(startVT[1].TriIndex);

					if(startVT.Length > 2 && endVT.Length > 2)
					{
						triangles.Add(startVT[2].TriIndex);
						triangles.Add(endVT[2].TriIndex);
						triangles.Add(endVT[3].TriIndex);

						triangles.Add(startVT[2].TriIndex);
						triangles.Add(endVT[3].TriIndex);
						triangles.Add(startVT[3].TriIndex);
					}
				}
			}

			return endVT;
		}

		static int GetIndex(float y, VertsAndTris[] VTSet)
		{
			for(int i = 0; i < VTSet.Length; i++)
			{
				if(VTSet[i].IsValid == false)
					continue;

				if(Mathf.Abs(VTSet[i].Vert.y - y) < Vector3.kEpsilon)
					return VTSet[i].TriIndex;
			}

			return -1;
		}

		/// <summary>
		/// Gets the vert indexes and points.
		/// </summary>
		/// <returns>Number of triangles added</returns>
//		static VertsAndTris[] GetVertIndexesAndPoints(List<Vector3> startPoints, List<Vector3> endPoints, bool hasOpening, bool isExterior,
//			VertsAndTris[] lastVertsAndTris, float startUDistance, float endUDistance, float maxWallHeight,
//			ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector2> uvs,
//			int fillIn = -1)
//		{
//			List<int> ignoreEndPoints = new List<int>();
//			List<int> ignoreStartPoints = new List<int>();
//
//			VertsAndTris[] startVT = new VertsAndTris[startPoints.Count + lastVertsAndTris.Length];
//			for(int i = 0; i < lastVertsAndTris.Length; i++) // Add in all the startPoints from the last point so we conserve stuff
//			{
//				startVT[i].IsValid = true;
//				startVT[i].TriIndex = lastVertsAndTris[i].TriIndex;
//				startVT[i].Vert = lastVertsAndTris[i].Vert;
//			}
//
//			VertsAndTris[] endVT = new VertsAndTris[endPoints.Count];
//
//			int closestPoint, secondClosestPoint;
//			int openingIndex = 0;
//
//			for(int sIndex = 0; sIndex < startPoints.Count; sIndex++)
//			{
//				int breaker = 0;
//				bool breakIt = false;
//				while(breaker < 128)
//				{
//					FindClosestAndSecondClosestPoint(startPoints[sIndex], endPoints, out closestPoint, out secondClosestPoint, ignoreEndPoints);
//					if(closestPoint > -1)
//					{
//						float sqrDistance = (startPoints[sIndex] - endPoints[closestPoint]).sqrMagnitude;
//						float otherSqrDistance = float.MaxValue;
//						if(sIndex + 1 < startPoints.Count)
//							otherSqrDistance = (startPoints[sIndex + 1] - endPoints[closestPoint]).sqrMagnitude;
//
//						bool isTriAStarter = true;
//						bool isTriBStarter = false;
//						bool isTriCStarter = false;
//
//						Vector3 triA = startPoints[sIndex];
//						Vector3 triB = endPoints[closestPoint];
//						Vector3 triC = endPoints[secondClosestPoint];
//
//						// HACK - reverses the points to ensure that we all line up
//						if(sIndex == startPoints.Count - 1)
//						{
//							Vector3 otherTri = triB;
//							triB = triA;
//							triA = otherTri;
//							isTriAStarter = false;
//							isTriBStarter = true;
//						}
//
//						if(sqrDistance > otherSqrDistance)
//						{
//							ignoreStartPoints.Add(sIndex + 1);
//							triC = startPoints[sIndex + 1];
//							isTriCStarter = true;
//							breakIt = true;
//						}
//						else
//						{
//							ignoreEndPoints.Add(closestPoint);
//						}
//
//						if((openingIndex % 4 == 2 || openingIndex % 4 == 3) && hasOpening == true)
//						{
//
//						}
//						else
//						{
//							// Now we get the index for each tri
//
//							int triAIndex = -1;
//							int triBIndex = -1;
//							int triCIndex = -1;
//
//							if(isTriAStarter)
//								triAIndex = GetTriangle(triA.y, startVT);
//							else
//								triAIndex = GetTriangle(triA.y, endVT);
//
//							if(isTriBStarter)
//								triBIndex = GetTriangle(triB.y, startVT);
//							else
//								triBIndex = GetTriangle(triB.y, endVT);
//
//							if(isTriCStarter)
//								triCIndex = GetTriangle(triC.y, startVT);
//							else
//								triCIndex = GetTriangle(triC.y, endVT);
//
//							if(isExterior == false)
//							{
//								if(triAIndex < 0)
//								{
//									int vertCount = vertices.Count;
//									vertices.Add(triA);
//									triangles.Add(vertCount);
//									if(isTriAStarter)
//									{
//										uvs.Add(new Vector2(startUDistance, triA.y / maxWallHeight));
//										AddTriangle(triA, vertCount, ref startVT);
//									}	
//									else
//									{
//										uvs.Add(new Vector2(endUDistance, triA.y / maxWallHeight));
//										AddTriangle(triA, vertCount, ref endVT);
//									}
//								}
//								else
//									triangles.Add(triAIndex);
//
//								if(triBIndex < 0)
//								{
//									int vertCount = vertices.Count;
//									vertices.Add(triB);
//									triangles.Add(vertCount);
//									if(isTriBStarter)
//									{
//										uvs.Add(new Vector2(startUDistance, triB.y / maxWallHeight));
//										AddTriangle(triB, vertCount, ref startVT);
//									}
//
//									else
//									{
//										uvs.Add(new Vector2(endUDistance, triB.y / maxWallHeight));
//										AddTriangle(triB, vertCount, ref endVT);
//									}
//
//								}
//								else
//								{
//									triangles.Add(triBIndex);
//								}
//
//								if(triCIndex < 0)
//								{
//									int vertCount = vertices.Count;
//									vertices.Add(triC);
//									triangles.Add(vertCount);
//									if(isTriCStarter)
//									{
//										uvs.Add(new Vector2(startUDistance, triC.y / maxWallHeight));
//										AddTriangle(triC, vertCount, ref startVT);
//									}	
//									else
//									{
//										uvs.Add(new Vector2(endUDistance, triC.y / maxWallHeight));
//										AddTriangle(triC, vertCount, ref endVT);
//									}
//								}
//								else
//									triangles.Add(triCIndex);
//							}
//							else
//							{
//								if(triBIndex < 0)
//								{
//									int vertCount = vertices.Count;
//									vertices.Add(triB);
//									triangles.Add(vertCount);
//									if(isTriBStarter)
//									{
//										uvs.Add(new Vector2(startUDistance, triB.y / maxWallHeight));
//										AddTriangle(triB, vertCount, ref startVT);
//									}
//
//									else
//									{
//										uvs.Add(new Vector2(endUDistance, triB.y / maxWallHeight));
//										AddTriangle(triB, vertCount, ref endVT);
//									}
//
//								}
//								else
//								{
//									triangles.Add(triBIndex);
//								}
//
//								if(triAIndex < 0)
//								{
//									int vertCount = vertices.Count;
//									vertices.Add(triA);
//									triangles.Add(vertCount);
//									if(isTriAStarter)
//									{
//										uvs.Add(new Vector2(startUDistance, triA.y / maxWallHeight));
//										AddTriangle(triA, vertCount, ref startVT);
//									}	
//									else
//									{
//										uvs.Add(new Vector2(endUDistance, triA.y / maxWallHeight));
//										AddTriangle(triA, vertCount, ref endVT);
//									}
//								}
//								else
//									triangles.Add(triAIndex);
//
//								if(triCIndex < 0)
//								{
//									int vertCount = vertices.Count;
//									vertices.Add(triC);
//									triangles.Add(vertCount);
//									if(isTriCStarter)
//									{
//										uvs.Add(new Vector2(startUDistance, triC.y / maxWallHeight));
//										AddTriangle(triC, vertCount, ref startVT);
//									}	
//									else
//									{
//										uvs.Add(new Vector2(endUDistance, triC.y / maxWallHeight));
//										AddTriangle(triC, vertCount, ref endVT);
//									}
//								}
//								else
//									triangles.Add(triCIndex);
//							}
//
////							if(fillIn > -1)
////								DrawTriangle(triA, triB, triC, Colour.ColourFromIndex(openingIndex, 0.5f), fillIn);
//						}
//
//						openingIndex++;
//					}
//					else
//						break;
//
//					if(breakIt)
//						break;
//
//					breaker++;
//				}
//			}
//
//			if(endPoints.Count < 1)
//				return endVT;
//
//			// Now back fill from the bottom the points we are not ignoring
//			Vector3 finalPoint = endPoints[endPoints.Count - 1];
//
//			// All these points will be from the end point to the start points that weren't ignored
//			for(int i = startPoints.Count - 1; i > 0; i--)
//			{
//				if(ignoreStartPoints.Contains(i))
//					continue;
//
//				Vector3 end = finalPoint;
//				Vector3 negStart = startPoints[i - 1];
//				Vector3 start = startPoints[i];
//
//				int endIndex = GetTriangle(end.y, endVT);
//				int negStartIndex = GetTriangle(negStart.y, startVT);
//				int startIndex = GetTriangle(start.y, startVT);
//
//				if(isExterior == false)
//				{
//					//NEG START
//					if(negStartIndex < 0)
//					{
//						int vertCount = vertices.Count;
//						vertices.Add(negStart);
//						triangles.Add(vertCount);
//						AddTriangle(negStart, vertCount, ref startVT);
//						uvs.Add(new Vector2(startUDistance, negStart.y / maxWallHeight));
//					}
//					else
//						triangles.Add(negStartIndex);
//
//					//END
//					if(endIndex < 0)
//					{
//						int vertCount = vertices.Count;
//						vertices.Add(end);
//						triangles.Add(vertCount);
//						AddTriangle(end, vertCount, ref endVT);
//						uvs.Add(new Vector2(endUDistance, end.y / maxWallHeight));
//					}
//					else
//						triangles.Add(endIndex);
//
//					//START
//					if(startIndex < 0)
//					{
//						int vertCount = vertices.Count;
//						vertices.Add(start);
//						triangles.Add(vertCount);
//						AddTriangle(start, vertCount, ref startVT);
//						uvs.Add(new Vector2(startUDistance, start.y / maxWallHeight));
//					}
//					else
//						triangles.Add(startIndex);
//				}
//				else
//				{
//					//END
//					if(endIndex < 0)
//					{
//						int vertCount = vertices.Count;
//						vertices.Add(end);
//						triangles.Add(vertCount);
//						AddTriangle(end, vertCount, ref endVT);
//						uvs.Add(new Vector2(endUDistance, end.y / maxWallHeight));
//					}
//					else
//						triangles.Add(endIndex);
//
//					//NEG START
//					if(negStartIndex < 0)
//					{
//						int vertCount = vertices.Count;
//						vertices.Add(negStart);
//						triangles.Add(vertCount);
//						AddTriangle(negStart, vertCount, ref startVT);
//						uvs.Add(new Vector2(startUDistance, negStart.y / maxWallHeight));
//					}
//					else
//						triangles.Add(negStartIndex);
//
//					//START
//					if(startIndex < 0)
//					{
//						int vertCount = vertices.Count;
//						vertices.Add(start);
//						triangles.Add(vertCount);
//						AddTriangle(start, vertCount, ref startVT);
//						uvs.Add(new Vector2(startUDistance, start.y / maxWallHeight));
//					}
//					else
//						triangles.Add(startIndex);
//				}
//
////				if(fillIn > -1)
////					DrawTriangle(finalPoint, startPoints[i], startPoints[i - 1], Colour.ColourFromIndex(colorIndex++, 0.5f), fillIn);
//			}
//
//			return endVT;
//		}

		private static float GetLocalUDistance(float totalUDistance, float maxUDistance)
		{
			return totalUDistance % maxUDistance;
		}

		// Used to help visualize 

//		private static void DrawTriangle(Vector3 point1, Vector3 point2, Vector3 point3, Color color, int fillInAmount = 0)
//		{
//			Utility.DrawLine(point1, point2, color);
//			Utility.DrawLine(point2, point3, color);
//			Utility.DrawLine(point3, point1, color);
//
//			FillInTriangle(point1, point2, point3, color, fillInAmount);
//		}

//		static void FillInTriangle(Vector3 point1, Vector3 point2, Vector3 point3, Color color, int numTimes)
//		{
//			numTimes--;
//
//			if(numTimes < 0)
//				return;
//
//			Utility.DrawLine(point1, point2, color);
//			Utility.DrawLine(point2, point3, color);
//			Utility.DrawLine(point3, point1, color);
//
//			Vector3 middle = (point1 + point2 + point3) / 3f;
//
//			Vector3 midPoint1 = (point1 + point2) / 2f;
//			Vector3 midPoint2 = (point2 + point3) / 2f;
//			Vector3 midPoint3 = (point3 + point1) / 2f;
//
//			Utility.DrawLine(midPoint1, middle, color);
//			Utility.DrawLine(midPoint2, middle, color);
//			Utility.DrawLine(midPoint3, middle, color);
//
//			if(numTimes < 0)
//				return;
//
//			FillInTriangle(midPoint1, point3, point1, color, numTimes);
//			FillInTriangle(midPoint2, point1, point2, color, numTimes);
//			FillInTriangle(midPoint3, point2, point3, color, numTimes);
//		}

		public static void GetVecDistanceDirectionAndCross(Vector3 start, Vector3 end, out Vector3 vector, out Vector3 direction, out Vector3 outsetCross, out float distance)
		{
			// Get the basic information about the wall
			vector = end - start;
			distance = vector.magnitude;
			direction = vector.normalized;
			outsetCross = Vector3.Cross(Vector3.down, direction);
		}

		public static bool GetStartAndEndOutsetPoints(int index, WallInformation[] wallInfos,  float outset, out Vector3 wallStartPos, out Vector3 wallEndPos)
		{
			if(index < 0 || index >= wallInfos.Length)
			{
				wallStartPos = wallEndPos = Vector3.zero;
				return false;
			}	
	
			wallStartPos = wallInfos[index].Start;
			wallEndPos = wallInfos[index].End;
	
			Vector3 thisVector, thisDirection, thisCross;
			float thisDistance;
	
			// Find the offset position
			GetVecDistanceDirectionAndCross(wallStartPos, wallEndPos, out thisVector, out thisDirection, out thisCross,  out thisDistance);
			wallStartPos += thisCross * outset;
			wallEndPos += thisCross * outset;
	
			// Update the direction and information
			GetVecDistanceDirectionAndCross(wallStartPos, wallEndPos, out thisVector, out thisDirection, out thisCross, out thisDistance);
	
			int nextIndex = index + 1;
			int prevIndex = index - 1;
	
			bool isLineLooped = BCUtils.ArePointsCloseEnough(wallInfos[0].Start, wallInfos[wallInfos.Length - 1].End);
				
			if(isLineLooped == true)
			{
				if(nextIndex >= wallInfos.Length)
					nextIndex = 0;
	
				if(prevIndex < 0)
					prevIndex = wallInfos.Length - 1;
			}
	
			// now we check to see if we can grab previous and next positions
			if(prevIndex >= 0)
			{
				Vector3 prevStart = wallInfos[prevIndex].Start;
				Vector3 prevEnd = wallInfos[prevIndex].End;
				Vector3 prevVector, prevDirection, prevCross;
				float prevDistance;
				GetVecDistanceDirectionAndCross(prevStart, prevEnd, out prevVector, out prevDirection, out prevCross, out prevDistance);
	
				prevStart += prevCross * wallInfos[prevIndex].Outset;
				prevEnd += prevCross * wallInfos[prevIndex].Outset;
	
				Vector3 newIntersect;
	
				if(BCUtils.FindIntersectOfTwoInfinityLinesXZ(prevStart, prevEnd, wallStartPos, wallEndPos, out newIntersect))
					wallStartPos = newIntersect;
			}
	
			if(nextIndex < wallInfos.Length)
			{
				Vector3 nextStart = wallInfos[nextIndex].Start;
				Vector3 nextEnd = wallInfos[nextIndex].End;
				Vector3 nextVector, prevDirection, prevCross;
				float nextDistance;
				GetVecDistanceDirectionAndCross(nextStart, nextEnd, out nextVector, out prevDirection, out prevCross, out nextDistance);
	
				nextStart += prevCross * wallInfos[nextIndex].Outset;
				nextEnd += prevCross * wallInfos[nextIndex].Outset;
	
				Vector3 newIntersect;
	
				if(BCUtils.FindIntersectOfTwoInfinityLinesXZ(nextStart, nextEnd, wallStartPos, wallEndPos, out newIntersect))
					wallEndPos = newIntersect;
			}
	
			return true;
		}

		static int GetNextOpening(float distanceAlongWall, Opening[] openings)
		{
			if(openings == null)
				return -1;

			float closeDistance = float.MaxValue;
			int closeIndex = -1;

			for(int i = 0; i < openings.Length; i++)
			{
				Opening opening = openings[i];

				if(opening.StartDistance < distanceAlongWall)
					continue;

				float distanceToNext = opening.StartDistance - distanceAlongWall;
				if(distanceToNext < closeDistance)
				{
					closeIndex = i;
					closeDistance = distanceToNext;
				}
			}

			return closeIndex;
		}
//
//		#region IndexPoints
//
//		/// <summary>
//		/// Negative 1 means we have only found one or zero points
//		/// </summary>
//		/// <param name="origin">Origin.</param>
//		/// <param name="points">Points.</param>
//		/// <param name="closestIndex">Closest index.</param>
//		/// <param name="secondClosestIndex">Second closest index.</param>
//		/// <param name="ignoreIndexes">Ignore indexes.</param>
//		static void FindClosestAndSecondClosestPoint(Vector3 origin, List<Vector3> points, out int closestIndex, out int secondClosestIndex, List<int> ignoreIndexes)
//		{
//			int first = -1;
//			int second = -1;
//			float firstSqrDistance = float.MaxValue;
//			float secondSqrDistance = float.MaxValue;
//
//			// First find the closest one
//			for(int i = 0; i < points.Count; i++)
//			{			
//				if(ignoreIndexes.Contains(i))
//					continue;
//
//				float sqrDistance = (origin - points[i]).sqrMagnitude;
//				if(sqrDistance < firstSqrDistance)
//				{
//					first = i;
//					firstSqrDistance = sqrDistance;
//				}
//			}
//
//			for(int i = 0; i < points.Count; i++)
//			{			
//				if(ignoreIndexes.Contains(i))
//					continue;
//
//				if(i == first)
//					continue;
//
//				float sqrDistance = (origin - points[i]).sqrMagnitude;
//				if(sqrDistance < secondSqrDistance)
//				{
//					second = i;
//					secondSqrDistance = sqrDistance;
//				}
//			}
//
//			if(first < 0 || second < 0)
//			{
//				first = -1;
//				second = -1;
//			}
//
//			closestIndex = first;
//			secondClosestIndex = second;
//		}
//
//
//		static int GetTriangle(float y, VertsAndTris[] VTSet)
//		{
//			for(int i = 0; i < VTSet.Length; i++)
//			{
//				if(VTSet[i].IsValid == false)
//					continue;
//
//				if(Mathf.Abs(VTSet[i].Vert.y - y) < Vector3.kEpsilon)
//					return VTSet[i].TriIndex;
//			}
//
//			return -1;
//		}
//
//		static void AddTriangle(Vector3 vert, int triIndex, ref VertsAndTris[] VTSet)
//		{
//			for(int i = 0; i < VTSet.Length; i++)
//			{
//				if(VTSet[i].IsValid == true)
//					continue;
//
//				VTSet[i].IsValid = true;
//				VTSet[i].Vert = vert;
//				VTSet[i].TriIndex = triIndex;
//				return;	
//			}
//
//			// Expands the VTSet if we run out of room
//			VertsAndTris[] newVTs = new VertsAndTris[VTSet.Length * 2];
//			for(int i = 0; i < VTSet.Length; i++)
//				newVTs[i] = VTSet[i];
//
//			VTSet = newVTs;
//			AddTriangle(vert, triIndex, ref VTSet);
//		}
//
//		#endregion
	}
}
