using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BuildingCrafter
{
	[System.Serializable]
	public class RoomBlueprint
	{
		public RoomBlueprint()
		{
			
		}

		public RoomBlueprint(List<Vector3> walls)
		{
			this.SetPerimeterWalls(walls);
		}

		/// <summary>
		/// SETTER USED ONLY FOR THE SERIALIZER
		/// </summary>
		public List<Vector3> PerimeterWalls 
		{ 
			get { return perimeterWalls; } 
			set { perimeterWalls = value; } 
		}
		[SerializeField]
		private List<Vector3> perimeterWalls;

		// Specific Info for this room
		public RoomType RoomType = RoomType.Generic;
		public float CeilingHeight = 2.9f;
		public float Outset = -0.1f;

		/// <summary>
		/// The Room Style specific to this room, overrides the RoomType.
		/// </summary>
		public RoomStyle OverrideRoomStyle = null;

		public string SpecificRoomStyle
		{
			get 
			{
				if(OverrideRoomStyle != null)
					return OverrideRoomStyle.name; 
				return null;
			}
			set { SetOverridenStyle(value); }
		}

		/// <summary>
		/// Gets the plain wall info WITHOUT getting it ready for generation
		/// </summary>
		/// <returns>The wall infos.</returns>
		public WallInformation[] GetWallInfos()
		{
			WallInformation[] wallInfos = new WallInformation[this.perimeterWalls.Count - 1];
			for(int i = 0; i < perimeterWalls.Count - 1; i++)
			{
				wallInfos[i].Start = perimeterWalls[i];
				wallInfos[i].End = perimeterWalls[i + 1];
				wallInfos[i].Outset = this.Outset;
			}

			return wallInfos;
		}

		// TODO: CeilingSpaceCutout
		// TODO: InteriorWallCutouts (for rooms like a donut)

		public bool SetPerimeterWalls(List<Vector3> newWalls)
		{
			// ensures empty walls are not set to anything
			if(newWalls.Count == 0 || newWalls.Count == 1)
				return false;

			// Ensure the 0 and count - 1 match up. If not return false
			if(newWalls[0] != newWalls[newWalls.Count - 1])
				return false;
			
			perimeterWalls = newWalls;
			
			return true;
		}

		/// <summary>
		/// Used to set the overriden style by string
		/// </summary>
		/// <param name="name">Name.</param>
		private void SetOverridenStyle(string name)
		{
			if(name == null || name == "")
				return;

			RoomStyle roomStyle = Resources.Load<RoomStyle>(@"RoomStyles/" + name) as RoomStyle;
			if(roomStyle == null)
			{
				Debug.LogError("A room type style wasn't loaded: " + name);
				return;
			}
				
			this.OverrideRoomStyle = roomStyle;
		}

		public static bool operator ==(RoomBlueprint a, RoomBlueprint b)
		{
			// If both are null, or both are same instance, return true.
			if (System.Object.ReferenceEquals(a, b))
				return true;
			
			// If one is null, but not both, return false.
			if (((object)a == null) || ((object)b == null))
				return false;
			
			if(a.CeilingHeight != b.CeilingHeight
			   || a.RoomType != b.RoomType
			   || a.OverrideRoomStyle != b.OverrideRoomStyle
			   || a.perimeterWalls.Count != b.perimeterWalls.Count)
				return false;

			for(int i = 0; i < a.perimeterWalls.Count; i++)
			{
				if(Vector3.Equals(a.perimeterWalls[i], b.perimeterWalls[i]) == false)
				   return false;
			}

			return true;
		}
		
		public static bool operator !=(RoomBlueprint a, RoomBlueprint b)
		{
			return !(a == b);
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return this == (RoomBlueprint)obj;
		}
	}
}
