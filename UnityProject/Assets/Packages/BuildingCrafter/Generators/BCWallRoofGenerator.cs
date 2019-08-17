using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BuildingCrafter;
using System.Linq;
using ClipperLib;

namespace BuildingCrafter
{
	public static partial class BCWallRoofGenerator
	{
		/// <summary>
		/// Generates all the roofs and walls in a blueprint
		/// </summary>
		public static void GenerateWallsAndRoofs(BuildingBlueprint buildingBp, bool generateWalls, bool generateRoofs, bool generateWallCappers)
		{
			if(generateWalls == false && generateRoofs == false && generateWallCappers)
				return;

			List<List<WallInformation[]>> allFloorOutlines = new List<List<WallInformation[]>>();

			// Generate ALL the outlines for ALL the floors first
			for(int i = 0; i < buildingBp.Floors.Count; i++)
				allFloorOutlines.Add(GetWholeFloorOutlines(buildingBp, i));
			
			// 1. We need to generate an outline for each floor at the base
			List<WallInformation[]> floorAboveOutline = null;
			List<WallInformation[]> floorBelowOutline = null;

			for(int floorIndex = buildingBp.Floors.Count - 1; floorIndex >= 0; floorIndex--)
			{
				List<WallInformation[]> wholeFloorOutlines = allFloorOutlines[floorIndex];
				floorAboveOutline = null;
				floorBelowOutline = null;

				if(floorIndex + 1 < buildingBp.Floors.Count)
					floorAboveOutline = allFloorOutlines[floorIndex + 1];
					
				if(floorIndex - 1 >= 0)
					floorBelowOutline = allFloorOutlines[floorIndex - 1];

				GenerateWallsAndRoofs(buildingBp, floorIndex,wholeFloorOutlines, floorAboveOutline, floorBelowOutline, generateWalls, generateRoofs, generateWallCappers);

//				if(generateWalls)
//					GenerateOutsideWallForFloor(wholeFloorOutlines, buildingBp, floorIndex);
				
//				if(generateRoofs)
//				{
//					List<Vector3[]> exteriorVectors, interiorVectors;
//					GetRoofCutouts(wholeFloorOutlines, floorAboveOutline, out exteriorVectors, out interiorVectors, floorIndex);
//
//					// Generate the overhangs
//					if(floorIndex > 0)
//					{
//						List<Vector3[]> floorBelowVectors = ConvertWallsToOutsetPaths(floorBelowOutline);
//						GenerateOverhangs(exteriorVectors, interiorVectors, floorBelowVectors, buildingBp, floorIndex);
//					}
//
//					if(floorIndex == buildingBp.Floors.Count - 1 && buildingBp.RoofInfos.Count > 0)
//					{
//						BCGenerator.GenerateSlantedRoofs(buildingBp);
//					}
//					else
//					{
//						GenerateFlatRoofs(exteriorVectors, interiorVectors, buildingBp, floorIndex);
//
//						// Generate the roof walls AND the building lips
//						List<Vector3[]> exteriorRoofLips = GenerateLipWalls(exteriorVectors, floorAboveOutline);
//						List<WallInformation> exteriorRoofWallLips = CreateWallInfoFromRoofLipPath(exteriorRoofLips, wholeFloorOutlines, buildingBp);
//
//						GenerateRoofWalls(exteriorRoofWallLips, buildingBp, floorIndex);
//						GenerateRoofLips(exteriorRoofWallLips, floorAboveOutline, buildingBp, floorIndex, buildingBp.LastFancySideIndex);
//
//						// Generate the interior roof lips
//						// This needs to be cut away from some stuff
//						List<Vector3[]> interiorRoofLips = GenerateLipWalls(interiorVectors, floorAboveOutline);
//
//						// If there is no plain siding, generate only fancy side
//						int interiorSiding = 1;
//						if(buildingBp.BuildingStyle.PlainSiding == null)
//							interiorSiding = 0;
//						List<WallInformation> interiorRoofWallLips = BCGenerator.StackVectorsToOutsetWallInfos(interiorRoofLips, buildingBp, interiorSiding);
//
//						GenerateRoofWalls(interiorRoofWallLips, buildingBp, floorIndex);
//						GenerateRoofLips(interiorRoofWallLips, floorAboveOutline, buildingBp, floorIndex, buildingBp.LastFancySideIndex);
//					}
//				}
//
//				if(generateWallCappers)
//				{
//					List<WallInformation[]> exteriorWalls, interiorWalls;
//					GetExteriorInteriorWalls(wholeFloorOutlines, out exteriorWalls, out interiorWalls);
//					for(int i = 0; i < buildingBp.Floors[floorIndex].RoomBlueprints.Count; i++)
//					{
//						// TODO - need to transfer the side index from the previous stuff
//
//						WallInformation[] newWall = BCGenerator.CreateWallInfos(buildingBp.Floors[floorIndex].RoomBlueprints[i].PerimeterWalls.ToArray<Vector3>(), -0.1f, 0, buildingBp, buildingBp.Floors[floorIndex]);
//						BCGenerator.OutsetWallInfos(ref newWall);
//						interiorWalls.Add(newWall);
//					}
//
//					GenerateWallCappers(exteriorWalls, interiorWalls, buildingBp, floorIndex);
//				}
			}
		}

		public static void GenerateWallsAndRoofs(BuildingBlueprint buildingBp, int floorIndex, 
			List<WallInformation[]> currentFloor, List<WallInformation[]> aboveFloor, List<WallInformation[]> belowFloor,
			bool generateWalls, bool generateRoofs, bool generateWallCappers)
		{
			if(floorIndex < 0)
				return;

			if(generateWalls)
				GenerateOutsideWallForFloor(currentFloor, buildingBp, floorIndex);

			if(generateRoofs)
			{
				List<Vector3[]> exteriorVectors, interiorVectors;
				GetRoofCutouts(currentFloor, aboveFloor, out exteriorVectors, out interiorVectors, floorIndex);

				GenerateOverhangs(currentFloor, belowFloor, floorIndex, buildingBp);

				if(floorIndex == buildingBp.Floors.Count - 1 && buildingBp.RoofInfos.Count > 0)
				{
					GenerateSlantedRoofs(buildingBp);
				}
				else
				{
					GenerateFlatRoofs(exteriorVectors, interiorVectors, buildingBp, floorIndex);

					// Generate the roof walls AND the building lips
					List<Vector3[]> exteriorRoofLips = GenerateLipWalls(exteriorVectors, aboveFloor, floorIndex);
					List<WallInformation> exteriorRoofWallLips = CreateWallInfoFromRoofLipPath(exteriorRoofLips, currentFloor, buildingBp);

					GenerateRoofWalls(exteriorRoofWallLips, buildingBp, floorIndex);
					GenerateRoofLips(exteriorRoofWallLips, aboveFloor, buildingBp, floorIndex, buildingBp.LastFancySideIndex);

					// Generate the interior roof lips
					// This needs to be cut away from some stuff
					List<Vector3[]> interiorRoofLips = GenerateLipWalls(interiorVectors, aboveFloor, floorIndex);

					// If there is no plain siding, generate only fancy side
					int interiorSiding = 1;
					if(buildingBp.BuildingStyle.PlainSiding == null || buildingBp.BuildingIsAllFancy)
						interiorSiding = 0;
					
					List<WallInformation> interiorRoofWallLips = BCGenerator.StackVectorsToOutsetWallInfos(interiorRoofLips, buildingBp, interiorSiding);

					GenerateRoofWalls(interiorRoofWallLips, buildingBp, floorIndex);
					GenerateRoofLips(interiorRoofWallLips, aboveFloor, buildingBp, floorIndex, buildingBp.LastFancySideIndex);
				}
			}

			if(floorIndex < buildingBp.Floors.Count && floorIndex >= 0 && generateWallCappers)
			{
				List<WallInformation[]> exteriorWalls, interiorWalls;
				GetExteriorInteriorWalls(currentFloor, out exteriorWalls, out interiorWalls);

				for(int i = 0; i < buildingBp.Floors[floorIndex].RoomBlueprints.Count; i++)
				{
					// TODO - need to transfer the side index from the previous stuff
					WallInformation[] newWall = BCGenerator.CreateWallInfos(buildingBp.Floors[floorIndex].RoomBlueprints[i].PerimeterWalls.ToArray<Vector3>(), -0.1f, 0, buildingBp, buildingBp.Floors[floorIndex]);
					BCGenerator.OutsetWallInfos(ref newWall);
					interiorWalls.Add(newWall);
				}

				GenerateWallCappers(exteriorWalls, interiorWalls, buildingBp, floorIndex);
			}
		}

