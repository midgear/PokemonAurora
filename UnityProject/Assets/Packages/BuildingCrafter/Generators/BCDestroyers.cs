using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BuildingCrafter
{
	public static partial class BCGenerator
	{
		public static void DestroyOutsideWalls(BuildingBlueprint buildingBp)
		{
			for(int i = 0; i < buildingBp.Floors.Count; i++)
			{
				DestroyOutsideWallForFloor(buildingBp, i);
			}
		}

		public static void DestroyOutsideWallForFloor (BuildingBlueprint buildingBp, int floorIndex)
		{
			FloorHolder floorHolder = BCBlueprintUtils.FindRealFloor(buildingBp, floorIndex);

			if(floorHolder == null)
				return;

			DestroyGameObjectsWithComponent<RoofHolder>(floorHolder.gameObject);
			DestroyGameObjectsWithComponent<OverhangHolder>(floorHolder.gameObject);
			DestroyGameObjectsWithComponent<OutsideWallHolder>(floorHolder.gameObject);
			DestroyGameObjectsWithComponent<RoofLipHolder>(floorHolder.gameObject);
			DestroyGameObjectsWithComponent<TopWallsHolder>(floorHolder.gameObject);
		}

		public static void DestroyOverhangsForFloor(BuildingBlueprint buildingBp, int floorIndex)
		{
			FloorHolder floorHolder = BCBlueprintUtils.FindRealFloor(buildingBp, floorIndex);

			if(floorHolder == null)
				return;

			DestroyGameObjectsWithComponent<OverhangHolder>(floorHolder.gameObject);
		}

		public static void DestroyGameObjectWithComponent<T>(GameObject gameObject) where T : Component
		{
			Component component = gameObject.GetComponentInChildren<T>();

			if(component != null)
				DestroyAllProceduralMeshes(component.gameObject, true);
		}

		public static void DestroyGameObjectsWithComponent<T>(GameObject gameObject) where T : Component
		{
			Component[] components = gameObject.GetComponentsInChildren<T>();

			for(int i = 0; i < components.Length; i++)
			{
				if(components[i] != null)
					DestroyAllProceduralMeshes(components[i].gameObject, true);
			}
		}

		public static void DestroyGameObjectWithName(FloorHolder floorHolder, string wildCardName)
		{
			bool destroyedAll = false;

			while(destroyedAll == false)
			{
				destroyedAll = true;

				for(int i = 0; i < floorHolder.transform.childCount; i++)
				{
					if(floorHolder.transform.GetChild(i).name.Contains(wildCardName))
					{
						DestroyAllProceduralMeshes(floorHolder.transform.GetChild(i).gameObject, true);
						destroyedAll = false;
						break;
					}
				}
			}
		}
		
		public static void DestroyGeneratedBuilding (BuildingBlueprint buildingBp)
		{		
//#if UNITY_EDITOR
//			Undo.RegisterFullObjectHierarchyUndo(buildingBp, "Destroy Building");
//#endif
			// Ensure we kill any light rendering as this can crash Unity.
			
			// Cleans up old items that were generated in the building game object
			// Possibly shouldn't do this here as it destroys the walls
			DestroyAllBuildingMeshes(buildingBp);
			RemoveLODGroup(buildingBp.gameObject);
		}
		
		public static void DestroyFloor(BuildingBlueprint buildingBp, int floorIndex)
		{
//			int tickCount = System.Environment.TickCount;
//			Debug.Log(System.Environment.TickCount - tickCount);
			// Ensure we kill any light rendering as this can crash Unity.
			
			// Cleans up old items that were generated in the building game object
			// Possibly shouldn't do this here as it destroys the walls
			FloorHolder[] floorHolders = buildingBp.GetComponentsInChildren<FloorHolder>();
			
			for(int i = 0; i < floorHolders.Length; i++)
			{
				if(floorHolders[i].FloorIndex == floorIndex)
				{
					DestroyAllProceduralMeshes(floorHolders[i].gameObject, true);
					return;
				}
			}
		}

		public static void DestroyRoom(BuildingBlueprint buildingBp, int floorIndex, int roomIndex)
		{
			FloorHolder floorHolder = BCBlueprintUtils.FindRealFloor(buildingBp, floorIndex);
			RoomHolder roomHolder = BCBlueprintUtils.FindRealRoom(floorHolder, roomIndex);

			if(roomHolder == null)
				return;

			DestroyAllProceduralMeshes(roomHolder.gameObject, true);
		}

		public static void DestroyAllRoomsOnFloor(BuildingBlueprint buildingBp, int floorIndex)
		{
			FloorHolder floorHolder = BCBlueprintUtils.FindRealFloor(buildingBp, floorIndex);
			if(floorHolder == null)
				return;

			RoomHolder[] roomHolders = floorHolder.GetComponentsInChildren<RoomHolder>();

			for(int i = 0; i < roomHolders.Length; i++)
				DestroySpecificRoom(buildingBp, floorIndex, roomHolders[i].RoomIndex);
		}

		public static WindowHolder FindSpecificWindow(BuildingBlueprint buildingBp, int floorIndex, int windowIndex)
		{
			FloorHolder floorHolder = BCBlueprintUtils.FindRealFloor(buildingBp, floorIndex);

			if(floorHolder == null)
				return null;

			WindowHolder holder = BCBlueprintUtils.FindRealWindow(floorHolder, windowIndex);

			return holder;
		}

		public static void DestroySpecificWindow (BuildingBlueprint buildingBp, int floorIndex, int windowIndex)
		{
			WindowHolder holder = FindSpecificWindow(buildingBp, floorIndex, windowIndex);
			
			if(holder != null)
				DestroyAllProceduralMeshes(holder.gameObject, true);
		}

		public static DoorHolder FindSpecificDoor(BuildingBlueprint buildingBp, int floorIndex, int windowIndex)
		{
			FloorHolder floorHolder = BCBlueprintUtils.FindRealFloor(buildingBp, floorIndex);
			
			if(floorHolder == null)
				return null;
			
			DoorHolder holder = BCBlueprintUtils.FindRealDoor(floorHolder, windowIndex);
			
			return holder;
		}

		public static void DestroySpecificDoor(BuildingBlueprint buildingBp, int floorIndex, int doorIndex)
		{
			DoorHolder holder = FindSpecificDoor(buildingBp, floorIndex, doorIndex);
			
			if(holder != null)
				DestroyAllProceduralMeshes(holder.gameObject, true);
		}

		public static void DestroySpecificRoom (BuildingBlueprint buildingBp, int floorIndex, int roomIndex)
		{
			FloorHolder floorHolder = BCBlueprintUtils.FindRealFloor(buildingBp, floorIndex);
			RoomHolder roomHolder = BCBlueprintUtils.FindRealRoom(floorHolder, roomIndex);

			if(roomHolder != null)
				DestroyAllProceduralMeshes(roomHolder.gameObject, true);
		}

		public static void DestroyAllStairs(BuildingBlueprint buildingBp, int floorIndex)
		{
			FloorHolder floorHolder = BCBlueprintUtils.FindRealFloor(buildingBp, floorIndex);

			if(floorHolder == null)
				return;

			StairsRef[] allStairs = floorHolder.GetComponentsInChildren<StairsRef>();

			for(int i = 0; i < allStairs.Length; i++)
			{
				if(allStairs[i] != null)
					GameObject.DestroyImmediate(allStairs[i].gameObject, false);
			}

		}
	}
}
