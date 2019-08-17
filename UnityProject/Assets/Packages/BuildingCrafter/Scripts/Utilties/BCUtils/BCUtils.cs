using UnityEngine;
using System.Collections;
using LibTessDotNet;
using System.Linq;
using System.Collections.Generic;

namespace BuildingCrafter
{
	public static partial class BCUtils 
	{	
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

		public static Vector3 FindClosestPointOnPlane(Vector3 point, Vector3 planeNormal, Vector3 planePos)
		{
			Plane plane = new Plane(planeNormal, planePos);

			Vector3 towardsPlane = planeNormal;
			if(plane.GetSide(point) == true)
				towardsPlane *= -1;

			Ray newRay = new Ray(point, towardsPlane);

			Vector3 positionOnPlane = point;
			float distanceToPlane = 0;
			if(plane.Raycast(newRay, out distanceToPlane))
				positionOnPlane = point + distanceToPlane * towardsPlane;

			return positionOnPlane;
		}

		// Imported Stuff

		public static bool IsClockwisePolygon(Vector3[] polygon)
		{
			
			bool isClockwise = false;
			double sum = 0;
			
			for ( int i = 0; i < polygon.Length-1; i++)
			{
				sum += (polygon[i + 1].x - polygon[i].x) * (polygon[i + 1].z + polygon[i].z);
			}
			
			isClockwise = (sum > 0) ? true : false;
			return isClockwise;
		}

		/// <summary>
		/// DEPRECIATED: Tests to see if a point lies between two lines OR on one point. MUST USE Compass directions, no slants
		/// </summary>
		public static bool TestBetweenTwoPoints(Vector3 pointAlongLine, Vector3 p1, Vector3 p2)
		{
			bool isBetween = false;
			
			if(p1 == p2 && pointAlongLine == p1)
				return true;
			
			if(pointAlongLine.x == p1.x && pointAlongLine.x == p2.x)
			{
				if(p1.z > p2.z)
				{
					if(pointAlongLine.z <= p1.z && pointAlongLine.z >= p2.z)
						isBetween = true;
				}
				if(p1.z < p2.z)
				{
					if(pointAlongLine.z >= p1.z && pointAlongLine.z <= p2.z)
						isBetween = true;
				}
			}
			if(pointAlongLine.z == p1.z && pointAlongLine.z == p2.z)
			{
				
				if(p1.x > p2.x)
				{
					if(pointAlongLine.x <= p1.x && pointAlongLine.x >= p2.x)
						return true;
				}
				if(p1.x < p2.x)
				{
					if(pointAlongLine.x >= p1.x && pointAlongLine.x <= p2.x)
						return true;
				}
			}
			return isBetween;
		}

		public static int GetIndexOfComplexWall(Vector3 point, Vector3[] wall)
		{
			for(int i = 0; i < wall.Length - 1; i++)
			{
				int nextPoint = i + 1;
				if(nextPoint > wall.Length - 2)
					nextPoint = 0;
				
				if(point.x == wall[i].x && point.z == wall[i].z)
					return i;

				if(point.x == wall[nextPoint].x && point.z == wall[nextPoint].z)
					return nextPoint;

				if(BCUtils.TestBetweenTwoPointsXZ(point, wall[i], wall[nextPoint], 0.01f))
				{
					return i;
				}

				if((wall[i] - wall[nextPoint]).sqrMagnitude < 0.0000001)
				{
					Debug.Log("Testing between two points on top of each other " + i + " " + wall[i]);
				}
			}

			return -1;
		}

		/// <summary>
		/// Tests if a point is between two points ont he XZ axis
		/// </summary>
		/// <returns><c>true</c>, if between two points X was tested, <c>false</c> otherwise.</returns>
		/// <param name="pointAlongLine">Point along line.</param>
		/// <param name="p1">P1.</param>
		/// <param name="p2">P2.</param>
		/// <param name="epsilon">Epsilon.</param>
		public static bool TestBetweenTwoPointsXZ(Vector3 pointAlongLine, Vector3 p1, Vector3 p2, float epsilon = 0.1f)
		{
			// TODO: This needs to have error rounding introduced into it (an episilon)

			float areaOfTriangle = CalculateAreaOfTriangle(pointAlongLine, p1, p2);

			if(pointAlongLine == p1 || pointAlongLine == p2)
				return true;

			if(areaOfTriangle <= epsilon)
			{
				bool betweenZ = false;

				if(pointAlongLine.z == p1.z && pointAlongLine.z == p2.z)
					betweenZ = true;
				else if(pointAlongLine.z < p1.z - epsilon)
				{
					if(pointAlongLine.z > p2.z + epsilon)
						betweenZ = true;
				}
				else if(pointAlongLine.z > p1.z + epsilon)
				{
					if(pointAlongLine.z < p2.z - epsilon)
						betweenZ = true;
				}

				bool betweenX = false;

				if(pointAlongLine.x == p1.x && pointAlongLine.x == p2.x)
					betweenX = true;
				else if(pointAlongLine.x < p1.x)
				{
					if(pointAlongLine.x > p2.x)
						betweenX = true;
				}
				else if(pointAlongLine.x > p1.x)
				{
					if(pointAlongLine.x < p2.x)
						betweenX = true;
				}

				if(betweenX && betweenZ)
					return true;

				return false;
			}
				
			return false;

		}

		private static bool IsPointBetweenTwoPoints (Vector3 point, Vector3 p1, Vector3 p2)
		{
			if(point.x < p1.x && point.x > p2.x)
			{
				if(point.z < p1.z && point.z > p2.z)
					return true;
			}

			return false;
		}

		private static float CalculateAreaOfTriangle (Vector3 p1, Vector3 p2, Vector3 p3)
		{
			float a = (p1 - p2).magnitude; // pt1.DistanceTo(pt2);
			float b = (p2 - p3).magnitude; // pt2.DistanceTo(pt3);
			float c = (p1 - p3).magnitude; // pt3.DistanceTo(pt1);
			float s = (a + b + c) / 2;
			return Mathf.Sqrt(s * (s-a) * (s-b) * (s-c));
		}

		/// <summary>
		/// Tests to see if a point lies between two lines. MUST USE Compass directions, no slants
		/// </summary>
		public static bool TestWithinTwoPoints(Vector3 pointAlongLine, Vector3 p1, Vector3 p2)
		{
			bool isBetween = false;

			// Round everything for floating point issues
	//		p1 = new Vector3((float)System.Math.Round(p1.x, 3), p1.y, (float)System.Math.Round(p1.z, 3));
	//		p2 = new Vector3((float)System.Math.Round(p2.x, 3), p2.y, (float)System.Math.Round(p2.z, 3));
	//		pointAlongLine = new Vector3((float)System.Math.Round(pointAlongLine.x, 3), pointAlongLine.y, (float)System.Math.Round(pointAlongLine.z, 3));
			
			if(p1 == p2 && pointAlongLine == p1)
				return false;
			
			if(pointAlongLine.x == p1.x && pointAlongLine.x == p2.x)
			{
				if(p1.z > p2.z)
				{
					if(pointAlongLine.z < p1.z && pointAlongLine.z > p2.z)
						isBetween = true;
				}
				if(p1.z < p2.z)
				{
					if(pointAlongLine.z > p1.z && pointAlongLine.z < p2.z)
						isBetween = true;
				}
			}

			// This is along the X axis
			if(pointAlongLine.z == p1.z && pointAlongLine.z == p2.z)
			{
				
				if(p1.x > p2.x)
				{
					if(pointAlongLine.x < p1.x && pointAlongLine.x > p2.x)
						return true;
				}
				if(p1.x < p2.x)
				{
					if(pointAlongLine.x > p1.x && pointAlongLine.x < p2.x)
						return true;
				}
			}
			return isBetween;
		}


		/// <summary>
		/// Gets the index list of where the grid item is between
		/// </summary>
		public static List<int> BetweenTwoPointOnALine(Vector3 gridPoint, List<Vector3> wallLine)
		{
			List<int> points = new List<int>();

			for(int n = 0; n < wallLine.Count - 1; n++) // the -1 removes the closing point
			{
				if(gridPoint.x == wallLine[n].x && gridPoint.z == wallLine[n].z)
				{
					points.Clear();
					if(n == 0)
					{
						points.Add(n); // Adds current wall
						points.Add(wallLine.Count - 2); // Adds the point before
						points.Add(n + 1); // Adds the point after
					}
					else if(n == wallLine.Count - 2)
					{
						points.Add(n);
						points.Add(n - 1);
						points.Add(0);
					}
					else
					{
						points.Add(n);
						points.Add(n - 1);
						points.Add(n + 1);
					}
					break;
				}
				// What happens if the grid point is on the same X, but is not on the same z
				else if(gridPoint.x == wallLine[n].x)
				{
					int beforeIndex = n - 1;
					int afterIndex = n + 1;
					if(n == 0)
						beforeIndex = wallLine.Count - 2;
					if(n == wallLine.Count - 2)
						afterIndex = 0;
					
					// First test the upper bound
					if(gridPoint.z <= wallLine[n].z && gridPoint.z >= wallLine[beforeIndex].z)
					{
						points.Add(n);
						points.Add(beforeIndex);
					}
					else if(gridPoint.z <= wallLine[n].z && gridPoint.z >= wallLine[afterIndex].z)
					{
						points.Add(n);
						points.Add(afterIndex);
					}
				}
				else if(gridPoint.z == wallLine[n].z)
				{
					int beforeIndex = n - 1;
					int afterIndex = n + 1;
					if(n == 0)
						beforeIndex = wallLine.Count - 2;
					if(n == wallLine.Count - 2)
						afterIndex = 0;
					
					// First test the upper bound
					if(gridPoint.x <= wallLine[n].x && gridPoint.x >= wallLine[beforeIndex].x)
					{
						points.Add(n);
						points.Add(beforeIndex);
					}
					else if(gridPoint.x <= wallLine[n].x && gridPoint.x >= wallLine[afterIndex].x)
					{
						points.Add(n);
						points.Add(afterIndex);
					}
				}
			}
			
			return points.ToList<int>();
		}

		public static List<int> RoomOverlapIndexes(Vector3 testPoint, FloorBlueprint floorBp)
		{
			List<int> indexes = new List<int>();
			
			for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
			{
				if(BCUtils.IsPointInARoom(testPoint, floorBp.RoomBlueprints[i]) == true)
				{
					indexes.Add(i);
				}
			}
			
			return indexes;
		}

		public static float FindDistanceToNextPoint(Vector3 insertPoint, int index, RoomBlueprint roomBp)
		{
			if(index >= roomBp.PerimeterWalls.Count)
				return -1;
			
			int nextPoint = index + 1;
			if(nextPoint >= roomBp.PerimeterWalls.Count - 1)
				nextPoint = 0;
			
			return (roomBp.PerimeterWalls[index] - insertPoint).magnitude;
		}

		/// <summary>
		/// Tests along a plane axis for a room opening
		/// </summary>
		public static bool TestValidRoomOpening(Vector3 start, Vector3 end, FloorBlueprint floorBlueprint)
		{
			if(start == end) // Still shows true because we want the image to show up for the player.
				return true;

			List<int> indexes = new List<int>();

			// Test along walls
			if(start.z == end.z)
			{
				float distance = end.x - start.x;
				int mod = 1;
				if(distance < 0)
					mod = -1; 

				distance = Mathf.Abs(distance);

				// Always start just away from the point to test if it is good
				// If the start is at the end, it won't matter how many this item touches
				for(float f = 0.5f; f <= distance; f += 0.5f)
				{
					// Test this point against all walls in the system. Add this information to the indexes
					// If the index count is greater than 4, (meaning it has hit a T or a +), then only if 
					// it is the end of the line should it return true
					indexes.Clear();
					Vector3 point = new Vector3(start.x + f * mod, start.z, start.z);

					for(int i = 0; i < floorBlueprint.RoomBlueprints.Count; i++)
					{
						indexes.AddRange(BetweenTwoPointOnALine(point, floorBlueprint.RoomBlueprints[i].PerimeterWalls));
					}

					// If it does not end on a corner, or a T, then return false
					if(indexes.Count == 3 & f != distance || indexes.Count > 4 && f != distance)
						return false;

					if(indexes.Count < 2)
						return false;

					for(int i = 0; i < floorBlueprint.Doors.Count; i++)
					{
						DoorInfo door = floorBlueprint.Doors[i];

						if(TestWithinTwoPoints(point, door.Start, door.End))
							return false;
					}

					for(int i = 0; i < floorBlueprint.Windows.Count; i++)
					{
						WindowInfo window = floorBlueprint.Windows[i];

						if(TestWithinTwoPoints(point, window.Start, window.End))
							return false;
					}
				}

				return true;
			}
			else if(start.x == end.x)
			{
				float distance = end.z - start.z;
				int mod = 1;
				if(distance < 0)
					mod = -1; 
				
				distance = Mathf.Abs(distance);
				
				// Always start just away from the point to test if it is good
				// If the start is at the end, it won't matter how many this item touches
				for(float f = 0.5f; f <= distance; f += 0.5f)
				{
					// Test this point against all walls in the system. Add this information to the indexes
					// If the index count is greater than 4, (meaning it has hit a T or a +), then only if 
					// it is the end of the line should it return true
					indexes.Clear();
					Vector3 point = new Vector3(start.x, start.z, start.z + f * mod);

					for(int i = 0; i < floorBlueprint.RoomBlueprints.Count; i++)
					{
						indexes.AddRange(BetweenTwoPointOnALine(point, floorBlueprint.RoomBlueprints[i].PerimeterWalls));
					}
					
					// If it does not end on a corner, or a T, then return false
					if(indexes.Count == 3 & f != distance || indexes.Count > 4 && f != distance)
						return false;
					
					if(indexes.Count < 2)
						return false;

					for(int i = 0; i < floorBlueprint.Doors.Count; i++)
					{
						DoorInfo door = floorBlueprint.Doors[i];

						if(TestWithinTwoPoints(point, door.Start, door.End))
							return false;
					}

					for(int i = 0; i < floorBlueprint.Windows.Count; i++)
					{
						WindowInfo window = floorBlueprint.Windows[i];

						if(TestWithinTwoPoints(point, window.Start, window.End))
							return false;
					}
				}

				return true;
			}

			return false;
		}