		public static void GenerateOverhangs(List<WallInformation[]> currentFloor, List<WallInformation[]> belowFloor, int floorIndex, BuildingBlueprint buildingBp)
		{
			// Generate the overhangs
			if(floorIndex > 0)
			{
				List<Vector3[]> floorBelowVectors = ConvertWallsToOutsetPaths(belowFloor);
				List<Vector3[]> floorLists = ConvertWallsToOutsetPaths(currentFloor);

				List<Vector3[]> overhangExteriorVectors, overhangInteriorVectors;
				GetExteriorInteriorWalls(floorLists, out overhangExteriorVectors, out overhangInteriorVectors);

				GenerateOverhangs(overhangExteriorVectors, overhangInteriorVectors, floorBelowVectors, buildingBp, floorIndex);
			}
		}

		/// <summary>
		/// Generate the mesh for a slanted roof
		/// </summary>
		/// <param name="buildingBp">Building bp.</param>
		public static void GenerateSlantedRoofs(BuildingBlueprint buildingBp)
		{
			List<MeshInfo> meshInfos = new List<MeshInfo>();

			for(int j = 0; j < buildingBp.RoofInfos.Count; j++)
			{
				RoofInfo roof = buildingBp.RoofInfos[j];

				meshInfos.Add(BCMesh.GenerateGenericMeshInfo(roof.FrontRoof));
				meshInfos.Add(BCMesh.GenerateGenericMeshInfo(roof.BackRoof));
				meshInfos.Add(BCMesh.GenerateGenericMeshInfo(roof.RightRoof));
				meshInfos.Add(BCMesh.GenerateGenericMeshInfo(roof.LeftRoof));

				// Generates the bottom overhang of the roof
				meshInfos.Add(BCMesh.GenerateGenericMeshInfo(new Vector3[4] { 
					roof.BackLeftCorner + (Vector3.left - Vector3.back) * 0.2f, 
					roof.FrontLeftCorner + (Vector3.left - Vector3.forward) * 0.2f, 
					roof.FrontRightCorner + (Vector3.right - Vector3.forward) * 0.2f, 
					roof.BackRightCorner + (Vector3.right - Vector3.back) * 0.2f}));
			}

			Mesh mesh = BCMesh.GetMeshFromMeshInfo(meshInfos, buildingBp.transform.position);
			mesh.name = "Procedural Slanted Roof";

			BCMesh.CalculateMeshTangents(mesh);

			GameObject slantedRoof = BCMesh.GenerateEmptyGameObject("Slanted Roof", true);
			RoofHolder roofHolder = slantedRoof.AddComponent<RoofHolder>();
			roofHolder.FloorIndex = buildingBp.Floors.Count - 1;
			slantedRoof.name = "Slanted Roof";

			MeshFilter meshFilter = slantedRoof.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = slantedRoof.AddComponent<MeshRenderer>();
			meshRenderer.material = buildingBp.BuildingStyle.Rooftop;

			slantedRoof.transform.position = buildingBp.BlueprintXZCenter;

			meshFilter.mesh = mesh;

			slantedRoof.transform.position += buildingBp.Floors.Count * Vector3.up * 3;
			slantedRoof.transform.SetParent(buildingBp.transform);
			slantedRoof.transform.rotation = buildingBp.BuildingRotation;

			// Add a mesh collider to the roof for colliding
			MeshCollider meshCol = slantedRoof.AddComponent<MeshCollider>();
			meshCol.sharedMesh = meshFilter.sharedMesh;

			slantedRoof.transform.SetParent(BCBlueprintUtils.FindOrCreateFloorGameObject(buildingBp, roofHolder.FloorIndex).transform);
		}


		/// <summary>
		/// For generate roofs and walls for a few areas
		/// </summary>
		/// <param name="buildingBp">Building bp.</param>
		/// <param name="floorIndex">Floor index.</param>
		public static void GenerateWallsForFloor(BuildingBlueprint buildingBp, int floorIndex)
		{
			int floorBelowIndex = floorIndex - 1;

			// Destroy the two levels
			BCGenerator.DestroyOutsideWallForFloor(buildingBp, floorIndex);
			BCGenerator.DestroyOutsideWallForFloor(buildingBp, floorBelowIndex);
			BCGenerator.DestroyOverhangsForFloor(buildingBp, floorIndex + 1);
		

			List<WallInformation[]> currentFloor = BCWallRoofGenerator.GetWholeFloorOutlines(buildingBp, floorIndex);
			List<WallInformation[]> aboveFloor = BCWallRoofGenerator.GetWholeFloorOutlines(buildingBp, floorIndex + 1);
			List<WallInformation[]> belowFloor = BCWallRoofGenerator.GetWholeFloorOutlines(buildingBp, floorIndex - 1);
			List<WallInformation[]> belowBelowFloor = BCWallRoofGenerator.GetWholeFloorOutlines(buildingBp, floorIndex - 2);

			BCWallRoofGenerator.GenerateWallsAndRoofs(buildingBp, floorIndex, currentFloor, aboveFloor, belowFloor, true, true, buildingBp.GenerateCappers);
			BCWallRoofGenerator.GenerateWallsAndRoofs(buildingBp, floorIndex - 1, belowFloor, currentFloor, belowBelowFloor, true, true, buildingBp.GenerateCappers);

			// Generates the overhangs above the current floor
			if(floorIndex + 1 < buildingBp.Floors.Count)
			{
				BCWallRoofGenerator.GenerateOverhangs(aboveFloor, currentFloor, floorIndex + 1, buildingBp);
			}
		}

		#region Wall Cutter

