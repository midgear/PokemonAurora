using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ClipperLib;

namespace BuildingCrafter
{
	public static partial class BCPaths 
	{
		#region Converters

		public static List<IntPoint> GetIntPoints(Vector3[] points, int accuracy = 10000)
		{
			List<IntPoint> newPoints = new List<IntPoint>(points.Length - 1);
			for(int i = 0; i < points.Length - 1; i++)
				newPoints.Add(new IntPoint((double)points[i].x * accuracy, (double)points[i].z * accuracy));

			return newPoints;
		}

		public static List<IntPoint> GetIntPoints(List<Vector3> points, int accuracy = 10000)
		{
			List<IntPoint> newPoints = new List<IntPoint>(points.Count - 1);
			for(int i = 0; i < points.Count - 1; i++)
				newPoints.Add(new IntPoint((double)points[i].x * accuracy, (double)points[i].z * accuracy));

			return newPoints;
		}

		public static IntPoint GetIntPoint(Vector3 point, int accuracy = 10000)
		{
			return new IntPoint((double)point.x * accuracy, (double)point.z * accuracy);
		}

		public static Vector3 GetPoint(IntPoint intPoint, float accuracy = 10000)
		{
			return new Vector3((float)intPoint.X / accuracy, 0, (float)intPoint.Y / accuracy);
		}

		public static Vector3[] GetPoints(List<IntPoint> points, float accuracy = 10000)
		{
			Vector3[] newPoints = new Vector3[points.Count + 1];
			for(int i = 0; i < points.Count; i++)
			{
				newPoints[i] = GetPoint(points[i], accuracy);
			}

			newPoints[newPoints.Length - 1] = newPoints[0];

			return newPoints;
		}

		#endregion

		#region Is Polygon within Polygon

		/// <summary>
		/// DOES NOT DEAL WITH TWO POLYGONS CROSSING EACH OTHER
		/// </summary>
		public static bool PolygonInPolygon(Vector3[] insidePoly, Vector3[] outsidePoly)
		{
			for(int polyIndex = 0; polyIndex < insidePoly.Length; polyIndex++)
			{
				if(PointInPolygonXZ(insidePoly[polyIndex], outsidePoly) == true)
				{
					return true;
				}
			}

//			for(int i = 0; i < insidePoly.Length - 1; i++)
//			{
//				Vector3 junk;
//				if(GetClosestsIntersectOfPath(insidePoly[i], insidePoly[i + 1], outsidePoly, out junk))
//					return true;
//			}

			return false;
		}

		public static bool PolygonEncompasingOtherPolygone(Vector3[] insidePoly, Vector3[] outsidePoly)
		{
			for(int polyIndex = 0; polyIndex < insidePoly.Length; polyIndex++)
			{
				if(PointInPolygonXZ(insidePoly[polyIndex], outsidePoly) == false)
				{
					return false;
				}
			}

			for(int i = 0; i < insidePoly.Length - 1; i++)
			{
				Vector3 junk;
				if(GetClosestsIntersectOfPath(insidePoly[i], insidePoly[i + 1], outsidePoly, out junk))
					return false;
			}

			return true;
		}

		#endregion

		#region Is Point Within Or Along Polygon

		static Clipper lineClipper;

		public static bool PointInOrOnPolygonXZ(Vector3 point, Vector3[] polyPoints, int accuracy = 10000)
		{
			return PointInPolygonXZ(GetIntPoint(point, accuracy), GetIntPoints(polyPoints, accuracy));
		}

//		public static bool PointAlongLine(Vector3 point, Vector3 startLine, Vector3 endLine, int accuracy = 10000)
//		{
//			if(lineClipper == null)
//				lineClipper = new Clipper();
//
//			IntPoint pointInt = GetIntPoint(point, accuracy);
//			IntPoint startInt = GetIntPoint(startLine, accuracy);
//			IntPoint endInt = GetIntPoint(endLine, accuracy);
//
//			return lineClipper.PointOnLineSegment(pointInt, startInt, endInt, true);
//		}

//		public static bool PointAlongPolygonXZ(Vector3 point, Vector3[] polyPoints, int accuracy = 5000)
//		{
//			if(lineClipper == null)
//				lineClipper = new Clipper();
//
//			List<IntPoint> intPoints = GetIntPoints(polyPoints, accuracy);
//			IntPoint pointInt = GetIntPoint(point, accuracy);
//
//			// Check along the line first
//			for(int i = 0; i < intPoints.Count - 1; i++)
//			{
//				if(lineClipper.PointOnLineSegment(pointInt, intPoints[i], intPoints[i + 1], false))
//					return true;
//			}
//
//			return PointInPolygonXZ(pointInt, intPoints);
//		}

