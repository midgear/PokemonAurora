using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BuildingCrafter
{
	public static partial class BCGenerator
	{
		public static void CalculatePartyWalls(BuildingBlueprint buildingBp)
		{
			if(buildingBp.Floors == null || buildingBp.Floors.Count < 1 || buildingBp.Floors[0].RoomBlueprints == null || buildingBp.Floors[0].RoomBlueprints.Count < 1)
				return;

			RoomBlueprint roomBp = buildingBp.Floors[0].RoomBlueprints[0];
			if(roomBp.PerimeterWalls == null || roomBp.PerimeterWalls.Count < 3)
				return;

			// Find the bounds of ALL the rooms in the entire building
			Bounds allBounds = new Bounds(roomBp.PerimeterWalls[0], Vector3.zero);

			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				for(int roomIndex = 0; roomIndex < buildingBp.Floors[floorIndex].RoomBlueprints.Count; roomIndex++)
				{
					for(int wallIndex = 0; wallIndex < buildingBp.Floors[floorIndex].RoomBlueprints[roomIndex].PerimeterWalls.Count; wallIndex++)
					{
						allBounds.Encapsulate(buildingBp.Floors[floorIndex].RoomBlueprints[roomIndex].PerimeterWalls[wallIndex]);
					}
				}
			}

			// Calculate negative X side
			float negativeXSide =  allBounds.center.x - allBounds.extents.x;
			bool isNegXPartyWall = true;

			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				for(int doorIndex = 0; doorIndex < buildingBp.Floors[floorIndex].Doors.Count; doorIndex++)
				{
					if(buildingBp.Floors[floorIndex].Doors[doorIndex].End.x == buildingBp.Floors[floorIndex].Doors[doorIndex].Start.x // Ensures that the plane is the same
						&& buildingBp.Floors[floorIndex].Doors[doorIndex].Start.x == negativeXSide)
					{
						isNegXPartyWall = false;
						break;
					}
				}

				for(int windowIndex = 0; windowIndex < buildingBp.Floors[floorIndex].Windows.Count; windowIndex++)
				{
					if(buildingBp.Floors[floorIndex].Windows[windowIndex].End.x == buildingBp.Floors[floorIndex].Windows[windowIndex].Start.x // Ensures that the plane is the same
						&& buildingBp.Floors[floorIndex].Windows[windowIndex].Start.x == negativeXSide)
					{
						isNegXPartyWall = false;
						break;
					}
				}

				if(isNegXPartyWall == false)
					break;
			}

			float positiveXSide =  allBounds.center.x + allBounds.extents.x;
			bool isPosXPartyWall = true;

			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				for(int doorIndex = 0; doorIndex < buildingBp.Floors[floorIndex].Doors.Count; doorIndex++)
				{
					if(buildingBp.Floors[floorIndex].Doors[doorIndex].End.x == buildingBp.Floors[floorIndex].Doors[doorIndex].Start.x // Ensures that the plane is the same
						&& buildingBp.Floors[floorIndex].Doors[doorIndex].Start.x == positiveXSide)
					{
						isPosXPartyWall = false;
						break;
					}
				}

				for(int windowIndex = 0; windowIndex < buildingBp.Floors[floorIndex].Windows.Count; windowIndex++)
				{
					if(buildingBp.Floors[floorIndex].Windows[windowIndex].End.x == buildingBp.Floors[floorIndex].Windows[windowIndex].Start.x // Ensures that the plane is the same
						&& buildingBp.Floors[floorIndex].Windows[windowIndex].Start.x == positiveXSide)
					{
						isPosXPartyWall = false;
						break;
					}
				}

				if(isPosXPartyWall == false)
					break;
			}

			float negativeZSide =  allBounds.center.z - allBounds.extents.z;
			bool isNegZPartyWall = true;

			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				for(int doorIndex = 0; doorIndex < buildingBp.Floors[floorIndex].Doors.Count; doorIndex++)
				{
					if(buildingBp.Floors[floorIndex].Doors[doorIndex].End.z == buildingBp.Floors[floorIndex].Doors[doorIndex].Start.z // Ensures that the plane is the same
						&& buildingBp.Floors[floorIndex].Doors[doorIndex].Start.z == negativeZSide)
					{
						isNegZPartyWall = false;
						break;
					}
				}

				for(int windowIndex = 0; windowIndex < buildingBp.Floors[floorIndex].Windows.Count; windowIndex++)
				{
					if(buildingBp.Floors[floorIndex].Windows[windowIndex].End.z == buildingBp.Floors[floorIndex].Windows[windowIndex].Start.z // Ensures that the plane is the same
						&& buildingBp.Floors[floorIndex].Windows[windowIndex].Start.z == negativeZSide)
					{
						isNegZPartyWall = false;
						break;
					}
				}

				if(isNegZPartyWall == false)
					break;
			}

			float positiveZSide =  allBounds.center.z + allBounds.extents.z;
			bool isPosZPartyWall = true;

			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				for(int doorIndex = 0; doorIndex < buildingBp.Floors[floorIndex].Doors.Count; doorIndex++)
				{
					if(buildingBp.Floors[floorIndex].Doors[doorIndex].End.z == buildingBp.Floors[floorIndex].Doors[doorIndex].Start.z // Ensures that the plane is the same
						&& buildingBp.Floors[floorIndex].Doors[doorIndex].Start.z == positiveZSide)
					{
						isPosZPartyWall = false;
						break;
					}
				}

				for(int windowIndex = 0; windowIndex < buildingBp.Floors[floorIndex].Windows.Count; windowIndex++)
				{
					if(buildingBp.Floors[floorIndex].Windows[windowIndex].End.z == buildingBp.Floors[floorIndex].Windows[windowIndex].Start.z // Ensures that the plane is the same
						&& buildingBp.Floors[floorIndex].Windows[windowIndex].Start.z == positiveZSide)
					{
						isPosZPartyWall = false;
						break;
					}
				}

				if(isPosZPartyWall == false)
					break;
			}

			buildingBp.PartyWalls.Clear();

			if(isNegXPartyWall)
				buildingBp.PartyWalls.Add(new PartyWall(new Vector3(negativeXSide, 0, 0), Vector3.right));

			if(isPosXPartyWall)
				buildingBp.PartyWalls.Add(new PartyWall(new Vector3(positiveXSide, 0, 0), Vector3.right));

			if(isNegZPartyWall)
				buildingBp.PartyWalls.Add(new PartyWall(new Vector3(0, 0, negativeZSide), Vector3.forward));

			if(isPosZPartyWall)
				buildingBp.PartyWalls.Add(new PartyWall(new Vector3(0, 0, positiveZSide), Vector3.forward));
		}

		/// <summary>
		/// Run to calculate what planes are party walls. Party walls are walls at the bounds of the building which don't have windows or doors
		/// </summary>
		public static void CalculatePartyWallsOld(BuildingBlueprint buildingBp)
		{
			if(buildingBp.Floors == null || buildingBp.Floors.Count < 1 || buildingBp.Floors[0].RoomBlueprints == null || buildingBp.Floors[0].RoomBlueprints.Count < 1)
				return;
			
			RoomBlueprint roomBp = buildingBp.Floors[0].RoomBlueprints[0];
			if(roomBp.PerimeterWalls == null || roomBp.PerimeterWalls.Count < 3)
				return;
			
			// Find the bounds of ALL the rooms in the entire building
			Bounds allBounds = new Bounds(roomBp.PerimeterWalls[0], Vector3.zero);
			
			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				for(int roomIndex = 0; roomIndex < buildingBp.Floors[floorIndex].RoomBlueprints.Count; roomIndex++)
				{
					for(int wallIndex = 0; wallIndex < buildingBp.Floors[floorIndex].RoomBlueprints[roomIndex].PerimeterWalls.Count; wallIndex++)
					{
						allBounds.Encapsulate(buildingBp.Floors[floorIndex].RoomBlueprints[roomIndex].PerimeterWalls[wallIndex]);
					}
				}
			}
			
			// Calculate negative X side
			float negativeXSide =  allBounds.center.x - allBounds.extents.x;
			bool isNegXPartyWall = true;
			
			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				for(int doorIndex = 0; doorIndex < buildingBp.Floors[floorIndex].Doors.Count; doorIndex++)
				{
					if(buildingBp.Floors[floorIndex].Doors[doorIndex].End.x == buildingBp.Floors[floorIndex].Doors[doorIndex].Start.x // Ensures that the plane is the same
					   && buildingBp.Floors[floorIndex].Doors[doorIndex].Start.x == negativeXSide)
					{
						isNegXPartyWall = false;
						break;
					}
				}
				
				for(int windowIndex = 0; windowIndex < buildingBp.Floors[floorIndex].Windows.Count; windowIndex++)
				{
					if(buildingBp.Floors[floorIndex].Windows[windowIndex].End.x == buildingBp.Floors[floorIndex].Windows[windowIndex].Start.x // Ensures that the plane is the same
					   && buildingBp.Floors[floorIndex].Windows[windowIndex].Start.x == negativeXSide)
					{
						isNegXPartyWall = false;
						break;
					}
				}
				
				if(isNegXPartyWall == false)
					break;
			}
			
			float positiveXSide =  allBounds.center.x + allBounds.extents.x;
			bool isPosXPartyWall = true;
			
			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				for(int doorIndex = 0; doorIndex < buildingBp.Floors[floorIndex].Doors.Count; doorIndex++)
				{
					if(buildingBp.Floors[floorIndex].Doors[doorIndex].End.x == buildingBp.Floors[floorIndex].Doors[doorIndex].Start.x // Ensures that the plane is the same
					   && buildingBp.Floors[floorIndex].Doors[doorIndex].Start.x == positiveXSide)
					{
						isPosXPartyWall = false;
						break;
					}
				}
				
				for(int windowIndex = 0; windowIndex < buildingBp.Floors[floorIndex].Windows.Count; windowIndex++)
				{
					if(buildingBp.Floors[floorIndex].Windows[windowIndex].End.x == buildingBp.Floors[floorIndex].Windows[windowIndex].Start.x // Ensures that the plane is the same
					   && buildingBp.Floors[floorIndex].Windows[windowIndex].Start.x == positiveXSide)
					{
						isPosXPartyWall = false;
						break;
					}
				}
				
				if(isPosXPartyWall == false)
					break;
			}
			
			float negativeZSide =  allBounds.center.z - allBounds.extents.z;
			bool isNegZPartyWall = true;
			
			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				for(int doorIndex = 0; doorIndex < buildingBp.Floors[floorIndex].Doors.Count; doorIndex++)
				{
					if(buildingBp.Floors[floorIndex].Doors[doorIndex].End.z == buildingBp.Floors[floorIndex].Doors[doorIndex].Start.z // Ensures that the plane is the same
					   && buildingBp.Floors[floorIndex].Doors[doorIndex].Start.z == negativeZSide)
					{
						isNegZPartyWall = false;
						break;
					}
				}
				
				for(int windowIndex = 0; windowIndex < buildingBp.Floors[floorIndex].Windows.Count; windowIndex++)
				{
					if(buildingBp.Floors[floorIndex].Windows[windowIndex].End.z == buildingBp.Floors[floorIndex].Windows[windowIndex].Start.z // Ensures that the plane is the same
					   && buildingBp.Floors[floorIndex].Windows[windowIndex].Start.z == negativeZSide)
					{
						isNegZPartyWall = false;
						break;
					}
				}
				
				if(isNegZPartyWall == false)
					break;
			}
			
			float positiveZSide =  allBounds.center.z + allBounds.extents.z;
			bool isPosZPartyWall = true;
			
			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				for(int doorIndex = 0; doorIndex < buildingBp.Floors[floorIndex].Doors.Count; doorIndex++)
				{
					if(buildingBp.Floors[floorIndex].Doors[doorIndex].End.z == buildingBp.Floors[floorIndex].Doors[doorIndex].Start.z // Ensures that the plane is the same
					   && buildingBp.Floors[floorIndex].Doors[doorIndex].Start.z == positiveZSide)
					{
						isPosZPartyWall = false;
						break;
					}
				}
				
				for(int windowIndex = 0; windowIndex < buildingBp.Floors[floorIndex].Windows.Count; windowIndex++)
				{
					if(buildingBp.Floors[floorIndex].Windows[windowIndex].End.z == buildingBp.Floors[floorIndex].Windows[windowIndex].Start.z // Ensures that the plane is the same
					   && buildingBp.Floors[floorIndex].Windows[windowIndex].Start.z == positiveZSide)
					{
						isPosZPartyWall = false;
						break;
					}
				}
				
				if(isPosZPartyWall == false)
					break;
			}
			
			buildingBp.XPartyWalls.Clear();
			buildingBp.ZPartyWalls.Clear();
			
			// Adds all the party walls we have on the building
			if(isNegXPartyWall)
				buildingBp.XPartyWalls.Add(negativeXSide);
			
			if(isPosXPartyWall)
				buildingBp.XPartyWalls.Add(positiveXSide);
			
			if(isNegZPartyWall)
				buildingBp.ZPartyWalls.Add(negativeZSide);
			
			if(isPosZPartyWall)
				buildingBp.ZPartyWalls.Add(positiveZSide);
		}
	}
}