		/// <summary>
		/// Finds the stair outline that will be inset by 0.1 on each side except the end of it
		/// </summary>
		/// <returns>The stairs outline.</returns>
		/// <param name="stair">Stair.</param>
		/// <param name="ceilingHeight">Ceiling height.</param>
		public static Vector3[] GetStairsOutline(StairInfo stair, float ceilingHeight = 0, float inset = 0.1f)
		{
			// Rounds to 4 meters 
			Vector3 stairsDirection = (stair.Start - stair.End).normalized;
			
			Vector3 rectOffset = Vector3.zero;
			
			if(stairsDirection.x < 0)
				rectOffset = Vector3.forward;
			if(stairsDirection.x > 0)
				rectOffset = Vector3.back;
			if(stairsDirection.z < 0)
				rectOffset = Vector3.left;
			if(stairsDirection.z > 0)
				rectOffset = Vector3.right;

			// To prevent Z-fighting
			inset += 0.001f;

			Vector3 cross = Vector3.Cross(stairsDirection, Vector3.up);
//			Utility.Draw3DCross(stair.Start + rectOffset + Vector3.up * ceilingHeight + cross * inset, .25f, Color.white, 5);
//			Utility.Draw3DCross(stair.End + rectOffset + Vector3.up * ceilingHeight + cross *inset, .25f, Color.yellow, 5);

			Vector3[] stairsOutline = new Vector3[5]
			{
				stair.Start + rectOffset + Vector3.up * ceilingHeight + cross * inset - stairsDirection * inset, 
				stair.End + rectOffset + Vector3.up * ceilingHeight + cross * inset, 
				stair.End - rectOffset + Vector3.up * ceilingHeight - cross * inset,
				stair.Start - rectOffset + Vector3.up * ceilingHeight - cross * inset - stairsDirection * inset,
				stair.Start + rectOffset + Vector3.up * ceilingHeight + cross * inset - stairsDirection * inset
			};
			
			return stairsOutline;
		}

		public static Vector3[] Get3DStairsOutline(StairInfo stair, Vector3 direction)
		{
			Vector3 rectOffset = direction;

			return new Vector3[10]
				{
					stair.Start + rectOffset, stair.End + rectOffset + Vector3.up * 3, stair.End - rectOffset + Vector3.up * 3, stair.Start - rectOffset,
					stair.Start + rectOffset, stair.End + rectOffset, stair.End + rectOffset + Vector3.up * 3,  stair.End - rectOffset + Vector3.up * 3,
					stair.End - rectOffset, stair.Start - rectOffset,
				};
		}

		public static Vector3[] GetOffsetStairsOutline(StairInfo stair, float stepStartOffset, float ceilingHeight = 0)
		{
			Vector3 stairsDirection = (stair.Start - stair.End).normalized;

			Vector3[] stairsOutline = GetStairsOutline(stair, ceilingHeight);
//			stairsOutline[0] -= stairsDirection * stepStartOffset;
//			stairsOutline[3] -= stairsDirection * stepStartOffset;
//			stairsOutline[4] -= stairsDirection * stepStartOffset;

			return stairsOutline;
		}

		/// <summary>
		/// Given the floorplan, does this point fall within one of the stairs
		/// </summary>
		public static bool PointWithinFloorStairs (Vector3 testPoint, FloorBlueprint floorBp)
		{
			if(floorBp == null)
				return false;

			for(int i = 0; i < floorBp.Stairs.Count; i++)
			{
				if(PointInPolygonXZ(testPoint, GetStairsOutline(floorBp.Stairs[i])))
				{
					return true;
				}
			}
				
			return false;
		}

		public static void CollapseWallLines (ref RoomBlueprint roomBp)
		{
			bool breakIt = false;
			int breaker = 0;

			// Looks at all lines in a wall and removes any midpoints in the wall
			while(breaker < 10000 && breakIt == false)
			{
				breaker++;
				breakIt = true;

				for(int i = 0; i < roomBp.PerimeterWalls.Count - 1; i++)
				{
					int nextPoint = i + 1;
					int nextNextPoint = i + 2;
					if(nextNextPoint >= roomBp.PerimeterWalls.Count)
						nextNextPoint = 0;

					Vector3 p1 = roomBp.PerimeterWalls[i];
					Vector3 p2 = roomBp.PerimeterWalls[nextPoint];
					Vector3 p3 = roomBp.PerimeterWalls[nextNextPoint];

					if(p1 == p2)
					{
						roomBp.PerimeterWalls.RemoveAt(nextPoint);
						breakIt = false;
						break;
					}

					Vector3 thisPointWallDirection = (p2 - p1).normalized;
					Vector3 nextPointWallDirection = (p3 - p2).normalized;

					if(thisPointWallDirection == nextPointWallDirection)
					{
						roomBp.PerimeterWalls.RemoveAt(nextPoint);
						breakIt = false;
						break;
					}

				}

				// Removes any thin walls
				for(int i = 0; i < roomBp.PerimeterWalls.Count - 1; i++)
				{
					int prevPoint = BCUtils.GetIndexAtPlus(i, -1, roomBp.PerimeterWalls);
					int thisPoint = BCUtils.GetIndexAtPlus(i, 0, roomBp.PerimeterWalls);
					int nextPoint = BCUtils.GetIndexAtPlus(i, 1, roomBp.PerimeterWalls);
					
					Vector3 p0 = roomBp.PerimeterWalls[prevPoint];
					Vector3 p1 = roomBp.PerimeterWalls[thisPoint];
					Vector3 p2 = roomBp.PerimeterWalls[nextPoint];
					
					Vector3 dir1 = (p2 - p1).normalized;
					Vector3 dir2 = (p0 - p1).normalized;
					
					if(dir1 == dir2)
					{
						roomBp.PerimeterWalls.RemoveAt(thisPoint);

						breakIt = false;
						break;
					}
				}
			}

			if(roomBp.PerimeterWalls[1].x == roomBp.PerimeterWalls[roomBp.PerimeterWalls.Count - 2].x)
			{
				roomBp.PerimeterWalls[roomBp.PerimeterWalls.Count - 1] = roomBp.PerimeterWalls[1];
				roomBp.PerimeterWalls.RemoveAt(0);
			}
			else if(roomBp.PerimeterWalls[1].z == roomBp.PerimeterWalls[roomBp.PerimeterWalls.Count - 2].z)
			{
				roomBp.PerimeterWalls[roomBp.PerimeterWalls.Count - 1] = roomBp.PerimeterWalls[1];
				roomBp.PerimeterWalls.RemoveAt(0);
			}

			if(roomBp.PerimeterWalls.Count < 3)
			{
				roomBp.PerimeterWalls.Clear();
				return;
			}
				
			// Test for proper loop around
			if((roomBp.PerimeterWalls[0] - roomBp.PerimeterWalls[roomBp.PerimeterWalls.Count - 1]).sqrMagnitude > 0.001)
			{
				roomBp.PerimeterWalls[roomBp.PerimeterWalls.Count - 1] = roomBp.PerimeterWalls[0];
			}
		}

		public static void CollapseWallLines (List<Vector3> perimeterWalls)
		{
			bool breakIt = false;
			int breaker = 0;
			
			// Looks at all lines in a wall and removes any midpoints in the wall
			while(breaker < 10000 && breakIt == false)
			{
				breaker++;
				breakIt = true;
				
				for(int i = 0; i < perimeterWalls.Count - 1; i++)
				{
					int nextPoint = i + 1;
					int nextNextPoint = i + 2;
					if(nextNextPoint >= perimeterWalls.Count)
						nextNextPoint = 0;
					
					Vector3 p1 = perimeterWalls[i];
					Vector3 p2 = perimeterWalls[nextPoint];
					Vector3 p3 = perimeterWalls[nextNextPoint];
					
					if(p1 == p2)
					{
						perimeterWalls.RemoveAt(nextPoint);
						breakIt = false;
						break;
					}
					
					Vector3 thisPointWallDirection = (p2 - p1).normalized;
					Vector3 nextPointWallDirection = (p3 - p2).normalized;
					
					if(thisPointWallDirection == nextPointWallDirection)
					{
						perimeterWalls.RemoveAt(nextPoint);
						breakIt = false;
					}
				}
			}
			
			if(perimeterWalls[1].x == perimeterWalls[perimeterWalls.Count - 2].x)
			{
				perimeterWalls[perimeterWalls.Count - 1] = perimeterWalls[1];
				perimeterWalls.RemoveAt(0);
			}
			else if(perimeterWalls[1].z == perimeterWalls[perimeterWalls.Count - 2].z)
			{
				perimeterWalls[perimeterWalls.Count - 1] = perimeterWalls[1];
				perimeterWalls.RemoveAt(0);
			}
		}

		/// <summary>
		/// Gets the index of the point on the wall. The index is the point before provided unless RIGHT on the next point
		/// </summary>
		public static int GetIndexOfWall(Vector3 point, RoomBlueprint roomBp)
		{
			return GetIndexOfWall(point, roomBp.PerimeterWalls);
		}

		public static int GetIndexOfWall(Vector3 point, List<Vector3> wall)
		{
			return GetIndexOfWall(point, wall.ToArray<Vector3>());
		}

		/// <summary>
		/// Gets the index of the point on the wall. The index is the point before provided unless RIGHT on the next point
		/// </summary>
		public static int GetIndexOfWall(Vector3 point, Vector3[] wall)
		{
			for(int i = 0; i < wall.Length - 1; i++)
			{
				int nextPoint = i + 1;
				if(nextPoint > wall.Length - 2)
					nextPoint = 0;
				
				if(point.x == wall[i].x && point.z == wall[i].z)
				{
					return i;
				}
					

				if(point.x == wall[nextPoint].x && point.z == wall[nextPoint].z)
				{
					return nextPoint;
				}
					
				if(BCUtils.TestBetweenTwoPoints(point, wall[i], wall[nextPoint]))
				{
					return i;
				}
			}
			
			return -1;
		}
			