		public static int PointIndexAlongPolygon(Vector3 point, Vector3[] polyPoints, int accuracy = 5000)
		{
			List<IntPoint> intPoints = GetIntPoints(polyPoints, accuracy);
			IntPoint pointInt = GetIntPoint(point, accuracy);
			if(lineClipper == null)
				lineClipper = new Clipper();

			for(int i = 0; i < intPoints.Count - 1; i++)
			{
				if(lineClipper.PointOnLineSegment(pointInt, intPoints[i], intPoints[i + 1], false))
					return i;
			}

			return -1;
		}

		#endregion

		#region Is Point On Polygon Corner

		public static bool PointOnPolygonCorner(Vector3 point, Vector3[] polyPoints, int accuracy = 10000)
		{
			return PointOnPolygonCorner(GetIntPoint(point, accuracy), GetIntPoints(polyPoints, accuracy));
		}

		private static bool PointOnPolygonCorner(IntPoint point, List<IntPoint> polyPoints) 
		{
			for(int i = 0; i < polyPoints.Count; i++)
			{
				if(point.X == polyPoints[i].X && point.Y == polyPoints[i].Y)
					return true;
			}

			return false;
		}

		#endregion

		#region Is Point Within Polygon

		public static bool PointInPolygonXZ(Vector3 point, Vector3[] polyPoints, int accuracy = 10000)
		{
			return PointInPolygonXZ(GetIntPoint(point, accuracy), GetIntPoints(polyPoints, accuracy));
		}

		private static bool PointInPolygonXZ(IntPoint point, List<IntPoint> polyPoints) 
		{
			if(polyPoints == null)
				return false;

			int polyCorners = polyPoints.Count;

			int i;
			int j = polyCorners - 1;
			bool oddNodes = false;

			for (i = 0; i < polyCorners; i++) 
			{
				if ((polyPoints[i].Y < point.Y && polyPoints[j].Y >= point.Y
					||   polyPoints[j].Y < point.Y && polyPoints[i].Y >= point.Y)
					&&  (polyPoints[i].X <= point.X || polyPoints[j].X <= point.X)) 
				{
					if (polyPoints[i].X + (point.Y - polyPoints[i].Y) 
						/ (polyPoints[j].Y - polyPoints[i].Y) 
						* (polyPoints[j].X - polyPoints[i].X ) < point.X) 
					{
						oddNodes = !oddNodes; 
					}
				}
				j=i; 
			}

			return oddNodes; 
		}

		#endregion

		#region Intersector

		public static bool GetClosestsIntersectOfPath(Vector3 pathStart, Vector3 pathEnd, List<Vector3[]> pathToIntersect, out Vector3 closestPoint)
		{
			closestPoint = Vector3.zero;

			float maxDistance = float.MaxValue;
			bool foundPoint = false;

			for(int i = 0; i < pathToIntersect.Count; i++)
			{
				Vector3 nearPoint;

				if(GetClosestsIntersectOfPath(pathStart, pathEnd, pathToIntersect[i], out nearPoint))
				{
					float distance = (nearPoint - pathStart).sqrMagnitude;
					if(distance < maxDistance)
					{
						maxDistance = distance;
						closestPoint = nearPoint;
						foundPoint = true;
					}
				}
			}

			return foundPoint;
		}

