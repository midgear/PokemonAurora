using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BuildingCrafter;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CenterVaseExtension: ScriptableObject, IRoomExtension
{

	public GameObject vasePrefab;

	#region IRoomExtension implementation

	public void ExecuteUponRoomGeneration (GameObject newRoom, BuildingBlueprint buildingBp, int floorIndex, int roomIndex)
	{
		// Find the center of the new room.

		MeshFilter filter = newRoom.GetComponent<MeshFilter>();
		Bounds bounds = filter.sharedMesh.bounds;

		if(vasePrefab != null)
		{
			GameObject vase = GameObject.Instantiate(vasePrefab, bounds.center + newRoom.transform.position, Quaternion.identity) as GameObject;
			vase.transform.SetParent(newRoom.transform);
			Debug.Log("Generate vase in " + roomIndex + " on floor " + (floorIndex + 1));
		}
		else
			Debug.Log("Vase Prefab is not loaded");
	}

	public void ShowPanel (object serializedObject)
	{
		#if UNITY_EDITOR

		GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField("Vase Prefab", vasePrefab, typeof(GameObject), false);

		if(newPrefab != vasePrefab)
		{
			Undo.RegisterCompleteObjectUndo(this, "Undo Vase Change");
			vasePrefab = newPrefab;
			EditorUtility.SetDirty(this);
		}

		#endif
	}

	#endregion


}