		public static List<WallInformation[]> GetWholeFloorOutlines(BuildingBlueprint buildingBp, int floorIndex)
		{
			if(buildingBp == null || floorIndex < 0 || floorIndex >= buildingBp.Floors.Count) return null;

			// Combine all the room walls into 
			List<Vector3[]> wallInfoOutlines = new List<Vector3[]>();
			for(int i = 0; i < buildingBp.Floors[floorIndex].RoomBlueprints.Count; i++)
			{
				Vector3[] outline = buildingBp.Floors[floorIndex].RoomBlueprints[i].PerimeterWalls.ToArray<Vector3>();
				wallInfoOutlines.Add(outline);
			}

			List<Vector3[]> allWallPaths = GetTotalFloorOutline(wallInfoOutlines);

			// now find the interior and exterior paths so we can get the rotated and inset the correct way
			List<Vector3[]> outsideWalls = new List<Vector3[]>();
			List<Vector3[]> interiorWalls = new List<Vector3[]>();

			for(int testingIndex = 0; testingIndex < allWallPaths.Count; testingIndex++)
			{
				bool isInsideAVector = false;
				for(int otherPaths = 0; otherPaths < allWallPaths.Count; otherPaths++)
				{
					if(otherPaths == testingIndex)
						continue;

					if(BCPaths.PolygonInPolygon(allWallPaths[testingIndex], allWallPaths[otherPaths]))
					{
						isInsideAVector = true;
					}
				}

				if(isInsideAVector == false)
					outsideWalls.Add(allWallPaths[testingIndex]);
				else
					interiorWalls.Add(allWallPaths[testingIndex]);
			}

			List<WallInformation[]> wallInfos = new List<WallInformation[]>();
			for(int outsideWallIndex = 0; outsideWallIndex < outsideWalls.Count; outsideWallIndex++)
			{
				Vector3[] wallPath = outsideWalls[outsideWallIndex];

				// Make sure all paths are clockwise
				if(BCUtils.IsClockwisePolygon(wallPath) == false)
					wallPath = wallPath.Reverse().ToArray<Vector3>();
				
				// Now from these exterior walls, we need to figure out which side is "fancy" versus "plain"
				int sideIndex = 0;
				WallInformation[] wallInfo = BCGenerator.CreateWallInfos(wallPath, 0.1f, sideIndex, buildingBp, buildingBp.Floors[floorIndex]);

				// GETTING THE PROPER WALL INFORMATION HERE
				// Find the average direction of the wall, then compare it to the directions to see which way it is pointing
				int[] fourCorners = GetFourIndexCornersClockwise(wallPath);

				// if the path loops, everthing after the loop has to have the length of the path added so it can be calculated right
				int startFront = fourCorners[0];
				int startLeft = fourCorners[1];
				int startBack = fourCorners[2];
				int startRight = fourCorners[3];
				int pathLength = wallPath.Length;

				for(int i = 0; i < wallPath.Length - 1; i++)
				{
					// FRONT
					if(FallsWithinTwoIndexes(startFront, startLeft, i, pathLength))
					{
						if(buildingBp.FancyFront == false)
							wallInfo[i].SideIndex = 1;
						continue;
					}

					// LEFT
					if(FallsWithinTwoIndexes(startLeft, startBack, i, pathLength))
					{
						if(buildingBp.FancyLeftSide == false)
							wallInfo[i].SideIndex = 1;
						continue;
					}

					// BACK
					if(FallsWithinTwoIndexes(startBack, startRight, i, pathLength))
					{
						if(buildingBp.FancyBack == false)
							wallInfo[i].SideIndex = 1;
						continue;
					}

					// Right
					if(FallsWithinTwoIndexes(startRight, startLeft, i, pathLength))
					{
						if(buildingBp.FancyRightSide == false)
							wallInfo[i].SideIndex = 1;
						continue;
					}
				}

				BCGenerator.OutsetWallInfos(ref wallInfo);

				wallInfos.Add(wallInfo);
			}

			// Create all the interior walls with a plain side index
			for(int i = 0; i < interiorWalls.Count; i++)
			{
				Vector3[] wallPath = interiorWalls[i];

				// Make sure all paths are clockwise
				if(BCUtils.IsClockwisePolygon(wallPath) == false)
					wallPath = wallPath.Reverse().ToArray<Vector3>();

				// Make ALL the interior walls plain unless the whole building is fancy
				int sideIndex = 1;
				if(buildingBp.BuildingIsAllFancy)
					sideIndex = 0;

				WallInformation[] wallInfo = BCGenerator.CreateWallInfos(wallPath, -0.1f, sideIndex, buildingBp, buildingBp.Floors[floorIndex]);
				BCGenerator.OutsetWallInfos(ref wallInfo);

				wallInfos.Add(wallInfo);
			}

			// At this point we have the floors "walls" including the offsets they may have and all the information for the openings
			return wallInfos;
		}	

		public static bool FallsWithinTwoIndexes(int start, int end, int index, int pathLength)
		{
			int currentIndex = index;
			int startIndex = start;
			int endIndex = end;
			if(startIndex > endIndex)
			{
				if(currentIndex < startIndex)
					currentIndex += pathLength;
				endIndex += pathLength;
			}

			if(currentIndex >= startIndex && currentIndex < endIndex)
				return true;

			return false;
		}

		public static int GetSideIndex(Vector3 directionOfWallSide, BuildingBlueprint buildingBp)
		{
			if(buildingBp.BuildingStyle.PlainSiding == null)
				return 0;

			if(directionOfWallSide == BCWallRoofGenerator.forwardBuilding && buildingBp.FancyFront == false)
				return 1;
			else if(directionOfWallSide == BCWallRoofGenerator.leftBuilding && buildingBp.FancyLeftSide == false)
				return 1;
			else if(directionOfWallSide == BCWallRoofGenerator.backwardBuilding && buildingBp.FancyBack == false)
				return 1;
			else if(directionOfWallSide == BCWallRoofGenerator.rightBuilding && buildingBp.FancyRightSide == false)
				return 1;

			return 0;
		}

		#endregion

		#region Wall Conversion

		public static List<Vector3[]> ConvertWallsToOutsetPaths(List<WallInformation[]> wallInfos)
		{
			if(wallInfos == null)
				return null;

			List<Vector3[]> newPaths = new List<Vector3[]>();
			for(int wIndex = 0; wIndex  < wallInfos.Count; wIndex++)
			{
				List<Vector3> newPath = new List<Vector3>(wallInfos[wIndex].Length + 1);

				for(int i = 0; i < wallInfos[wIndex].Length; i++)
				{
					newPath.Add(wallInfos[wIndex][i].StartOffset);
				}
				newPath.Add(wallInfos[wIndex][wallInfos[wIndex].Length - 1].EndOffset);
				newPaths.Add(newPath.ToArray<Vector3>());
			}

			return newPaths;
		}

		static Clipper wallOutlineClippy;
		static List<List<IntPoint>> floorOutlineSolutions = new List<List<IntPoint>>();

		/// <summary>
		/// Returns the floor outline of the given paths
		/// </summary>
		/// <returns>The floor outline.</returns>
		/// <param name="paths">Paths.</param>
		public static List<Vector3[]> GetTotalFloorOutline(List<Vector3[]> paths)
		{
			if(paths.Count < 1)
				return paths;

			if(wallOutlineClippy == null)
			{
				wallOutlineClippy = new Clipper();
				floorOutlineSolutions = new List<List<IntPoint>>();
			}
			else
			{
				wallOutlineClippy.Clear();
				floorOutlineSolutions.Clear();
			}

			wallOutlineClippy.AddPath(BCPaths.GetIntPoints(paths[0]), PolyType.ptSubject, true);

			for(int i = 0; i < paths.Count; i++)
			{
				wallOutlineClippy.AddPath(BCPaths.GetIntPoints(paths[i]), PolyType.ptClip, true);
			}

			if(wallOutlineClippy.Execute(ClipType.ctUnion, floorOutlineSolutions, PolyFillType.pftNonZero))
			{
				List<Vector3[]> newPath = new List<Vector3[]>();

				for(int clipIndex = 0; clipIndex < floorOutlineSolutions.Count; clipIndex++)
					newPath.Add(BCPaths.GetPoints(floorOutlineSolutions[clipIndex]));

				return newPath;
			}

			return paths;
		}

		public static List<Vector3[]> WallInfosToVectors(List<WallInformation[]> wallInfos, bool useOutset)
		{
			if(wallInfos == null)
				return null;
			
			List<Vector3[]> newVectors = new List<Vector3[]>();

			for(int wIndex = 0; wIndex < wallInfos.Count; wIndex++)
				newVectors.Add(WallInfosToVectors(wallInfos[wIndex], useOutset));
			
			return newVectors;
		}

		public static Vector3[] WallInfosToVectors(WallInformation[] wallInfo, bool useOutset)
		{
			Vector3[] newVector = new Vector3[wallInfo.Length + 1];

			if(useOutset == true)
			{
				for(int i = 0; i < wallInfo.Length; i++)
					newVector[i] = wallInfo[i].StartOffset;
				newVector[newVector.Length - 1] =  wallInfo[wallInfo.Length - 1].EndOffset;
			}
			else
			{
				for(int i = 0; i < wallInfo.Length; i++)
					newVector[i] = wallInfo[i].Start;
				newVector[newVector.Length - 1] =  wallInfo[wallInfo.Length - 1].End;
			}

			return newVector;
		}

		public static void GetExteriorInteriorWalls(List<WallInformation[]> wallInfos, out List<WallInformation[]> exteriorWalls, out List<WallInformation[]> interiorWalls)
		{
			// now find the interior and exterior paths so we can get the rotated and inset the correct way
			exteriorWalls = new List<WallInformation[]>();
			interiorWalls = new List<WallInformation[]>();

			List<Vector3[]> testingPoints = WallInfosToVectors(wallInfos, false);

			if(wallInfos == null || wallInfos.Count < 1)
				return;

			for(int testingIndex = 0; testingIndex < testingPoints.Count; testingIndex++)
			{
				bool isInsideAVector = false;
				for(int otherPaths = 0; otherPaths < testingPoints.Count; otherPaths++)
				{
					if(otherPaths == testingIndex)
						continue;

					if(BCPaths.PolygonInPolygon(testingPoints[testingIndex], testingPoints[otherPaths]))
					{
						isInsideAVector = true;
					}
				}

				if(isInsideAVector == false)
					exteriorWalls.Add(wallInfos[testingIndex]);
				else
					interiorWalls.Add(wallInfos[testingIndex]);
			}
		}