		/// <summary>
		/// PROBABLY BROKEN
		/// </summary>
		public static bool GetClosestsIntersectOfPath(Vector3 pathStart, Vector3 pathEnd, Vector3[] pathToIntersect, out Vector3 closestPoint)
		{
			closestPoint = Vector3.zero;

			if(lineClipper == null)
				lineClipper = new Clipper();

			List<Vector3> intersectionPoints = new List<Vector3>(4);

			Vector3 parallelIntersection;

			if(CheckForParallelLineCollision(pathStart, pathEnd, pathToIntersect, out parallelIntersection))
			{
				intersectionPoints.Add(parallelIntersection);
			}

			for(int i = 0; i < pathToIntersect.Length - 1; i++)
			{
				// Are two lines parallel here

				Vector3 intersection;

				// Check for standard interaction on angles
				if(FindIntersectionOfTwoLinesXZ(pathStart, pathEnd, pathToIntersect[i], pathToIntersect[i + 1], out intersection))
				{
					intersectionPoints.Add(intersection);
				}

				// Check for two points on top of each other
				if(PointOnPolygonCorner(pathEnd, pathToIntersect, 5000))
				{
					intersectionPoints.Add(pathEnd);
				}

				// check for points along a poly
				int polyPoint = BCPaths.PointIndexAlongPolygon(pathEnd, pathToIntersect, 5000);
				if(polyPoint > -1)
				{
					float distanceToEnd = (pathStart - pathEnd).sqrMagnitude;
					float distanceToOtherStart = (pathStart - pathToIntersect[polyPoint]).sqrMagnitude;
					float distanceToOtherEnd = (pathStart - pathToIntersect[polyPoint + 1]).sqrMagnitude;
					Vector3 newPoint = pathEnd;

					if(distanceToOtherStart < distanceToEnd)
					{
						distanceToEnd = distanceToOtherStart;
						newPoint = pathToIntersect[polyPoint];
					}

					if(distanceToOtherEnd < distanceToEnd)
						newPoint = pathToIntersect[polyPoint + 1];

					intersectionPoints.Add(newPoint);
				}
			}

			float maxDistance = float.MaxValue;
			int indexToUse = -1;

			for(int i = 0; i < intersectionPoints.Count; i++)
			{
				float distance = (pathStart - intersectionPoints[i]).sqrMagnitude;

				if(distance < maxDistance)
				{
					indexToUse = i;
					maxDistance = distance;
				}
			}

			if(indexToUse > -1)
			{
				closestPoint = intersectionPoints[indexToUse];
				return true;
			}

			return false;
		}

		public static bool CheckForParallelLineCollision(Vector3 start, Vector3 end, Vector3[] pathToIntersect, out Vector3 intersection)
		{
			List<Vector3> intersectionPoints = new List<Vector3>(4);
			intersection = Vector3.zero;

			for(int i = 0; i < pathToIntersect.Length - 1; i++)
			{
				Vector3 pathStart = pathToIntersect[i];
				Vector3 pathEnd = pathToIntersect[i + 1];

				if(AreLinesParallel(start, end, pathStart, pathEnd))
				{
					// Check to see if at least one point is on the line
					if(BCUtils.IsPointAlongLineXZ(pathStart, start, end))
						intersectionPoints.Add(pathStart);
					if(BCUtils.IsPointAlongLineXZ(pathEnd, start, end))
						intersectionPoints.Add(pathEnd);
				}
			}

			float maxDistance = float.MaxValue;
			int indexToUse = -1;

			for(int i = 0; i < intersectionPoints.Count; i++)
			{
				float distance = (start - intersectionPoints[i]).sqrMagnitude;

				if(distance < maxDistance)
				{
					indexToUse = i;
					maxDistance = distance;
				}
			}

			if(indexToUse > -1)
			{
				intersection = intersectionPoints[indexToUse];
				return true;
			}

			return false;
		}

		public static bool AreLinesParallel(Vector3 start, Vector3 end, Vector3 otherLineStart, Vector3 otherLineEnd, int accuracy = 10000)
		{
			return AreLinesParallel(GetIntPoint(start, accuracy), GetIntPoint(end, accuracy), GetIntPoint(otherLineStart, accuracy), GetIntPoint(otherLineEnd, accuracy));
		}

		private static bool AreLinesParallel(IntPoint p1, IntPoint p2, IntPoint p3, IntPoint p4)
		{

			// Get the segments' parameters.
			long dx12 = p2.X - p1.X;
			long dz12 = p2.Y - p1.Y;
			long dx34 = p4.X - p3.X;
			long dz34 = p4.Y - p3.Y;

			// Solve for t1 and t2
			long denominator = (dz12 * dx34 - dx12 * dz34);
			if(denominator == 0)
				return true;

			return false;
		}

		public static bool FindIntersectionOfTwoLinesXZ(
			Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4,
			out Vector3 intersection,
			float accuracy = 10000)
		{
			bool intersected, junkBool;
			Vector3 junkVector;

			FindIntersectionOfTwoLinesXZ(p1, p2, p3, p4, out junkBool, out intersected, out intersection, out junkVector, out junkVector);

			return intersected;
		}

