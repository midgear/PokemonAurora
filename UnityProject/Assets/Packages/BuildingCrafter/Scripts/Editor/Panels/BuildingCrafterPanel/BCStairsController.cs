using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{

	public partial class BuildingCrafterPanel : Editor
	{

		bool layingStairs = false;
		StairInfo blankStair = new StairInfo();

		private void LayingStairs (Event currentEvent)
		{
			Vector3 gridPoint;
			bool properClick = GetGridPoint(out gridPoint, false, currentEvent);

			if(properClick == false)
			{
				gridCursorColor = invisibleCursor;
				convexColor = invisibleCursor;
				return;
			}
				
			
			if(layingStairs == false)
			{
				gridCursorColor = greenGridCursor;
				gridCursor = gridPoint + currentFloorHeight;
			}
			else
			{
				gridPoint = GetCompassGridPoint(gridPoint, startOpening);
				// Rounds to 4 meters 
				Vector3 stairsDirection = (gridPoint - startOpening).normalized;
				
				float pointLength = 4;
				
				Vector3 endOpeningOfStairs = startOpening + stairsDirection * pointLength;
				Vector3 rectOffset = Vector3.zero;
				
				if(stairsDirection.x < 0)
					rectOffset = Vector3.forward;
				if(stairsDirection.x > 0)
					rectOffset = Vector3.back;
				if(stairsDirection.z < 0)
					rectOffset = Vector3.left;
				if(stairsDirection.z > 0)
					rectOffset = Vector3.right;

				if(rectOffset.sqrMagnitude < 0.001)
				{
					gridCursorColor = redGridCursor;
					gridCursor = gridPoint;

				}
				else
				{
					gridCursorColor = invisibleCursor;

					blankStair.Start = startOpening;
					blankStair.End = endOpeningOfStairs;
					
					Vector3[] stairsOutline = BCUtils.Get3DStairsOutline(blankStair, rectOffset);
									
					Handles.color = Color.green;
					for(int i = 0; i < stairsOutline.Length; i++)
						stairsOutline[i] += currentFloorHeight;
					
					Handles.DrawAAPolyLine(4, stairsOutline);
					
					endOpening = endOpeningOfStairs;
				}
			}
			
			if(TestMouseClick(currentEvent))
			{
				if(layingStairs == true && (startOpening - endOpening).sqrMagnitude < 0.001)
				{

				}
				else if(layingStairs == false)
				{
					startOpening = gridPoint;
					layingStairs = true;
				}
				// Laying 
				else
				{
					Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Add Stair");

					layingStairs = false;
					// Here we lay the stairs into the system
					Script.CurrentFloorBlueprint.Stairs.Add(new StairInfo(startOpening, endOpening));
					DrawNewStairsOutlines(currentFloorHeight);
				}
				
				
			}
			
			if(layingStairs == true && TestMouseClick(currentEvent, 1))
			{
				layingStairs = false;
				startOpening = Vector3.zero;
			}
			
			// Must be here to allow to break away
			ResetClickUp(currentEvent);
			
		}

		private void DeletingStairs (Event currentEvent)
		{
	//		Vector3 gridPoint;
	//		bool isHitting = GetGridPoint(out gridPoint, true, currentEvent);

			Vector3 precisePoint;
			bool isHitting = GetPrecisePoint(out precisePoint, currentEvent);
			
			if(isHitting == true)
			{
				int deleteIndex = -1;

				gridCursorColor = redGridCursor;
				gridCursor = precisePoint + currentFloorHeight;

				for(int i = 0; i < Script.CurrentFloorBlueprint.Stairs.Count; i++)
				{
					var stairs = Script.CurrentFloorBlueprint.Stairs[i];
					
					if(BCUtils.PointInPolygonXZ(precisePoint, BCUtils.GetStairsOutline(stairs)))
					{
						deleteIndex = i;
						break;
					}
				}
				
				if(deleteIndex > -1)
				{
					convexColor = gridCursorColor;

					convexOutline.Add(BCUtils.GetStairsOutline(Script.CurrentFloorBlueprint.Stairs[deleteIndex], currentFloorHeight.y));
				
					if(TestMouseClick(currentEvent))
					{
						Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Delete Stair");

						Script.CurrentFloorBlueprint.Stairs.RemoveAt(deleteIndex);
					}
				}
				else
				{
					convexOutline.Clear();
					TestMouseClick(currentEvent);
				}

				ResetClickUp(currentEvent);
			}
			else
			{
				gridCursorColor = invisibleCursor;
				convexColor = invisibleCursor;
			}
		}
	}
}