		/// <summary>
		/// Finds the direction of the inset in a poly, only in XZ axis
		/// </summary>
		/// <returns>The inset direction.</returns>
		/// <param name="p1">Point 1</param>
		/// <param name="p2">Point 2</param>
		/// <param name="polyGon">Poly gon.</param>
		public static Vector3 FindInsetDirection (Vector3 p1, Vector3 p2, Vector3[] polyGon)
		{
			Vector3 offset = Vector3.zero;
			float offsetTestAmount = 0.1f;
			
			if(p1.x == p2.x && p1.z == p2.z)
				return offset;

			bool isOneForwardLeftEmpty = true;
			bool isOneForwardRightEmpty = true;
			bool isOneBackLeftEmpty = true;
			bool isOneBackRightEmpty = true;
			
			bool isTwoForwardLeftEmpty = true;
			bool isTwoForwardRightEmpty = true;
			bool isTwoBackLeftEmpty = true;
			bool isTwoBackRightEmpty = true;
			
			// Test all these cases for p1
			if(PointInPolygonXZ(p1 + (Vector3.forward + Vector3.left) * offsetTestAmount, polyGon) == true)
				isOneForwardLeftEmpty = false;
			if(PointInPolygonXZ(p1 + (Vector3.forward + Vector3.right) * offsetTestAmount, polyGon) == true)
				isOneForwardRightEmpty = false;
			if(PointInPolygonXZ(p1 + (Vector3.back + Vector3.left) * offsetTestAmount, polyGon) == true)
				isOneBackLeftEmpty = false;
			if(PointInPolygonXZ(p1 + (Vector3.back + Vector3.right) * offsetTestAmount, polyGon) == true)
				isOneBackRightEmpty = false;
			
			if(PointInPolygonXZ(p2 + (Vector3.forward + Vector3.left) * offsetTestAmount, polyGon) == true)
				isTwoForwardLeftEmpty = false;
			if(PointInPolygonXZ(p2 + (Vector3.forward + Vector3.right) * offsetTestAmount, polyGon) == true)
				isTwoForwardRightEmpty = false;
			if(PointInPolygonXZ(p2 + (Vector3.back + Vector3.left) * offsetTestAmount, polyGon) == true)
				isTwoBackLeftEmpty = false;
			if(PointInPolygonXZ(p2 + (Vector3.back + Vector3.right) * offsetTestAmount, polyGon) == true)
				isTwoBackRightEmpty = false;

			// If the point is nowhere near the wall, then return a zero vector
			if(isOneForwardLeftEmpty && isOneForwardRightEmpty && isOneBackLeftEmpty && isOneBackRightEmpty 
			   && isTwoForwardLeftEmpty && isTwoForwardRightEmpty && isTwoBackLeftEmpty && isTwoBackRightEmpty)
				return Vector3.zero;

			if(p1.x == p2.x)
			{
				if(p1.z < p2.z)
				{
					if(isOneForwardRightEmpty == isTwoBackRightEmpty && isOneForwardRightEmpty == true)
						offset = Vector3.left;
				}
				if(p1.z > p2.z)
				{
					if(isOneBackRightEmpty == isTwoForwardRightEmpty && isOneBackRightEmpty == true)
						offset = Vector3.left;
				}
				
				// backward facing items
				
				if(p1.z < p2.z)
				{
					if(isOneForwardLeftEmpty == isTwoBackLeftEmpty && isOneForwardLeftEmpty == true)
						offset = Vector3.right;
				}
				if(p1.z > p2.z)
				{
					if(isOneBackLeftEmpty == isTwoForwardLeftEmpty && isOneBackLeftEmpty == true)
						offset = Vector3.right;
				}
			}
			
			if(p1.z == p2.z)
			{
				// forward facing items
				if(p1.x < p2.x)
				{
					if(isOneForwardRightEmpty == isTwoForwardLeftEmpty && isOneForwardRightEmpty == true)
						offset = Vector3.back;
				}
				if(p1.x > p2.x)
				{
					if(isOneForwardLeftEmpty == isTwoForwardRightEmpty && isTwoForwardRightEmpty == true)
						offset = Vector3.back;
				}
				// backward facing items
				
				if(p1.x < p2.x)
				{
					if(isOneBackRightEmpty == isTwoBackLeftEmpty && isOneBackRightEmpty == true)
						offset = Vector3.forward;
				}
				if(p1.x > p2.x)
				{
					if(isOneBackLeftEmpty == isTwoBackRightEmpty && isOneBackLeftEmpty == true)
						offset = Vector3.forward;
				}
			}
			return offset;
		}
		
		/// <summary>
		/// Finds the direction of the inset in a poly, only in XZ axis
		/// </summary>
		/// <returns>The inset direction.</returns>
		/// <param name="p1">Point 1</param>
		/// <param name="p2">Point 2</param>
		/// <param name="polyGon">Poly gon.</param>
		public static Vector3 Get8InsetDirection (Vector3 testPoint, Vector3 nextPoint, Vector3[] polyGon)
		{
			Vector3 offset = Vector3.zero;
			float offsetTestAmount = 0.1f;
			
			if(testPoint.x == nextPoint.x && testPoint.z == nextPoint.z)
				return offset;
			
			offset = FindInsetDirection(testPoint, nextPoint, polyGon);
			
			bool isForwardLeftEmpty = true;
			bool isForwardRightEmpty = true;
			bool isBackLeftEmpty = true;
			bool isBackRightEmpty = true;
			
			
			// Test all these cases for p1
			if(PointInPolygonXZ(testPoint + (Vector3.forward + Vector3.left) * offsetTestAmount, polyGon) == true)
				isForwardLeftEmpty = false;
			if(PointInPolygonXZ(testPoint + (Vector3.forward + Vector3.right) * offsetTestAmount, polyGon) == true)
				isForwardRightEmpty = false;
			if(PointInPolygonXZ(testPoint + (Vector3.back + Vector3.left) * offsetTestAmount, polyGon) == true)
				isBackLeftEmpty = false;
			if(PointInPolygonXZ(testPoint + (Vector3.back + Vector3.right) * offsetTestAmount, polyGon) == true)
				isBackRightEmpty = false;
			
			// Deals with inside corner points
			if(isForwardLeftEmpty == true && isForwardRightEmpty == false && isBackLeftEmpty == false && isBackRightEmpty == false)
				offset += Vector3.back;
			
			if(isForwardRightEmpty == true && isForwardLeftEmpty == false && isBackLeftEmpty == false && isBackRightEmpty == false)
				offset += Vector3.left;
			
			if(isBackLeftEmpty == true && isForwardLeftEmpty == false && isForwardRightEmpty == false && isBackRightEmpty == false)
				offset += Vector3.right;
			
			if(isBackRightEmpty == true && isForwardLeftEmpty == false && isForwardRightEmpty == false && isBackLeftEmpty == false)
				offset += Vector3.forward;
			
			// Deals with outside corner points
			if(isForwardLeftEmpty == false && isForwardRightEmpty == true && isBackLeftEmpty == true && isBackRightEmpty == true)
				offset += Vector3.left;
			
			if(isForwardRightEmpty == false && isForwardLeftEmpty == true && isBackLeftEmpty == true && isBackRightEmpty == true)
				offset += Vector3.forward;
			
			if(isBackLeftEmpty == false && isForwardLeftEmpty == true && isForwardRightEmpty == true && isBackRightEmpty == true)
				offset += Vector3.back;
			
			if(isBackRightEmpty == false && isForwardLeftEmpty == true && isForwardRightEmpty == true && isBackLeftEmpty == true)
				offset += Vector3.right;
			
			return offset;
		}

		/// <summary>
		/// Finds the direction of the inset in a poly, only in XZ axis
		/// </summary>
		/// <returns>The inset direction.</returns>
		/// <param name="p1">Point 1</param>
		/// <param name="p2">Point 2</param>
		/// <param name="polyGon">Poly gon.</param>
		public static Vector3 GetOutsetFromManyRooms (Vector3 testPoint, FloorBlueprint floorBp)
		{
			Vector3 offset = Vector3.zero;
			float offsetTestAmount = 0.1f;

			// In order to find the offset for many of these walls, we need to scan UpperLeft, UpperRight, LowerLeft, LowerRight
			bool forwardLeftEmpty = true;
			bool forwardRightEmpty = true;
			bool backLeftEmpty = true;
			bool backRightEmpty = true;

			Vector3 offsetTest = (Vector3.forward + Vector3.left) * offsetTestAmount;

			// Test upper Left
			for(int roomIndex = 0; roomIndex < floorBp.RoomBlueprints.Count; roomIndex++)
			{
				if(BCUtils.PointInPolygonXZ(offsetTest + testPoint, floorBp.RoomBlueprints[roomIndex].PerimeterWalls.ToArray<Vector3>()))
				{
					forwardLeftEmpty = false;
					break;
				}
			}

			offsetTest = (Vector3.forward + Vector3.right) * offsetTestAmount;

			// Test upper Right
			for(int roomIndex = 0; roomIndex < floorBp.RoomBlueprints.Count; roomIndex++)
			{
				if(BCUtils.PointInPolygonXZ(offsetTest + testPoint, floorBp.RoomBlueprints[roomIndex].PerimeterWalls.ToArray<Vector3>()))
				{
					forwardRightEmpty = false;
					break;
				}
			}

			offsetTest = (Vector3.back + Vector3.left) * offsetTestAmount;
			
			// Test back Left
			for(int roomIndex = 0; roomIndex < floorBp.RoomBlueprints.Count; roomIndex++)
			{
				if(BCUtils.PointInPolygonXZ(offsetTest + testPoint, floorBp.RoomBlueprints[roomIndex].PerimeterWalls.ToArray<Vector3>()))
				{
					backLeftEmpty = false;
					break;
				}
			}
			
			offsetTest = (Vector3.back + Vector3.right) * offsetTestAmount;
			
			// Test back right
			for(int roomIndex = 0; roomIndex < floorBp.RoomBlueprints.Count; roomIndex++)
			{
				if(BCUtils.PointInPolygonXZ(offsetTest + testPoint, floorBp.RoomBlueprints[roomIndex].PerimeterWalls.ToArray<Vector3>()))
				{
					backRightEmpty = false;
					break;
				}
			}

			// First figure out the 4 way direction

			if(forwardLeftEmpty && forwardRightEmpty && backLeftEmpty == false && backRightEmpty == false)
				return Vector3.forward;

			if(backLeftEmpty && backRightEmpty && forwardLeftEmpty == false && forwardRightEmpty == false)
				return Vector3.back;

			if(forwardLeftEmpty && backLeftEmpty && forwardRightEmpty == false && backRightEmpty == false)
				return Vector3.left;

			if(forwardRightEmpty && backRightEmpty && forwardLeftEmpty == false &&  backLeftEmpty == false)
				return  Vector3.right;

			// If three sides are covered
			if(forwardLeftEmpty && forwardRightEmpty == false && backLeftEmpty == false && backRightEmpty == false)
				return Vector3.forward + Vector3.left;

			if(forwardRightEmpty && forwardLeftEmpty  == false && backLeftEmpty == false && backRightEmpty == false)
				return Vector3.forward + Vector3.right;
			
			if(backLeftEmpty && backRightEmpty == false &&  forwardLeftEmpty == false && forwardRightEmpty == false)
				return Vector3.back + Vector3.left;

			if(backRightEmpty && backLeftEmpty == false &&  forwardLeftEmpty == false && forwardRightEmpty == false)
				return Vector3.back + Vector3.right;

			// If three sides are open
			if(forwardLeftEmpty == false && forwardRightEmpty && backLeftEmpty && backRightEmpty)
				return Vector3.back + Vector3.right;
			
			if(forwardRightEmpty == false && forwardLeftEmpty && backLeftEmpty && backRightEmpty)
				return Vector3.back + Vector3.left;
			
			if(backLeftEmpty == false && backRightEmpty &&  forwardLeftEmpty  && forwardRightEmpty )
				return Vector3.forward + Vector3.right;
			
			if(backRightEmpty == false && backLeftEmpty &&  forwardLeftEmpty && forwardRightEmpty)
				return Vector3.forward + Vector3.left;

			// If the offset is kitty corner
			// TODO: Create a more general solution so that kitty corners are dealt with
			if(backRightEmpty == false && forwardLeftEmpty == false && backLeftEmpty && forwardRightEmpty)
				Debug.LogError("Warning: You have a kitty corner, these are not supported. It is at position: " + testPoint);
			if(backLeftEmpty == false && forwardRightEmpty == false && backRightEmpty && forwardLeftEmpty)
				Debug.LogError("Warning: You have a kitty corner, these are not supported. It is at position: " + testPoint);

			return offset;
		}