		public static void FindIntersectionOfTwoLinesXZ(
			Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4,
			out bool lines_intersect, out bool segments_intersect,
			out Vector3 intersection,
			out Vector3 close_p1, out Vector3 close_p2,
			int accuracy = 10000)
		{
			if(lineClipper == null)
				lineClipper = new Clipper();

			IntPoint point1 = GetIntPoint(p1, accuracy);
			IntPoint point2 = GetIntPoint(p2, accuracy);
			IntPoint point3 = GetIntPoint(p3, accuracy);
			IntPoint point4 = GetIntPoint(p4, accuracy);

			IntPoint intersectionInt, closeP1Int, closeP2Int;
			FindIntersectionOfTwoLinesXZ(point1, point2, point3, point4, out lines_intersect, out segments_intersect, out intersectionInt, out  closeP1Int, out closeP2Int);

			intersection = GetPoint(intersectionInt, accuracy);
			close_p1 = GetPoint(closeP1Int, accuracy);
			close_p2 = GetPoint(closeP2Int, accuracy);

		}

		private static void FindIntersectionOfTwoLinesXZ(
			IntPoint p1, IntPoint p2, IntPoint p3, IntPoint p4,
			out bool lines_intersect, out bool segments_intersect,
			out IntPoint intersection,
			out IntPoint close_p1, out IntPoint close_p2)
		{
			
			// Get the segments' parameters.
			long dx12 = p2.X - p1.X;
			long dz12 = p2.Y - p1.Y;
			long dx34 = p4.X - p3.X;
			long dz34 = p4.Y - p3.Y;

			// Solve for t1 and t2
			long denominator = (dz12 * dx34 - dx12 * dz34);

			if(denominator == 0)
			{
				lines_intersect = false;
				segments_intersect = false;
				intersection = new IntPoint(-1, -1);
				close_p1 = new IntPoint(-1, -1);
				close_p2 = new IntPoint(-1, -1);
				return;
			}

			float t1 =
				((p1.X - p3.X) * dz34 + (p3.Y - p1.Y) * dx34)
				/ denominator;
//			if (float.IsInfinity(t1))
//			{
//				// The lines are parallel (or close enough to it).
//				lines_intersect = false;
//				segments_intersect = false;
//				intersection = new IntPoint(-1, -1);
//				close_p1 = new IntPoint(-1, -1);
//				close_p2 = new IntPoint(-1, -1);
//				return;
//			}
			lines_intersect = true;

			float t2 =
				((p3.X - p1.X) * dz12 + (p1.Y - p3.Y) * dx12) / - denominator;

			// Find the point of intersection.
			intersection = new IntPoint(p1.X + dx12 * t1, p1.Y + dz12 * t1);

			// The segments intersect if t1 and t2 are between 0 and 1.
			segments_intersect =
				((t1 >= 0) && (t1 <= 1) &&
					(t2 >= 0) && (t2 <= 1));

			// Find the closest points on the segments.
			if (t1 < 0)
			{
				t1 = 0;
			}
			else if (t1 > 1)
			{
				t1 = 1;
			}

			if (t2 < 0)
			{
				t2 = 0;
			}
			else if (t2 > 1)
			{
				t2 = 1;
			}

			close_p1 = new IntPoint(p1.X + dx12 * t1, p1.Y + dz12 * t1);
			close_p2 = new IntPoint(p3.X + dx34 * t2, p3.Y + dz34 * t2);
		}

		#endregion

		#region Get Room Outlines

		static Clipper roomOutlineClippy;
		static List<List<IntPoint>> roomOutlineSolutions = new List<List<IntPoint>>();

