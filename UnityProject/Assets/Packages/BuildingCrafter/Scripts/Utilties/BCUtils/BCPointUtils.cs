using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{
	public static partial class BCUtils 
	{
		/// <summary>
		/// Determines if a test point is close to a wall corner point
		/// </summary>
		/// <returns>-1 if no close point, else returns the index of the wall
		public static int IsPointOnCornerPoint(Vector3 testPoint, List<Vector3> outline)
		{
			for(int i = 0; i < outline.Count - 1; i++)
			{
				if((testPoint - outline[i]).sqrMagnitude < 0.01)
				{
					return i;
				}
			}

			return -1;
		}

		public static void SetPointOnWall(Vector3 newPosition, int index, RoomBlueprint roomBp)
		{
			roomBp.PerimeterWalls[index] = newPosition;

			if(index == 0)
				roomBp.PerimeterWalls[roomBp.PerimeterWalls.Count - 1] = newPosition;
			if(index == roomBp.PerimeterWalls.Count - 1)
				roomBp.PerimeterWalls[0] = newPosition;
		}


		public static bool IsPointCloseToLine(Vector2 point, Vector2 p1, Vector2 p2, float maxDistance)
		{
			float maxDistanceSqr = maxDistance * maxDistance;
			
			float distanceToP1 = (point - p1).sqrMagnitude;
			float distanceToP2 = (point - p2).sqrMagnitude;
			
			if(distanceToP1 < maxDistanceSqr || distanceToP2 < maxDistanceSqr)
				return true;
			
			float lineDistance = (p1 - p2).sqrMagnitude;
			
			if(distanceToP1 > lineDistance || distanceToP2 > lineDistance)
				return false;
			
			float distanceToLine = DistanceFromPointToInfiniteLine(point, p1, p2);
			
			if(distanceToLine > maxDistance)
				return false;
			
			return true;
		}
		
		
		public static float DistanceFromPointToInfiniteLine(Vector2 point, Vector2 start, Vector2 end)
		{
			return Mathf.Abs((end.x - start.x) * (start.y - point.y) - (start.x - point.x) * (end.y - start.y))
				/ Mathf.Sqrt(Mathf.Pow(end.x - start.x, 2) + Mathf.Pow(end.y - start.y, 2));
		}
	}
}
