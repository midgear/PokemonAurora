using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BuildingCrafter
{
	public class RoomStyle : ScriptableObject 
	{
		/// <summary>
		/// A list of all the coverings that are possible in this room
		/// </summary>
		public List<RoomMaterials> RoomMaterials = new List<RoomMaterials>();

		public List<ScriptableObject> RoomExtenders = new List<ScriptableObject>();
	}
}