		/// <summary>
		/// Finds the type of outset from the rooms. 
		/// -1 for nothing, 
		/// 0 for three corners in rooms, 
		/// 1 for half and half, 
		/// 2 for one corner in room, 
		/// 3 for entirely surrounded and
		/// 4 for kitty corner
		/// </summary>
		public static int GetOutsetTypeFromManyRooms (Vector3 testPoint, FloorBlueprint floorBp)
		{
			float offsetTestAmount = 0.1f;
			
			// In order to find the offset for many of these walls, we need to scan UpperLeft, UpperRight, LowerLeft, LowerRight
			bool forwardLeftEmpty = true;
			bool forwardRightEmpty = true;
			bool backLeftEmpty = true;
			bool backRightEmpty = true;
			
			Vector3 offsetTest = (Vector3.forward + Vector3.left) * offsetTestAmount;
			
			// Test upperLeft
			for(int roomIndex = 0; roomIndex < floorBp.RoomBlueprints.Count; roomIndex++)
			{
				if(BCUtils.PointInPolygonXZ(offsetTest + testPoint, floorBp.RoomBlueprints[roomIndex].PerimeterWalls.ToArray<Vector3>()))
				{
					forwardLeftEmpty = false;
					break;
				}
			}
			
			offsetTest = (Vector3.forward + Vector3.right) * offsetTestAmount;
			
			// Test upperLeft
			for(int roomIndex = 0; roomIndex < floorBp.RoomBlueprints.Count; roomIndex++)
			{
				if(BCUtils.PointInPolygonXZ(offsetTest + testPoint, floorBp.RoomBlueprints[roomIndex].PerimeterWalls.ToArray<Vector3>()))
				{
					forwardRightEmpty = false;
					break;
				}
			}
			
			offsetTest = (Vector3.back + Vector3.left) * offsetTestAmount;
			
			// Test upperLeft
			for(int roomIndex = 0; roomIndex < floorBp.RoomBlueprints.Count; roomIndex++)
			{
				if(BCUtils.PointInPolygonXZ(offsetTest + testPoint, floorBp.RoomBlueprints[roomIndex].PerimeterWalls.ToArray<Vector3>()))
				{
					backLeftEmpty = false;
					break;
				}
			}
			
			offsetTest = (Vector3.back + Vector3.right) * offsetTestAmount;
			
			// Test upperLeft
			for(int roomIndex = 0; roomIndex < floorBp.RoomBlueprints.Count; roomIndex++)
			{
				if(BCUtils.PointInPolygonXZ(offsetTest + testPoint, floorBp.RoomBlueprints[roomIndex].PerimeterWalls.ToArray<Vector3>()))
				{
					backRightEmpty = false;
					break;
				}
			}
			
			// First figure out the 4 way direction
			
			if(forwardLeftEmpty && forwardRightEmpty && backLeftEmpty == false && backRightEmpty == false)
				return 1;
			
			if(backLeftEmpty && backRightEmpty && forwardLeftEmpty == false && forwardRightEmpty == false)
				return 1;
			
			if(forwardLeftEmpty && backLeftEmpty && forwardRightEmpty == false && backRightEmpty == false)
				return 1;
			
			if(forwardRightEmpty && backRightEmpty && forwardLeftEmpty == false &&  backLeftEmpty == false)
				return 1;
			
			// If three sides are covered
			if(forwardLeftEmpty && forwardRightEmpty == false && backLeftEmpty == false && backRightEmpty == false)
				return 0;
			
			if(forwardRightEmpty && forwardLeftEmpty  == false && backLeftEmpty == false && backRightEmpty == false)
				return 0;
			
			if(backLeftEmpty && backRightEmpty == false &&  forwardLeftEmpty == false && forwardRightEmpty == false)
				return 0;
			
			if(backRightEmpty && backLeftEmpty == false &&  forwardLeftEmpty == false && forwardRightEmpty == false)
				return 0;
			
			// If three sides are open
			if(forwardLeftEmpty == false && forwardRightEmpty && backLeftEmpty && backRightEmpty)
				return 2;
			
			if(forwardRightEmpty == false && forwardLeftEmpty && backLeftEmpty && backRightEmpty)
				return 2;
			
			if(backLeftEmpty == false && backRightEmpty &&  forwardLeftEmpty  && forwardRightEmpty )
				return 2;
			
			if(backRightEmpty == false && backLeftEmpty &&  forwardLeftEmpty && forwardRightEmpty)
				return 2;

			// Entirely surrounded
			if(forwardLeftEmpty == false && forwardRightEmpty == false && backLeftEmpty == false && backRightEmpty == false)
				return 3;

			// If the offset is kitty corner
			if(backRightEmpty == false && forwardLeftEmpty == false && backLeftEmpty && forwardRightEmpty)
				return 4;
			
			if(backLeftEmpty == false && forwardRightEmpty == false && backRightEmpty && forwardLeftEmpty)
				return 4;

			return -1;
		}

		/// <summary>
		/// Finds the direction of the inset in a poly, only in XZ axis
		/// </summary>
		/// <returns>The inset direction.</returns>
		/// <param name="p1">Point 1</param>
		/// <param name="p2">Point 2</param>
		/// <param name="polyGon">Poly gon.</param>
		public static Vector3 GetCompassOutsetFromManyRooms (Vector3 p1, Vector3 p2, FloorBlueprint floorBp)
		{
			Vector3 offset = Vector3.zero;
			float offsetTestAmount = 0.1f;
			
			if(p1.x == p2.x && p1.z == p2.z)
				return offset;
			
			bool isOneForwardLeftEmpty = true;
			bool isOneForwardRightEmpty = true;
			bool isOneBackLeftEmpty = true;
			bool isOneBackRightEmpty = true;
			
			bool isTwoForwardLeftEmpty = true;
			bool isTwoForwardRightEmpty = true;
			bool isTwoBackLeftEmpty = true;
			bool isTwoBackRightEmpty = true;
			
			// Test all these cases for p1
			for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
			{
				if(PointInPolygonXZ(p1 + (Vector3.forward + Vector3.left) * offsetTestAmount, floorBp.RoomBlueprints[i].PerimeterWalls.ToArray<Vector3>()) == true)
				{
					isOneForwardLeftEmpty = false;
					break;
				}
			}

			for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
			{
				if(PointInPolygonXZ(p1 + (Vector3.forward + Vector3.right) * offsetTestAmount,  floorBp.RoomBlueprints[i].PerimeterWalls.ToArray<Vector3>()) == true)
				{
					isOneForwardRightEmpty = false;
					break;
				}
			}

			for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
			{
				if(PointInPolygonXZ(p1 + (Vector3.back + Vector3.left) * offsetTestAmount,  floorBp.RoomBlueprints[i].PerimeterWalls.ToArray<Vector3>()) == true)
				{
					isOneBackLeftEmpty = false;
					break;
				}
			}

			for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
			{
				if(PointInPolygonXZ(p1 + (Vector3.back + Vector3.right) * offsetTestAmount,  floorBp.RoomBlueprints[i].PerimeterWalls.ToArray<Vector3>()) == true)
				{
					isOneBackRightEmpty = false;
					break;
				}
			}

			
			for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
			{
				if(PointInPolygonXZ(p2 + (Vector3.forward + Vector3.left) * offsetTestAmount,  floorBp.RoomBlueprints[i].PerimeterWalls.ToArray<Vector3>()) == true)
				{
					isTwoForwardLeftEmpty = false;
					break;
				}
			}
			
			for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
			{
				if(PointInPolygonXZ(p2 + (Vector3.forward + Vector3.right) * offsetTestAmount,  floorBp.RoomBlueprints[i].PerimeterWalls.ToArray<Vector3>()) == true)
				{
					isTwoForwardRightEmpty = false;
					break;
				}
			}
			for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
			{
				if(PointInPolygonXZ(p2 + (Vector3.back + Vector3.left) * offsetTestAmount,  floorBp.RoomBlueprints[i].PerimeterWalls.ToArray<Vector3>()) == true)
				{
					isTwoBackLeftEmpty = false;
					break;
				}
			}
				
			for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
			{
				if(PointInPolygonXZ(p2 + (Vector3.back + Vector3.right) * offsetTestAmount,  floorBp.RoomBlueprints[i].PerimeterWalls.ToArray<Vector3>()) == true)
				{
					isTwoBackRightEmpty = false;
					break;
				}
			}
					
			
			// If the point is nowhere near the wall, then return a zero vector
			if(isOneForwardLeftEmpty && isOneForwardRightEmpty && isOneBackLeftEmpty && isOneBackRightEmpty 
			   && isTwoForwardLeftEmpty && isTwoForwardRightEmpty && isTwoBackLeftEmpty && isTwoBackRightEmpty)
				return Vector3.zero;
			
			if(p1.x == p2.x)
			{
				if(p1.z < p2.z)
				{
					if(isOneForwardRightEmpty == isTwoBackRightEmpty && isOneForwardRightEmpty == true)
						offset = Vector3.left;
				}
				if(p1.z > p2.z)
				{
					if(isOneBackRightEmpty == isTwoForwardRightEmpty && isOneBackRightEmpty == true)
						offset = Vector3.left;
				}
				
				// backward facing items
				
				if(p1.z < p2.z)
				{
					if(isOneForwardLeftEmpty == isTwoBackLeftEmpty && isOneForwardLeftEmpty == true)
						offset = Vector3.right;
				}
				if(p1.z > p2.z)
				{
					if(isOneBackLeftEmpty == isTwoForwardLeftEmpty && isOneBackLeftEmpty == true)
						offset = Vector3.right;
				}
			}
			
			if(p1.z == p2.z)
			{
				// forward facing items
				if(p1.x < p2.x)
				{
					if(isOneForwardRightEmpty == isTwoForwardLeftEmpty && isOneForwardRightEmpty == true)
						offset = Vector3.back;
				}
				if(p1.x > p2.x)
				{
					if(isOneForwardLeftEmpty == isTwoForwardRightEmpty && isTwoForwardRightEmpty == true)
						offset = Vector3.back;
				}
				// backward facing items
				
				if(p1.x < p2.x)
				{
					if(isOneBackRightEmpty == isTwoBackLeftEmpty && isOneBackRightEmpty == true)
						offset = Vector3.forward;
				}
				if(p1.x > p2.x)
				{
					if(isOneBackLeftEmpty == isTwoBackRightEmpty && isOneBackLeftEmpty == true)
						offset = Vector3.forward;
				}
			}
			return offset;

		}

		# region Point versus Walls Testers

		/// <summary>
		/// Finds if a point is within a bounded polygon on the x z axis (does not account for y)
		/// </summary>
		/// <returns><c>true</c>, if point is within x z bounds of polys, <c>false</c> otherwise.</returns>
		public static bool PointInPolygonXZ(Vector3 point, Vector3[] polyPoints) 
		{
			if(polyPoints == null)
				return false;

			int polyCorners = polyPoints.Length;
			
			int i;
			int j = polyCorners - 1;
			bool oddNodes = false;
			
			for (i = 0; i < polyCorners; i++) 
			{
				if ((polyPoints[i].z < point.z && polyPoints[j].z >= point.z
				     ||   polyPoints[j].z < point.z && polyPoints[i].z >= point.z)
				    &&  (polyPoints[i].x <= point.x || polyPoints[j].x <= point.x)) 
				{
					if (polyPoints[i].x + (point.z - polyPoints[i].z) 
					    / (polyPoints[j].z - polyPoints[i].z) 
					    * (polyPoints[j].x - polyPoints[i].x ) < point.x) 
					{
						oddNodes = !oddNodes; 
					}
				}
				j=i; 
			}
			
			return oddNodes; 
		}

		public static bool PointOnlyInPolygonXZ(Vector3 point, Vector3[] polyPoints) 
		{
			if(polyPoints == null)
				return false;

			// First find if the point is on any of the walls
			for(int i = 0; i < polyPoints.Length - 1; i++)
			{
				if(IsPointAlongLineXZ(point, polyPoints[i], polyPoints[i + 1]))
					return false;
			}

			// If the point is NOT on the walls, then test for the point inside the walls
			return PointInPolygonXZ(point, polyPoints); 
		}

		public static bool PointInOrOnPolygonXZ(Vector3 point, Vector3[] polyPoints) 
		{
			if(polyPoints == null)
				return false;

			// First find if the point is on any of the walls
			for(int i = 0; i < polyPoints.Length - 1; i++)
			{
				if(IsPointAlongLineXZ(point, polyPoints[i], polyPoints[i + 1]))
					return true;
			}

			// If the point is NOT on the walls, then test for the point inside the walls
			return PointInPolygonXZ(point, polyPoints); 
		}

		public static bool PointOnPolygonXZ(Vector3 point, Vector3[] polyPoints, float epsilon = 0.0001f) 
		{
			if(polyPoints == null)
				return false;

			// First find if the point is on any of the walls
			for(int i = 0; i < polyPoints.Length - 1; i++)
			{
				if(IsPointAlongLineXZ(point, polyPoints[i], polyPoints[i + 1], epsilon))
					return true;
			}

			// If the point is NOT on the walls, then test for the point inside the walls
			return false;
		}

		public static bool IsPointAlongLineXZ(Vector3 point, Vector3 start, Vector3 end, float epsilon = 0.0001f)
		{
			// TODO - Use math instead of creating planes
			point.y = 0; start.y = 0; end.y = 0;

			if((point - start).sqrMagnitude < epsilon)
				return true;

			if((point - end).sqrMagnitude < epsilon)
				return true;

			// Calculates if a segment runs into the end of another segmenet // MAY BE EXPENSIVE TO CALCULATE
			Plane closePlane = new Plane(Vector3.Cross((end - start).normalized, Vector3.down), start);

			float distanceToPlane = closePlane.GetDistanceToPoint(point);

			if(distanceToPlane < epsilon && distanceToPlane > -1 * epsilon)
			{
				// Now make sure the point is between the two points
				Plane planeStart = new Plane((end - start).normalized, start);
				Plane planeEnd = new Plane((start - end).normalized, end);

				if(planeStart.GetSide(point) && planeEnd.GetSide(point))
					return true;
			}

			return false;
		}

		public static bool ArePointsCloseEnough(Vector3 point1, Vector3 point2, float epsilon = 0.000001f)
		{
			return (point1 - point2).sqrMagnitude < epsilon;
		}

