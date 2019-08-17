using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{
	public static partial class BCUtils 
	{
		/// <summary>
		/// Returns what the next index is for a wall, handy for loop arounds
		/// </summary>
		public static int GetIndexAtPlus(int startIndex, int indexAdd, Vector3[] wall)
		{
			if(wall == null || wall.Length < 2)
				return -1;

			int index = startIndex + indexAdd;

			bool wallLoops = true;

			// Is the wall a loop or not
	//		if((wall[0] - wall[wall.Length - 1]).sqrMagnitude < 0.001f)
	//			wallLoops = true;

			if(wallLoops == false)
			{
				Debug.LogError("Wall does not loop");
				return -1;
			}

			if(wallLoops)
			{
				// Deal with loop forwards

				if(index >= wall.Length - 1) // adds an index to skip over the closing point
				{
					index++;
					index -= wall.Length;
					return index;
				}

				// Deal with loop backwards
				if(index < 0)
				{
					index--;
					index += wall.Length;
					return index;
				}

				return index;
			}

			return -1;
		}

		public static int GetIndexAtPlus(int startIndex, int indexAdd, List<Vector3> wall)
		{
			return GetIndexAtPlus(startIndex, indexAdd, wall.ToArray<Vector3>());
		}

//		public static int GetNextIndex(int startIndex, Vector3[] wall)
//		{
//			return GetIndexAtPlus(startIndex, 1, wall);
//		}
	}
}