		/// <summary>
		/// The first vector SHOULD always be the outline and the rest of the vectors are cutouts
		/// </summary>
		/// <returns>The room outline.</returns>
		/// <param name="roomBp">Room bp.</param>
		/// <param name="cutoutFloor">Cutout floor.</param>
		public static List<Vector3[]> GetRoomOutline(RoomBlueprint roomBp, FloorBlueprint cutoutFloor)
		{
			List<Vector3[]> newPath = new List<Vector3[]>();

			if(cutoutFloor == null)
			{
				newPath.Add(roomBp.PerimeterWalls.ToArray<Vector3>());
				return newPath;
			}
				

			if(roomOutlineClippy == null)
			{
				roomOutlineClippy = new Clipper();
				roomOutlineSolutions = new List<List<IntPoint>>();
			}
			else
			{
				roomOutlineClippy.Clear();
				roomOutlineSolutions.Clear();
			}

			roomOutlineClippy.AddPath(GetIntPoints(roomBp.PerimeterWalls), PolyType.ptSubject, true);

			for(int i = 0; i < cutoutFloor.Stairs.Count; i++)
			{
				Vector3[] stairCutout = BCUtils.GetStairsOutline(cutoutFloor.Stairs[i], 0);
				roomOutlineClippy.AddPath(GetIntPoints(stairCutout), PolyType.ptClip, true);
			}

			List<Vector3[]> cutoutsBelow = new List<Vector3[]>();

			if(roomOutlineClippy.Execute(ClipType.ctIntersection, roomOutlineSolutions, PolyFillType.pftNegative))
			{
				for(int clipIndex = 0; clipIndex < roomOutlineSolutions.Count; clipIndex++)
					cutoutsBelow.Add(GetPoints(roomOutlineSolutions[clipIndex]));
			}

			roomOutlineClippy.Clear();
			roomOutlineSolutions.Clear();

			roomOutlineClippy.AddPath(GetIntPoints(roomBp.PerimeterWalls), PolyType.ptSubject, true);

			for(int i = 0; i < cutoutsBelow.Count; i++)
				roomOutlineClippy.AddPath(GetIntPoints(cutoutsBelow[i]), PolyType.ptClip, true);

			if(roomOutlineClippy.Execute(ClipType.ctDifference, roomOutlineSolutions, PolyFillType.pftEvenOdd))
			{
				for(int clipIndex = 0; clipIndex < roomOutlineSolutions.Count; clipIndex++)
					newPath.Add(GetPoints(roomOutlineSolutions[clipIndex]));
			}

			return newPath;
		}

		#endregion

		#region Lip Outline

		static Clipper lipOutlineClippy;
		static List<List<IntPoint>> lipfloorOutlineSolutions = new List<List<IntPoint>>();

		public static List<Vector3[]> GetRoofEdges(List<Vector3[]> currentFloor, List<Vector3[]> floorAbove)
		{
			if(floorAbove == null)
				return currentFloor;

			if(currentFloor == null)
				return null;

			if(lipOutlineClippy == null)
			{
				lipOutlineClippy = new Clipper();
				lipfloorOutlineSolutions = new List<List<IntPoint>>();
			}
			else
			{
				lipOutlineClippy.Clear();
				lipfloorOutlineSolutions.Clear();
			}

			List<Vector3[]> fullLips = new List<Vector3[]>();

			for(int currentIndex = 0; currentIndex < currentFloor.Count; currentIndex++)
			{
				lipOutlineClippy.Clear();
				lipOutlineClippy.AddPath(GetIntPoints(currentFloor[currentIndex]), PolyType.ptSubject, true);

				for(int i = 0; i < floorAbove.Count; i++)
				{
					lipOutlineClippy.AddPath(GetIntPoints(floorAbove[i]), PolyType.ptClip, true);
				}

				if(lipOutlineClippy.Execute(ClipType.ctDifference, lipfloorOutlineSolutions, PolyFillType.pftNonZero))
				{
					for(int clipIndex = 0; clipIndex < lipfloorOutlineSolutions.Count; clipIndex++)
						fullLips.Add(GetPoints(lipfloorOutlineSolutions[clipIndex]));
				}
			}

			return fullLips;
		}

		#endregion

		#region Overhang Outline

		static Clipper overhangOutlineClippy;
		static List<List<IntPoint>> overhangOutlineSolutions = new List<List<IntPoint>>();

		public static List<Vector3[]> GetOverhang(List<Vector3[]> currentFloor, List<Vector3[]> floorBelow)
		{
			if(floorBelow == null)
				return currentFloor;

			if(currentFloor == null)
				return null;
			
			if(overhangOutlineClippy == null)
			{
				overhangOutlineClippy = new Clipper();
				overhangOutlineSolutions = new List<List<IntPoint>>();
			}
			else
			{
				overhangOutlineClippy.Clear();
				overhangOutlineSolutions.Clear();
			}

			List<Vector3[]> fullLips = new List<Vector3[]>();

			for(int currentIndex = 0; currentIndex < currentFloor.Count; currentIndex++)
			{
				overhangOutlineClippy.Clear();
				overhangOutlineClippy.AddPath(GetIntPoints(currentFloor[currentIndex]), PolyType.ptSubject, true);

				for(int i = 0; i < floorBelow.Count; i++)
				{
					overhangOutlineClippy.AddPath(GetIntPoints(floorBelow[i]), PolyType.ptClip, true);
				}

				if(overhangOutlineClippy.Execute(ClipType.ctDifference, overhangOutlineSolutions, PolyFillType.pftEvenOdd))
				{
					for(int clipIndex = 0; clipIndex < overhangOutlineSolutions.Count; clipIndex++)
						fullLips.Add(GetPoints(overhangOutlineSolutions[clipIndex]));
				}
			}

			return fullLips;
		}