		public static void GetExteriorInteriorWalls(List<Vector3[]> wallInfos, out List<Vector3[]> exteriorWalls, out List<Vector3[]> interiorWalls)
		{
			// now find the interior and exterior paths so we can get the rotated and inset the correct way
			exteriorWalls = new List<Vector3[]>();
			interiorWalls = new List<Vector3[]>();

			if(wallInfos == null)
				return;
		
			for(int testingIndex = 0; testingIndex < wallInfos.Count; testingIndex++)
			{
				// Make sure the wall is going the correct way (clockwise)
				bool isInsideAVector = false;
				for(int otherPaths = 0; otherPaths < wallInfos.Count; otherPaths++)
				{
					if(otherPaths == testingIndex)
						continue;

					if(BCPaths.PolygonInPolygon(wallInfos[testingIndex], wallInfos[otherPaths]))
					{
						isInsideAVector = true;
					}
				}

				if(isInsideAVector == false)
					exteriorWalls.Add(wallInfos[testingIndex]);
				else
					interiorWalls.Add(wallInfos[testingIndex]);
			}
		}

		#endregion

		#region Roof Cutouts

		public static void GetRoofCutouts(List<WallInformation[]> floor, List<WallInformation[]> wallAbove, out List<Vector3[]> exteriorVectors, out List<Vector3[]> interiorVectors, int floorIndex)
		{
			List<WallInformation[]> exteriorWalls, interiorWalls;
			GetExteriorInteriorWalls(floor, out exteriorWalls, out interiorWalls);

			List<Vector3[]> floorAboveVectors = ConvertWallsToOutsetPaths(wallAbove);
			List<Vector3[]> exVectors = ConvertWallsToOutsetPaths(exteriorWalls);
			List<Vector3[]> intVectors = ConvertWallsToOutsetPaths(interiorWalls);

			List<Vector3[]> tempInteriors = BCPaths.GetOverhang(exVectors, floorAboveVectors);
			tempInteriors = BCPaths.CutOutInteriorsFromOverhang(tempInteriors, intVectors);

			GetExteriorInteriorWalls(tempInteriors, out exteriorVectors, out interiorVectors);
		}

		public static List<Vector3[]> GetTotalFloorOutline(WallInformation[] paths,  List<WallInformation[]> interiorWalls, List<WallInformation[]> abovePaths)
		{
			if(wallOutlineClippy == null)
			{
				wallOutlineClippy = new Clipper();
				floorOutlineSolutions = new List<List<IntPoint>>();
			}
			else
			{
				wallOutlineClippy.Clear();
				floorOutlineSolutions.Clear();
			}

			Vector3[] vectorPath = WallInfosToVectors(paths, true);

			wallOutlineClippy.AddPath(BCPaths.GetIntPoints(vectorPath), PolyType.ptSubject, true);

			if(abovePaths != null)
			{			
				for(int i = 0; i < abovePaths.Count; i++)
				{
					wallOutlineClippy.AddPath(BCPaths.GetIntPoints(WallInfosToVectors(abovePaths[i], true)), PolyType.ptClip, true);
				}
			}

			for(int i = 0; i < interiorWalls.Count; i++)
			{
				wallOutlineClippy.AddPath(BCPaths.GetIntPoints(WallInfosToVectors(interiorWalls[i], true)), PolyType.ptClip, true);
			}

			List<Vector3[]> newPath = new List<Vector3[]>();
			if(wallOutlineClippy.Execute(ClipType.ctDifference, floorOutlineSolutions, PolyFillType.pftNonZero))
			{
				for(int clipIndex = 0; clipIndex < floorOutlineSolutions.Count; clipIndex++)
					newPath.Add(BCPaths.GetPoints(floorOutlineSolutions[clipIndex]));
			}

			return newPath;
		}

		#endregion

		#region

		private static bool IsPointIntersecting(Vector3 point, List<Vector3[]> otherPaths, int epsilon = 5000)
		{
			for(int i = 0; i < otherPaths.Count; i++)
			{
				if(BCPaths.PointOnPolygonCorner(point, otherPaths[i], epsilon) == true 
					|| BCPaths.PointInPolygonXZ(point, otherPaths[i], epsilon) == true
					|| BCUtils.PointOnPolygonXZ(point, otherPaths[i], 0.001f) == true
					|| BCPaths.PointIndexAlongPolygon(point, otherPaths[i], 100) > -1)
				{	
					return true;
				}
			}
			return false;
		}

		private static bool IsPointOnPath(Vector3 point, List<Vector3[]> otherPaths)
		{
			for(int i = 0; i < otherPaths.Count; i++)
			{
				if(BCPaths.PointOnPolygonCorner(point, otherPaths[i], 5000) == true 
					|| BCUtils.PointOnPolygonXZ(point, otherPaths[i], 0.001f) == true
					|| BCPaths.PointIndexAlongPolygon(point, otherPaths[i], 100) > -1)
				{	
					return true;
				}
			}
			return false;
		}

		public static List<Vector3[]> GenerateLipWalls(List<Vector3[]> vectorGroups, List<WallInformation[]> aboveWallsGroups, int floorIndex)
		{
			if(aboveWallsGroups == null)
			{
				// Properly order all the cutouts for creation
				List<Vector3[]> properDirection = new List<Vector3[]>(vectorGroups.Count);
				for(int i = 0; i < vectorGroups.Count; i++)
				{
					properDirection.Add(vectorGroups[i].ToArray<Vector3>());
				}

				return properDirection;
			}

			List<Vector3[]> separateLips = new List<Vector3[]>();
			List<Vector3[]> thisLipSection = new List<Vector3[]>();
			List<Vector3> lipSection = new List<Vector3>();

			List<Vector3[]> aboveGroups = WallInfosToVectors(aboveWallsGroups, true);
			List<Vector3[]> interiorVectors = new List<Vector3[]>();
			List<Vector3[]> exteriorVectors = new List<Vector3[]>();

			GetExteriorInteriorWalls(aboveGroups, out exteriorVectors, out interiorVectors);

			for(int vIndex = 0; vIndex < vectorGroups.Count; vIndex++)
			{
				lipSection.Clear();
				thisLipSection.Clear();

				bool lastPointIsIntersecting = false;

				for(int pointIndex = 0; pointIndex < vectorGroups[vIndex].Length; pointIndex++)
				{
					Vector3 point = vectorGroups[vIndex][pointIndex];
					bool pointIsIntersecting = IsPointIntersecting(vectorGroups[vIndex][pointIndex], aboveGroups);

					// Always add the point at the start
					lipSection.Add(point);

					if(pointIsIntersecting == true && lastPointIsIntersecting == true)
						lipSection.Clear();

					if(lipSection.Count == 0 && pointIsIntersecting)
						lipSection.Add(point);

					if(pointIsIntersecting == true && lipSection.Count >= 2)
					{
						thisLipSection.Add(lipSection.ToArray<Vector3>());
						lipSection.Clear();
					}	

					// Completes the loop
					if(pointIndex == vectorGroups[vIndex].Length - 1)
					{
						if(lipSection.Count >= 2)
						{
							thisLipSection.Add(lipSection.ToArray<Vector3>());
							lipSection.Clear();
							break;
						}
					}

					lastPointIsIntersecting = pointIsIntersecting;
				}

				// Join the end and start IF they are valid combos
				if(thisLipSection.Count >= 2)
				{
					Vector3 startPoint = thisLipSection[0][0];

					int sectionCount = thisLipSection.Count;
					int pointCount = thisLipSection[sectionCount - 1].Length;
					Vector3 endPoint = thisLipSection[sectionCount - 1][pointCount - 1];

					// Test to see if their is a loop, if loop join start
					if(BCUtils.ArePointsCloseEnough(startPoint, endPoint))
					{
						if(IsPointIntersecting(startPoint, aboveGroups) == false)
						{
							Vector3[] newSection = new Vector3[thisLipSection[sectionCount - 1].Length + thisLipSection[0].Length - 1];

							for(int i = 0; i < thisLipSection[sectionCount - 1].Length; i++)
								newSection[i] = thisLipSection[sectionCount - 1][i];

							for(int i = 0; i < thisLipSection[0].Length - 1; i++)
								newSection[i + pointCount] = thisLipSection[0][i + 1];
							
							thisLipSection.RemoveAt(thisLipSection.Count - 1);
							thisLipSection.RemoveAt(0);

							thisLipSection.Add(newSection);
						}
					}
				}
				separateLips.AddRange(thisLipSection);
			}

			// Find if this point AND next point are along an edge
			// If they are both along an edge, test the mid point and see if it is inside. if not, then add just that section
			for(int vIndex = 0; vIndex < vectorGroups.Count; vIndex++)
			{
				lipSection.Clear();
				thisLipSection.Clear();

				for(int pointIndex = 0; pointIndex < vectorGroups[vIndex].Length - 1; pointIndex++)
				{
					Vector3 point = vectorGroups[vIndex][pointIndex];
					Vector3 nextPoint = vectorGroups[vIndex][pointIndex + 1];
					bool pointIsIntersecting = IsPointIntersecting(point, aboveGroups);
					bool nextPointIsIntersecting = IsPointIntersecting(nextPoint, aboveGroups);

					if(pointIsIntersecting && nextPointIsIntersecting)
					{	
						// Test for midpoint inside the above group or ON the group line
						Vector3 midPoint = (point + nextPoint) / 2;

						if(IsPointIntersecting(midPoint, aboveGroups) == false)
						{
							separateLips.Add(new Vector3[] { point, nextPoint} );
						}
						if(IsPointIntersecting(midPoint, interiorVectors) == true && IsPointOnPath(midPoint, interiorVectors) == false)
						{
							separateLips.Add(new Vector3[] { point, nextPoint} );
						}
					}
				}
			}

			return separateLips;
		}