		/// <summary>
		/// Is the LINE entirely along the other line
		/// </summary>
		/// <returns><c>true</c>, if encompases was lined, <c>false</c> otherwise.</returns>
		public static bool LineEncompases(Vector3 lineStart, Vector3 lineEnd, Vector3 lineStartEncompase,  Vector3 lineEndEncompase)
		{
			return IsPointAlongLineXZ(lineStart, lineStartEncompase, lineEndEncompase) && IsPointAlongLineXZ(lineEnd, lineStartEncompase, lineEndEncompase);
				
		}

		/// <summary>
		/// DEPRECIATED: Determines if a point is along a wall or inside the wall bounds, uses Ordinal for along walls
		/// </summary>
		/// <returns><c>true</c> if testPoint is inside the wall OR along the wall; otherwise, <c>false</c>.</returns>
		public static bool IsPointInARoom(Vector3 testPoint, Vector3[] walls)
		{
			for(int i = 0; i < walls.Length; i++)
			{
				int nextPoint = i + 1;
				if(nextPoint >= walls.Length - 1)
					nextPoint = 0;
				
				if(testPoint.x == walls[i].x && testPoint.z == walls[i].z)
					return true;
				
				if(BCUtils.TestBetweenTwoPoints(testPoint, walls[i], walls[nextPoint]))
					return true;
			}
			
			if(BCUtils.PointInPolygonXZ(testPoint, walls.ToArray()))
			{
				return true;
			}

			return false;
		}

		public static bool IsPointInARoom(Vector3 testPoint, WallInformation[] walls)
		{
			if(walls == null)
				return false;

			if(IsPointOnAWall(testPoint, walls))
				return true;

			int polyCorners = walls.Length;

			int i;
			int j = polyCorners;
			bool oddNodes = false;

			for (i = 0; i < polyCorners + 1; i++) 
			{
				Vector3 jWall = Vector3.zero;
				Vector3 iWall = Vector3.zero;
				if(i < polyCorners)
					iWall = walls[i].Start;
				else
					iWall = walls[polyCorners - 1].End;

				if(j < polyCorners)
					jWall = walls[j].Start;
				else
					jWall = walls[polyCorners - 1].End;

				if ((iWall.z < testPoint.z && jWall.z >= testPoint.z || jWall.z < testPoint.z && iWall.z >= testPoint.z) 
					&&  (iWall.x <= testPoint.x || jWall.x <= testPoint.x)) 
				{
					if (iWall.x + (testPoint.z -iWall.z) / (jWall.z - iWall.z) 
						* (jWall.x - iWall.x ) < testPoint.x) 
					{
						oddNodes = !oddNodes; 
					}
				}
				j = i; 
			}

			return oddNodes; 
		}