		public static List<Vector3[]> CutOutInteriorsFromOverhang(List<Vector3[]> overhangs, List<Vector3[]> interiors)
		{
			if(interiors == null)
				return overhangs;

			if(overhangs == null)
				return null;

			if(overhangOutlineClippy == null)
			{
				overhangOutlineClippy = new Clipper();
				overhangOutlineSolutions = new List<List<IntPoint>>();
			}
			else
			{
				overhangOutlineClippy.Clear();
				overhangOutlineSolutions.Clear();
			}

			List<Vector3[]> fullLips = new List<Vector3[]>();

			for(int currentIndex = 0; currentIndex < overhangs.Count; currentIndex++)
			{
				overhangOutlineClippy.Clear();
				overhangOutlineClippy.AddPath(GetIntPoints(overhangs[currentIndex]), PolyType.ptSubject, true);

				for(int i = 0; i < interiors.Count; i++)
				{
					overhangOutlineClippy.AddPath(GetIntPoints(interiors[i]), PolyType.ptClip, true);
				}

				if(overhangOutlineClippy.Execute(ClipType.ctDifference, overhangOutlineSolutions, PolyFillType.pftEvenOdd))
				{
					for(int clipIndex = 0; clipIndex < overhangOutlineSolutions.Count; clipIndex++)
						fullLips.Add(GetPoints(overhangOutlineSolutions[clipIndex]));
				}
			}

			return fullLips;
		}

		#endregion

		#region Get Floor Outlines

		public static List<Vector3[]> GetFloorOutline(BuildingBlueprint buildingBp, int floorIndex, bool withOutsets = false)
		{
			if(floorIndex < 0 || floorIndex >= buildingBp.Floors.Count)
				return null;

			if(withOutsets == false)
				return GetFloorOutline(buildingBp.Floors[floorIndex]);

			return null;
		}

		public static List<Vector3[]> GetFloorOutline(FloorBlueprint floorBp)
		{
			if(floorBp == null || floorBp.RoomBlueprints.Count < 1)
				return null;

			List<Vector3[]> paths = new List<Vector3[]>();

			for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
				paths.Add(floorBp.RoomBlueprints[i].PerimeterWalls.ToArray<Vector3>());

			return GetFloorOutline(paths);
		}

//		public static List<Vector3[]> GetFloorOutlineWithOffsets(BuildingBlueprint buildingBp, int floorIndex)
//		{
//			List<Vector3[]> floorOutlines = GetFloorOutline(buildingBp.Floors[floorIndex]);
//			if(floorOutlines == null)
//				return null;
//
//			List<Vector3[]> returnedOutlines = new List<Vector3[]>();
//			for(int outlineIndex = 0; outlineIndex < floorOutlines.Count; outlineIndex++)
//			{
//				// Now that we have the walls, we have to go through and create the wall infos from the outline
//				WallInformation[] wallInfos = BCWallRoofGenerator.GenerateWallInfosForMeshGeneration(floorOutlines[outlineIndex], buildingBp, buildingBp.Floors[floorIndex]);
//
//				// change the wall infos back into an outline
//				returnedOutlines.Add(ConvertWallInfosToList(wallInfos));
//			}
//
//			return returnedOutlines;
//		}

		public static Vector3[] ConvertWallInfosToList(WallInformation[] wallInfo)
		{
			List<Vector3> newPoints = new List<Vector3>(wallInfo.Length);
			if(wallInfo.Length < 1)
				return newPoints.ToArray<Vector3>();
			
			newPoints.Add(wallInfo[0].StartOffset);
			newPoints.Add(wallInfo[0].EndOffset);
			Vector3 lastWallInfoEnd = wallInfo[0].EndOffset;

			for(int i = 1; i < wallInfo.Length; i++)
			{
				if(BCUtils.ArePointsCloseEnough(lastWallInfoEnd, wallInfo[i].StartOffset) == false)
					newPoints.Add(wallInfo[i].StartOffset);

				newPoints.Add(wallInfo[i].EndOffset);
			}

			return newPoints.ToArray<Vector3>();
		}

