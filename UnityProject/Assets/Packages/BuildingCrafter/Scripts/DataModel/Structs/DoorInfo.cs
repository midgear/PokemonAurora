using UnityEngine;
using System.Collections;

namespace BuildingCrafter
{
	[System.Serializable]
	public struct DoorInfo
	{
		// ANYTHING ADDED HERE MUST BE ADDED TO THE != and the ==
		public DoorTypeEnum DoorType;
		public Vector3 Start;
		public Vector3 End;
		public int Direction;
		public bool isLockable; // NOTE: Mistakenly set this to camel case, should be pascal case
		public bool isAutolocking;
		public bool isFireDoor;
		public bool IsForcedPlain;

		public bool IsStartOpen;
		public float StartOpenAngle; // The door open angle if 0 it should default to 90 degrees
		/// <summary>
		/// value between -85 and 90. 0 means the door opens to 90 degrees
		/// </summary>
		public float MaxOpenAngleOffset; // how far the door can go, should max out at 180 
		public float MaxOpeningAngle { get { return MaxOpenAngleOffset + 90; } }

		// Only appears true if the door is 2 meters wide
		public bool IsDoubleDoor
		{
			get
			{
				if((Start - End).magnitude == 2 && (this.DoorType == DoorTypeEnum.Standard || this.DoorType == DoorTypeEnum.Heavy))
					return true;

				return false;
			}
		}
		
		public DoorInfo(Vector3 start, Vector3 end)
		{
			Start = start;
			End = end;
			Direction = 1;
			DoorType = DoorTypeEnum.Standard;
			isLockable = false;
			isAutolocking = false;
			isFireDoor = false;
			IsForcedPlain = false;
			IsStartOpen = false;
			StartOpenAngle = 0;
			MaxOpenAngleOffset = 0;
		}

		public DoorInfo(Vector3 start, Vector3 end, int direction) : this (start, end)
		{
			Direction = direction;
			DoorType = DoorTypeEnum.Standard;
		}

		public float DoorHeight
		{
			get
			{			
				float height = 2;

				switch(this.DoorType)
				{
				case DoorTypeEnum.Standard:
				case DoorTypeEnum.Heavy:
				case DoorTypeEnum.Open:
				case DoorTypeEnum.Closet:
				case DoorTypeEnum.SkinnyOpen:
					height = 2f;
					break;
				case DoorTypeEnum.TallOpen:
				case DoorTypeEnum.DoorToRoof:
					height = 2.5f;
					break;
				}

				return height;
			}
		}

		public static bool operator ==(DoorInfo a, DoorInfo b)
		{
			// If both are null, or both are same instance, return true.
			if (System.Object.ReferenceEquals(a, b))
				return true;
			
			// If one is null, but not both, return false.
			if (((object)a == null) || ((object)b == null))
				return false;
			
			if(a.Direction != b.Direction
				|| a.DoorType != b.DoorType
				|| a.isAutolocking != b.isAutolocking
				|| a.isLockable != b.isLockable
				|| a.isAutolocking != b.isAutolocking
				|| a.isFireDoor != b.isFireDoor
				|| a.IsForcedPlain != b.IsForcedPlain
				|| a.IsStartOpen != b.IsStartOpen
				|| a.StartOpenAngle != b.StartOpenAngle
				|| a.MaxOpenAngleOffset != b.MaxOpenAngleOffset
			   )
				return false;

			if(a.DoorType != b.DoorType)
				return false;

			if(Vector3.Equals(a.Start, b.Start) == false || Vector3.Equals(a.End, b.End) == false)
				return false;

			return true;
		}

		public static bool operator !=(DoorInfo a, DoorInfo b)
		{
			if(a == b)
				return false;

			return true;
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if(obj.GetType() != typeof(DoorInfo))
				return false;

			return (DoorInfo)obj == this;
		}
	}
}