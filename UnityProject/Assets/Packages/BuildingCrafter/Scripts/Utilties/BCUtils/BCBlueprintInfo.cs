using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{

	public static partial class BCUtils 
	{
		public static Bounds CalculateBlueprintBounds(BuildingBlueprint buildingBp)
		{
			Bounds totalBounds = new Bounds(buildingBp.Floors[0].RoomBlueprints[0].PerimeterWalls[0], Vector3.zero);
			for(int i = 0; i < buildingBp.Floors.Count; i++)
			{
				for(int n = 0; n < buildingBp.Floors[i].RoomBlueprints.Count; n++)
				{
					for(int j = 0; j < buildingBp.Floors[i].RoomBlueprints[n].PerimeterWalls.Count; j++)
					{
						totalBounds.Encapsulate(buildingBp.Floors[i].RoomBlueprints[n].PerimeterWalls[j]);
					}
				}
			}

			return totalBounds;
		}

		public static Vector3 CalculateBlueprintCentre(BuildingBlueprint buildingBp)
		{
			if(ValidRoom(buildingBp, 0, 0) == false)
			{
				Debug.LogError("Building " + buildingBp.name + " doesn't have valid first floor");
				return Vector3.zero;
			}
				
			Bounds totalBounds = CalculateBlueprintBounds(buildingBp);

			return totalBounds.center - new Vector3(totalBounds.extents.x, 0, totalBounds.extents.z);
		}

		public static bool ValidRoom(BuildingBlueprint buildingBp, int floorIndex, int roomIndex )
		{
			if(buildingBp.Floors == null 
			   || buildingBp.Floors.Count < floorIndex + 1 
			   || buildingBp.Floors[floorIndex].RoomBlueprints == null 
			   || buildingBp.Floors[floorIndex].RoomBlueprints.Count < roomIndex + 1
			   || buildingBp.Floors[floorIndex].RoomBlueprints[roomIndex].PerimeterWalls == null
			   || buildingBp.Floors[floorIndex].RoomBlueprints[roomIndex].PerimeterWalls.Count < 4)
				return false;

			return true;
		}
	//
	//	public static void UpdateBuildingPivot (BuildingBlueprint buildingBp)
	//	{
	//		Vector3 cornerPoint = CalculateBlueprintCentre(buildingBp);
	//
	//		// Find if the point is within the bounds
	//		Bounds blueprintBounds = CalculateBlueprintBounds(buildingBp);
	//		blueprintBounds.Expand(new Vector3(0, 1, 0));
	//
	//		if(blueprintBounds.Contains(Vector3.zero) == true)
	//			return;
	//
	//		// First we must compare the blueprint's current location to the old reference point. Get the offset X, Y, Z.
	//		Vector3 offset3D = Vector3.zero + cornerPoint;
	//		
	//		Vector3 updateOffset = new Vector3(offset3D.x, 0, offset3D.z); // NOTE: All floor points should ALWAYS be in just the XZ plane
	//		
	//		// Then we update all the points by the difference amount
	//		for(int i = 0; i < buildingBp.Floors.Count; i ++)
	//		{
	//			FloorBlueprint floor = buildingBp.Floors[i];
	//			
	//			for(int j = 0; j < floor.RoomBlueprints.Count; j++)
	//			{
	//				RoomBlueprint roomBp = floor.RoomBlueprints[j];
	//				
	//				for(int n = 0; n < roomBp.PerimeterWalls.Count; n++)
	//				{
	//					roomBp.PerimeterWalls[n] -= updateOffset;
	//				}
	//			}
	//			
	//			for(int j = 0; j < floor.Doors.Count; j++)
	//			{
	//				DoorInfo doorInfo = floor.Doors[j];
	//				
	//				doorInfo.Start -= updateOffset;
	//				doorInfo.End -= updateOffset;
	//				
	//				floor.Doors[j] = doorInfo;
	//			}
	//			
	//			for(int j = 0; j < floor.Windows.Count; j++)
	//			{
	//				WindowInfo windowInfo = floor.Windows[j];
	//				
	//				windowInfo.Start -= updateOffset;
	//				windowInfo.End -= updateOffset;
	//				floor.Windows[j] = windowInfo;
	//			}
	//			
	//			for(int j = 0; j < floor.Stairs.Count; j++)
	//			{
	//				StairInfo stairInfo = floor.Stairs[j];
	//				
	//				stairInfo.Start -= updateOffset;
	//				stairInfo.End -= updateOffset;
	//				
	//				floor.Stairs[j] = stairInfo;
	//			}
	//			
	//			for(int j = 0; j < floor.YardLayouts.Count; j++)
	//			{
	//				YardLayout yardInfo = floor.YardLayouts[j];
	//				
	//				for(int n = 0; n < yardInfo.PerimeterWalls.Count; n++)
	//				{
	//					yardInfo.PerimeterWalls[n] -= updateOffset;
	//				}
	//			}
	//		}
	//		
	//		for(int i = 0; i < buildingBp.RoofInfos.Count; i++)
	//		{
	//			RoofInfo roof = buildingBp.RoofInfos[i];
	//			
	//			roof.BackLeftCorner -= updateOffset;
	//			roof.FrontRightCorner -= updateOffset;
	//			roof.UpdateBaseOutline();
	//			
	//			buildingBp.RoofInfos[i] = roof;
	//		}
	//		
	//		buildingBp.BlueprintCenter -= updateOffset;
	//		buildingBp.BuildingRotation = buildingBp.transform.rotation;
	//
	//	}
	}
}