using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BuildingCrafter
{
	[System.Serializable]
	public struct PartyWall
	{
		public PartyWall(Vector3 startPos, Vector3 normal)
		{
			this.wallPlane = new Plane(normal, startPos);
			this.IsValid = true;
		}

		public bool IsValid;
		public Plane wallPlane;

		public bool IsOnPartyWall(Vector3 start, Vector3 end, float epislon = 0.0000001f)
		{
			if(IsValid == false)
				return false;

			if(Mathf.Abs(wallPlane.GetDistanceToPoint(start)) < epislon && Mathf.Abs(wallPlane.GetDistanceToPoint(end)) < epislon)
				return true;

			return false;
		}

		public static bool operator ==(PartyWall a, PartyWall b)
		{
			if(a.wallPlane.normal != b.wallPlane.normal || a.wallPlane.distance != b.wallPlane.distance)
				return false;
						
			return true;
		}

		public static bool operator !=(PartyWall a, PartyWall b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			return (PartyWall)obj == this;
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode();
		}

		public override string ToString ()
		{
			return string.Format ("Party Wall Distance: " + this.wallPlane.distance + " normal: " + this.wallPlane.normal);
		}
	}
}