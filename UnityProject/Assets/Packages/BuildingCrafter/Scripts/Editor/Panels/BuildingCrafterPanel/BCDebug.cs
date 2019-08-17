using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using LibTessDotNet;

namespace BuildingCrafter
{

	public partial class BuildingCrafterPanel : Editor 
	{
		// Temp Testing Things

		public static List<Vector3[]> TempDisplayOutline;
		public static List<Vector3> TempDisplayPoints;
		public bool showLines = false;
		public bool showPoints = false;

		private void DrawDebugLinesAndPoints()
		{
	//		return; // TURN THIS OFF TO ACTIVATE POINTS

			if(TempDisplayOutline != null && showLines)
			{
				for(int i = 0; i < TempDisplayOutline.Count; i++)
				{
					if(TempDisplayOutline[i].Length < 1)
						break;

	//				if(i > 0)
	//					Handles.color = Color.black;
	//
	//				if(i > 1)
	//					Handles.color = Color.blue;

					Handles.DrawSolidDisc(TempDisplayOutline[i][0], Vector3.up, 0.05f);
					for(int j = 0; j < TempDisplayOutline[i].Length; j++)
					{
						Handles.Label(TempDisplayOutline[i][j] + Vector3.up * 0.1f, j.ToString());
					}
					Handles.color = Color.white;
					Handles.DrawPolyLine(TempDisplayOutline[i]);
					Handles.DrawSolidDisc(TempDisplayOutline[i][TempDisplayOutline[i].Length - 1], Vector3.up, 0.05f);
				}
			}

			if(TempDisplayPoints != null && showPoints)
			{
				for(int i = 0; i < TempDisplayPoints.Count; i++)
				{
					Handles.DrawSolidDisc(TempDisplayPoints[i], Vector3.up, 0.10f);
					Handles.Label(TempDisplayPoints[i] + Vector3.up * 0.2f, i.ToString());
				}
			}

			if(Script.BuildingBlueprint != null && Script.BuildingBlueprint.DebugOutline != null && showPoints)
			{
				for(int i = 0; i < Script.BuildingBlueprint.DebugOutline.Count; i++)
				{
					if(Script.BuildingBlueprint.DebugOutline[i].Length < 1)
						break;
					
	//				if(i > 0)
	//					Handles.color = Color.black;
	//
	//				if(i > 1)
	//					Handles.color = Color.blue;
					
					Handles.DrawSolidDisc(Script.BuildingBlueprint.DebugOutline[i][0], Vector3.up, 0.05f);
					for(int j = 0; j < Script.BuildingBlueprint.DebugOutline[i].Length; j++)
					{
						Handles.Label(Script.BuildingBlueprint.DebugOutline[i][j] + Vector3.up * 0.1f, j.ToString());
					}
					Handles.color = Color.white;
					Handles.DrawPolyLine(Script.BuildingBlueprint.DebugOutline[i]);
					Handles.DrawSolidDisc(Script.BuildingBlueprint.DebugOutline[i][Script.BuildingBlueprint.DebugOutline[i].Length - 1], Vector3.up, 0.05f);
				}
			}
		}

		/// <summary>
		/// Returns building edges from the entire building
		/// </summary>
//		public Vector3[] GetBuildingEdges(BuildingBlueprint buildingBp)
//		{
//			Vector3[] outline = BCMesh.GenerateOutlineFloor(buildingBp.Floors[0]);
//
//			for(int i = 1; i < buildingBp.Floors.Count; i++)
//			{
//				Vector3[] thisFloor = BCMesh.GenerateOutlineFloor(buildingBp.Floors[i]);
//				outline = CombineTwoVectors(outline, thisFloor);
//			}
//
//			return outline.ToArray<Vector3>();
//		}

