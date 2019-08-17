using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace BuildingCrafter
{

	[CustomEditor(typeof(RoomStyle))]
	public class RoomStylePanel : Editor
	{
		private static float wideButtonWidth = 30f;
		private static float superWideButton = 50f;
		private static float buttonHeight = 40f;
		private static float shortButton = 14f;

		RoomStyle Script;

		void OnEnable()
		{
			Script = (RoomStyle)target;
		}

		public override void OnInspectorGUI ()
		{
			if(Script == null)
				this.OnEnable();

			serializedObject.Update();

			GUILayout.Label("Determine the style of a single room");

			DisplayRoomStyleAndFiller(Script);

			if(GUI.changed)
				EditorUtility.SetDirty(Script);

			serializedObject.ApplyModifiedProperties();
		}

		public static void DisplayRoomStyleAndFiller(RoomStyle roomStyle)
		{
			DisplayRoomStyle(roomStyle.RoomMaterials, "Materials", roomStyle);
			DisplayRoomExtenders(roomStyle, "Room Extenders");
		}

		private static void DisplayRoomStyle(List<RoomMaterials> roomMaterials, string name, RoomStyle undoableRoomStyle)
		{
			GUILayout.Label(name, EditorStyles.helpBox);
			
			for(int i = 0; i < roomMaterials.Count; i++)
			{
				RoomMaterials roomMat = roomMaterials[i];

				GUILayout.BeginHorizontal();

				GUILayout.BeginVertical();

				Material newCeiling = (Material)EditorGUILayout.ObjectField("Ceiling", roomMat.CeilingMaterial, typeof(Material), false);
				Material newWall = (Material)EditorGUILayout.ObjectField("Walls", roomMat.WallMaterial, typeof(Material), false);
				Material newFloor = (Material)EditorGUILayout.ObjectField("Floor", roomMat.FloorMaterial, typeof(Material), false);

				if(newCeiling != roomMaterials[i].CeilingMaterial
				   || newWall != roomMaterials[i].WallMaterial
				   || newFloor != roomMaterials[i].FloorMaterial)
				{
					Undo.RecordObject(undoableRoomStyle, "Room Material Change");
					roomMaterials[i].CeilingMaterial =  newCeiling;
					roomMaterials[i].WallMaterial = newWall;
					roomMaterials[i].FloorMaterial = newFloor;
					EditorUtility.SetDirty(undoableRoomStyle);
				}

				GUILayout.EndVertical();

				if(GUILayout.Button("x", GUILayout.Width(wideButtonWidth), GUILayout.Height(buttonHeight)))
				{
					Undo.RecordObject(undoableRoomStyle, "Delete Materials");
					roomMaterials.RemoveAt(i);
					EditorUtility.SetDirty(undoableRoomStyle);
					return;
				}


				GUILayout.EndHorizontal();
				EditorGUILayout.Separator();
			}
			
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			
			if(GUILayout.Button("+", GUILayout.Width(superWideButton)))
			{
				Undo.RecordObject(undoableRoomStyle, "Add Room Materials");
				if(roomMaterials.Count > 1)
				{
					roomMaterials.Add(new RoomMaterials());
					roomMaterials[roomMaterials.Count-1].CeilingMaterial = roomMaterials[roomMaterials.Count - 2].CeilingMaterial;
					roomMaterials[roomMaterials.Count-1].WallMaterial = roomMaterials[roomMaterials.Count - 2].WallMaterial;
					roomMaterials[roomMaterials.Count-1].FloorMaterial = roomMaterials[roomMaterials.Count - 2].FloorMaterial;
				}
				else
					roomMaterials.Add(new RoomMaterials());

				EditorUtility.SetDirty(undoableRoomStyle);
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

	//	private static void DisplayRoomFillers (ref RoomFiller roomFiller, string roomFillers)
	//	{
	//		GUILayout.Label(roomFillers, EditorStyles.helpBox);
	//		GUILayout.BeginHorizontal();
	//		roomFiller = (RoomFiller)EditorGUILayout.ObjectField("Room Filler", roomFiller, typeof(RoomFiller), false);
	//		if(roomFiller != null)
	//		{
	//			if(GUILayout.Button("x", GUILayout.Width(wideButtonWidth), GUILayout.Height(14)))
	//				roomFiller = null;
	//		}
	//		else
	//		{
	//			// Show a button to create a new building style
	//			if(GUILayout.Button("Create", GUILayout.Width(55), GUILayout.Height(14)))
	//			{
	//				RoomFiller newRoomFiller = CreateBuildStyleAsset.CreateRoomFillerAsset();
	//				if(newRoomFiller != null)
	//					roomFiller = newRoomFiller;
	//			}
	//		}
	//
	//		GUILayout.EndHorizontal();
	//
	//		if(roomFiller != null)
	//		{
	//			roomFiller.ShowPanel();
	//			RoomFillerPanel.DisplayRoomGenerator(roomFiller, foldOuts);
	//			if(GUI.changed)
	//				EditorUtility.SetDirty(roomFiller);
	//		}		
	//	}

		private static void DisplayRoomExtenders (RoomStyle roomStyle, string roomExtenders)
		{
			for(int i = 0; i < roomStyle.RoomExtenders.Count; i++)
			{
				GUILayout.Label("(" + (i + 1).ToString() + ") " + roomExtenders, EditorStyles.helpBox);
				DisplayRoomExtender(roomStyle, i);
			}

			EditorGUILayout.Separator();

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Add Extender"))
			{
				Undo.RecordObject(roomStyle, "Add Extender");
				roomStyle.RoomExtenders.Add(null);
				EditorUtility.SetDirty(roomStyle);
			}
			EditorGUILayout.EndHorizontal();


		}

		private static void DisplayRoomExtender (RoomStyle roomStyle, int extendIndex)
		{
			EditorGUILayout.Separator();
			EditorGUILayout.BeginHorizontal();
			ScriptableObject newSObj = (ScriptableObject)EditorGUILayout.ObjectField("Room Extender", roomStyle.RoomExtenders[extendIndex], typeof(ScriptableObject), false);
			if(GUILayout.Button("x", GUILayout.Width(wideButtonWidth), GUILayout.Height(shortButton)))
			{
				Undo.RecordObject(roomStyle, "Remove Extender");
				roomStyle.RoomExtenders.RemoveAt(extendIndex);
				EditorUtility.SetDirty(roomStyle);
				return;
			}
			EditorGUILayout.EndHorizontal();

			IRoomExtension roomExtend = newSObj as IRoomExtension;
			
			if(roomExtend == null && newSObj != null)
			{
				Debug.LogError(newSObj.name + " does not inherit Interface IRoomExtension, please add this to the Scriptable Object to use as an extension");
				roomStyle.RoomExtenders[extendIndex] = null;
			}

			if(roomExtend != null)
			{
				if(newSObj != null 
				        || (roomStyle.RoomExtenders[extendIndex] != null && newSObj.GetInstanceID() != roomStyle.RoomExtenders[extendIndex].GetInstanceID()))
				{
					Undo.RecordObject(roomStyle, "Add Extender");
					roomStyle.RoomExtenders[extendIndex] = newSObj;
					EditorUtility.SetDirty(roomStyle);
				}

				roomExtend.ShowPanel(newSObj);
			}
		}
	}
}
