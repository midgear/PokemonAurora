using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{
	public static partial class BCUtils 
	{
		public static bool IsWindowBackwards(WindowInfo window, FloorBlueprint floorBp)
		{
			Vector3 realDirection = (window.End - window.Start).normalized;
			Vector3 cross = Vector3.Cross(realDirection, Vector3.up).normalized;
			Vector3 testPoint = (window.End + window.Start) / 2 + cross * 0.1f;

			return BCUtils.IsPointOnlyInsideAnyRoom(testPoint, floorBp) == false;
		}

		public static bool IsWindowBackwards(WindowInfo window, RoomBlueprint roomBp)
		{
			Vector3 realDirection = (window.End - window.Start).normalized;
			Vector3 cross = Vector3.Cross(realDirection, Vector3.up).normalized;
			Vector3 testPoint = (window.End + window.Start) / 2 + cross * 0.1f;
			
			return BCUtils.IsPointInARoom(testPoint, roomBp) == false;
		}

		public static bool IsWindowInterior(WindowInfo window, FloorBlueprint floorBp)
		{
			Vector3 realDirection = (window.End - window.Start).normalized;
			Vector3 cross = Vector3.Cross(realDirection, Vector3.up).normalized;
			Vector3 leftSide = (window.End + window.Start) / 2 + cross * 0.1f;
			Vector3 rightSide =(window.End + window.Start) / 2 + cross * -0.1f;

			if(BCUtils.IsPointOnlyInsideAnyRoom(leftSide, floorBp) && BCUtils.IsPointOnlyInsideAnyRoom(rightSide, floorBp))
				return true;
			
			return false;
		}

		public static bool IsWindowAlongWall(WindowInfo window, RoomBlueprint roomBp)
		{
			for(int i = 0; i < roomBp.PerimeterWalls.Count - 1; i++)
			{
				if(BCUtils.IsPointAlongLineXZ(window.Start, roomBp.PerimeterWalls[i], roomBp.PerimeterWalls[i + 1]))
				{
					if(BCUtils.IsPointAlongLineXZ(window.End, roomBp.PerimeterWalls[i], roomBp.PerimeterWalls[i + 1]))
					{
						return true;
					}
				}
			}
			return false;
		}

//		/// <summary>
//		/// Returns an array of the rooms around a window. NOTE: Only returns array of 2 based on the center of a window
//		/// </summary>
//		/// <returns>The rooms around window.</returns>
//		/// <param name="window">Window.</param>
//		/// <param name="floorBp">Floor bp.</param>
//		public static RoomBlueprint[] FindRoomsAroundWindow(WindowInfo window, FloorBlueprint floorBp)
//		{
//			Vector3 realDirection = (window.End - window.Start).normalized;
//			Vector3 cross = Vector3.Cross(realDirection, Vector3.up).normalized;
//			Vector3 leftSide  = (window.End + window.Start) / 2 + cross * 0.1f;
//			Vector3 rightSide = (window.End + window.Start) / 2 + cross * -0.1f;
//
//			RoomBlueprint leftRoom = GetRoomFromPoint(leftSide, floorBp);
//			RoomBlueprint rightRoom = GetRoomFromPoint(rightSide, floorBp);
//
//			if(leftRoom == null && rightRoom == null)
//				return null;
//
//			if(leftRoom == null)
//				return new RoomBlueprint[] { rightRoom };
//
//			if(rightRoom == null)
//				return new RoomBlueprint[] { leftRoom };
//
//			return new RoomBlueprint[] { leftRoom, rightRoom };
//		}

//		public static int[] FindRoomIndexesAroundWindow(WindowInfo window, FloorBlueprint floorBp)
//		{
//			Vector3 realDirection = (window.End - window.Start).normalized;
//			Vector3 cross = Vector3.Cross(realDirection, Vector3.up).normalized;
//			Vector3 leftSide  = (window.End + window.Start) / 2 + cross * 0.1f;
//			Vector3 rightSide = (window.End + window.Start) / 2 + cross * -0.1f;
//
//			int leftIndex = -1;
//			int rightIndex = -1;
//
//			RoomBlueprint leftRoom = GetRoomFromPoint(leftSide, floorBp, out leftIndex);
//			RoomBlueprint rightRoom = GetRoomFromPoint(rightSide, floorBp, out rightIndex);
//			
//			if(leftRoom == null && rightRoom == null)
//				return null;
//			
//			if(leftRoom == null)
//				return new int[] { rightIndex };
//			
//			if(rightRoom == null)
//				return new int[] { leftIndex };
//			
//			return new int[] { leftIndex, rightIndex };
//		}

		public static int[] FindRoomIndexesAroundLine(Vector3 start, Vector3 end, FloorBlueprint floorBp)
		{
			Vector3 realDirection = (end - start).normalized;
			Vector3 cross = Vector3.Cross(realDirection, Vector3.up).normalized;
			Vector3 leftSide  = (end + start) / 2 + cross * 0.1f;
			Vector3 rightSide = (end + start) / 2 + cross * -0.1f;
			
			int leftIndex = -1;
			int rightIndex = -1;
			
			RoomBlueprint leftRoom = GetRoomFromPoint(leftSide, floorBp, out leftIndex);
			RoomBlueprint rightRoom = GetRoomFromPoint(rightSide, floorBp, out rightIndex);
			
			if(leftRoom == null && rightRoom == null)
				return null;
			
			if(leftRoom == null)
			return new int[] { rightIndex };
			
			if(rightRoom == null)
			return new int[] { leftIndex };
			
			return new int[] { leftIndex, rightIndex };
		}

		public static BCWindow GetWindowPrefabTypeFromWindowInfo(WindowInfo window, BuildingStyle buildingStyle)
		{
			// Find the width of the window
			float windowWidth = (window.Start - window.End).magnitude;
			float windowHeight = window.WindowHeight;
			
			// First check to see if there is override window in its place
			// Its either the override OR if that doesn't fit, then the basic window
			if(window.OverriddenWindowType != null)
			{
				if(BCWindowStretcher.TestForWindowHeightFit(window.OverriddenWindowType, windowHeight) == false)
					return buildingStyle.BaseWindow.GetComponent<BCWindow>();
				
				if(BCWindowStretcher.TestForWindowWidthFit(window.OverriddenWindowType, windowWidth) == false)
					return buildingStyle.BaseWindow.GetComponent<BCWindow>();
				
				return window.OverriddenWindowType;
			}
			
			BCWindow bcWindow = null;
			int breaker = 0;
			int fancyIndex = 0;
			
			// Now go through each window in order to get the one that will be used
			while(bcWindow == null && breaker < 128)
			{
				if(buildingStyle.BaseWindow == null)
				{
					Debug.LogError("WARNING: There is no 'base window' as part of the Building Style " + buildingStyle.name + ". Please add one.");
					break;
				}
				
				breaker++;
				
				if(buildingStyle.FancyWindows == null || (fancyIndex < buildingStyle.FancyWindows.Count && buildingStyle.FancyWindows[fancyIndex] == null))
				{
					fancyIndex++;
					continue;
				}
				
				if(fancyIndex < buildingStyle.FancyWindows.Count)
				{
					// This window does not go into any spaces (marked as nothing on the flag)
					if((int)buildingStyle.FancyWindowTypes[fancyIndex] == 0)
					{
						fancyIndex++;
						continue;
					}
					else if((window.WindowType & buildingStyle.FancyWindowTypes[fancyIndex]) == window.WindowType)
					{
						bcWindow = buildingStyle.FancyWindows[fancyIndex].GetComponent<BCWindow>();
					}
					else
					{
						fancyIndex++;
						continue;
					}
				}
				if(fancyIndex == buildingStyle.FancyWindows.Count && buildingStyle.BaseWindow != null)
				{
					bcWindow = buildingStyle.BaseWindow.GetComponent<BCWindow>();
					break;
				}
				
				// Now test to see if the window will fit in correctly
				if(BCWindowStretcher.TestForWindowHeightFit(bcWindow, windowHeight) == false)
					bcWindow = null;
				
				if(BCWindowStretcher.TestForWindowWidthFit(bcWindow, windowWidth) == false)
					bcWindow = null;
				
				// If it fits, return the window type
				if(bcWindow != null)
					break;
				
				fancyIndex++;
			}
			
			if(bcWindow == null)
				Debug.LogError("Your base window is not set OR your base window can't fit into height: " + windowHeight + " width: " + windowWidth);
			
			return bcWindow;
		}

	}
}
