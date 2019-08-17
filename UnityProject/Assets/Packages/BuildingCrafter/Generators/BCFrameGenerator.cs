using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{
	public static partial class BCFrameGenerator
	{
		#region Create Tiled Plane

		public static MeshInfo CreateFrame(Vector3 startOpening, Vector3 endOpening, Vector3 insetDirection, float insetAmount, float height, Vector3 startUvPoint, 
			bool exteriorWall, float tileSize, bool generateBottomFrame = true)
		{
			Vector3 wallDirection = (endOpening - startOpening);
			float openingWidth = wallDirection.magnitude;
			wallDirection = wallDirection.normalized;

			bool reverseNormal = false;
			if(exteriorWall == false)
			{
				insetDirection *= -1;
				reverseNormal = true;
			}

			MeshInfo meshInfo = new MeshInfo();

			meshInfo = BCFrameGenerator.CreateTiledPlane(startOpening, insetDirection, insetAmount, Vector3.up, height, false, false, reverseNormal, startUvPoint, tileSize);
			meshInfo = BCMesh.CombineMeshInfos(meshInfo, 
				BCFrameGenerator.CreateTiledPlane(endOpening, insetDirection, insetAmount, Vector3.up, height, true, false, !reverseNormal, startUvPoint, tileSize));
			meshInfo = BCMesh.CombineMeshInfos(meshInfo, 
				BCFrameGenerator.CreateTiledPlane(startOpening + Vector3.up * height, wallDirection, openingWidth, insetDirection, insetAmount, false, true, !reverseNormal, startUvPoint, tileSize));

			if(generateBottomFrame)
			{
				meshInfo = BCMesh.CombineMeshInfos(meshInfo, 
					BCFrameGenerator.CreateTiledPlane(startOpening, wallDirection, openingWidth, insetDirection, insetAmount, false, false, reverseNormal, startUvPoint, tileSize));
			}

			return meshInfo;
		}


		private static List<Vector3> vertices = new List<Vector3>();
		private static List<int> triangles = new List<int>();
		private static List<Vector2> uvs = new List<Vector2>();
		private static List<Vector4> tangents = new List<Vector4>();

		public static MeshInfo CreateTiledPlane(Vector3 startPos, Vector3 uDirection, float uDistance, Vector3 vDirection, float vDistance, 
			bool uDecreases, bool vDecreases, bool flipDirection, 
			Vector3 startUVPos,
			float tileSize)
		{
			vertices.Clear();
			triangles.Clear();
			uvs.Clear();
			tangents.Clear();

			uDirection = uDirection.normalized;
			vDirection = vDirection.normalized;

			uDistance = Mathf.Abs(uDistance);
			vDistance = Mathf.Abs(vDistance);

			//1. Find out how many remaining sections should be generated in the u direction

			//1. find the UV based on (Up)
			Plane startPlane = new Plane(Vector3.up, startUVPos);
			float startV = Mathf.Abs(startPlane.GetDistanceToPoint(startPos));
			float startU = Mathf.Sqrt((startPos - startUVPos).sqrMagnitude - (startV * startV));

			//1. Now we have how far away the UV points are in real distances
			float endV = startV + vDistance;
			float endU = startU + uDistance;

			// Find out how many sections should be built for both V and U

			if(float.IsNaN(endU) || float.IsNaN(startU))
				return new MeshInfo();

			int uSections = Mathf.CeilToInt((endU - startU) / tileSize);
			int vSections = Mathf.CeilToInt((endV - startV) / tileSize);

			if(uSections <= 0)
			{
				uSections = Mathf.Abs(uSections) + 1;
			}

			if(vSections <= 0)
				vSections = Mathf.Abs(vSections) + 1;

			int vertCount = 0;

			// Finds the distance that should initially be used
			float initialUDistance = startU / tileSize;
			initialUDistance -= Mathf.FloorToInt(initialUDistance);
			initialUDistance = 1 - initialUDistance;
			initialUDistance *= tileSize;

			if(uDecreases)
			{
				initialUDistance = startU / tileSize;
				initialUDistance -= Mathf.FloorToInt(initialUDistance);
				initialUDistance *= tileSize;
			}

			if(initialUDistance == 0)
				initialUDistance = 3;


			float initialVDistance = startV / tileSize;
			initialVDistance -= Mathf.FloorToInt(initialVDistance);
			initialVDistance = 1 - initialVDistance;
			initialVDistance *= tileSize;

			if(vDecreases)
			{
				initialVDistance = startV / tileSize;
				initialVDistance -= Mathf.FloorToInt(initialVDistance);
				initialVDistance *= tileSize;
			}

			if(initialUDistance == 0)
				initialUDistance = 3;

			for(int v = 0; v <= vSections; v++)
			{
				float startVDistance = 0;
				float endVDistance = tileSize;

				if(v == 0)
				{
					startVDistance = 0;
					endVDistance = initialVDistance;
				}
				else
				{
					startVDistance = initialVDistance + (v - 1) * tileSize;
					endVDistance = initialVDistance + (v * tileSize);
				}

				if(endVDistance > vDistance)
					endVDistance = vDistance;

				if(startVDistance >= endVDistance)
					break;

				for(int u = 0; u <= uSections; u++)
				{
					Vector3 closeBottom, farBottom, closeTop, farTop;
					closeBottom = farBottom = closeTop = farTop = startPos;

					float startUDistance = 0;
					float endUDistance = tileSize;

					if(u == 0)
					{
						startUDistance = 0;
						endUDistance = initialUDistance;
					}
					else
					{
						startUDistance = initialUDistance + (u - 1) * tileSize;
						endUDistance = initialUDistance + (u * tileSize);
					}

					if(endUDistance > uDistance)
						endUDistance = uDistance;

					if(startUDistance >= endUDistance)
						break;

					closeBottom = startPos + uDirection * startUDistance + vDirection * startVDistance;
					closeTop = startPos + uDirection * startUDistance + vDirection * endVDistance;
					farBottom = startPos + uDirection * endUDistance + vDirection * startVDistance;
					farTop = startPos + uDirection * endUDistance + vDirection * endVDistance;

					vertices.Add(closeBottom);
					vertices.Add(farBottom);
					vertices.Add(closeTop);
					vertices.Add(farTop);

					float nearU = 1;
					float farU = 0;
					float bottomV = 0;
					float topV = 1;

					// Calculating all the U values
					if(uDecreases == false)
					{
						if(u == 0)
							nearU = 1 - (startU / tileSize - Mathf.FloorToInt(startU / tileSize));

						if(u == uSections || (u == uSections - 1) && endUDistance == uDistance)
						{
							farU = 1 - (endU / tileSize - Mathf.FloorToInt(endU / tileSize));
						}

						if(nearU == farU)
							farU = 0;
					}
					else
					{
						nearU = 0;
						farU = 1;

						if(u == 0)
							nearU = 1 - (startU / tileSize - Mathf.FloorToInt(startU / tileSize));

						if(u == uSections - 1 && endUDistance == uDistance)
							farU = (farBottom - closeBottom).magnitude / tileSize + nearU;

						if(u == uSections)
							farU = (farBottom - closeBottom).magnitude / tileSize;

						if(farU == nearU)
							nearU = 0;
					}

					// Calculating all the V values
					if(vDecreases == false)
					{
						if(v == 0)
							bottomV = (startV / tileSize) - Mathf.FloorToInt(startV / tileSize);

						if(v == vSections || (v == vSections - 1) && endVDistance == vDistance)
						{
							topV = (endV / tileSize - Mathf.FloorToInt(endV / tileSize));
						}
					}
					else
					{
						bottomV = 1;
						topV = 0;

						nearU = 0;
						farU = 1;

						if(u == 0)
							nearU = (startU / tileSize - Mathf.FloorToInt(startU / tileSize));

						if(u == uSections - 1 && endUDistance == uDistance)
							farU = (farBottom - closeBottom).magnitude / tileSize + nearU;

						if(u == uSections)
							farU = (farBottom - closeBottom).magnitude / tileSize;

						if(farU == nearU)
							nearU = 0;

						if(v == 0)
							bottomV = (startV / tileSize - Mathf.FloorToInt(startV / tileSize));

						if(v == vSections - 1 && endVDistance == vDistance)
						{
							topV = 1 - (closeTop - closeBottom).magnitude / tileSize;
						}

						if(v == vSections)
							topV = 1 - (closeTop - closeBottom).magnitude / tileSize;

						if(v == 0 && vSections == 1)
						{
							topV = bottomV - (closeTop - closeBottom).magnitude / tileSize;
							topV = (float)System.Math.Round(topV, 5);
						}
					}

					// See if we have to flip the normals
					if(flipDirection == false && uDecreases)
					{
						farU = 1 - farU;
						nearU = 1 - nearU;
					}

					if(flipDirection == true && uDecreases == false)
					{
						farU = 1 - farU;
						nearU = 1 - nearU;
					}

					// HACK fixes weird points when the far is on a starting point
					if(uDecreases == false && vDecreases == false)
					{
						if(farU == 1 && flipDirection)
							farU = 0;

						if(farU == 0 && flipDirection)
							farU = 1;
					}

					// HACK to stop some weird tiling problems
					if(farU > 1)
					{
						farU -= 1;
						nearU -= 1;
						//						Debug.Log(nearU);
						//						Debug.Log(farU);
						//						Utility.Draw3DCross(farTop, Color.white);
						//						Utility.Draw3DCross(farTop, 0.5f, Color.red, 5);
						//						Utility.Draw3DCross(farBottom, 0.5f, Color.red, 5);
					}

					if(farU < 0)
					{
						farU += 1;
						nearU += 1;
					}

					if(nearU < 0)
					{
						farU += 1;
						nearU += 1;
					}

					if(nearU > 1)
					{
						farU -= 1;
						nearU -= 1;
					}

					// Need to calculate how much u distance has been had and how much start v distance has been had
					Vector2 closeBottomUv = new Vector2(nearU, bottomV);
					Vector2 farBottomUv = new Vector2(farU, bottomV);
					Vector2 closeTopUv = new Vector2(nearU, topV);
					Vector2 farTopUv = new Vector2(farU, topV);

					uvs.Add(closeBottomUv);
					uvs.Add(farBottomUv);
					uvs.Add(closeTopUv);
					uvs.Add(farTopUv);

					if(flipDirection == false)
					{
						triangles.Add(0 + vertCount);
						triangles.Add(1 + vertCount);
						triangles.Add(2 + vertCount);

						triangles.Add(3 + vertCount);
						triangles.Add(2 + vertCount);
						triangles.Add(1 + vertCount);
					}
					else
					{
						triangles.Add(0 + vertCount);
						triangles.Add(2 + vertCount);
						triangles.Add(1 + vertCount);

						triangles.Add(3 + vertCount);
						triangles.Add(1 + vertCount);
						triangles.Add(2 + vertCount);
					}

					vertCount += 4;
				}
			}

			return new MeshInfo(vertices, triangles, uvs, tangents);
		}

		#endregion

//		/// <summary>
//		/// Generates either an interor or exterior offset. NOTE: Generate bottom is only needed when the bottom is at 0
//		/// </summary>
//		public static MeshInfo GetFrameFromOutline(Vector3 wallStartPos, Vector3 wallEndPos, Vector3 startPos, Vector3 endPos, float bottomHeight, float topHeight, 
//			float outset = -0.1f, bool generateBottom = false)
//		{
//			if(outset == 0)
//				return new MeshInfo();
//
//			if(outset > 0)
//			{
//				wallStartPos = wallEndPos + (wallEndPos - wallStartPos).normalized * 0.2f;
//
//				Vector3 tempPos = startPos;
//				startPos = endPos;
//				endPos = tempPos;
//
//				outset *= -1;
//			}
//
//
//			startPos.y = 0;
//			endPos.y = 0;
//
//			Vector3 startBottom = startPos + Vector3.up * bottomHeight;
//			Vector3 startTop = startPos + Vector3.up * topHeight;
//			Vector3 endBottom = endPos + Vector3.up * bottomHeight;
//			Vector3 endTop = endPos + Vector3.up * topHeight;
//
//			Vector3 directionOfOpening = (endBottom - startBottom).normalized;
//			Vector3 cross = Vector3.Cross(directionOfOpening, Vector3.up);
//
//			Vector3 outsetStartBottom = startBottom + cross * outset;
//			Vector3 outsetEndBottom = endBottom + cross * outset;
//			Vector3 outsetStartTop = startTop + cross * outset;
//			Vector3 outsetEndTop = endTop + cross * outset;
//
//			MeshInfo meshInfo = new MeshInfo();
//
//			meshInfo = BCMesh.CombineMeshInfos(meshInfo, GenerateFirstVerticalSill(wallStartPos, startBottom, startTop, outsetStartBottom, outsetStartTop, bottomHeight));
//			meshInfo = BCMesh.CombineMeshInfos(meshInfo, GenerateSecondVerticalSill(wallStartPos, endBottom, endTop, outsetEndBottom, outsetEndTop, bottomHeight));
//			meshInfo = BCMesh.CombineMeshInfos(meshInfo, GenerateTopSill(wallStartPos, startTop, endTop, outsetStartTop, outsetEndTop));
//
//			if(bottomHeight > 0 || generateBottom)
//				meshInfo = BCMesh.CombineMeshInfos(meshInfo, GenerateBottomSill(wallStartPos, startBottom, endBottom, outsetStartBottom, outsetEndBottom));
//
//			return meshInfo;
//		}
//
//		// The bottom of the wall has to be parallel to the ground or stuff will get funky
//		private static MeshInfo GenerateWallSectionFromFlatSection(Vector3 wholeWallStartPoint,		
//			Vector3 wholeBottomStart, Vector3 wholeBottomEnd, Vector3 wholeTopStart, Vector3 wholeTopEnd,
//			float vStartDistance,
//			out float vertDistanceTravelled, float startUDistance = 0, float maxWallDistance = 3)
//		{
//			MeshInfo meshInfo = new MeshInfo();
//
//			vertDistanceTravelled = 0;
//
//			Vector3 wallDirection = wholeBottomEnd - wholeBottomStart;
//			Plane startPlane = new Plane(wallDirection.normalized, wholeWallStartPoint);
//
//			float bottomStartFromWorldStart = startPlane.GetDistanceToPoint(wholeBottomStart);
//			float topStartFromWorldStart = startPlane.GetDistanceToPoint(wholeTopStart);
//
//			float bottomEndFromWorldStart = startPlane.GetDistanceToPoint(wholeBottomEnd);
//			float topEndFromWorldStart = startPlane.GetDistanceToPoint(wholeTopEnd);
//			float totalUDistance = bottomEndFromWorldStart;
//			if(bottomEndFromWorldStart < topEndFromWorldStart)
//				totalUDistance = topEndFromWorldStart;
//
//			Vector3 thisBottom = wholeBottomStart;
//			Vector3 thisTop = wholeTopStart;
//			Vector3 nextBottom = wholeBottomEnd;
//			Vector3 nextTop = wholeTopEnd;
//
//			// How far along we have travelled so far. If the distance is greater than the UDistance plus the max wall distance, then we have to set end points
//			float currentUDistance = startUDistance;
//			int breaker = 0;
//
//			Vector3 verticalVector = (BCUtils.FindClosestPointOnPlane(thisTop, startPlane) - BCUtils.FindClosestPointOnPlane(thisBottom, startPlane));
//
//			while(currentUDistance < totalUDistance && breaker < 128)
//			{
//				breaker++;
//				if(breaker == 128)
//					Debug.Log("broke it");
//
//				if(startPlane.GetDistanceToPoint(nextBottom) - currentUDistance > maxWallDistance)
//					nextBottom = (wallDirection.normalized * (maxWallDistance - bottomStartFromWorldStart + currentUDistance)) + wholeBottomStart;
//
//				if(startPlane.GetDistanceToPoint(nextTop) - currentUDistance > maxWallDistance)
//					nextTop = (wallDirection.normalized * (maxWallDistance - topStartFromWorldStart + currentUDistance)) + wholeTopStart;
//
//				List<Vector3> verts = new List<Vector3>();
//				verts.Add(thisTop);
//				verts.Add(thisBottom);
//				verts.Add(nextBottom);
//				verts.Add(nextTop);
//
//				List<Vector2> uvs = new List<Vector2>();
//				uvs = new List<Vector2>();
//
//				float vBottom = 1 - vStartDistance / maxWallDistance;
//				float vTop = 1 - (vStartDistance + verticalVector.magnitude) / maxWallDistance;
//				vertDistanceTravelled = verticalVector.magnitude;
//
//				float uTopStart = 1 - startPlane.GetDistanceToPoint(thisTop) / maxWallDistance;
//				float uBottomStart = 1 - startPlane.GetDistanceToPoint(thisBottom) / maxWallDistance;
//				float uBottomEnd = 1 - startPlane.GetDistanceToPoint(nextBottom) / maxWallDistance;
//				float uTopEnd = 1 - startPlane.GetDistanceToPoint(nextTop) / maxWallDistance;
//
//				uvs.Add(new Vector2(uTopStart, vTop));
//				uvs.Add(new Vector2(uBottomStart, vBottom));
//				uvs.Add(new Vector2(uBottomEnd, vBottom));
//				uvs.Add(new Vector2(uTopEnd, vTop));
//
//				List<int> tris = new List<int>();
//				// first triangle
//				tris.Add(2);
//				tris.Add(1);
//				tris.Add(0);
//
//				// second triangle
//				tris.Add(3);
//				tris.Add(2);
//				tris.Add(0);
//
//				thisTop = nextTop;
//				thisBottom = nextBottom;
//
//				nextBottom = wholeBottomEnd;
//				nextTop = wholeTopEnd;
//
//				currentUDistance += maxWallDistance;
//
//				meshInfo = BCMesh.CombineMeshInfos(meshInfo, new MeshInfo(verts, tris, uvs, new List<Vector4>()));
//			}
//
//			return meshInfo;
//		}
//
//		private static MeshInfo GenerateFirstVerticalSill(Vector3 wholeWallStartPoint,		
//			Vector3 wholeBottom, Vector3 wholeTop, Vector3 wholeBottomInset, Vector3 wholeTopInset,
//			float startVDistance, float maxWallDistance = 3)
//		{
//			MeshInfo meshInfo = new MeshInfo();
//			Vector3 point = BCUtils.FindClosestPointOnPlane(wholeBottom, Vector3.up, wholeWallStartPoint);
//			float startUDistance = (point - wholeWallStartPoint).magnitude;
//			float currentVDistance = startVDistance;
//
//			// TODO - Allow greater than 3 meter high insets. Simple to code, but I wanna do other stuff
//			Vector3 insetVector = (wholeBottomInset - wholeBottom);
//			Vector3 insetDirection = insetVector.normalized;
//			float insetDistance = (wholeBottom - wholeBottomInset).magnitude;
//			float currentUDistance = startUDistance;
//
//			float totalUDistance = insetVector.magnitude + startUDistance;
//			float UDistanceTravelled = currentUDistance;
//
//			Vector3 bottom = wholeBottom;
//			Vector3 top = wholeTop;
//			Vector3 insetBottom = wholeBottomInset;
//			Vector3 insetTop = wholeTopInset;
//			Plane wallPlane = new Plane(insetDirection, bottom);
//
//			int breaker = 0;
//			while(UDistanceTravelled < totalUDistance && breaker < 128)
//			{
//				breaker++;
//				if(breaker == 128)
//					Debug.Log("broke it");
//
//				// NOTE: Not entirely clearly why this works, the code in the second window sill 
//				// works better but I don't wanna mess with something that works
//				float thisInsetDistance = (insetBottom - bottom).magnitude;
//				if(thisInsetDistance + currentUDistance >= maxWallDistance)
//				{
//					insetTop = wholeTop + insetDirection * (maxWallDistance - currentUDistance);
//					insetBottom = wholeBottom + insetDirection * (maxWallDistance - currentUDistance);
//
//					if(wallPlane.GetDistanceToPoint(insetTop) < 0)
//					{
//						bottom = wholeBottom;
//						top = wholeTop;
//						insetBottom = wholeBottomInset;
//						insetTop = wholeTopInset;
//						currentUDistance -= maxWallDistance;
//						continue;
//					}
//				}
//
//				// Makes sure that the outset doesn't go above the wall UV : FIX LATER!
//				if(top.y > maxWallDistance)
//					top.y = maxWallDistance;
//				if(insetTop.y > maxWallDistance)
//					insetTop.y = maxWallDistance;
//
//				List<Vector3> verts = new List<Vector3>();
//				verts.Add(top);
//				verts.Add(bottom);
//				verts.Add(insetBottom);
//				verts.Add(insetTop);
//
//				List<Vector2> uvs = new List<Vector2>();
//				uvs = new List<Vector2>();
//
//				float vBottom = currentVDistance / maxWallDistance;
//				float vTop = top.y / maxWallDistance;
//
//				float distanceAlongOutset = (bottom - insetBottom).magnitude;
//				float uStart = ((currentUDistance / maxWallDistance));
//				float uEnd = (currentUDistance + distanceAlongOutset) / maxWallDistance;
//
//				uStart = 1 - uStart;
//				uEnd = 1 - uEnd;
//
//				uvs.Add(new Vector2(uStart, vTop));
//				uvs.Add(new Vector2(uStart, vBottom));
//				uvs.Add(new Vector2(uEnd, vBottom));
//				uvs.Add(new Vector2(uEnd, vTop));
//
//				List<int> tris = new List<int>();
//
//				// first triangle
//				tris.Add(0);
//				tris.Add(1);
//				tris.Add(2);
//
//				// second triangle
//				tris.Add(2);
//				tris.Add(3);
//				tris.Add(0);
//
//				UDistanceTravelled += (insetTop - top).magnitude;
//
//				top = insetTop;
//				bottom = insetBottom;
//				insetTop = wholeTopInset;
//				insetBottom = wholeBottomInset;
//
//				currentUDistance = 0;
//
//				meshInfo = BCMesh.CombineMeshInfos(meshInfo, new MeshInfo(verts, tris, uvs, new List<Vector4>()));
//			}
//
//			return meshInfo;
//		}
//
//		private static MeshInfo GenerateSecondVerticalSill(Vector3 wholeWallStartPoint,		
//			Vector3 wholeBottom, Vector3 wholeTop, Vector3 wholeBottomInset, Vector3 wholeTopInset,
//			float startVDistance, float maxWallDistance = 3)
//		{
//			MeshInfo meshInfo = new MeshInfo();
//
//			Vector3 point = BCUtils.FindClosestPointOnPlane(wholeBottom, Vector3.up, wholeWallStartPoint);
//			float UStartDistance = (point - wholeWallStartPoint).magnitude;
//			float currentVDistance = startVDistance;
//
//			// TODO - Allow greater than 3 meter high insets. Simple to code, but I wanna do other stuff
//			Vector3 insetVector = (wholeBottomInset - wholeBottom);
//			Vector3 insetDirection = insetVector.normalized;
//			float insetDistance = (wholeBottom - wholeBottomInset).magnitude;
//
//			float totalUDistance = insetDistance + UStartDistance;
//			float UDistanceTravelled = 0;
//
//			Vector3 bottom = wholeBottom;
//			Vector3 top = wholeTop;
//			Vector3 insetBottom = wholeBottomInset;
//			Vector3 insetTop = wholeTopInset;
//
//			float sectionDistance = UStartDistance;
//
//			bool renderStart = true;
//
//			float lastUEnd = 0;
//
//			int breaker = 0;
//			while(UDistanceTravelled < totalUDistance && breaker < 128)
//			{
//				breaker++;
//				if(breaker == 128)
//					Debug.Log("broke it");
//
//				if(sectionDistance > maxWallDistance)
//				{
//					sectionDistance -= maxWallDistance;
//					continue;
//				}
//
//				// Now we know the section distance is less than 3
//				if(renderStart == true)
//				{	
//					insetTop = wholeTop + insetVector.normalized * (maxWallDistance - sectionDistance);
//					insetBottom = wholeBottom + insetVector.normalized * (maxWallDistance - sectionDistance);
//				}
//				else
//				{
//					insetTop = top + insetVector.normalized * maxWallDistance;
//					insetBottom = bottom + insetVector.normalized * maxWallDistance;
//				}
//
//				if((insetBottom - wholeBottom).magnitude > insetDistance)
//				{
//					insetTop = wholeTopInset;
//					insetBottom = wholeBottomInset;
//					UDistanceTravelled = float.MaxValue; // This just breaks the while loop
//				}
//
//				// Makes sure that the outset doesn't go above the wall UV : FIX LATER!
//				if(top.y > maxWallDistance)
//					top.y = maxWallDistance;
//				if(insetTop.y > maxWallDistance)
//					insetTop.y = maxWallDistance;
//
//				List<Vector3> verts = new List<Vector3>();
//				verts.Add(top);
//				verts.Add(bottom);
//				verts.Add(insetBottom);
//				verts.Add(insetTop);
//
//				List<Vector2> uvs = new List<Vector2>();
//				uvs = new List<Vector2>();
//
//				float vBottom = currentVDistance / maxWallDistance;
//				float vTop = top.y / maxWallDistance;
//
//				float distanceAlongOutset = (bottom - insetBottom).magnitude;
//
//				float uStart = 1;
//				float uEnd = 0;
//
//				if(renderStart == true)
//				{
//					uStart = 1 - (sectionDistance / maxWallDistance);
//					uEnd = (insetBottom - bottom).magnitude / maxWallDistance + uStart;
//				}
//				else
//				{
//					uStart = lastUEnd;
//					uEnd = (insetBottom - bottom).magnitude / maxWallDistance + uStart;
//				}
//
//				uvs.Add(new Vector2(uStart, vTop));
//				uvs.Add(new Vector2(uStart, vBottom));
//				uvs.Add(new Vector2(uEnd, vBottom));
//				uvs.Add(new Vector2(uEnd, vTop));
//
//				List<int> tris = new List<int>();
//
//				// first triangle
//				tris.Add(0);
//				tris.Add(2);
//				tris.Add(1);
//
//				// second triangle
//				tris.Add(2);
//				tris.Add(0);
//				tris.Add(3);
//
//				top = insetTop;
//				bottom = insetBottom;
//				insetTop = wholeTopInset;
//				insetBottom = wholeBottomInset;
//
//				sectionDistance = 0;
//
//				lastUEnd = uEnd;
//
//				renderStart = false;
//				meshInfo = BCMesh.CombineMeshInfos(meshInfo, new MeshInfo(verts, tris, uvs, new List<Vector4>()));
//			}
//
//			return meshInfo;
//		}
//
//		#region BottomSill
//
//		private static MeshInfo GenerateBottomSill(Vector3 wholeWallStartPoint,		
//			Vector3 start, Vector3 end, Vector3 startInset, Vector3 endInset, 
//			float maxWallDistance = 3)
//		{
//			// Generate the first sill
//			MeshInfo meshInfo = GenerateSingleBottomSill(wholeWallStartPoint, start, end, startInset, endInset, maxWallDistance);
//
//			// Then calculate how many extra sills need to be generated till the end of the wall
//			int breaker = 0;
//
//			Vector3 baseStart = BCUtils.FindClosestPointOnPlane(start, Vector3.up, wholeWallStartPoint);
//			Vector3 baseEnd = BCUtils.FindClosestPointOnPlane(end, Vector3.up, wholeWallStartPoint);
//			float UStartDistance = (baseStart - wholeWallStartPoint).magnitude;
//			float uSectionDistance = UStartDistance;
//
//			int sectionsToFirstPoint = 0;
//			while(breaker < 32)
//			{
//				breaker++;
//				if(breaker == 32)
//					Debug.Log("broke it");
//
//				if(uSectionDistance > maxWallDistance)
//				{
//					uSectionDistance -= maxWallDistance;
//					sectionsToFirstPoint++;
//					continue;
//				}
//				break;
//			}
//
//			Vector3 sillDirection = (end - start).normalized;
//
//			Vector3 newWallStartPoint = wholeWallStartPoint + sillDirection * maxWallDistance * (sectionsToFirstPoint + 1);
//
//			Vector3 insetVector = (startInset - start);
//
//			breaker = 0;
//			while(breaker < 64)
//			{
//				breaker++;
//				newWallStartPoint = wholeWallStartPoint + sillDirection * maxWallDistance * (sectionsToFirstPoint + 1) + start.y * Vector3.up;
//				Vector3 newPointBase = BCUtils.FindClosestPointOnPlane(newWallStartPoint, Vector3.up, wholeWallStartPoint);
//				if((newPointBase - wholeWallStartPoint).sqrMagnitude > (baseEnd - wholeWallStartPoint).sqrMagnitude)
//					return meshInfo;
//
//				Vector3 insetStartPos = newWallStartPoint + insetVector;
//
//				meshInfo = BCMesh.CombineMeshInfos(meshInfo, GenerateSingleBottomSill(newWallStartPoint, 
//					newWallStartPoint, end, 
//					insetStartPos, endInset, maxWallDistance));
//
//				sectionsToFirstPoint++;
//				//				newWallStartPoint = wholeWallStartPoint + sillDirection * maxWallDistance * (sectionsToFirstPoint + 1) +  + start.y * Vector3.up;
//				//
//				//				if((newWallStartPoint - wholeWallStartPoint).sqrMagnitude >= (baseEnd - wholeWallStartPoint).sqrMagnitude)
//				//					return meshInfo;
//			}
//
//			return meshInfo;
//		}
//
//		// 1 generate the first wall section including v distances
//		private static MeshInfo GenerateSingleBottomSill(Vector3 wholeWallStartPoint,		
//			Vector3 start, Vector3 end, Vector3 startInset, Vector3 endInset, 
//			float maxWallDistance = 3)
//		{
//			MeshInfo meshInfo = new MeshInfo();
//
//			float vDistance = start.y;
//
//			Vector3 point = BCUtils.FindClosestPointOnPlane(start, Vector3.up, wholeWallStartPoint);
//			float UStartDistance = (point - wholeWallStartPoint).magnitude;
//
//			Vector3 wallDirection = (end - start).normalized;
//
//			// TODO - Allow greater than 3 meter high insets. Simple to code, but I wanna do other stuff
//			Vector3 insetVector = (startInset - start);
//			Vector3 insetDirection = insetVector.normalized;
//			float insetDistance = insetVector.magnitude;
//
//			Vector3 sectionStart = start;
//			Vector3 sectionEnd = end;
//			Vector3 sectionInsetStart = startInset;
//			Vector3 sectionInsetEnd = endInset;
//
//			float uSectionDistance = UStartDistance;
//			bool renderStart = true;
//			int breaker = 0;
//
//			// Add a while statement here
//			bool finishedUSection = false;
//
//			while(finishedUSection == false && breaker < 32)
//			{
//				breaker++;
//				if(breaker == 32)
//					Debug.Log("broke it");
//
//				if(uSectionDistance > maxWallDistance)
//				{
//					uSectionDistance -= maxWallDistance;
//					continue;
//				}
//
//				float thisInsetDistance = (sectionStart - sectionInsetStart).magnitude;
//				if(thisInsetDistance + vDistance > maxWallDistance)
//				{
//					sectionInsetStart = sectionStart + insetDirection * (maxWallDistance - vDistance);
//					sectionInsetEnd = sectionEnd + insetDirection * (maxWallDistance - vDistance);
//				}	
//
//				float thisDistance = (sectionEnd - sectionStart).magnitude;
//				if(thisDistance + uSectionDistance > maxWallDistance)
//				{
//					sectionEnd = sectionStart + wallDirection * (maxWallDistance - uSectionDistance);
//					sectionInsetEnd = sectionInsetStart + wallDirection * (maxWallDistance - uSectionDistance);
//				}
//
//				if((start - sectionInsetStart).magnitude >= insetDistance)
//				{
//					sectionInsetStart = startInset;
//					sectionInsetEnd = startInset + wallDirection * (sectionEnd - sectionStart).magnitude;
//					finishedUSection = true;
//				}
//
//				List<Vector3> verts = new List<Vector3>();
//				verts.Add(sectionStart);
//				verts.Add(sectionEnd);
//				verts.Add(sectionInsetStart);
//				verts.Add(sectionInsetEnd);
//
//				List<Vector2> uvs = new List<Vector2>();
//				uvs = new List<Vector2>();
//
//				float vBottom = vDistance / maxWallDistance;
//				if(renderStart == false)
//					vBottom = 0;
//				float vTop = ((sectionInsetEnd - sectionEnd).magnitude + vDistance) / maxWallDistance;
//				if(renderStart == false)
//					vTop = ((sectionInsetEnd - sectionEnd).magnitude) / maxWallDistance;
//
//				float distanceAlongOutset = (sectionStart - sectionInsetStart).magnitude;
//
//				float uStart = 1;
//				float uEnd = 0;
//
//				uStart = 1 - (uSectionDistance / maxWallDistance);
//
//				Vector3 endUPoint = sectionStart + (wallDirection * (maxWallDistance - uSectionDistance));
//				uEnd = (endUPoint - sectionEnd).magnitude / maxWallDistance;
//
//				uvs.Add(new Vector2(uStart, vBottom));
//				uvs.Add(new Vector2(uEnd, vBottom));
//				uvs.Add(new Vector2(uStart, vTop));
//				uvs.Add(new Vector2(uEnd, vTop));
//
//				List<int> tris = new List<int>();
//
//				// first triangle
//				tris.Add(0);
//				tris.Add(1);
//				tris.Add(2);
//
//				// second triangle
//				tris.Add(1);
//				tris.Add(3);
//				tris.Add(2);
//
//				sectionEnd = sectionInsetEnd;
//				sectionStart = sectionInsetStart;
//				sectionInsetStart = startInset;
//				sectionInsetEnd = endInset;
//
//				vDistance = 0;
//
//				renderStart = false;
//				meshInfo = BCMesh.CombineMeshInfos(meshInfo, new MeshInfo(verts, tris, uvs, new List<Vector4>()));
//			}
//
//			return meshInfo;
//		}
//
//		#endregion
//
//		#region TopSill
//		private static MeshInfo GenerateTopSill(Vector3 wholeWallStartPoint,		
//			Vector3 start, Vector3 end, Vector3 startInset, Vector3 endInset, 
//			float maxWallDistance = 3)
//		{
//			// Generate the first sill
//			MeshInfo meshInfo = GenerateSingleTopSill(wholeWallStartPoint, start, end, startInset, endInset, maxWallDistance);
//			// Then calculate how many extra sills need to be generated till the end of the wall
//			int breaker = 0;
//
//			Vector3 baseStart = BCUtils.FindClosestPointOnPlane(start, Vector3.up, wholeWallStartPoint);
//			Vector3 baseEnd = BCUtils.FindClosestPointOnPlane(end, Vector3.up, wholeWallStartPoint);
//			float UStartDistance = (baseStart - wholeWallStartPoint).magnitude;
//			float uSectionDistance = UStartDistance;
//
//			int sectionsToFirstPoint = 0;
//			while(breaker < 32)
//			{
//				breaker++;
//				if(breaker == 32)
//					Debug.Log("broke it");
//
//				if(uSectionDistance > maxWallDistance)
//				{
//					uSectionDistance -= maxWallDistance;
//					sectionsToFirstPoint++;
//					continue;
//				}
//				break;
//			}
//
//			Vector3 sillDirection = (end - start).normalized;
//
//			Vector3 newWallStartPoint = wholeWallStartPoint + sillDirection * maxWallDistance * (sectionsToFirstPoint + 1);
//
//			Vector3 insetVector = (startInset - start);
//
//			breaker = 0;
//			while(breaker < 64)
//			{
//				breaker++;
//				newWallStartPoint = wholeWallStartPoint + sillDirection * maxWallDistance * (sectionsToFirstPoint + 1) + start.y * Vector3.up;
//				Vector3 newPointBase = BCUtils.FindClosestPointOnPlane(newWallStartPoint, Vector3.up, wholeWallStartPoint);
//
//				if((newPointBase - wholeWallStartPoint).sqrMagnitude > (baseEnd - wholeWallStartPoint).sqrMagnitude)
//					return meshInfo;
//
//				Vector3 insetStartPos = newWallStartPoint + insetVector;
//
//				meshInfo = BCMesh.CombineMeshInfos(meshInfo, GenerateSingleTopSill(newWallStartPoint, 
//					newWallStartPoint, end, 
//					insetStartPos, endInset, maxWallDistance));
//
//				sectionsToFirstPoint++;
//			}
//
//			return meshInfo;
//		}
//
//		// 1 generate the first wall section including v distances
//		private static MeshInfo GenerateSingleTopSill(Vector3 wholeWallStartPoint,		
//			Vector3 start, Vector3 end, Vector3 startInset, Vector3 endInset, 
//			float maxWallDistance = 3)
//		{
//			MeshInfo meshInfo = new MeshInfo();
//
//			Vector3 point = BCUtils.FindClosestPointOnPlane(start, Vector3.up, wholeWallStartPoint);
//			float UStartDistance = (point - wholeWallStartPoint).magnitude;
//
//			Vector3 wallDirection = (end - start).normalized;
//
//			// TODO - Allow greater than 3 meter high insets. Simple to code, but I wanna do other stuff
//			Vector3 insetVector = (startInset - start);
//			Vector3 insetDirection = insetVector.normalized;
//			float insetDistance = insetVector.magnitude;
//
//			Vector3 sectionStart = start;
//			Vector3 sectionEnd = end;
//			Vector3 sectionInsetStart = startInset;
//			Vector3 sectionInsetEnd = endInset;
//
//			float uSectionDistance = UStartDistance;
//			bool renderStart = true;
//			int breaker = 0;
//
//			// Add a while statement here
//			bool finishedUSection = false;
//
//			float firstInsetDistance = maxWallDistance - (maxWallDistance - start.y);
//			while(finishedUSection == false && breaker < 32)
//			{
//				breaker++;
//				if(breaker == 32)
//					Debug.Log("broke it");
//
//				if(uSectionDistance > maxWallDistance)
//				{
//					uSectionDistance -= maxWallDistance;
//					continue;
//				}
//
//				float thisInsetDistance = (sectionStart - sectionInsetStart).magnitude;
//				if(thisInsetDistance > firstInsetDistance)
//				{
//					sectionInsetStart = sectionStart + insetDirection * (firstInsetDistance);
//					sectionInsetEnd = sectionEnd + insetDirection * (firstInsetDistance);
//				}	
//
//				float thisDistance = (sectionEnd - sectionStart).magnitude;
//				if(thisDistance + uSectionDistance > maxWallDistance)
//				{
//					sectionEnd = sectionStart + wallDirection * (maxWallDistance - uSectionDistance);
//					sectionInsetEnd = sectionInsetStart + wallDirection * (maxWallDistance - uSectionDistance);
//				}
//
//				if((start - sectionInsetStart).magnitude >= insetDistance)
//				{
//					sectionInsetStart = startInset;
//					sectionInsetEnd = startInset + wallDirection * (sectionEnd - sectionStart).magnitude;
//					finishedUSection = true;
//				}
//
//				List<Vector3> verts = new List<Vector3>();
//				verts.Add(sectionStart);
//				verts.Add(sectionEnd);
//				verts.Add(sectionInsetStart);
//				verts.Add(sectionInsetEnd);
//
//				List<Vector2> uvs = new List<Vector2>();
//				uvs = new List<Vector2>();
//
//				float vStart = 1;
//				float vEnd = 0;
//
//				float sectionInsetDistance = (sectionInsetStart - sectionStart).magnitude;
//
//				vStart = start.y / maxWallDistance;
//				if(renderStart == false)
//					vStart = 1;
//				vEnd = vStart - sectionInsetDistance / maxWallDistance;
//
//				float uStart = 1;
//				float uEnd = 0;
//
//				uStart = 1 - (uSectionDistance / maxWallDistance);
//
//				Vector3 endUPoint = sectionStart + (wallDirection * (maxWallDistance - uSectionDistance));
//				uEnd = (endUPoint - sectionEnd).magnitude / maxWallDistance;
//
//				uvs.Add(new Vector2(uStart, vStart));
//				uvs.Add(new Vector2(uEnd, vStart));
//				uvs.Add(new Vector2(uStart, vEnd));
//				uvs.Add(new Vector2(uEnd, vEnd));
//
//				List<int> tris = new List<int>();
//
//				// first triangle
//				tris.Add(0);
//				tris.Add(2);
//				tris.Add(1);
//
//				// second triangle
//				tris.Add(1);
//				tris.Add(2);
//				tris.Add(3);
//
//				sectionEnd = sectionInsetEnd;
//				sectionStart = sectionInsetStart;
//				sectionInsetStart = startInset;
//				sectionInsetEnd = endInset;
//
//				renderStart = false;
//				meshInfo = BCMesh.CombineMeshInfos(meshInfo, new MeshInfo(verts, tris, uvs, new List<Vector4>()));
//
//				firstInsetDistance = maxWallDistance;
//			}
//
//			return meshInfo;
//		}
//		#endregion


	}
}