		public static bool CombineEnds(ref List<Vector3[]> section, List<Vector3[]> aboveGroups, ref List<int> segementSideIndex)
		{
			int tIndex = -1;
			int oIndex = -1;
			bool foundPoint = false;

			for(int testingIndex = 0; testingIndex < section.Count; testingIndex++)
			{
				for(int otherIndex = 0; otherIndex < section.Count; otherIndex++)
				{
					if(otherIndex == testingIndex) // Do not test against each other
						continue;

					// Test the start and end
					Vector3 startPoint = section[testingIndex][0];
					Vector3 endPoint = section[otherIndex][section[otherIndex].Length - 1];
	
					if(BCUtils.ArePointsCloseEnough(startPoint, endPoint))
					{
						if(aboveGroups != null && IsPointIntersecting(startPoint, aboveGroups) == true)
							continue;
						
						// Check for different sides here, if the sides aren't different, then we just return true
						if(segementSideIndex[testingIndex] != segementSideIndex[otherIndex])
							continue;

						tIndex = testingIndex;
						oIndex = otherIndex;

						foundPoint = true;
						break;
					}

					if(foundPoint) break;
				}

				if(foundPoint) break;
			}

			if(foundPoint == true && tIndex != oIndex)
			{
				Vector3[] newSection = new Vector3[section[oIndex].Length + section[tIndex].Length - 1];
				int pointCount = section[oIndex].Length;

				for(int i = 0; i < section[oIndex].Length; i++)
					newSection[i] = section[oIndex][i];

				for(int i = 0; i < section[tIndex].Length - 1; i++)
					newSection[i + pointCount] = section[tIndex][i + 1];

				int sideIndex = segementSideIndex[tIndex];

				if(tIndex > oIndex)
				{
					section.RemoveAt(tIndex);
					section.RemoveAt(oIndex);

					segementSideIndex.RemoveAt(tIndex);
					segementSideIndex.RemoveAt(oIndex);

				}
				else
				{
					section.RemoveAt(oIndex);
					section.RemoveAt(tIndex);

					segementSideIndex.RemoveAt(oIndex);
					segementSideIndex.RemoveAt(tIndex);
				}

				// Need to remove the two side indexes and put in the new one at the end
				section.Add(newSection);
				// Need to update the reference of what type of side is used here
				segementSideIndex.Add(sideIndex);

				if(CombineEnds(ref section, aboveGroups, ref segementSideIndex) == false)
					return false;

				return true;
			}

			return false;
		}

		#endregion

		#region

		public static void GenerateRoofWalls(List<WallInformation> stackedWallInfos, BuildingBlueprint buildingBp, int floorIndex)
		{
			int fancySideIndex = buildingBp.LastFancySideIndex;

			List<MeshInfo> fancyMeshInfos = new List<MeshInfo>();
			List<MeshInfo> plainMeshInfos = new List<MeshInfo>();

			for(int i = 0; i < stackedWallInfos.Count; i++)
			{
				WallInformation wallInfo = stackedWallInfos[i];

				wallInfo.Openings = null; // REMOVE any opening information from this wall section
				float startDistance = 0;

				WallInformation newWall = new WallInformation(wallInfo);

				newWall.StartOffset = wallInfo.EndOffset;
				newWall.EndOffset = wallInfo.StartOffset;

				if(newWall.SideIndex == 0)
					fancyMeshInfos.Add(BCTiledWall.CreateSingleWall(newWall, 0.5f, 3f, startDistance, out startDistance, true));
				else
					plainMeshInfos.Add(BCTiledWall.CreateSingleWall(newWall, 0.5f, 3f, startDistance, out startDistance, true));
			}

			if(fancyMeshInfos.Count > 0)
			{	
				Mesh m = BCMesh.GetMeshFromMeshInfo(fancyMeshInfos, buildingBp.BlueprintXZCenter);
				m.name = "Procedural Outside RoofWall " + floorIndex;

				// Recalculates the tangents for the mesh
				BCMesh.CalculateMeshTangents(m);

				GameObject outsideWalls = BCMesh.GenerateEmptyGameObject("Create Outside Walls", true);
				outsideWalls.AddComponent<RoofLipHolder>().FloorIndex = floorIndex;

				outsideWalls.transform.position = buildingBp.BlueprintXZCenter;
				outsideWalls.name = "Outside Fancy RoofWall (Floor " + (floorIndex + 1) + ")";
				MeshRenderer meshRenderer = outsideWalls.AddComponent<MeshRenderer>();
				MeshFilter meshFilter = outsideWalls.AddComponent<MeshFilter>();

				meshFilter.mesh = m;

				if(buildingBp.BuildingStyle.FancySidings.Length < 1)
				{
					Debug.LogError(buildingBp.BuildingStyle.name + " does not contain a fancy siding option, please fix.");
					return;
				}

				Material fancySiding = buildingBp.BuildingStyle.FancySidings[fancySideIndex];
				if(fancySiding == null)
					Debug.LogError(buildingBp.BuildingStyle.name + " does not contain a fancy siding option, please fix.");

				meshRenderer.material = fancySiding;

				// Sets the parent of the outside wall to this thing
				outsideWalls.transform.SetParent(BCBlueprintUtils.FindOrCreateFloorGameObject(buildingBp, floorIndex).transform);
				outsideWalls.transform.localPosition = new Vector3(outsideWalls.transform.localPosition.x, 3, outsideWalls.transform.localPosition.z);
				outsideWalls.transform.rotation = buildingBp.BuildingRotation;
			}

			if(plainMeshInfos.Count > 0)
			{	
				Mesh m = BCMesh.GetMeshFromMeshInfo(plainMeshInfos, buildingBp.BlueprintXZCenter);
				m.name = "Procedural Outside RoofWall " + floorIndex;

				// Recalculates the tangents for the mesh
				BCMesh.CalculateMeshTangents(m);

				GameObject outsideWalls = BCMesh.GenerateEmptyGameObject("Create Outside Walls", true);
				outsideWalls.AddComponent<RoofLipHolder>().FloorIndex = floorIndex;

				outsideWalls.transform.position = buildingBp.BlueprintXZCenter;
				outsideWalls.name = "Outside Plain RoofWall (Floor " + (floorIndex + 1) + ")";
				MeshRenderer meshRenderer = outsideWalls.AddComponent<MeshRenderer>();
				MeshFilter meshFilter = outsideWalls.AddComponent<MeshFilter>();

				meshFilter.mesh = m;

				if(buildingBp.BuildingStyle.FancySidings.Length < 1)
				{
					Debug.LogError(buildingBp.BuildingStyle.name + " does not contain a fancy siding option, please fix.");
					return;
				}

				Material plainSiding = buildingBp.BuildingStyle.PlainSiding;
				if(plainSiding == null)
					plainSiding = buildingBp.BuildingStyle.FancySidings[fancySideIndex];

				meshRenderer.material = plainSiding;

				// Sets the parent of the outside wall to this thing
				outsideWalls.transform.SetParent(BCBlueprintUtils.FindOrCreateFloorGameObject(buildingBp, floorIndex).transform);
				outsideWalls.transform.localPosition = new Vector3(outsideWalls.transform.localPosition.x, 3, outsideWalls.transform.localPosition.z);
				outsideWalls.transform.rotation = buildingBp.BuildingRotation;
			}
		}

