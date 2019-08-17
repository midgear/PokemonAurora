using UnityEngine;
using System.Collections;

namespace BuildingCrafter
{
	public static partial class BCValidator
	{
		/// <summary>
		/// Confirms that an array has a first and last same point, has more than 3 points
		/// </summary>
		public static bool IsVector3ArrayValid(Vector3[] vector3Array)
		{
			if(vector3Array == null)
				return false;

			// Check for validity of poly's
			if(vector3Array.Length < 4)
				return false;

			float epsilon = 0.0001f;
			
			// Confirms that both inside and outside polys are loops
			if((vector3Array[0] - vector3Array[vector3Array.Length - 1]).sqrMagnitude > epsilon)
				return false;

			return true;
		}
	}
}