		public static Vector3[] CombineTwoVectors(Vector3[] outline, Vector3[] toAddOutline)
		{
			List<Vector3> newOutline = new List<Vector3>();

			// Make them both go clockwise;
			if(BCUtils.IsClockwisePolygon(outline) == false)
				outline = outline.Reverse().ToArray<Vector3>();

			if(BCUtils.IsClockwisePolygon(toAddOutline) == false)
				toAddOutline = toAddOutline.Reverse().ToArray<Vector3>();

			int starter = 0;

			// First find a point on the outline where it does not overlap with the other toAddOutline
			for(int i = 0; i < outline.Length; i++)
			{
				if(BCUtils.IsPointInARoom(outline[i], toAddOutline) == false)
				{
					starter = i;
					break;
				}
			}

			newOutline.Add(outline[starter]);

			// TODO: make sure the outline is a loop. If not, make it become a loop
			// TODO: Check to make sure that the two items overlap at some point, at any point. If not feed back the two vectors separately

			// Check to see if one item is ENTIRELY inside another one
			bool outlineInAdd = true;

			for(int i = 0; i < outline.Length; i++)
			{
				if(BCUtils.IsPointOnlyInsideARoom(outline[i], toAddOutline) == false)
				{
					outlineInAdd = false;
					break;
				}
			}

			if(outlineInAdd) return toAddOutline;
				
			bool addInOutline = true;
			for(int i = 0; i < toAddOutline.Length; i++)
			{
				if(BCUtils.IsPointOnlyInsideARoom(toAddOutline[i], outline) == false)
				{
					addInOutline = false;
					break;
				}
			}

			if(addInOutline) return outline;

			int breaker = 0;

			while(breaker < 40)
			{
				breaker++;
				if(newOutline.Count > 2 && newOutline[0] == newOutline[newOutline.Count -1])
					break;

				bool skipFirst = false;


				int startIndex = BCUtils.GetIndexOfComplexWall(newOutline[newOutline.Count - 1], outline);
				int secondIndex = BCUtils.GetIndexOfComplexWall(newOutline[newOutline.Count - 1], toAddOutline);

				// Solves for a certain case where some outjets would be skipped
				if(startIndex != -1 && secondIndex != -1)
				{
					// TODO: Test for looping vector, from second last point to last point and from last point to start point
					Vector3 startDirection = (outline[startIndex + 1] - outline[startIndex]).normalized;
					Vector3 secondDirection = (toAddOutline[secondIndex + 1] - toAddOutline[secondIndex]).normalized;

					if(startDirection == secondDirection)
					{
						// Find the closest next point
						Vector3 nextStart = outline[startIndex + 1];
						Vector3 nextSecond = toAddOutline[secondIndex + 1];

						float distanceToNextStart = (nextStart - newOutline.Last()).sqrMagnitude;
						float distanceToNextSecond = (nextSecond - newOutline.Last()).sqrMagnitude;

						if(distanceToNextStart > distanceToNextSecond)
						{
							skipFirst = true;
						}
					}
				}

				if(startIndex != -1 && skipFirst == false)
				{
					startIndex = startIndex + 1;
					newOutline.AddRange(FindNextIntersection(newOutline[newOutline.Count - 1], startIndex, newOutline[0], outline, toAddOutline));

					if(newOutline.Count > 2 && newOutline[0] == newOutline[newOutline.Count - 1])
					{
						break;
					}
				}

				secondIndex = BCUtils.GetIndexOfComplexWall(newOutline[newOutline.Count - 1], toAddOutline);
				if(secondIndex != -1)			
				{
					secondIndex += 1;

					newOutline.AddRange(FindNextIntersection(newOutline[newOutline.Count - 1], secondIndex, newOutline[0], toAddOutline, outline));

					if(newOutline.Count > 2 && newOutline[0] == newOutline[newOutline.Count - 1])
					{
						break;
					}
				}
			}

			BCUtils.CollapseWallLines(newOutline);

			return newOutline.ToArray();
		}

		private static List<Vector3> FindNextIntersection(Vector3 startPoint, int nextIndexStart, Vector3 loopComplete, Vector3[] outline, Vector3[] toAddOutline)
		{
			List<Vector3> newOutline = new List<Vector3>();

			int outlineLength = outline.Length;

			for(int i = 0; i < outlineLength; i++)
			{
				int nextIndex = i + nextIndexStart;

				if(nextIndex == outline.Length - 1)
					nextIndex = 0;

				if(nextIndex > outline.Length - 1)
					nextIndex -= outline.Length - 1;

				Vector3 nextPoint = outline[nextIndex];
				Vector3 direction = (nextPoint - startPoint).normalized;

				Vector3 firstIntersection;

				Vector3 testPoint = startPoint + direction * 0.1f;

				if(BCUtils.IsPointOnlyInsideARoom(testPoint, toAddOutline))
				{
					return newOutline;
				}

				if(BCUtils.IsPointAlongAWall(testPoint, outline) && BCUtils.IsPointAlongAWall(testPoint, toAddOutline))
				{
					BCUtils.FindFirstSegementOverlap(testPoint, nextPoint, outline, out firstIntersection);

					firstIntersection = new Vector3(
						(float)System.Math.Round(firstIntersection.x, 3),
						(float)System.Math.Round(firstIntersection.y, 3),
						(float)System.Math.Round(firstIntersection.z, 3));

					newOutline.Add(firstIntersection);
					return newOutline;
				}

				if(BCUtils.FindFirstSegementOverlap(testPoint, nextPoint, toAddOutline, out firstIntersection))
				{
					// Now round the first interesection so it lines up correctly. Stupid floating point stuff
					firstIntersection = new Vector3(
						(float)System.Math.Round(firstIntersection.x, 3),
						(float)System.Math.Round(firstIntersection.y, 3),
						(float)System.Math.Round(firstIntersection.z, 3));

					newOutline.Add(firstIntersection);
					return newOutline;
				}
				else
				{
					newOutline.Add(nextPoint);

					// If this has been completed and arrived back at the start, break out of this
					if( newOutline[newOutline.Count - 1] == loopComplete)
					{
						return newOutline;
					}

					startPoint = nextPoint;
				}
			}

			return newOutline;
		}
	}
}