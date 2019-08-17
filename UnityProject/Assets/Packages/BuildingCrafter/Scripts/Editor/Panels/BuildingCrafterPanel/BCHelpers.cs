using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{

	public partial class BuildingCrafterPanel : Editor 
	{
		// Used to detect if the user is right clicking or dragging in TestRightQuickClick()
		bool possibleMouseDrag = false;


		private int GetFloorIndex(int selection)
		{
			if(Script.BuildingBlueprint == null)
				return 0;
			return Script.BuildingBlueprint.Floors.Count - 1 - selection;
		}
		
		private int GetBasementIndex(int selection)
		{
			return selection;
		}

		public bool GetWorldPoint(Event currentEvent, out Vector3 gridPoint)
		{
			gridPoint = Vector3.zero;

			Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
			RaycastHit rayHit;

			if(Physics.Raycast(ray, out rayHit))
			{
				gridPoint = rayHit.point;
				gridPoint = new Vector3(Mathf.Round(gridPoint.x), gridPoint.y, Mathf.Round(gridPoint.z));
				return true;
			}

			return false;
		}

		public Vector3 GetPlanePoint(Event currentEvent)
		{
			Plane plane = new Plane(Vector3.up, new Vector3(0, this.currentFloorHeight.y, 0));

			Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);

			float distanceToPlane = 0;

			if(plane.Raycast(ray, out distanceToPlane))
			{
				return ray.GetPoint(distanceToPlane);
			}

			return new Vector3(0, -1000, 0);
		}


		private bool GetGridPoint(out Vector3 gridPoint, bool isHalfSize, Event currentEvent, int splitPointBy = 1)
		{
			gridPoint = GetPlanePoint(currentEvent);

			if(gridPoint.y < -950)
				return false;

			float floorHeight = 0;

			if(isHalfSize == true)
			{
				gridPoint = new Vector3(Mathf.Round(gridPoint.x * 2) / 2, floorHeight, Mathf.Round(gridPoint.z * 2) / 2);
				return true;
			}

			gridPoint = new Vector3(Mathf.Round(gridPoint.x * splitPointBy) / splitPointBy, floorHeight, Mathf.Round(gridPoint.z * splitPointBy) / splitPointBy);
			return true;
	//
	//		gridPoint = new Vector3(); // The point where the player is aiming
	//		Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
	//		RaycastHit[] rayHit; // The raycast result that hits all
	//		rayHit = Physics.RaycastAll(ray, 100);
	//		
	//		// We are actually always running the zero height of any point
	//		float floorHeight = 0;
	//		
	//		// If nothing is hit, return false;
	//		if(rayHit.Length < 0)
	//			return false;
	//		
	//		for(int i = 0; i < rayHit.Length; i++)
	//		{
	//			if(rayHit[i].collider.gameObject != this.buildingCreator)
	//				continue;
	//			
	//			// Gets the locations that a window can be placed and returns the grid point
	//			if(isHalfSize == false)
	//			{
	//				gridPoint = new Vector3(Mathf.Round(rayHit[i].point.x), floorHeight, Mathf.Round(rayHit[i].point.z));
	//				return true;
	//			}
	//			else // return grid points rounded to the nearest 0.5m;
	//			{
	//				gridPoint = new Vector3(Mathf.Round(rayHit[i].point.x * 2) / 2, floorHeight, Mathf.Round(rayHit[i].point.z * 2) / 2);
	//				return true;
	//			}
	//		}
	//		return false;
		}
		
	//	private bool GetGridPointSnapped(out Vector3 gridPoint, Vector3 lastPoint, Event currentEvent)
	//	{
	//		gridPoint = Vector3.zero;
	//		Vector3 actualGridPoint;
	//		if(GetGridPoint(out actualGridPoint, true, currentEvent) == false)
	//			return false;
	//		
	//		int xDiff = (int)(actualGridPoint.x - lastPoint.x);
	//		int zDiff = (int)(actualGridPoint.z - lastPoint.z);
	//		
	//		if(Mathf.Abs(xDiff) > Mathf.Abs(zDiff))
	//		{
	//			gridPoint = new Vector3(actualGridPoint.x, 0, lastPoint.z);
	//		}
	//		else
	//			gridPoint = new Vector3(lastPoint.x, 0, actualGridPoint.z);
	//		
	//		return true;
	//	}
		
		private bool GetPrecisePoint(out Vector3 precisePoint, Event currentEvent)
		{
			precisePoint = GetPlanePoint(currentEvent);
			
			if(precisePoint.y < -950)
				return false;

			precisePoint = new Vector3(precisePoint.x, 0, precisePoint.z);
			return true;

	//		precisePoint = new Vector3(); // The point where the player is aiming
	//		Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
	//		RaycastHit[] rayHit; // The raycast result that hits all
	//		rayHit = Physics.RaycastAll(ray, 100);
	//		
	//		// If nothing is hit, return false;
	//		if(rayHit.Length < 0)
	//			return false;
	//		
	//		for(int i = 0; i < rayHit.Length; i++)
	//		{
	//			if(rayHit[i].collider.gameObject != this.buildingCreator)
	//				continue;
	//			
	//			precisePoint = new Vector3(rayHit[i].point.x, 0, rayHit[i].point.z);
	//			return true;
	//		}
	//		return false;
		}

		/// <summary>
		/// Returns a grid point in one of the direction
		/// </summary>
		private Vector3 GetCompassGridPoint(Vector3 newPoint, Vector3 lastPoint)
		{
			Vector3 actualGridPoint = newPoint;
			int xDiff = (int)(actualGridPoint.x - lastPoint.x);
			int zDiff = (int)(actualGridPoint.z - lastPoint.z);
			
			if(Mathf.Abs(xDiff) > Mathf.Abs(zDiff))
				return new Vector3(actualGridPoint.x, lastPoint.y, lastPoint.z);
			else
				return new Vector3(lastPoint.x, lastPoint.y, actualGridPoint.z);
		}

		private int GetWallPoint(out Vector3 gridPoint, bool isHalfSized, Event currentEvent)
		{
			GetGridPoint(out gridPoint, isHalfSized, currentEvent);
			
			for(int j = 0; j < Script.CurrentFloorBlueprint.RoomBlueprints.Count; j++)
			{
				// Creates the wall you will need to test against
				List<Vector3> walls = Script.CurrentFloorBlueprint.RoomBlueprints[j].PerimeterWalls;
				
				List<int> indexes = BCUtils.BetweenTwoPointOnALine(gridPoint, walls);
				
				if(indexes.Count == 2)
				{
					if(walls[indexes[0]].x == walls[indexes[1]].x)
					{
						gridPoint = new Vector3(walls[indexes[0]].x, gridPoint.y, gridPoint.z);
						return 1;
					}
					if(walls[indexes[0]].z == walls[indexes[1]].z)
					{
						gridPoint = new Vector3(gridPoint.x, gridPoint.y, walls[indexes[0]].z);
						return 1;
					}
				}
				else if(indexes.Count == 3)
				{
					gridPoint = walls[indexes[0]]; // NOTE: the 0 index is the actual point that was detected as the join)
					return 2;
				}
			}
			return 0;
		}

		
		/// <summary>
		/// Finds the wall that the player is currently hovering over
		/// </summary>
		private bool HoverSelectWall(Event currentEvent, FloorBlueprint floorBp, out RoomBlueprint roomFound, out int wallIndexStart)
		{
			roomFound = null;
			wallIndexStart = -1;
			
			if(floorBp.RoomBlueprints.Count < 1)
				return false;
			
			// Select a room to modify
			RoomBlueprint roomBp = null;
			
			// Gets the points in the world, if false, returns;
			Vector3 gridPoint;
			if(GetGridPoint(out gridPoint, true, currentEvent) == false)
				return false;
			
			Vector3 precisePoint;
			if(GetPrecisePoint(out precisePoint, currentEvent) == false)
				return false;
			
			// Sees if the player is dragging
			
			int roomIndex = -1;
			
			for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
			{
				roomBp = floorBp.RoomBlueprints[i];
				if(BCUtils.PointInPolygonXZ(precisePoint, roomBp.PerimeterWalls.ToArray<Vector3>()))
				{
					roomIndex = i;
					break;
				}
			}
			
			if(selectedRoomIndex > -1)
				roomIndex = selectedRoomIndex;
			
			if(roomIndex < 0)
				return false;
			
			roomFound = floorBp.RoomBlueprints[roomIndex];
			wallIndexStart = BCUtils.GetIndexOfWall(gridPoint, roomBp);
			return true;
		}

		#region Click Events

		bool TestMouseClick(Event currentEvent, int button = 0)
		{
			if(currentEvent.alt == true
			   || currentEvent.command == true
			   || currentEvent.control == true)
				return false;

			// Test for mouse down, and make sure there are no modifiers
			if(currentEvent.type == EventType.MouseDown && currentEvent.button == button)
			{
				EditorGUIUtility.hotControl = controlId;
				currentEvent.Use();
				return true;
			}

			return false;
		}

		bool ResetClickUp(Event currentEvent)
		{
			if(EditorGUIUtility.hotControl == controlId && currentEvent.type == EventType.MouseUp)
			{
				EditorGUIUtility.hotControl = 0;
				currentEvent.Use();
				return true;
			}
			return false;
		}

		bool TestRightQuickClick(Event currentEvent)
		{
			if(currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
			{
				possibleMouseDrag = true;
			}

			if(possibleMouseDrag == true && currentEvent.type == EventType.MouseDrag)
			{
				possibleMouseDrag = false;
			}

			if(currentEvent.type == EventType.MouseUp && possibleMouseDrag == true && currentEvent.button == 1)
			{
				possibleMouseDrag = false;
				return true;
			}

			return false;
		}

		#endregion

		bool CheckForEscape (Event currentEvent, bool resetEditingState)
		{
			if(currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape)
			{
				if(EditorGUIUtility.hotControl == controlId)
				{
					EditorGUIUtility.hotControl = 0;
				}
				if(resetEditingState)
					Script.EditingState = EditingState.None;

				return true;
			}

			return false;
		}
	}
}