using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BuildingCrafter;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ExampleExtension: ScriptableObject, IRoomExtension
{
	#region IRoomExtension implementation

	public void ExecuteUponRoomGeneration (GameObject newRoom, BuildingBlueprint buildingBp, int floorIndex, int roomIndex)
	{
		Debug.Log("Room " + roomIndex + " executed upon generation on floor " + (floorIndex + 1));
	}

	public void ShowPanel (object serializedObject)
	{
		#if UNITY_EDITOR

		EditorGUILayout.LabelField("Custom editor for this Scriptable Object");

		#endif
	}

	#endregion


}
