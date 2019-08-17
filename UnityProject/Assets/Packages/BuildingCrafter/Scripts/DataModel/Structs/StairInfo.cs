using UnityEngine;
using System.Collections;

namespace BuildingCrafter
{

	[System.Serializable]
	public struct StairInfo
	{
	//	public WindowTypeEnum WindowType;
		public Vector3 Start;
		public Vector3 End;
		public int StairsWidth; // NOTE: Must always be even
		
		public StairInfo (Vector3 start, Vector3 end)
		{
			StairsWidth = 2;
			Start = start;
			End = end;
		}

		public static bool operator ==(StairInfo a, StairInfo b)
		{
			// If both are null, or both are same instance, return true.
			if (System.Object.ReferenceEquals(a, b))
				return true;
			
			// If one is null, but not both, return false.
			if (((object)a == null) || ((object)b == null))
				return false;
			
			if(a.StairsWidth != b.StairsWidth)
				return false;
			
			if(Vector3.Equals(a.Start, b.Start) == false || Vector3.Equals(a.End, b.End) == false)
				return false;
			
			return true;
		}
		
		public static bool operator !=(StairInfo a, StairInfo b)
		{
			return !(a == b);
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}
		
		public override bool Equals(object obj)
		{
			if(obj.GetType() != typeof(StairInfo))
				return false;
			
			return (StairInfo)obj == this;
		}
	}
}