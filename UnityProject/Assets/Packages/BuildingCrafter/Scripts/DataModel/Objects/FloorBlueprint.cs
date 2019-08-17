using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BuildingCrafter
{
	[System.Serializable]
	public class FloorBlueprint
	{
		// NOTE: If adding anything, add to the == operator below

		// Blueprints for each of the rooms.
		public List<RoomBlueprint> RoomBlueprints = new List<RoomBlueprint>();

		// All the Doors on this floor
		public List<DoorInfo> Doors = new List<DoorInfo>();

		// All the windows on this floor
		public List<WindowInfo> Windows = new List<WindowInfo>();

		// All the stairs on this floor
		public List<StairInfo> Stairs = new List<StairInfo>();

		// Yards
		public List<YardLayout> YardLayouts = new List<YardLayout>();

		// FloorHeight
		public float Height = 3;

		// Elevator Shafts

		// GETTERS

		public static bool operator ==(FloorBlueprint a, FloorBlueprint b)
		{
			// If both are null, or both are same instance, return true.
			if (System.Object.ReferenceEquals(a, b))
				return true;

			// If one is null, but not both, return false.
			if (((object)a == null) || ((object)b == null))
				return false;

			if((a.Doors == null && b.Doors != null)
			   || (b.Doors == null && a.Doors != null))
			   return false;

			if(a.Doors != null && b.Doors != null && a.Doors.Count != b.Doors.Count)
			   return false;
			
			if((a.Windows == null && b.Windows != null)
			   || (b.Windows == null && a.Windows != null))
				return false;
			
			if(a.Windows != null && b.Windows != null && a.Windows.Count != b.Windows.Count)
				return false;

			if((a.RoomBlueprints == null && b.RoomBlueprints != null)
			   || (b.RoomBlueprints == null && a.RoomBlueprints != null))
				return false;
			
			if(a.RoomBlueprints != null && b.RoomBlueprints != null && a.RoomBlueprints.Count != b.RoomBlueprints.Count)
				return false;

			if((a.Stairs == null && b.Stairs != null)
			   || (b.Stairs == null && a.Stairs != null))
				return false;
			
			if(a.Stairs != null && b.Stairs != null && a.Stairs.Count != b.Stairs.Count)
				return false;

			if((a.YardLayouts == null && b.YardLayouts != null)
			   || (b.YardLayouts == null && a.YardLayouts != null))
				return false;
			
			if(a.YardLayouts != null && b.YardLayouts != null && a.YardLayouts.Count != b.YardLayouts.Count)
				return false;

			for(int i = 0; i < a.Doors.Count; i++)
			{
				if(a.Doors[i] != b.Doors[i])
					return false;
			}

			for(int i = 0; i < a.Windows.Count; i++)
			{
				if(a.Windows[i] != b.Windows[i])
					return false;
			}

			for(int i = 0; i < a.Stairs.Count; i++)
			{
				if(a.Stairs[i] != b.Stairs[i])
					return false;
			}

			for(int i = 0; i < a.RoomBlueprints.Count; i++)
			{
				if(a.RoomBlueprints[i] != b.RoomBlueprints[i])
					return false;
			}

			for(int i = 0; i < a.YardLayouts.Count; i++)
			{
				if(a.YardLayouts[i] != b.YardLayouts[i])
					return false;
			}
			
			return true;
		}

		public static bool operator !=(FloorBlueprint a, FloorBlueprint b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			return (FloorBlueprint)obj == this;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}