		#endregion

		#region Mesh Generation

		public static void GenerateOutsideWallForFloor(List<WallInformation[]> wallInfos, BuildingBlueprint buildingBp, int floorIndex)
		{
			if(wallInfos == null)
				return;

			int fancySideIndex = buildingBp.LastFancySideIndex;

			List<MeshInfo> fancyMeshInfos = new List<MeshInfo>();
			List<MeshInfo> plainMeshInfos = new List<MeshInfo>();

			for(int wallIndex = 0; wallIndex < wallInfos.Count; wallIndex++)
			{
				for(int i = 0; i < wallInfos[wallIndex].Length; i++)
				{
					bool interiorWall = wallInfos[wallIndex][i].Outset < 0;
					WallInformation wallInfo = wallInfos[wallIndex][i];

					MeshInfo openingMeshes = new MeshInfo();

					float startDistance = 0;

					if(wallInfo.Openings != null && wallInfo.Openings.Length > 0)
					{
						for(int openingIndex = 0; openingIndex < wallInfo.Openings.Length; openingIndex++)
						{
							if(wallInfo.Openings[openingIndex].NoFrame == true)
								continue;

							Vector3 wallStart = wallInfo.StartOffset;
							Vector3 openingStart = wallInfo.Openings[openingIndex].GetStartPositionOutset(wallInfo, 0) + Vector3.up *  wallInfo.Openings[openingIndex].Bottom;
							Vector3 openingEnd = wallInfo.Openings[openingIndex].GetEndPositionOutset(wallInfo, 0) + Vector3.up *  wallInfo.Openings[openingIndex].Bottom;

							bool generateBottomLip = false;
							if(wallInfo.Openings[openingIndex].Bottom > 0)
								generateBottomLip = true;
							if(floorIndex > 0)
								generateBottomLip = true;


							Vector3 wallDirection = (openingEnd - openingStart).normalized;
							Vector3 cross = Vector3.Cross(wallDirection.normalized, Vector3.up) * -1;

							float height =  wallInfo.Openings[openingIndex].Top -  wallInfo.Openings[openingIndex].Bottom;
							openingMeshes = BCMesh.CombineMeshInfos(openingMeshes, 
								BCFrameGenerator.CreateFrame(openingStart, openingEnd, cross, 0.1f, height, wallStart, true, 3, generateBottomLip));
						}
					}
					if(wallInfo.SideIndex == 0)
					{
						fancyMeshInfos.Add(BCTiledWall.CreateSingleWall(wallInfos[wallIndex][i], buildingBp.Floors[floorIndex].Height, 3, startDistance, out startDistance, !interiorWall));
						if(openingMeshes.IsValid)
							fancyMeshInfos.Add(openingMeshes);
					}
					else
					{
						plainMeshInfos.Add(BCTiledWall.CreateSingleWall(wallInfos[wallIndex][i], buildingBp.Floors[floorIndex].Height, 3, startDistance, out startDistance, !interiorWall));
						if(openingMeshes.IsValid)
							plainMeshInfos.Add(openingMeshes);
					}
				}
			}

			// CREATE THE FANCY MESH INFO SIDE FIRST
			if(fancyMeshInfos.Count > 0)
			{	
				Mesh m = BCMesh.GetMeshFromMeshInfo(fancyMeshInfos, buildingBp.BlueprintXZCenter);
				m.name = "Procedural Outside Wall Floor " + floorIndex;

				// Recalculates the tangents for the mesh
				BCMesh.CalculateMeshTangents(m);


				GameObject outsideWalls = BCMesh.GenerateEmptyGameObject("Create Outside Walls", true);
				outsideWalls.AddComponent<OutsideWallHolder>().FloorIndex = floorIndex;

				outsideWalls.transform.position = buildingBp.BlueprintXZCenter;
				outsideWalls.name = "Outside Fancy Wall (Floor " + (floorIndex + 1) + ")";
				MeshRenderer meshRenderer = outsideWalls.AddComponent<MeshRenderer>();
				MeshFilter meshFilter = outsideWalls.AddComponent<MeshFilter>();

				meshFilter.mesh = m;

				if(buildingBp.BuildingStyle.FancySidings.Length < 1)
				{
					Debug.LogError(buildingBp.BuildingStyle.name + " does not contain a fancy siding option, please fix.");
					return;	
				}

				Material fancySiding = buildingBp.BuildingStyle.FancySidings[fancySideIndex];
				if(fancySiding == null)
					Debug.LogError(buildingBp.BuildingStyle.name + " does not contain a fancy siding option, please fix.");

				meshRenderer.material = fancySiding;

				// Sets the parent of the outside wall to this thing
				outsideWalls.transform.SetParent(BCBlueprintUtils.FindOrCreateFloorGameObject(buildingBp, floorIndex).transform);
				outsideWalls.transform.localPosition = new Vector3(outsideWalls.transform.localPosition.x, 0, outsideWalls.transform.localPosition.z);
				outsideWalls.transform.rotation = buildingBp.BuildingRotation;
			}

			if(plainMeshInfos.Count > 0)
			{	
				Mesh m = BCMesh.GetMeshFromMeshInfo(plainMeshInfos, buildingBp.BlueprintXZCenter);
				m.name = "Procedural Outside Wall Floor " + floorIndex;

				// Recalculates the tangents for the mesh
				BCMesh.CalculateMeshTangents(m);

				GameObject outsideWalls = BCMesh.GenerateEmptyGameObject("Create Outside Walls", true);
				outsideWalls.AddComponent<OutsideWallHolder>().FloorIndex = floorIndex;

				outsideWalls.transform.position = buildingBp.BlueprintXZCenter;
				outsideWalls.name = "Outside Plain Wall (Floor " + (floorIndex + 1) + ")";
				MeshRenderer meshRenderer = outsideWalls.AddComponent<MeshRenderer>();
				MeshFilter meshFilter = outsideWalls.AddComponent<MeshFilter>();

				meshFilter.mesh = m;

				if(buildingBp.BuildingStyle.FancySidings.Length < 1)
				{
					Debug.LogError(buildingBp.BuildingStyle.name + " does not contain a fancy siding option, please fix.");
					return;	
				}

				Material fancySiding = buildingBp.BuildingStyle.FancySidings[fancySideIndex];
				Material plainSiding = buildingBp.BuildingStyle.PlainSiding;

				if(plainSiding == null)
					plainSiding = fancySiding;

				if(fancySiding == null)
					Debug.LogError(buildingBp.BuildingStyle.name + " does not contain a fancy siding option, please fix.");

				meshRenderer.material = plainSiding;

				// Sets the parent of the outside wall to this thing
				outsideWalls.transform.SetParent(BCBlueprintUtils.FindOrCreateFloorGameObject(buildingBp, floorIndex).transform);
				outsideWalls.transform.localPosition = new Vector3(outsideWalls.transform.localPosition.x, 0, outsideWalls.transform.localPosition.z);
				outsideWalls.transform.rotation = buildingBp.BuildingRotation;
			}
		}

