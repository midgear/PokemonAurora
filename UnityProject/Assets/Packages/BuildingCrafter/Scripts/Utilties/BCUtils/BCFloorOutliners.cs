using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{
	public static partial class BCUtils 
	{
		/// <summary>
		/// Returns a 4 length vector that is the bottom start, bottom end, top end, top start
		/// </summary>
		/// <returns>The window sizing.</returns>
		/// <param name="window">Window.</param>
		public static Vector3[] GetWindowSizing(WindowInfo window, bool isWindowBackwards, float windowFrameInset = 0.1f)
		{
			// TODO - move this to a better Util area
			Vector3 direction = (window.End - window.Start).normalized;
			float length = (window.End - window.Start).magnitude;

			float bottomHeight = window.BottomHeight;
			float topHeight = window.TopHeight;

			Vector3 windowBottomStart = Vector3.up * bottomHeight + direction * windowFrameInset;
			Vector3 windowBottomEnd = Vector3.up * bottomHeight + direction * length - direction * windowFrameInset;
			Vector3 windowTopStart = Vector3.up * topHeight + windowFrameInset * direction;
			Vector3 windowTopEnd = Vector3.up * topHeight + direction * length - windowFrameInset * direction;

			if(isWindowBackwards == false)
				return new Vector3[] { windowBottomStart, windowBottomEnd, windowTopEnd, windowTopStart };
			else
				return new Vector3[] { windowBottomEnd, windowBottomStart, windowTopStart, windowTopEnd };
		}

//		public static List<Vector3[]> GenerateRoofLipVectors(BuildingBlueprint buildingBp, int floor)
//		{
//			Vector3[] thisFloorOutline = BCMesh.GenerateOutlineFloor(buildingBp.Floors[floor]);
//			Vector3[] aboveFloorOutline = null;
//			
//			if(floor < buildingBp.Floors.Count - 1)
//				aboveFloorOutline = BCMesh.GenerateOutlineFloor(buildingBp.Floors[floor + 1]);
//
//			// Now that we have the walls, we spit them into the differences
//
//			if(aboveFloorOutline == null)
//				aboveFloorOutline = new Vector3[0];
//
//			List<Vector3[]> lipVectors = new List<Vector3[]>();
//			List<Vector3> currentSection = new List<Vector3>();
//
//			for(int i = 0; i < thisFloorOutline.Length - 1; i++)
//			{
//				int index = i;
//				int next = i + 1;
//
//				Vector3 point = thisFloorOutline[index];
//				Vector3 nextPoint = thisFloorOutline[next];
//
//				// 1. Add the starting point to the current section if it isn't inside a wall
//				// 2. If it is inside a wall, then close off the current section, add it to the 
//				// overall vector list as an array and move to the next point
//
//				// 3. Create a list of vectors which are the line
//				List<Vector3> overlapping = new List<Vector3>();
//				overlapping.Add(point);
//				overlapping.AddRange(FindSegmentOverlap(point, nextPoint, aboveFloorOutline));
//				overlapping.Add(nextPoint);
//
//				for(int n = 0; n < overlapping.Count - 1; n++)
//				{
//					int tp = n;
//					int np = n + 1;
//
//					Vector3 midPoint = (overlapping[tp] + overlapping[np]) / 2;
//
//					// If the point is not in a room, it means this should be recored
//					if(BCUtils.IsPointInARoom(midPoint, aboveFloorOutline) == false
//					   && buildingBp.XPartyWalls.Contains(midPoint.x) == false && buildingBp.ZPartyWalls.Contains(midPoint.z) == false )
//					{
//						if(currentSection.Count == 0) // If a brand new section, also add the first point
//							currentSection.Add(overlapping[tp]);
//
//						currentSection.Add(overlapping[np]);
//					}
//					else // If we find an inside section, then close off the array and add it to the list
//					{
//						if(currentSection.Count > 0)
//						{
//							lipVectors.Add(currentSection.ToArray<Vector3>());
//						}
//						currentSection.Clear();
//					}
//				}
//
//				if(i == thisFloorOutline.Length - 2)
//				{
//					if(currentSection.Count > 0)
//					{
//						lipVectors.Add(currentSection.ToArray<Vector3>());
//					}
//				}
//
//			}
//
//			// Finally check to see if we have a loop
//
//			if(lipVectors.Count > 1)
//			{
//				int endOfLips = lipVectors.Count - 1;
//				int endOfLastLip = lipVectors[endOfLips].Length - 1;
//
//				if(lipVectors[0][0] == lipVectors[endOfLips][endOfLastLip])
//				{
//					List<Vector3> firstAndLast = new List<Vector3>();
//					firstAndLast.AddRange(lipVectors[endOfLips]);
//
//					for(int i = 1; i < lipVectors[0].Length; i++)
//						firstAndLast.Add(lipVectors[0][i]);
//
//					lipVectors.RemoveAt(endOfLips);
//					lipVectors.RemoveAt(0);
//					lipVectors.Add(firstAndLast.ToArray<Vector3>());
//				}
//			}
//
//			return lipVectors;
//		}

		/// <summary>
		/// DEPRECIATED
		/// </summary>
//		public static List<Vector3[]> GenerateRoofWallVectors(BuildingBlueprint buildingBp, int floor)
//		{
//			// NOTE THIS COPIES THE ABOVE CODE BUT DOESN'T BREAK UP PARTY WALL OFFSETS
//
//			Vector3[] thisFloorOutline = BCMesh.GenerateOutlineFloor(buildingBp.Floors[floor]);
//			Vector3[] aboveFloorOutline = null;
//
//			if(thisFloorOutline == null)
//				return null;
//			
//			if(floor < buildingBp.Floors.Count - 1)
//				aboveFloorOutline = BCMesh.GenerateOutlineFloor(buildingBp.Floors[floor + 1]);
//
//			if(aboveFloorOutline == null)
//				aboveFloorOutline = new Vector3[0];
//			// Now that we have the walls, we spit them into the differences
//			
//			List<Vector3[]> lipVectors = new List<Vector3[]>();
//			List<Vector3> currentSection = new List<Vector3>();
//			
//			for(int i = 0; i < thisFloorOutline.Length - 1; i++)
//			{
//				int index = i;
//				int next = i + 1;
//				
//				Vector3 point = thisFloorOutline[index];
//				Vector3 nextPoint = thisFloorOutline[next];
//				
//				// 1. Add the starting point to the current section if it isn't inside a wall
//				// 2. If it is inside a wall, then close off the current section, add it to the 
//				// overall vector list as an array and move to the next point
//				
//				// 3. Create a list of vectors which are the line
//				List<Vector3> overlapping = new List<Vector3>();
//				overlapping.Add(point);
//				overlapping.AddRange(FindSegmentOverlap(point, nextPoint, aboveFloorOutline));
//				overlapping.Add(nextPoint);
//				
//				for(int n = 0; n < overlapping.Count - 1; n++)
//				{
//					int tp = n;
//					int np = n + 1;
//					
//					Vector3 midPoint = (overlapping[tp] + overlapping[np]) / 2;
//					
//					// If the point is not in a room, it means this should be recored
//					if(BCUtils.IsPointInARoom(midPoint, aboveFloorOutline) == false)
//					{
//						if(currentSection.Count == 0) // If a brand new section, also add the first point
//							currentSection.Add(overlapping[tp]);
//						
//						currentSection.Add(overlapping[np]);
//					}
//					else // If we find an inside section, then close off the array and add it to the list
//					{
//						if(currentSection.Count > 0)
//						{
//							lipVectors.Add(currentSection.ToArray<Vector3>());
//						}
//						currentSection.Clear();
//					}
//				}
//				
//				if(i == thisFloorOutline.Length - 2)
//				{
//					if(currentSection.Count > 0)
//					{
//						lipVectors.Add(currentSection.ToArray<Vector3>());
//					}
//				}
//				
//			}
//			
//			// Finally check to see if we have a loop
//			
//			if(lipVectors.Count > 1)
//			{
//				int endOfLips = lipVectors.Count - 1;
//				int endOfLastLip = lipVectors[endOfLips].Length - 1;
//				
//				if(lipVectors[0][0] == lipVectors[endOfLips][endOfLastLip])
//				{
//					List<Vector3> firstAndLast = new List<Vector3>();
//					firstAndLast.AddRange(lipVectors[endOfLips]);
//					
//					for(int i = 1; i < lipVectors[0].Length; i++)
//						firstAndLast.Add(lipVectors[0][i]);
//					
//					lipVectors.RemoveAt(endOfLips);
//					lipVectors.RemoveAt(0);
//					lipVectors.Add(firstAndLast.ToArray<Vector3>());
//				}
//			}
//			
//			return lipVectors;
//		}


	}
}