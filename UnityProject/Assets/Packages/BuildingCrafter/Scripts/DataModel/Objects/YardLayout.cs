using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BuildingCrafter
{
	[System.Serializable]
	public class YardLayout
	{

		public YardTypeEmum YardType = YardTypeEmum.Generic;

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

		public static bool operator ==(YardLayout a, YardLayout b)
		{
			// If both are null, or both are same instance, return true.
			if (System.Object.ReferenceEquals(a, b))
				return true;
			
			// If one is null, but not both, return false.
			if (((object)a == null) || ((object)b == null))
				return false;
			
			if(a.YardType != b.YardType
			   || a.perimeterWalls.Count != b.perimeterWalls.Count)
				return false;
			
			for(int i = 0; i < a.perimeterWalls.Count; i++)
			{
				if(Vector3.Equals(a.perimeterWalls[i], b.perimeterWalls[i]) == false)
					return false;
			}
			
			return true;
		}
		
		public static bool operator !=(YardLayout a, YardLayout b)
		{
			return !(a == b);
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}

		public override bool Equals(object obj)
		{
			if(obj as YardLayout == null)
				return false;
			
			return (YardLayout)obj == this;
		}
	}
}