		/// <summary>
		/// Generates a flat roof of the building
		/// </summary>
		public static void GenerateFlatRoofs(List<Vector3[]> roofOutline, List<Vector3[]> cutAways, BuildingBlueprint buildingBp, int floorIndex)
		{
			MeshInfo meshInfo = new MeshInfo();

			List<Vector3[]> tiles = null;

			Vector3 startUVPoint = BCTilingFloors.FindFirstSquareTileStart(tiles);

			for(int domIndex = 0; domIndex < roofOutline.Count; domIndex++)
			{
				// FOR THE FAST SQUARE FLOOR, we need to make sure cutaways are ALWAYS cut away from the rest
				tiles = BCTilingFloors.GetSquareTiles(roofOutline[domIndex], cutAways, PolyFillType.pftEvenOdd);

				for(int i = 0; i < tiles.Count; i++)
					meshInfo = BCMesh.CombineMeshInfos(meshInfo, BCTilingFloors.GenerateSquareTiles(tiles[i], startUVPoint));
			}	

			if(meshInfo.Vertices == null)
				return;

			for(int i = 0; i < meshInfo.Vertices.Length; i++)
				meshInfo.Vertices[i] -= buildingBp.BlueprintXZCenter;

			Mesh m = BCMesh.GetMeshFromMeshInfo(meshInfo);
			m.name = "Procedural Roof Mesh";

			BCMesh.CalculateMeshTangents(m);

			// Generate the Flat Roof
			GameObject flatRoof = BCMesh.GenerateEmptyGameObject("Create Roof", true);
			RoofHolder roofHolder = flatRoof.AddComponent<RoofHolder>();
			roofHolder.FloorIndex = floorIndex;

			flatRoof.name = "Roof";
			MeshRenderer meshRenderer = flatRoof.AddComponent<MeshRenderer>();
			MeshFilter meshFilter = flatRoof.AddComponent<MeshFilter>();

			meshFilter.mesh = m;
			meshRenderer.material = buildingBp.BuildingStyle.Rooftop as Material;

			// TODO: Do not generate the roof collider within this task which builds the mesh
			// BCColliderBuilders.GenerateRoofColliders(flatRoof, buildingBp, true);

			// Parent the roof
			flatRoof.transform.position = buildingBp.BlueprintXZCenter;
			flatRoof.transform.SetParent(BCBlueprintUtils.FindOrCreateFloorGameObject(buildingBp, floorIndex).transform);
			flatRoof.transform.localRotation = Quaternion.identity;

			Vector3 localPos = flatRoof.transform.localPosition;
			localPos.y = buildingBp.Floors[floorIndex].Height + 0.5f;
			flatRoof.transform.localPosition = localPos;
		}

		/// <summary>
		/// Generates a flat roof of the building
		/// </summary>
		public static void GenerateOverhangs(List<Vector3[]> exteriorWalls, List<Vector3[]> interiorWalls, List<Vector3[]> floorBelow, BuildingBlueprint buildingBp, int floorIndex)
		{
			MeshInfo meshInfo = new MeshInfo();

			List<Vector3[]> overhang = BCPaths.GetOverhang(exteriorWalls, floorBelow);
			overhang = BCPaths.CutOutInteriorsFromOverhang(overhang, interiorWalls);

			List<Vector3[]> tiles = null;
			for(int domIndex = 0; domIndex < overhang.Count; domIndex++)
			{
				// FOR THE FAST SQUARE FLOOR, we need to make sure cutaways are ALWAYS cut away from the rest
				tiles = BCTilingFloors.GetSquareTiles(overhang[domIndex], null, PolyFillType.pftEvenOdd);
				Vector3 startUVPoint = BCTilingFloors.FindFirstSquareTileStart(tiles);

				for(int i = 0; i < tiles.Count; i++)
					meshInfo = BCMesh.CombineMeshInfos(meshInfo, BCTilingFloors.GenerateSquareTiles(tiles[i], startUVPoint, true));
			}

			if(meshInfo.Vertices == null)
				return;
			
			for(int i = 0; i < meshInfo.Vertices.Length; i++)
				meshInfo.Vertices[i] -= buildingBp.BlueprintXZCenter;
			
			Mesh m = BCMesh.GetMeshFromMeshInfo(meshInfo);
			m.name = "Procedural Overhang Mesh";

			BCMesh.CalculateMeshTangents(m);

			// Generate the Overhang
			GameObject flatRoof = BCMesh.GenerateEmptyGameObject("Create Overhang", true);
			OverhangHolder roofHolder = flatRoof.AddComponent<OverhangHolder>();
			roofHolder.FloorIndex = floorIndex;

			flatRoof.name = "Overhangs";
			MeshRenderer meshRenderer = flatRoof.AddComponent<MeshRenderer>();
			MeshFilter meshFilter = flatRoof.AddComponent<MeshFilter>();

			meshFilter.mesh = m;
			meshRenderer.material = buildingBp.BuildingStyle.Rooftop as Material;

			flatRoof.transform.position = buildingBp.BlueprintXZCenter;
			flatRoof.transform.SetParent(BCBlueprintUtils.FindOrCreateFloorGameObject(buildingBp, floorIndex).transform);

			Vector3 localPos = flatRoof.transform.localPosition;
			localPos.y = 0;
			flatRoof.transform.localPosition = localPos;
		}

		public static void GenerateWallCappers(List<WallInformation[]> exteriorWalls, List<WallInformation[]> interiorWalls, BuildingBlueprint buildingBp, int floorIndex)
		{
			MeshInfo meshInfo = new MeshInfo();

			List<Vector3[]> exteriors = WallInfosToVectors(exteriorWalls, true);
			List<Vector3[]> interiors = WallInfosToVectors(interiorWalls, true);

			List<Vector3[]> tiles = null;
			for(int exteriorIndex = 0; exteriorIndex < exteriors.Count; exteriorIndex++)
			{
				// FOR THE FAST SQUARE FLOOR, we need to make sure cutaways are ALWAYS cut away from the rest
				tiles = BCTilingFloors.GetSquareTiles(exteriors[exteriorIndex], interiors, PolyFillType.pftEvenOdd);

				for(int i = 0; i < tiles.Count; i++)
					meshInfo = BCMesh.CombineMeshInfos(meshInfo, BCMesh.GenerateGenericMeshInfo(tiles[i].Reverse().ToArray<Vector3>()));
			}	

			if(meshInfo.Vertices == null)
				return;

			for(int i = 0; i < meshInfo.Vertices.Length; i++)
				meshInfo.Vertices[i] -= buildingBp.BlueprintXZCenter;

			Mesh m = BCMesh.GetMeshFromMeshInfo(meshInfo);
			m.name = "Procedural Topper Mesh";

			BCMesh.CalculateMeshTangents(m);

			// Generate the Flat Roof
			GameObject newWallCapper = BCMesh.GenerateEmptyGameObject("Create Floor Topper", true);
			RoofHolder roofHolder = newWallCapper.AddComponent<RoofHolder>();
			roofHolder.FloorIndex = floorIndex;

			newWallCapper.name = "Floor Topper";
			MeshRenderer meshRenderer = newWallCapper.AddComponent<MeshRenderer>();
			MeshFilter meshFilter = newWallCapper.AddComponent<MeshFilter>();

			meshFilter.mesh = m;
			meshRenderer.material = buildingBp.BuildingStyle.DoorWindowFrames as Material;

			// TODO: Do not generate the roof collider within this task which builds the mesh
//			BCColliderBuilders.GenerateRoofColliders(flatRoof, buildingBp, true);

			// Parent the roof
			newWallCapper.transform.position = buildingBp.BlueprintXZCenter;

			newWallCapper.transform.SetParent(BCBlueprintUtils.FindOrCreateFloorGameObject(buildingBp, floorIndex).transform);
			Vector3 localPos = newWallCapper.transform.localPosition;
			localPos.y = buildingBp.Floors[floorIndex].Height - 0.0001f;
			newWallCapper.transform.localPosition = localPos;
			newWallCapper.transform.localRotation = Quaternion.identity;
		}

		#endregion

		#region Building Lips

