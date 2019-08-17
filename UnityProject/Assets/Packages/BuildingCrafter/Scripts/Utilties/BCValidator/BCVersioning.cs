using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BuildingCrafter
{
	public static partial class BCValidator
	{
		#region Test And Update Windows

		/// <summary>
		/// Checks to see if the windows is using non-flag from BC 0.72 and before.
		/// </summary>
		public static bool AreWindowsFromPreBC0p8(BuildingBlueprint buildingBp)
		{
			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				FloorBlueprint floorBp = buildingBp.Floors[floorIndex];

				for(int i = 0; i < floorBp.Windows.Count; i++)
				{
					int windowInt = (int)floorBp.Windows[i].WindowType;

					if(windowInt == 1
					   || windowInt == 3
					   || windowInt == 5
					   || windowInt == 6)
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Checks to see if building has already been updated
		/// </summary>
		/// <returns><c>true</c>, if have been update was windowsed, <c>false</c> otherwise.</returns>
		/// <param name="buildingBp">Building bp.</param>
		public static bool WindowsHaveBeenUpdate(BuildingBlueprint buildingBp)
		{
			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				FloorBlueprint floorBp = buildingBp.Floors[floorIndex];
				
				for(int i = 0; i < floorBp.Windows.Count; i++)
				{
					int windowInt = (int)floorBp.Windows[i].WindowType;

					if(windowInt == 8
					   || windowInt == 16
					   || windowInt == 32
					   || windowInt == 32768)
						return true;
				}
			}
			return false;
		}

		public static void UpdateWindowsFromOlderVersion(BuildingBlueprint buildingBp, bool recordUndo = true)
		{
			if(WindowsHaveBeenUpdate(buildingBp))
				return;

#if UNITY_EDITOR
			if(recordUndo)
				Undo.RegisterCompleteObjectUndo(buildingBp, "Update Windows");
#endif

			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				FloorBlueprint floorBp = buildingBp.Floors[floorIndex];
				
				for(int i = 0; i < floorBp.Windows.Count; i++)
				{
					int windowInt = (int)floorBp.Windows[i].WindowType;
					WindowInfo window = floorBp.Windows[i];
					
					switch(windowInt)
					{
					case 0: // Old None
					case 1: // Old Standard
					case 3: // Old Medium which was the same as standard
						window.WindowType = WindowTypeEnum.Standard;
						break;

					case 2: // Old Short
						window.WindowType = WindowTypeEnum.Short;
						break;

					case 4: // Old Tall
						window.WindowType = WindowTypeEnum.Tall2p8;
						break;

					case 5: // Old slightly less tall
						window.WindowType = WindowTypeEnum.Tall2p5;
						break;

					case 6: // Old High Small
						window.WindowType = WindowTypeEnum.HighSmall;
						break;
					}

					buildingBp.Floors[floorIndex].Windows[i] = window;
				}
			}
		}

		/// <summary>
		/// Updates all the windows across the system
		/// </summary>
		public static void UpdateAllWindowsFromOlderVersion()
		{
			BuildingBlueprint[] buildingBps = GameObject.FindObjectsOfType<BuildingBlueprint>();
#if UNITY_EDITOR
			Undo.RegisterCompleteObjectUndo(buildingBps, "Update All Windows");
#endif
			for(int i = 0; i < buildingBps.Length; i++)
			{
				UpdateWindowsFromOlderVersion(buildingBps[i], false);
			}
		}

		#endregion

		#region Test and Update Windows from Previous Version
		/// <summary>
		/// Checks to see if the doors is using non-flag from BC 0.72 and before.
		/// </summary>
		public static bool AreDoorFromPreBC0p8(BuildingBlueprint buildingBp)
		{
			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				FloorBlueprint floorBp = buildingBp.Floors[floorIndex];
				
				for(int i = 0; i < floorBp.Doors.Count; i++)
				{
					int doorTypeInt = (int)floorBp.Doors[i].DoorType;
					
					if(doorTypeInt == 1
					   || doorTypeInt == 3
					   || doorTypeInt == 5
					   || doorTypeInt == 6)
						return true;
				}
			}
			return false;
		}
		
		/// <summary>
		/// Checks to see if building has already been updated
		/// </summary>
		public static bool DoorsHaveBeenUpdate(BuildingBlueprint buildingBp)
		{
			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				FloorBlueprint floorBp = buildingBp.Floors[floorIndex];
				
				for(int i = 0; i < floorBp.Doors.Count; i++)
				{
					int doorTypeInt = (int)floorBp.Doors[i].DoorType;
					
					if(doorTypeInt == 8
					   || doorTypeInt == 16
					   || doorTypeInt == 32
					   || doorTypeInt == 64
					   || doorTypeInt == 32768)
						return true;
				}
			}
			return false;
		}
		
		public static void UpdateDoorsFromOlderVersion(BuildingBlueprint buildingBp, bool recordUndo = true)
		{
			if(DoorsHaveBeenUpdate(buildingBp))
				return;
			
			#if UNITY_EDITOR
			if(recordUndo)
				Undo.RegisterCompleteObjectUndo(buildingBp, "Update Doors");
			#endif
			
			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				FloorBlueprint floorBp = buildingBp.Floors[floorIndex];
				
				for(int i = 0; i < floorBp.Doors.Count; i++)
				{
					int doorTypeInt = (int)floorBp.Doors[i].DoorType;
					DoorInfo door = floorBp.Doors[i];
					
					switch(doorTypeInt)
					{
					case 0:
						door.DoorType = DoorTypeEnum.Standard;
						break;

					case 1:
						door.DoorType = DoorTypeEnum.Heavy;
						break;

					case 2:
						door.DoorType = DoorTypeEnum.Open;
						break;

					case 3:
						door.DoorType = DoorTypeEnum.SkinnyOpen;
						break;

					case 4:
						door.DoorType = DoorTypeEnum.Closet;
						break;
						
					case 5:
						door.DoorType = DoorTypeEnum.TallOpen;
						break;
						
					case 6:
						door.DoorType = DoorTypeEnum.DoorToRoof;
						break;
					}

					buildingBp.Floors[floorIndex].Doors[i] = door;
				}
			}
		}
		
		/// <summary>
		/// Updates all the windows across the system
		/// </summary>
		public static void UpdateAllDoorsFromOlderVersion()
		{
			BuildingBlueprint[] buildingBps = GameObject.FindObjectsOfType<BuildingBlueprint>();
#if UNITY_EDITOR
			Undo.RegisterCompleteObjectUndo(buildingBps, "Update All Doors");
#endif
			for(int i = 0; i < buildingBps.Length; i++)
			{
				UpdateDoorsFromOlderVersion(buildingBps[i], false);
			}
		}
		#endregion
	}
}
