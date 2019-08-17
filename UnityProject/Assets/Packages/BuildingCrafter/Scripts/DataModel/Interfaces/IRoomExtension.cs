using UnityEngine;
using System.Collections;

namespace BuildingCrafter
{

	public interface IRoomExtension
	{

		/// <summary>
		/// Executes when the 
		/// </summary>
		void ExecuteUponRoomGeneration(GameObject newRoom, BuildingBlueprint buildingBp, int floorIndex, int roomIndex);

		/// <summary>
		/// Used to show a panel for the editing of a scriptable object
		/// </summary>
		void ShowPanel(object serializedObject);
	}
}