		public static void GenerateRoofLips(List<WallInformation> wallInfos, List<WallInformation[]> aboveWallsGroups, BuildingBlueprint buildingBp, int floorIndex, int fancySideIndex)
		{
			if(wallInfos.Count == 0 || wallInfos == null)
				return;
			
			List<Vector3[]> segments = new List<Vector3[]>();
			List<Vector3> currentSegment = new List<Vector3>();

			List<Vector3[]> aboveGroups = WallInfosToVectors(aboveWallsGroups, true);
			int lastSideIndex = 0;

			// Must track the int of each segment to figure out which wall it is on
			List<int> segmentSideIndex = new List<int>();

			for(int i = 0; i < wallInfos.Count; i++)
			{
				if(i > 0)
					lastSideIndex = wallInfos[i - 1].SideIndex;

				// Party walls stop generation of the lip edge
				if(wallInfos[i].IsPartyWall)
				{
					if(currentSegment.Count > 0)
					{
						currentSegment.Add(wallInfos[i].StartOffset);
						currentSegment.Reverse();
						segments.Add(currentSegment.ToArray<Vector3>());
						segmentSideIndex.Add(lastSideIndex);
						currentSegment.Clear();
						continue;
					}
				}

				// Have to stop and start a new system
				if(wallInfos[i].SideIndex != lastSideIndex)
				{
					currentSegment.Add(wallInfos[i].StartOffset);
					currentSegment.Reverse();
					segments.Add(currentSegment.ToArray<Vector3>());
					segmentSideIndex.Add(lastSideIndex);
					currentSegment.Clear();
				}

				// If it is the end of the section, end it off
				if(wallInfos[i].IsEnd && wallInfos[i].IsPartyWall == false)
				{
					currentSegment.Add(wallInfos[i].StartOffset);
					currentSegment.Add(wallInfos[i].EndOffset);
					currentSegment.Reverse();
					segments.Add(currentSegment.ToArray<Vector3>());
					segmentSideIndex.Add(wallInfos[i].SideIndex);
					currentSegment.Clear();
					continue;
				}

				// If not a party wall, just continue adding the starts and continug on
				if(wallInfos[i].IsPartyWall == false)
					currentSegment.Add(wallInfos[i].StartOffset);
			}

			CombineEnds(ref segments, aboveGroups, ref segmentSideIndex);

			MeshInfo fancyMeshInfo = new MeshInfo();
			MeshInfo plainMeshInfo = new MeshInfo();

			for(int i = 0; i < segments.Count; i++)
			{
				if(segmentSideIndex[i] == 0)
					fancyMeshInfo = BCMesh.CombineMeshInfos(fancyMeshInfo, BCLipsGenerator.GenerateBuildingFloorLip(segments[i], buildingBp.BuildingStyle.FancyCrown));
				else
					plainMeshInfo = BCMesh.CombineMeshInfos(plainMeshInfo, BCLipsGenerator.GenerateBuildingFloorLip(segments[i], buildingBp.BuildingStyle.PlainCrown));
			}

			// Create the fancy sides
			if(fancyMeshInfo.IsValid)
			{
				Material material = buildingBp.BuildingStyle.FancySidings[fancySideIndex] as Material;

				GameObject newLipGameObject = BCMesh.GenerateGameObjectFromMesh(fancyMeshInfo, buildingBp.BlueprintXZCenter, "Roof Lips Fancy (Floor " + (floorIndex + 1) + ")", "Procedural Roof Lips", material);
				newLipGameObject.AddComponent<RoofLipHolder>().FloorIndex = floorIndex;

				newLipGameObject.transform.SetParent(BCBlueprintUtils.FindOrCreateFloorGameObject(buildingBp, floorIndex).transform);
				newLipGameObject.transform.localRotation = Quaternion.identity;
				newLipGameObject.transform.localPosition = new Vector3(newLipGameObject.transform.localPosition.x, buildingBp.Floors[floorIndex].Height + 0.5f, newLipGameObject.transform.localPosition.z);
			}

			// Create the plain sides
			if(plainMeshInfo.IsValid)
			{
				Material material = buildingBp.BuildingStyle.PlainSiding as Material;

				GameObject newLipGameObject = BCMesh.GenerateGameObjectFromMesh(plainMeshInfo, buildingBp.BlueprintXZCenter, "Roof Lips Plain (Floor " + (floorIndex + 1) + ")", "Procedural Roof Lips", material);
				newLipGameObject.AddComponent<RoofLipHolder>().FloorIndex = floorIndex;

				newLipGameObject.transform.SetParent(BCBlueprintUtils.FindOrCreateFloorGameObject(buildingBp, floorIndex).transform);
				newLipGameObject.transform.localRotation = Quaternion.identity;
				newLipGameObject.transform.localPosition = new Vector3(newLipGameObject.transform.localPosition.x, buildingBp.Floors[floorIndex].Height + 0.5f, newLipGameObject.transform.localPosition.z);
			}
		}

		private static List<WallInformation> CreateWallInfoFromRoofLipPath(List<Vector3[]> roofLips, List<WallInformation[]> wholeFloorOutlines, BuildingBlueprint buildingBp)
		{
			List<WallInformation> allWalls = BCGenerator.StackVectorsToOutsetWallInfos(roofLips, buildingBp);

			for(int wallIndex = 0; wallIndex < allWalls.Count; wallIndex++)
			{
				for(int outlineIndex = 0; outlineIndex < wholeFloorOutlines.Count; outlineIndex++)
				{
					for(int i = 0; i < wholeFloorOutlines[outlineIndex].Length; i++)
					{
						WallInformation testWall = wholeFloorOutlines[outlineIndex][i];
						WallInformation wall = allWalls[wallIndex];

						Vector3 midPoint = (wall.Start + wall.End) / 2;
						if(BCUtils.IsPointAlongLineXZ(midPoint, testWall.StartOffset, testWall.EndOffset, .001f) && buildingBp.BuildingStyle.PlainSiding != null)
						{
							wall.SideIndex = testWall.SideIndex;
							allWalls[wallIndex] = wall;
						}
					}
				}
			}

			return allWalls;
		}

		#endregion

		#region

		public static readonly Vector3 forwardBuilding = Vector3.forward;
		public static readonly Vector3 backwardBuilding = Vector3.back;
		public static readonly Vector3 leftBuilding = Vector3.left;
		public static readonly Vector3 rightBuilding = Vector3.right;

		public static int[] GetFourIndexCornersClockwise(Vector3[] floorOutline)
		{
			Vector3[] fourCorners = GetFourFloorCorners(floorOutline);
			int[] fourIndexCorners = new int[fourCorners.Length];
			// Finds the indexes that are closest to each of the four corners
			for(int i = 0; i < fourCorners.Length; i++)
				fourIndexCorners[i] = GetClosestPoint(fourCorners[i], floorOutline);

			return fourIndexCorners;
		}

		/// <summary>
		/// 0 - start of the fancy front
		/// 1 - start of the fancy left
		/// 2 - start of the fancy back
		/// 3 - start of the fancy right
		/// </summary>
		public static Vector3[] GetFourFloorCorners(Vector3[] edge)
		{
			Bounds newBounds = new Bounds();

			if(edge == null || edge.Length < 1)
				return new Vector3[0];

			newBounds.center = edge[0];

			for(int i = 0; i < edge.Length; i++)
			{
				newBounds.Encapsulate(edge[i]);
			}

			Vector3[] output = new Vector3[4]
			{
				newBounds.center + new Vector3(newBounds.extents.x, 0, -newBounds.extents.z),
				newBounds.center + new Vector3(-newBounds.extents.x, 0, -newBounds.extents.z),
				newBounds.center + new Vector3(-newBounds.extents.x, 0, newBounds.extents.z),
				newBounds.center + new Vector3(newBounds.extents.x, 0, newBounds.extents.z),
			};

			return output;
		}

		private static int GetClosestPoint(Vector3 point, Vector3[] outline)
		{
			if(outline.Length < 2)
				return -1;

			float closestDistanceToPoint = float.MaxValue;
			int index = -1;
			Vector3 closePoint = point;

			for(int i = 0; i < outline.Length; i++)
			{
				float distanceToPoint = (closePoint - outline[i]).sqrMagnitude;

				if(distanceToPoint < closestDistanceToPoint)
				{
					index = i;
					closestDistanceToPoint = distanceToPoint;
				}
			}
			return index;
		}

		#endregion
	}
}