		static Clipper floorOutlineClippy;
		static List<List<IntPoint>> floorOutlineSolutions = new List<List<IntPoint>>();

		/// <summary>
		/// Returns the floor outline of the given paths
		/// </summary>
		/// <returns>The floor outline.</returns>
		/// <param name="paths">Paths.</param>
		public static List<Vector3[]> GetFloorOutline(List<Vector3[]> paths)
		{
			if(paths.Count < 1)
				return paths;

			if(floorOutlineClippy == null)
			{
				floorOutlineClippy = new Clipper();
				floorOutlineSolutions = new List<List<IntPoint>>();
			}
			else
			{
				floorOutlineClippy.Clear();
				floorOutlineSolutions.Clear();
			}

			floorOutlineClippy.AddPath(GetIntPoints(paths[0]), PolyType.ptSubject, true);

			for(int i = 1; i < paths.Count; i++)
			{
				floorOutlineClippy.AddPath(GetIntPoints(paths[i]), PolyType.ptClip, true);
			}

			if(floorOutlineClippy.Execute(ClipType.ctUnion, floorOutlineSolutions, PolyFillType.pftEvenOdd))
			{
				List<Vector3[]> newPath = new List<Vector3[]>();

				for(int clipIndex = 0; clipIndex < floorOutlineSolutions.Count; clipIndex++)
				{
					newPath.Add(GetPoints(floorOutlineSolutions[clipIndex]));
				}

				return newPath;
			}

			return paths;
		}
//
//		static Clipper diffOutlineClippy;
//		static List<List<IntPoint>> diffOutlineSolutions = new List<List<IntPoint>>();
//
//		/// <summary>
//		/// Returns the roof cutouts after cutting out above the roof sections from above
//		/// </summary>
//		public static List<Vector3[]> FindRoofCutouts(List<Vector3[]> subject, List<Vector3[]> cutAway)
//		{
//			if(subject == null || cutAway == null)
//				return null;
//
//			List<Vector3[]> middlePaths = new List<Vector3[]>();
//
//			if(diffOutlineClippy == null)
//			{
//				diffOutlineClippy = new Clipper();
//				diffOutlineSolutions = new List<List<IntPoint>>();
//			}
//
//			// Have to subtract all the cut aways from each of the subjects
//
//			int indexerthing = 0;
//			for(int domIndex = 0; domIndex < subject.Count; domIndex++)
//			{
//				diffOutlineClippy.Clear();
//				diffOutlineSolutions.Clear();
//
//				diffOutlineClippy.AddPath(GetClippyPoints(subject[domIndex]), PolyType.ptSubject, true);
//
//				for(int i = 0; i < cutAway.Count; i++)
//					diffOutlineClippy.AddPath(GetClippyPoints(cutAway[i]), PolyType.ptClip, true);
//
//				if(diffOutlineClippy.Execute(ClipType.ctIntersection, floorOutlineSolutions, PolyFillType.pftNonZero))
//				{
//					for(int clipIndex = 0; clipIndex < floorOutlineSolutions.Count; clipIndex++)
//						middlePaths.Add(GetPoints(floorOutlineSolutions[clipIndex]));
//				}
//			}
//
//			diffOutlineClippy.Clear();
//			floorOutlineSolutions.Clear();
//
//			List<Vector3[]> newPaths = new List<Vector3[]>();
//
//			if(middlePaths.Count > 0)
//			{
//				for(int i = 0; i < subject.Count; i++)
//					diffOutlineClippy.AddPath(GetClippyPoints(subject[i]), PolyType.ptSubject, true);
//
//				for(int i = 0; i < middlePaths.Count; i++)
//					diffOutlineClippy.AddPath(GetClippyPoints(middlePaths[i]), PolyType.ptClip, true);
//
//				if(diffOutlineClippy.Execute(ClipType.ctDifference, floorOutlineSolutions, PolyFillType.pftEvenOdd))
//				{
//					for(int index = 0; index < floorOutlineSolutions.Count; index++)
//						newPaths.Add(GetPoints(floorOutlineSolutions[index]));
//				}
//			}
//
//			return newPaths;
//		}

		#endregion
	}
}