		public static bool IsPointOnAWall(Vector3 testPoint, WallInformation[] walls)
		{
			testPoint.y = 0;

			for(int i = 0; i < walls.Length; i++)
			{
				Vector3 direction = walls[i].End - walls[i].Start;
				direction.Normalize();

				float firstDot = Vector3.Dot((testPoint - walls[i].Start).normalized, direction);
				float secondDot = Vector3.Dot((testPoint - walls[i].End).normalized, direction);

				if(firstDot == 1 && secondDot == -1)
					return true;
				else if(firstDot == -1 && secondDot == 1)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Determines if a point is along a wall or inside the wall bounds
		/// </summary>
		/// <returns><c>true</c> if testPoint is inside the wall OR along the wall; otherwise, <c>false</c>.</returns>
		public static bool IsPointInARoom(Vector3 testPoint, List<Vector3> walls)
		{
			return IsPointInARoom(testPoint, walls.ToArray<Vector3>());
		}

		/// <summary>
		/// Determines if a point is along a wall or inside the wall bounds
		/// </summary>
		/// <returns><c>true</c> if testPoint is inside the wall OR along the wall; otherwise, <c>false</c>.</returns>
		public static bool IsPointInARoom(Vector3 testPoint, RoomBlueprint roomBp)
		{
			return IsPointInARoom(testPoint, roomBp.PerimeterWalls.ToArray<Vector3>());
		}

		public static bool IsPointOnlyInsideARoom(Vector3 testPoint, RoomBlueprint roomBp)
		{
			return IsPointOnlyInsideARoom(testPoint, roomBp.PerimeterWalls);
		}

		public static bool IsPointOnlyInsideARoom(Vector3 testPoint, List<Vector3> walls)
		{
			return IsPointOnlyInsideARoom(testPoint, walls.ToArray<Vector3>());
		}

		public static bool IsPointOnlyInsideARoom(Vector3 testPoint, Vector3[] walls)
		{
			for(int i = 0; i < walls.Length; i++)
			{
				int nextPoint = i + 1;
				if(nextPoint >= walls.Length - 1)
					nextPoint = 0;
				
				if(testPoint.x == walls[i].x && testPoint.z == walls[i].z)
					return false;
				
				if(BCUtils.TestBetweenTwoPoints(testPoint, walls[i], walls[nextPoint]))
					return false;
			}
			
			if(BCUtils.PointInPolygonXZ(testPoint, walls.ToArray()))
			{
				return true;
			}
			
			return false;
		}

		/// <summary>
		/// Tests to see if a point is in ANY room on a floor
		/// </summary>
		/// <returns><c>true</c> if is point only inside any room the specified testPoint in a floorBp; otherwise, <c>false</c>.</returns>
		public static bool IsPointOnlyInsideAnyRoom(Vector3 testPoint, FloorBlueprint floorBp)
		{
			if(floorBp == null)
				return false;

			for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
			{
				if(IsPointOnlyInsideARoom(testPoint, floorBp.RoomBlueprints[i]))
					return true;
			}
			return false;
		}

		/// <summary>
		/// DEPRECIATED: Determines if is polygon entirely inside other polygone X the specified outsidePoly insidePoly.
		/// </summary>
		/// <param name="outsidePoly">Poly that should surround other poly</param>
		/// <param name="insidePoly">Poly that should be surrounded</param>
		public static bool IsPolygonEntirelyInsideOtherPolygoneXZ(Vector3[] insidePoly, Vector3[] outsidePoly)
		{
			// confirms that the polygones are good
			if(BCValidator.IsVector3ArrayValid(outsidePoly) == false 
			   || BCValidator.IsVector3ArrayValid(insidePoly) == false)
				return false;

			// Checks if the first point is outside
			if(BCUtils.IsPointInARoom(insidePoly[0], outsidePoly) == false)
				return false;

			// for each line section, check if it intersects with any point in the outside poly
			// if it does, return false
			for(int i = 0; i < insidePoly.Length - 1; i++)
			{
				for(int j = 0; j < outsidePoly.Length - 1; j++)
				{
					bool segementsIntersect = LineSegmentsIntersectXZ(insidePoly[i], insidePoly[i + 1], outsidePoly[j], outsidePoly[j + 1]);

					if(segementsIntersect)
						return false;
				}
			}

			return true;
		}

		/// <summary>
		/// DEPRECIATED: Only Ordinal
		/// </summary>
		public static bool IsPointAlongAWall(Vector3 testPoint, RoomBlueprint roomBp)
		{
			return IsPointAlongAWall(testPoint, roomBp.PerimeterWalls);
		}

		/// <summary>
		/// DEPRECIATED: Only Ordinal
		/// </summary>
		public static bool IsPointAlongAWall(Vector3 testPoint, List<Vector3> walls)
		{
			return IsPointAlongAWall(testPoint, walls.ToArray<Vector3>());
		}

		/// <summary>
		/// DEPRECIATED: Only Ordinal
		/// </summary>
		public static bool IsPointAlongAWall(Vector3 testPoint, Vector3[] walls)
		{
			for(int i = 0; i < walls.Length; i++)
			{
				int nextPoint = i + 1;
				if(nextPoint >= walls.Length - 1)
					nextPoint = 0;
				
				if(BCUtils.TestBetweenTwoPoints(testPoint, walls[i], walls[nextPoint]))
					return true;
				
			}
			
			return false;
		}

		#endregion

		public static List<Vector3[]> GetSplitLineFromWall(Vector3 lineStart, Vector3 lineEnd, List<Vector3> wall)
		{
			// We need the wall in question, p1, p2
			// We need the wall being tested against

			List<Vector3[]> lineGroup = new List<Vector3[]>();

			// scratch line to add to the line group as we find starts and ended
			Vector3[] smallLine = new Vector3[2];

			Vector3 diff = lineEnd - lineStart;
			Vector3 direction = diff.normalized;
			float length = diff.magnitude;

			bool currentLineIsOverlapping = false;

			// Test for overlap of the first point in the line. If it is overlapping, then we make sure to note this and then it
			// allows the system to go along the current line until we find it
			if(BCUtils.IsPointAlongAWall(lineStart, wall) == true)
			{
				currentLineIsOverlapping = true;
			}
			else
				smallLine[0] = lineStart;

			for(float f = 0.5f; f <= length; f += 0.5f)
			{
				Vector3 testPoint = lineStart + f * direction;

				// When a point is found to collide and it previously wasn't, then we end the line and add it to the Vector3 List
				if(BCUtils.IsPointInARoom(testPoint, wall) == true && currentLineIsOverlapping == false)
				{
					smallLine[1] = testPoint;
					lineGroup.Add(smallLine.ToArray<Vector3>());
					currentLineIsOverlapping = true;
					continue;
				}

				if(f == length && currentLineIsOverlapping == false) // If we end on a corner with no problems, then close off the loop
				{
					smallLine[1] = testPoint;
					lineGroup.Add(smallLine.ToArray<Vector3>());
					break;
				}

				// This is checking to see if there is no longer a point, if it finds that suddenly it is open,
				// Then it starts up the overlapping
				if(BCUtils.IsPointInARoom(testPoint, wall) == false && currentLineIsOverlapping == true)
				{
					smallLine[0] = testPoint - 0.5f * direction; // Adds the last point where the overlapping was working
					currentLineIsOverlapping = false;
				}
			}

			int index = 0;

			// remove any short lines (less than .9f)
			while(index < 1000)
			{
				if(index > lineGroup.Count - 1)
					break;

				if((lineGroup[index][0] - lineGroup[index][1]).magnitude < 0.9f)
				{
					lineGroup.RemoveAt(index);
					continue;
				}

				index++;

			}


			return lineGroup;
		}

		public static List<Vector3[]> GetAllPoints(List<Vector3> wall, List<Vector3> overlappingWall)
		{
			List<Vector3[]> newWallPoints = new List<Vector3[]>();

			// HACK The top floor needs to be delivered back as a list of points in pairs
			if(overlappingWall == null)
			{
				for(int i = 0; i < wall.Count - 1; i++)
				{
					newWallPoints.Add(new Vector3[2] { wall[i], wall[i + 1] } );
				}
				return newWallPoints;
			}

			for(int i = 0; i < wall.Count - 1; i++)
			{
				newWallPoints.AddRange(GetSplitLineFromWall(wall[i], wall[i + 1], overlappingWall));
			}

			return newWallPoints;
		}

		public static FloorBlueprint DeepCopyFloor(FloorBlueprint floorBp)
		{
			FloorBlueprint copiedFloor = new FloorBlueprint();

			copiedFloor.Doors = floorBp.Doors.ToList();
			copiedFloor.Windows = floorBp.Windows.ToList();
			copiedFloor.Stairs = floorBp.Stairs.ToList();

			List<RoomBlueprint> newRooms = new List<RoomBlueprint>();

			for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
			{
				// Does not copy empty perimeter walls
				if(floorBp.RoomBlueprints[i].PerimeterWalls == null)
					continue;

				RoomBlueprint newRoom = new RoomBlueprint();
				newRoom.CeilingHeight = floorBp.RoomBlueprints[i].CeilingHeight;
				newRoom.RoomType = floorBp.RoomBlueprints[i].RoomType;
				newRoom.SetPerimeterWalls(floorBp.RoomBlueprints[i].PerimeterWalls.ToList<Vector3>());
				newRoom.OverrideRoomStyle = floorBp.RoomBlueprints[i].OverrideRoomStyle;
				newRooms.Add(newRoom);
			}

			// NOTE: Does not copy the Yard Layout
			copiedFloor.RoomBlueprints.AddRange(newRooms);

			return copiedFloor;
		}

		/// <summary>
		/// Gets the offsets for the party walls. NOTE: You must feed information into this in a decreasing manner to have it work
		/// </summary>
		/// <param name="buildingBp">Building bp.</param>
		/// <param name="floorBp">Floor bp.</param>
		/// <param name="floorOutline">Floor outline.</param>
		/// <param name="pointIndex">Point index.</param>
		/// <param name="firstPartyWallOffset">First party wall offset.</param>
		/// <param name="secondPartyWallOffset">Second party wall offset.</param>
		public static void GetPartyWallOffset(BuildingBlueprint buildingBp, FloorBlueprint floorBp, Vector3[] floorOutline, int pointIndex, out Vector3 firstPartyWallOffset, out Vector3 secondPartyWallOffset)
		{
			firstPartyWallOffset = Vector3.zero;
			secondPartyWallOffset = Vector3.zero;
			
			int nextPoint = pointIndex - 1;
			int nextNextPoint = pointIndex - 2;

			if(nextPoint == -1)
				nextPoint = floorOutline.Length - 2;
			
			// Handles loopover cases
			if(nextPoint == 0 && nextNextPoint == -1)
				nextNextPoint = floorOutline.Length - 2;

			if(nextNextPoint == -2)
				nextNextPoint = floorOutline.Length - 3;
			
			// Handles loopover cases
			int prevPoint = pointIndex + 1;
			if(prevPoint == floorOutline.Length - 1)
				prevPoint = 0;
			if(prevPoint == floorOutline.Length)
				prevPoint = 1;
			
			// ================== Deals with the party walls along the Z direction ==================
			if(buildingBp.ZPartyWalls.Contains(floorOutline[pointIndex].z) && buildingBp.ZPartyWalls.Contains(floorOutline[nextPoint].z))
			{
				Vector3 midPoint = (floorOutline[pointIndex] + floorOutline[nextPoint]) / 2;
				Vector3 partyWallOffset = BCUtils.GetOutsetFromManyRooms(midPoint, floorBp);
				
				firstPartyWallOffset = partyWallOffset;
				secondPartyWallOffset = partyWallOffset;
			}

			if(buildingBp.ZPartyWalls.Contains(floorOutline[pointIndex].z) && buildingBp.ZPartyWalls.Contains(floorOutline[nextPoint].z) == false)
			{
				Vector3 midPoint = (floorOutline[prevPoint] + floorOutline[pointIndex]) / 2;
				firstPartyWallOffset = BCUtils.GetOutsetFromManyRooms(midPoint, floorBp);
			}

			if(buildingBp.ZPartyWalls.Contains(floorOutline[pointIndex].z) == false && buildingBp.ZPartyWalls.Contains(floorOutline[nextPoint].z))
			{
				Vector3 midPoint = (floorOutline[nextPoint] + floorOutline[nextNextPoint]) / 2;
				secondPartyWallOffset = BCUtils.GetOutsetFromManyRooms(midPoint, floorBp);
			}
			
			// ================== End Z party Walls
			
			// ================== Deals with the party walls along the X direction ==================
			if(buildingBp.XPartyWalls.Contains(floorOutline[pointIndex].x) && buildingBp.XPartyWalls.Contains(floorOutline[nextPoint].x))
			{
				Vector3 partyWallOffset = BCUtils.GetOutsetFromManyRooms((floorOutline[pointIndex] + floorOutline[nextPoint]) / 2, floorBp);
				
				firstPartyWallOffset = partyWallOffset;
				secondPartyWallOffset = partyWallOffset;
			}
			
			if(buildingBp.XPartyWalls.Contains(floorOutline[pointIndex].x) && buildingBp.XPartyWalls.Contains(floorOutline[nextPoint].x) == false
			   && floorOutline[pointIndex].x == floorOutline[prevPoint].x)
			{
				Vector3 midPoint = (floorOutline[prevPoint] + floorOutline[pointIndex]) / 2;
				firstPartyWallOffset = BCUtils.GetOutsetFromManyRooms(midPoint,floorBp);
				
			}
			
			if(buildingBp.XPartyWalls.Contains(floorOutline[pointIndex].x) == false && buildingBp.XPartyWalls.Contains(floorOutline[nextPoint].x))
			{
				Vector3 midPoint = (floorOutline[nextPoint] + floorOutline[nextNextPoint]) / 2;
				secondPartyWallOffset = BCUtils.GetOutsetFromManyRooms(midPoint, floorBp);
				
			}
			
			// ================== End X party Walls ==========================
			
			// Deal with crazy corners
			if(buildingBp.XPartyWalls.Contains(floorOutline[pointIndex].x) && buildingBp.ZPartyWalls.Contains(floorOutline[pointIndex].z))
			{
				firstPartyWallOffset = BCUtils.GetOutsetFromManyRooms(floorOutline[pointIndex], floorBp);
				
				Vector3 midPoint = (floorOutline[pointIndex] + floorOutline[nextPoint]) / 2;
				secondPartyWallOffset = BCUtils.GetOutsetFromManyRooms(midPoint, floorBp);
				
			}

			// No clue why this works, but it does - KBH
			if(buildingBp.XPartyWalls.Contains(floorOutline[nextPoint].x) && buildingBp.ZPartyWalls.Contains(floorOutline[nextPoint].z))
			{
				secondPartyWallOffset = BCUtils.GetOutsetFromManyRooms(floorOutline[nextPoint], floorBp);
				
			}
			
			// ================ FINALLY IF A WALL SPANS FROM ONE PARTY WALL TO ANOTHER ======================
			//                        and the other side is a party wall too

			// Used when we have a wall from party wall to party wall
			if(buildingBp.XPartyWalls.Contains(floorOutline[pointIndex].x) && buildingBp.XPartyWalls.Contains(floorOutline[nextPoint].x))
			{
				if(floorOutline[pointIndex].x != floorOutline[nextPoint].x)
				{
					if(floorOutline[pointIndex].z == floorOutline[nextPoint].z && buildingBp.ZPartyWalls.Contains(floorOutline[pointIndex].z) == false)
					{
						Vector3 direction = (floorOutline[pointIndex] - floorOutline[nextPoint]).normalized;
						firstPartyWallOffset = direction;
						secondPartyWallOffset = -direction;
					}
				}
			}

			if(buildingBp.ZPartyWalls.Contains(floorOutline[pointIndex].z) && buildingBp.ZPartyWalls.Contains(floorOutline[nextPoint].z))
			{
				if(floorOutline[pointIndex].z != floorOutline[nextPoint].z)
				{
					if(floorOutline[pointIndex].x == floorOutline[nextPoint].x && buildingBp.XPartyWalls.Contains(floorOutline[pointIndex].x) == false)
					{
						Vector3 direction = (floorOutline[pointIndex] - floorOutline[nextPoint]).normalized;
						firstPartyWallOffset = direction;
						secondPartyWallOffset = -direction;
					}
				}
			}
		}


//		public static bool IsPartyWallOffset(BuildingBlueprint buildingBp, FloorBlueprint floorBp, WallInformation wallInfo)
//		{
//			Vector3 startPoint = wallInfo.Start;
//			Vector3 endPoint = wallInfo.End;
//
//			if(buildingBp.ZPartyWalls.Contains(startPoint.z) && buildingBp.ZPartyWalls.Contains(endPoint.z))
//			{
//				return true;
//			}
//
//			if(buildingBp.XPartyWalls.Contains(startPoint.x) && buildingBp.XPartyWalls.Contains(endPoint.x))
//			{
//				return true;
//			}
//				
//			return false;
//		}

		/// <summary>
		/// DEPRECIATED
		/// Gets the offsets for the party walls. NOTE: You must feed information into this in a decreasing manner to have it work
		/// </summary>
//		public static bool IsPartyWallOffset(BuildingBlueprint buildingBp, FloorBlueprint floorBp, Vector3[] floorOutline, int pointIndex)
//		{
//			int nextPoint = pointIndex - 1;
//			int nextNextPoint = pointIndex - 2;
//
//			if(nextPoint == -1)
//				nextPoint = floorOutline.Length - 2;
//
//			// Handles loopover cases
//			if(nextPoint == 0 && nextNextPoint == -1)
//				nextNextPoint = floorOutline.Length - 2;
//
//			if(nextNextPoint == -2)
//				nextNextPoint = floorOutline.Length - 3;
//
//			// Handles loopover cases
//			int prevPoint = pointIndex + 1;
//			if(prevPoint == floorOutline.Length - 1)
//				prevPoint = 0;
//			if(prevPoint == floorOutline.Length)
//				prevPoint = 1;
//
//			// ================== Deals with the party walls along the Z direction ==================
//			if(buildingBp.ZPartyWalls.Contains(floorOutline[pointIndex].z) && buildingBp.ZPartyWalls.Contains(floorOutline[nextPoint].z))
//			{
//				return true;
//			}
//
//			if(buildingBp.ZPartyWalls.Contains(floorOutline[pointIndex].z) && buildingBp.ZPartyWalls.Contains(floorOutline[nextPoint].z) == false)
//			{
//				return true;
//			}
//
//			if(buildingBp.ZPartyWalls.Contains(floorOutline[pointIndex].z) == false && buildingBp.ZPartyWalls.Contains(floorOutline[nextPoint].z))
//			{
//				return true;
//			}
//
//			// ================== End Z party Walls
//
//			// ================== Deals with the party walls along the X direction ==================
//			if(buildingBp.XPartyWalls.Contains(floorOutline[pointIndex].x) && buildingBp.XPartyWalls.Contains(floorOutline[nextPoint].x))
//			{
//				return true;
//			}
//
//			if(buildingBp.XPartyWalls.Contains(floorOutline[pointIndex].x) && buildingBp.XPartyWalls.Contains(floorOutline[nextPoint].x) == false
//				&& floorOutline[pointIndex].x == floorOutline[prevPoint].x)
//			{
//				return true;
//			}
//
//			if(buildingBp.XPartyWalls.Contains(floorOutline[pointIndex].x) == false && buildingBp.XPartyWalls.Contains(floorOutline[nextPoint].x))
//			{
//				return true;
//			}
//
//			// ================== End X party Walls ==========================
//
//			// Deal with crazy corners
//			if(buildingBp.XPartyWalls.Contains(floorOutline[pointIndex].x) && buildingBp.ZPartyWalls.Contains(floorOutline[pointIndex].z))
//			{
//				return true;
//
//			}
//
//			// No clue why this works, but it does - KBH
//			if(buildingBp.XPartyWalls.Contains(floorOutline[nextPoint].x) && buildingBp.ZPartyWalls.Contains(floorOutline[nextPoint].z))
//			{
//				return true;
//			}
//
//			// ================ FINALLY IF A WALL SPANS FROM ONE PARTY WALL TO ANOTHER ======================
//			//                        and the other side is a party wall too
//
//			// Used when we have a wall from party wall to party wall
//			if(buildingBp.XPartyWalls.Contains(floorOutline[pointIndex].x) && buildingBp.XPartyWalls.Contains(floorOutline[nextPoint].x))
//			{
//				if(floorOutline[pointIndex].x != floorOutline[nextPoint].x)
//				{
//					if(floorOutline[pointIndex].z == floorOutline[nextPoint].z && buildingBp.ZPartyWalls.Contains(floorOutline[pointIndex].z) == false)
//					{
//						return true;
//					}
//				}
//			}
//
//			if(buildingBp.ZPartyWalls.Contains(floorOutline[pointIndex].z) && buildingBp.ZPartyWalls.Contains(floorOutline[nextPoint].z))
//			{
//				if(floorOutline[pointIndex].z != floorOutline[nextPoint].z)
//				{
//					if(floorOutline[pointIndex].x == floorOutline[nextPoint].x && buildingBp.XPartyWalls.Contains(floorOutline[pointIndex].x) == false)
//					{
//						return true;
//					}
//				}
//			}
//
//			return false;
//		}

		/// <summary>
		/// Returns the point of supplied index, even if the wall loops
		/// </summary>
		/// <returns>The actual index in case wall has looped</returns>
//		public static int GetPointInWall(int indexNeeded, Vector3[] points)
//		{
//			int garbageInt = -1;
//			int newIndex = -1;
//
//			GetNextPointsInWall(indexNeeded, points, out newIndex, out garbageInt);
//
//			return newIndex;
//		}

		/// <summary>
		/// Spits out the new start index and new next index for the point in question
		/// </summary>
		/// <param name="index">Index.</param>
		/// <param name="points">Points.</param>
		/// <param name="newStart">New start.</param>
		/// <param name="newNext">New next.</param>
		public static void GetNextPointsInWall(int index, Vector3[] points, out int newThis, out int newNext)
		{
			newThis = index;
			newNext = index + 1;

			if(index == points.Length - 1) // When the start is right at the end
			{
				newThis = 0;
				newNext = 1;
			}
			else if(index >= points.Length - 1) // If the start goes past, need the right numbers
			{
				newThis = index - points.Length + 1;
				newNext = index + 1 - points.Length + 1;
			}
		}



		/// <summary>
		/// Assumes that last and first point match
		/// </summary>
		/// <param name="index">Index.</param>
		/// <param name="points">Points.</param>
		/// <param name="newStart">New start.</param>
		/// <param name="newPrev">New previous.</param>
		public static void GetPreviousPointsInWall(int index, Vector3[] points, out int newStart, out int newPrev)
		{
			newStart = index;
			newPrev = index - 1;

			if(index == 0) // When the start is right at the end
			{
				newStart = 0;
				newPrev = points.Length - 2;
			}
			else if(index >= points.Length - 1)
			{
				newStart = index - points.Length + 1;
				newPrev = index - 1 - points.Length + 1;
			}

			if(newPrev == -1)
			{
				newPrev = points.Length - 2;
			}
		}

		/// <summary>
		/// Recursively gets children from a parent objects. Similar to .GetComponentsInChildren but works in the editor
		/// </summary>
		/// <returns>The children of the parent with the parent added to it.</returns>
		/// <param name="parent">Parent.</param>
		public static List<GameObject> GetChildren(GameObject parent)
		{
			List<GameObject> children = new List<GameObject>();
			
			int childNumber = parent.transform.childCount;
			
			children.Add(parent);
			
			for(int i = 0; i < childNumber; i++)
			{
				children.AddRange(GetChildren(parent.transform.GetChild(i).gameObject));
			}
			
			return children;
		}

		/// <summary>
		/// Will only return true if the direction of the intersection is in the positive direction of the point
		/// </summary>
//		public static bool IntersectBetweenTwoPoints(Vector3 pointStart1, Vector3 p1direction, Vector3 pointStart2, Vector3 p2direction, out Vector3 intersectionPoint)
//		{
//			intersectionPoint = Vector3.zero;
//			p1direction.Normalize();
//			p2direction.Normalize();
//			
//			Vector3 p1s = new Vector3(pointStart1.x, 0, pointStart1.z);
//			Vector3 p1e = new Vector3((pointStart1 + p1direction).x, 0, (pointStart1 + p1direction).z);
//			Vector3 p2s = new Vector3(pointStart2.x, 0, pointStart2.z);
//			Vector3 p2e = new Vector3((pointStart2 + p2direction).x, 0, (pointStart2 + p2direction).z);
//
//			// Get A,B,C of first line - points : ps1 to pe1
//			float A1 = p1e.z-p1s.z;
//			float B1 = p1s.x-p1e.x;
//			float C1 = A1*p1s.x+B1*p1s.z;
//			
//			// Get A,B,C of second line - points : ps2 to pe2
//			float A2 = p2e.z-p2s.z;
//			float B2 = p2s.x-p2e.x;
//			float C2 = A2*p2s.x+B2*p2s.z;
//			
//			// Get delta and check if the lines are parallel
//			float delta = A1*B2 - A2*B1;
//			
//			if(delta == 0 || delta < 0.0001 && delta > -0.0001) // Ensures that almost zero deltas return false
//				return false;
//			
//			// now return the Vector2 intersection point
//			intersectionPoint = new Vector3((B2*C1 - B1*C2)/delta, 0, (A1*C2 - A2*C1)/delta);
//
//			// Checks to see if the intersection is not in the direction of the starting point
//			Vector3 difference = (intersectionPoint - p1s).normalized;
//			if(difference != (p1e - p1s).normalized)
//				return false;
//
//			Vector3 endDifference = (intersectionPoint - p2s).normalized;
//			if(endDifference != (p2e - p2s).normalized)
//				return false;
//
//			return true;
//		}

		
		public static bool FindIntersectOfTwoInfinityLinesXZ(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, out Vector3 intersection)
		{
			bool interesects = false;

			bool junkBool;
			Vector3 junkPoint;
			
			FindIntersectionOfTwoLinesXZ(p1, p2, p3, p4, out interesects, out junkBool, out intersection, out junkPoint, out junkPoint);
			
			return interesects;
		}

		public static bool FindIntersectionOfTwoLinesXZ(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, out Vector3 segementIntersection)
		{
			bool segementsIntersect = false;
			
			bool junkBool;
			Vector3 junkPoint;
			
			FindIntersectionOfTwoLinesXZ(p1, p2, p3, p4, out junkBool, out segementsIntersect, out segementIntersection, out junkPoint, out junkPoint);
			
			return segementsIntersect;
		}

		public static bool LineSegmentsIntersectXZ(Vector3 p1Start, Vector3 p1End, Vector3 p2Start, Vector3 p2End)
		{
			Vector3 junkPoint;

			return FindIntersectionOfTwoLinesXZ(p1Start, p1End, p2Start, p2End, out junkPoint);
		}
		
		public static void FindIntersectionOfTwoLinesXZ(
			Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4,
			out bool lines_intersect, out bool segments_intersect,
			out Vector3 intersection,
			out Vector3 close_p1, out Vector3 close_p2)
		{
			// Finds the average y value for the return
			float YAverage = (p1.y + p2.y + p3.y + p4.y) / 4;
			
			// Get the segments' parameters.
			float dx12 = p2.x - p1.x;
			float dz12 = p2.z - p1.z;
			float dx34 = p4.x - p3.x;
			float dz34 = p4.z - p3.z;
			
			// Solve for t1 and t2
			float denominator = (dz12 * dx34 - dx12 * dz34);
			
			float t1 =
				((p1.x - p3.x) * dz34 + (p3.z - p1.z) * dx34)
					/ denominator;
			if (float.IsInfinity(t1))
			{
				// The lines are parallel (or close enough to it).
				lines_intersect = false;
				segments_intersect = false;
				intersection = new Vector3(float.NaN, float.NaN, float.NaN);
				close_p1 = new Vector3(float.NaN, float.NaN, float.NaN);
				close_p2 = new Vector3(float.NaN, float.NaN, float.NaN);
				return;
			}
			lines_intersect = true;
			
			float t2 =
				((p3.x - p1.x) * dz12 + (p1.z - p3.z) * dx12) / - denominator;
			
			// Find the point of intersection.
			intersection = new Vector3(p1.x + dx12 * t1, YAverage, p1.z + dz12 * t1);
			
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
			
			close_p1 = new Vector3(p1.x + dx12 * t1, YAverage, p1.y + dz12 * t1);
			close_p2 = new Vector3(p3.x + dx34 * t2, YAverage, p3.y + dz34 * t2);
		}

//		public static void SegementsOverlapXZ(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, out bool segementsOverlap, out Vector3 overlapPoint)
//		{
//				// Get A,B,C of first line - points : ps1 to pe1
//			float A1 = p2.z-p1.z;
//			float B1 = p1.x-p2.x;
//			float C1 = A1*p1.z+B1*p2.z;
//				
//				// Get A,B,C of second line - points : ps2 to pe2
//			float A2 = p4.z-p3.z;
//			float B2 = p3.x-p4.x;
//			float C2 = A2*p3.x+B2*p4.z;
//				
//			// Get delta and check if the lines are parallel
//			float delta = A1*B2 - A2*B1;
//			if(delta == 0)
//			{
//				segementsOverlap = false;
//				overlapPoint = Vector3.zero;
//				return;
//			}
//				
//			segementsOverlap = true;
//			
//			// now return the Vector2 intersection point
//			overlapPoint = new Vector3(
//				(B2*C1 - B1*C2) / delta,
//				0,
//				(A1*C2 - A2*C1) / delta);
//
//		}

		# region RoofAngle calculators

		private static Vector3 GetRoofAngle(float angle, Vector3 direction, float distance)
		{
			float run = 1 - angle / 90;
			float rise = angle / 90;

			return Vector3.up * rise * distance + direction * run * distance;
		}

//		public static Vector3 GetRoofAngle(float angle, Vector3 direction)
//		{
//			return GetRoofAngle(angle, direction, 1).normalized;
//		}

		#endregion

		#region Blueprint Relocators
		// This section provides methods to completely move a blueprint from one location to another

		public static void UpdateBlueprintCentre(BuildingBlueprint buildingBp, Vector3 newReferencePoint)
		{
			// First we must compare the blueprint's current location to the old reference point. Get the offset X, Y, Z.
			Vector3 offset3D = buildingBp.BlueprintXZCenter - newReferencePoint;

			Vector3 updateOffset = new Vector3(offset3D.x, 0, offset3D.z); // NOTE: All floor points should ALWAYS be in just the XZ plane

			// Then we update all the points by the difference amount
			for(int i = 0; i < buildingBp.Floors.Count; i ++)
			{
				FloorBlueprint floor = buildingBp.Floors[i];

				for(int j = 0; j < floor.RoomBlueprints.Count; j++)
				{
					RoomBlueprint roomBp = floor.RoomBlueprints[j];

					for(int n = 0; n < roomBp.PerimeterWalls.Count; n++)
					{
						roomBp.PerimeterWalls[n] -= updateOffset;
					}
				}

				for(int j = 0; j < floor.Doors.Count; j++)
				{
					DoorInfo doorInfo = floor.Doors[j];

					doorInfo.Start -= updateOffset;
					doorInfo.End -= updateOffset;

					floor.Doors[j] = doorInfo;
				}

				for(int j = 0; j < floor.Windows.Count; j++)
				{
					WindowInfo windowInfo = floor.Windows[j];
					
					windowInfo.Start -= updateOffset;
					windowInfo.End -= updateOffset;
					floor.Windows[j] = windowInfo;
				}

				for(int j = 0; j < floor.Stairs.Count; j++)
				{
					StairInfo stairInfo = floor.Stairs[j];
					
					stairInfo.Start -= updateOffset;
					stairInfo.End -= updateOffset;

					floor.Stairs[j] = stairInfo;
				}

				for(int j = 0; j < floor.YardLayouts.Count; j++)
				{
					YardLayout yardInfo = floor.YardLayouts[j];

					for(int n = 0; n < yardInfo.PerimeterWalls.Count; n++)
					{
						yardInfo.PerimeterWalls[n] -= updateOffset;
					}
				}
			}

			for(int i = 0; i < buildingBp.RoofInfos.Count; i++)
			{
				RoofInfo roof = buildingBp.RoofInfos[i];

				roof.BackLeftCorner -= updateOffset;
				roof.FrontRightCorner -= updateOffset;
				roof.UpdateBaseOutline();

				buildingBp.RoofInfos[i] = roof;
			}

			buildingBp.BuildingRotation = buildingBp.transform.rotation;
		}

		public static void ShiftBlueprintCenter(BuildingBlueprint buildingBp, Vector3 originalPoint, Vector3 updatedPoint)
		{

			Vector3 updateOffset = originalPoint - updatedPoint;
			updateOffset = new Vector3(updateOffset.x, 0, updateOffset.z);

			bool nonZeroYStuff = false;

			// Then we update all the points by the difference amount
			for(int i = 0; i < buildingBp.Floors.Count; i ++)
			{
				FloorBlueprint floor = buildingBp.Floors[i];
				
				for(int j = 0; j < floor.RoomBlueprints.Count; j++)
				{
					RoomBlueprint roomBp = floor.RoomBlueprints[j];
					
					for(int n = 0; n < roomBp.PerimeterWalls.Count; n++)
					{
						roomBp.PerimeterWalls[n] -= updateOffset;

						if(roomBp.PerimeterWalls[n].y != 0) nonZeroYStuff = true;

					}
				}
				
				for(int j = 0; j < floor.Doors.Count; j++)
				{
					DoorInfo doorInfo = floor.Doors[j];
					
					doorInfo.Start -= updateOffset;
					doorInfo.End -= updateOffset;

					if(doorInfo.Start.y != 0) nonZeroYStuff = true;
					if(doorInfo.End.y != 0) nonZeroYStuff = true;

					floor.Doors[j] = doorInfo;
				}
				
				for(int j = 0; j < floor.Windows.Count; j++)
				{
					WindowInfo windowInfo = floor.Windows[j];
					
					windowInfo.Start -= updateOffset;
					windowInfo.End -= updateOffset;

					if(windowInfo.Start.y != 0) nonZeroYStuff = true;
					if(windowInfo.End.y != 0) nonZeroYStuff = true;

					floor.Windows[j] = windowInfo;
				}
				
				for(int j = 0; j < floor.Stairs.Count; j++)
				{
					StairInfo stairInfo = floor.Stairs[j];
					
					stairInfo.Start -= updateOffset;
					stairInfo.End -= updateOffset;

					if(stairInfo.Start.y != 0) nonZeroYStuff = true;
					if(stairInfo.End.y != 0) nonZeroYStuff = true;
					
					floor.Stairs[j] = stairInfo;
				}
				
				for(int j = 0; j < floor.YardLayouts.Count; j++)
				{
					YardLayout yardInfo = floor.YardLayouts[j];
					
					for(int n = 0; n < yardInfo.PerimeterWalls.Count; n++)
					{
						yardInfo.PerimeterWalls[n] -= updateOffset;

						if(yardInfo.PerimeterWalls[n].y != 0) nonZeroYStuff = true;
					}
				}
			}
			
			for(int i = 0; i < buildingBp.RoofInfos.Count; i++)
			{
				RoofInfo roof = buildingBp.RoofInfos[i];
				
				roof.BackLeftCorner -= updateOffset;
				roof.FrontRightCorner -= updateOffset;
				roof.UpdateBaseOutline();

				if(roof.BackLeftCorner.y != 0) nonZeroYStuff = true;
				if(roof.FrontRightCorner.y != 0) nonZeroYStuff = true;
				
				buildingBp.RoofInfos[i] = roof;
			}

			// All blueprint floorplans should always have y == 0. If they do not, reset the entire building to have y == 0
			if(nonZeroYStuff)
			{
				Debug.LogError("Blueprint " + buildingBp.name + " has a non 0 y height in a layout");
#if UNITY_EDITOR
				UnityEditor.Undo.RegisterCompleteObjectUndo(buildingBp, "Reset Y Heights on Blueprints");
#endif
				ResetBlueprintsToZeroY(buildingBp);
			}
		}

		/// <summary>
		/// Scans through all items in a building blueprint and sets the z to zero
		/// </summary>
		/// <param name="buildingBp">Building bp.</param>
		public static void ResetBlueprintsToZeroY(BuildingBlueprint buildingBp)
		{
			// Then we update all the points by the difference amount
			for(int i = 0; i < buildingBp.Floors.Count; i ++)
			{
				FloorBlueprint floor = buildingBp.Floors[i];
				
				for(int j = 0; j < floor.RoomBlueprints.Count; j++)
				{
					RoomBlueprint roomBp = floor.RoomBlueprints[j];
					
					for(int n = 0; n < roomBp.PerimeterWalls.Count; n++)
					{
						roomBp.PerimeterWalls[n] = new Vector3(roomBp.PerimeterWalls[n].x, 0, roomBp.PerimeterWalls[n].z);
					}
				}
				
				for(int j = 0; j < floor.Doors.Count; j++)
				{
					DoorInfo doorInfo = floor.Doors[j];
					
					doorInfo.Start = new Vector3(doorInfo.Start.x, 0, doorInfo.Start.z);
					doorInfo.End = new Vector3(doorInfo.End.x, 0, doorInfo.End.z);
					
					floor.Doors[j] = doorInfo;
				}
				
				for(int j = 0; j < floor.Windows.Count; j++)
				{
					WindowInfo windowInfo = floor.Windows[j];
					
					windowInfo.Start = new Vector3(windowInfo.Start.x, 0, windowInfo.Start.z);
					windowInfo.End = new Vector3(windowInfo.End.x, 0, windowInfo.End.z);

					floor.Windows[j] = windowInfo;
				}
				
				for(int j = 0; j < floor.Stairs.Count; j++)
				{
					StairInfo stairInfo = floor.Stairs[j];
					
					stairInfo.Start = new Vector3(stairInfo.Start.x, 0, stairInfo.Start.z);
					stairInfo.End = new Vector3(stairInfo.End.x, 0, stairInfo.End.z);

					floor.Stairs[j] = stairInfo;
				}
				
				for(int j = 0; j < floor.YardLayouts.Count; j++)
				{
					YardLayout yardInfo = floor.YardLayouts[j];
					
					for(int n = 0; n < yardInfo.PerimeterWalls.Count; n++)
					{
						yardInfo.PerimeterWalls[n] = new Vector3(yardInfo.PerimeterWalls[n].x, 0, yardInfo.PerimeterWalls[n].z);
					}
				}
			}
			
			for(int i = 0; i < buildingBp.RoofInfos.Count; i++)
			{
				RoofInfo roof = buildingBp.RoofInfos[i];
				
				roof.BackLeftCorner = new Vector3(roof.BackLeftCorner.x, 0, roof.BackLeftCorner.z);
				roof.FrontRightCorner = new Vector3(roof.FrontRightCorner.x, 0, roof.FrontRightCorner.z);
				roof.UpdateBaseOutline();

				buildingBp.RoofInfos[i] = roof;
			}
		}

		#endregion


		/// <summary>
		/// Finds all the points where the line given overlaps the walls
		/// </summary>
		/// <returns><c>true</c>, if segment overlap was found, <c>false</c> otherwise.</returns>
		/// <param name="line1Start">Line1 start.</param>
		/// <param name="line2Start">Line2 start.</param>
		/// <param name="walls">Walls.</param>
		public static List<Vector3> FindSegmentOverlap(Vector3 lineStart, Vector3 lineEnd, Vector3[] walls)
		{
			List<Vector3> overlappingVectors = new List<Vector3>();
			
			Vector3 lineDirection = (lineEnd - lineStart).normalized;
			
			// We want to find all points where segements cross
			for(int i = 0; i < walls.Length - 1; i++)
			{
				int startIndex = i;
				int nextIndex = i + 1;
				
	//			if(nextIndex >= walls.Length - 1)
	//				nextIndex = 0;
				
				Vector3 startPoint = walls[startIndex];
				Vector3 nextPoint = walls[nextIndex];
				
				bool linesCross = false;
				Vector3 closePoint = new Vector3();
				Vector3 otherClosePoint = new Vector3();
				
				bool segementsOverlap = false;
				Vector3 overlap = Vector3.zero;
				
				BCUtils.FindIntersectionOfTwoLinesXZ(
					lineStart, lineEnd, startPoint, nextPoint, out linesCross, out segementsOverlap, out overlap, out closePoint, out otherClosePoint);
				
				if(segementsOverlap == true)
				{
					overlappingVectors.Add(overlap);
				}
			}
			
			// Sort the lists so each line is in order in the direction of what the original line provided is
			
			if(lineDirection.z > 0)
				overlappingVectors = overlappingVectors.OrderBy(v => v.z).ToList<Vector3>();
			else
				overlappingVectors = overlappingVectors.OrderByDescending(v => v.z).ToList<Vector3>();
			
			if(lineDirection.x > 0)
				overlappingVectors = overlappingVectors.OrderBy(v => v.x).ToList<Vector3>();
			else
				overlappingVectors = overlappingVectors.OrderByDescending(v => v.x).ToList<Vector3>();
			
			return overlappingVectors;
		}

		public static bool FindSegmentOverlap(Vector3 lineStart, Vector3 lineEnd, Vector3[] walls, out List<Vector3> overlapPoints)
		{
			overlapPoints = FindSegmentOverlap(lineStart, lineEnd, walls);
			if(overlapPoints.Count < 1)
				return false;

			return true;
		}

		public static bool FindFirstSegementOverlap(Vector3 lineStart, Vector3 lineEnd, Vector3[] walls, out Vector3 result)
		{
			result = new Vector3(float.NaN, float.NaN, float.NaN);

			List<Vector3> lines = FindSegmentOverlap(lineStart, lineEnd, walls);

			// Round the results
	//		for(int i = 0; i <= 1; i++)
	//		{
	//			lines[i] = new Vector3(result.)
	//		}

			if(lines.Count < 1)
				return false;

			if(lineStart == result && lines.Count == 2)
			{
				result = lines[1];
				return true;
			}

			result = lines[0];
			return true;
			
		}

//		public static int[] GetFourIndexCornersClockwise(Vector3[] floorOutline)
//		{
//			Vector3[] fourCorners = GetFourFloorCorners(floorOutline);
//			int[] fourIndexCorners = new int[fourCorners.Length];
//			// Finds the indexes that are closest to each of the four corners
//			for(int i = 0; i < fourCorners.Length; i++)
//				fourIndexCorners[i] = GetClosestPoint(fourCorners[i], floorOutline);
//
//			return fourIndexCorners;
//		}

//		public static readonly Vector3 forwardBuilding = Vector3.forward;
//		public static readonly Vector3 backwardBuilding = Vector3.back;
//		public static readonly Vector3 leftBuilding = Vector3.left;
//		public static readonly Vector3 rightBuilding = Vector3.right;
		
//		public static int[] GetFourIndexCorners(Vector3[] floorOutline)
//		{
//			return GetFourIndexCornersClockwise(floorOutline);
//
			// TODO - Remove this big chunk of info

	//		BCTest.DestroyAllTestBoxes();
	//
	//		Vector3[] fourCorners = GetFourFloorCorners(floorOutline);
	//		int[] fourIndexCorners = new int[fourCorners.Length];
	//		// Finds the indexes that are closest to each of the four corners
	//		for(int i = 0; i < fourCorners.Length; i++)
	//			fourIndexCorners[i] = GetClosestPoint(fourCorners[i], floorOutline);
	//
	//		// Reverses the points
	//		fourIndexCorners = fourIndexCorners.OrderByDescending(x => x).ToArray<int>();
	//
	//		// Now goes through the ouline and figures out where it should start. Between index 0 and index 1 should always be front facing
	//		Vector3 direction = (floorOutline[fourIndexCorners[0] + 1] - floorOutline[fourIndexCorners[0]]).normalized;
	//		Vector3 normalOfFace = Vector3.Cross(direction, Vector3.up);
	//
	//		Vector3 forwardBuilding = new Vector3(0, 0, 1);
	//		Vector3 backwardBuilding = new Vector3(0, 0, -1);
	//		Vector3 leftBuilding = new Vector3(1, 0, 0);
	//		Vector3 rightBuilding = new Vector3(-1, 0, 0);



	//		BCTest.CreateArrow(((floorOutline[fourIndexCorners[0] + 1] + floorOutline[fourIndexCorners[0]]) / 2), normalOfFace, 5f);
	//
	//		// Need to shift around the 
	////		if(normalOfFace == forwardBuilding)
	////		{
	////			fourIndexCorners = new int[4] {fourIndexCorners[3], fourIndexCorners[0], fourIndexCorners[1], fourIndexCorners[2]};
	////			Debug.Log(ParseArray.Log(fourIndexCorners));
	//////			BCTest.CreateArrow(((floorOutline[fourIndexCorners[0] + 1] + floorOutline[fourIndexCorners[0]]) / 2), direction, 5f);
	//////			return fourIndexCorners;
	////			return new int[4] { fourIndexCorners[3], fourIndexCorners[0], fourIndexCorners[1], fourIndexCorners[2]};
	////		}
	//
	//		if(normalOfFace == rightBuilding)
	//		{
	//			fourIndexCorners = new int[4] {fourIndexCorners[3], fourIndexCorners[0], fourIndexCorners[1], fourIndexCorners[2]};
	////			Debug.Log(ParseArray.Log(fourIndexCorners));
	//			return fourIndexCorners;
	//		}
	//
	//		if(normalOfFace == forwardBuilding)
	//		{
	//			fourIndexCorners = new int[4] {fourIndexCorners[0], fourIndexCorners[2], fourIndexCorners[3], fourIndexCorners[4]};
	////			Debug.Log(ParseArray.Log(fourIndexCorners));
	//			return fourIndexCorners;
	//		}


	//		// Since the normal is not right, we rearrange the four corners index so they are ordered correctly
	//		if((forwardBuilding - normalOfFace).sqrMagnitude < 0.0001)
	//		{
	//			Debug.Log ("Opposite");
	//			return new int[4] { fourIndexCorners[2], fourIndexCorners[3],fourIndexCorners[0], fourIndexCorners[1]};
	//		}
	//		else if(normalOfFace == leftBuilding)
	//		{
	//			Debug.Log ("Left building");
	//			return new int[4] { fourIndexCorners[3], fourIndexCorners[1],fourIndexCorners[2], fourIndexCorners[3]};
	//		}
	//		else if(normalOfFace == rightBuilding)
	//		{
	//			return new int[4] { fourIndexCorners[1], fourIndexCorners[2],fourIndexCorners[3], fourIndexCorners[0]};
	//		}

	//		BCTest.CreateArrow((floorOutline[fourIndexCorners[0] + 1] + floorOutline[fourIndexCorners[0]]) / 2 + Vector3.up, Vector3.forward, 1f);

	//		return fourIndexCorners;
//		}

		
//		public static Vector3[] GetFourFloorCorners(Vector3[] edge)
//		{
//			Bounds newBounds = new Bounds();
//			
//			if(edge == null || edge.Length < 1)
//				return new Vector3[0];
//			
//			newBounds.center = edge[0];
//			
//			for(int i = 0; i < edge.Length; i++)
//			{
//				newBounds.Encapsulate(edge[i]);
//			}
//			
//			Vector3[] output = new Vector3[4]
//			{
//				newBounds.center + new Vector3(-newBounds.extents.x, 0, newBounds.extents.z),
//				newBounds.center + new Vector3(newBounds.extents.x, 0, newBounds.extents.z),
//				newBounds.center + new Vector3(newBounds.extents.x, 0, -newBounds.extents.z),
//				newBounds.center + new Vector3(-newBounds.extents.x, 0, -newBounds.extents.z),
//			};
//			
//			return output;
//		}

//		private static int GetClosestPoint(Vector3 point, Vector3[] outline)
//		{
//			if(outline.Length < 2)
//				return -1;
//			
//			float closestDistanceToPoint = 100000000;
//			int index = -1;
//			Vector3 closePoint = point;
//			
//			for(int i = 0; i < outline.Length; i++)
//			{
//				float distanceToPoint = (closePoint - outline[i]).sqrMagnitude;
//				
//				if(distanceToPoint < closestDistanceToPoint)
//				{
//					index = i;
//					closestDistanceToPoint = distanceToPoint;
//				}
//			}
//			return index;
//		}
	}
}
