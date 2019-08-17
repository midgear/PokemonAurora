using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{

	public static partial class BCUtils 
	{
		/// <summary>
		/// Gets the floor from blueprint.
		/// </summary>
		/// <returns>The floor from blueprint, or NULL if no floor exists</returns>
		public static FloorBlueprint GetFloorFromBlueprint(BuildingBlueprint buildingBp, int floor)
		{
			if(floor > buildingBp.Floors.Count - 1 || floor < 0)
				return null;

			return buildingBp.Floors[floor];
		}

		/// <summary>
		/// Gets a room from blueprint.
		/// </summary>
		/// <returns>The room from blueprint, or NULL if room does not exist</returns>
		public static RoomBlueprint GetRoomFromBlueprint(BuildingBlueprint buildingBp, int floor, int roomIndex)
		{
			if(floor > buildingBp.Floors.Count - 1 || floor < 0)
				return null;

			if(roomIndex > buildingBp.Floors[floor].RoomBlueprints.Count - 1  || roomIndex < 0)
				return null;

			return buildingBp.Floors[floor].RoomBlueprints[roomIndex];
		}

		public static RoomBlueprint GetRoomFromPoint(Vector3 precisePoint, BuildingBlueprint buildingBp, int floor, out int roomIndex)
		{
			roomIndex = -1;

			if(floor >= buildingBp.Floors.Count)
				return null;

			for(int i = 0; i < buildingBp.Floors[floor].RoomBlueprints.Count; i++)
			{
				RoomBlueprint room = buildingBp.Floors[floor].RoomBlueprints[i];
				if(BCUtils.PointInPolygonXZ(precisePoint, room.PerimeterWalls.ToArray<Vector3>()))
				{
					roomIndex = i;
					return buildingBp.Floors[floor].RoomBlueprints[i];
				}
			}

			return null;
		}

		public static RoomBlueprint GetRoomFromPoint(Vector3 precisePoint, FloorBlueprint floorBp)
		{
			int junkIndex;
			return GetRoomFromPoint(precisePoint, floorBp, out junkIndex);
		}

		public static RoomBlueprint GetRoomFromPoint(Vector3 precisePoint, FloorBlueprint floorBp, out int roomIndex)
		{
			roomIndex = -1;
			for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
			{
				RoomBlueprint room = floorBp.RoomBlueprints[i];
				if(BCUtils.PointInPolygonXZ(precisePoint, room.PerimeterWalls.ToArray<Vector3>()))
				{
					roomIndex = i;
					return floorBp.RoomBlueprints[i];
				}
			}
			
			return null;
		}
	}
}
