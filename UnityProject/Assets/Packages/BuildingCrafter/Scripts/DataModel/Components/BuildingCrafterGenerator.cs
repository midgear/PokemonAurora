using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BuildingCrafter
{
	/// <summary>
	/// The main component for the Building Crafter. Needed to generate the building
	/// </summary>
	public class BuildingCrafterGenerator : MonoBehaviour 
	{
		public BuildingBlueprint BuildingBlueprint;

		public FloorBlueprint CurrentFloorBlueprint
		{
			get
			{
				if(BuildingBlueprint == null || BuildingBlueprint.Floors.Count == 0)
					return null;
				if(CurrentFloor < 0)
					return null;
				if(CurrentFloor >= BuildingBlueprint.Floors.Count && BuildingBlueprint.Floors.Count > 0)
					return BuildingBlueprint.Floors[BuildingBlueprint.Floors.Count - 1];
				else
					return BuildingBlueprint.Floors[CurrentFloor];
			}
		}

		// The floor currently being looked at by the system
		public int CurrentFloor = -1;

		// Settings for the Building Crafter
		public bool ShowIndexPoints = false;
		public bool HighlightGenericRooms = false;

		// On screen representations
		public List<Vector3> CurrentWallPath = new List<Vector3>();
		public List<Vector3> SelectedPath = new List<Vector3>();

		// Floors
		public List<Vector3[]> RoomsBelowCurrentFloor = new List<Vector3[]>();
		public List<Vector3[]> RoomOutlineBelow = new List<Vector3[]>();

		// Openings and stairs
		public List<Vector3[]> DoorDisplays = new List<Vector3[]>();
		public List<Vector3[]> WindowDisplays = new List<Vector3[]>();
		public List<Vector3[]> StairsDisplay  = new List<Vector3[]>();

		// Other
		public List<Vector3> CurrentSideWall = new List<Vector3>();
		public List<Vector3[]> RoofRidge = new List<Vector3[]>();

		// Editing Options
		public EditingState EditingState = EditingState.None;
		public EditingState LastEditingState = EditingState.None;
		public EditFloorType FloorEditType = 0;
		public EditFloorType LastFloorEditType = 0;

		// Live View Stuff
		public GameObject TempWallDisplay = null;
		public float LastWallPathCount = 0;

		public int SelectedFloor
		{
			get
			{
				if(BuildingBlueprint != null)
					return BuildingBlueprint.SelectedFloor;
				return 0;
			}
			set
			{
				if(BuildingBlueprint != null)
					BuildingBlueprint.SelectedFloor = value;
			}
		}
		public int LastSelectedFloor
		{
			get
			{
				if(BuildingBlueprint != null)
					return BuildingBlueprint.LastSelectedFloor;
				return 0;
			}
			set
			{
				if(BuildingBlueprint != null)
					BuildingBlueprint.LastSelectedFloor = value;
			}
		}

		// The floor the player was looking at before heading to the roof
		public int PreviousFloor
		{
			get
			{
				if(BuildingBlueprint != null)
					return BuildingBlueprint.PreviousFloor;
				return 0;
			}
			set
			{
				if(BuildingBlueprint != null)
					BuildingBlueprint.PreviousFloor = value;
			}
		}

		// Opening Editing and Creating
		public DoorInfo NewDoor;
		public WindowInfo NewWindow;

	}

	public enum EditFloorType
	{
		Floor = 0,
		Roof = 1,
		Exterior = 2,
		Style = 3
	}

	public enum EditingState
	{
		None = 0,
		LayRoomFloors,
		LayWindows,
		LayDoors,
		LayStairs,
		LayRoof,
		LayYard,
		ModifyFloor,
		AddingFloorPoints,
		DeleteWalls,
		DeleteDoorsAndWindows,
		DeleteRooms,
		DeleteStairs,
		DeleteRoof,
		DeleteYard,
		EyedropDoorWindows,
		EditFloorProperties,
		EditDoorWindowProperties,
		EditRoofProperties,
		UpdatingPivot,
	}
}
