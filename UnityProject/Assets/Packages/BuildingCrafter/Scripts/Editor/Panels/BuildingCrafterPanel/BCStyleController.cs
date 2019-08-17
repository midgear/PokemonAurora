using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{

	public partial class BuildingCrafterPanel : Editor 
	{
		private static float buttonHeight = 14f;

		List<RoomStyle> overriddenRoomStyles = new List<RoomStyle>();

		private void DisplayStyleEditor()
		{
			BuildingBlueprint buildingBp = Script.BuildingBlueprint;
	
			// If the system has changed whatever editing state is going on, then update
			if(Script.FloorEditType != Script.LastFloorEditType)
			{
				buildingBlueprintGameObject = serializedObject.FindProperty("BuildingBlueprint");
				buildingBlueprintObject = new SerializedObject(buildingBlueprintGameObject.objectReferenceValue);
	
				UpdateCustomRooms(buildingBp); 

				DrawAllGreyFloorOutlines();

				Script.LastFloorEditType = Script.FloorEditType;
			}

			if(overriddenRoomStyles.Count > 0)
			{
				DisplayOverriddenRoomStyles(overriddenRoomStyles, Script.BuildingBlueprint.BuildingStyle);

				if(GUI.changed)
				{
					for(int f = 0; f < buildingBp.Floors.Count; f++)
					{
						FloorBlueprint floor = buildingBp.Floors[f];
						
						for(int r = 0; r < floor.RoomBlueprints.Count; r++)
						{
							RoomBlueprint room = floor.RoomBlueprints[r];
							
							if(room.OverrideRoomStyle != null)
							{
								EditorUtility.SetDirty(room.OverrideRoomStyle);
							}
						}
					}
				}

				EditorGUILayout.Separator();
				EditorGUILayout.Separator();
			}


			GUILayout.Label("Edit Global Building Style", EditorStyles.boldLabel);

			GUILayout.BeginHorizontal();
			BuildingStyle newBuildingStyle = (BuildingStyle)EditorGUILayout.ObjectField("Building Style", buildingBp.BuildingStyle, typeof(BuildingStyle), false);

			if(newBuildingStyle != null && buildingBp.BuildingStyle == null
			   || buildingBp.BuildingStyle != null && newBuildingStyle == null
			   || newBuildingStyle != buildingBp.BuildingStyle)
			{
				Undo.RegisterCompleteObjectUndo(buildingBp, "Modify Building Style");
				buildingBp.BuildingStyle = newBuildingStyle;
				EditorUtility.SetDirty(buildingBp);
			}


			if(buildingBp.BuildingStyle != null)
			{
				if(GUILayout.Button("X", GUILayout.Width(24), GUILayout.Height(buttonHeight)))
				{
					Undo.RegisterCompleteObjectUndo(buildingBp, "Remove Building Style");
					buildingBp.BuildingStyle = null;
					EditorUtility.SetDirty(buildingBp);
				}
			}
			else
			{
				// Show a button to create a new building style
				if(GUILayout.Button("Create", GUILayout.Width(55), GUILayout.Height(buttonHeight)))
				{
					BuildingStyle buildStyle = CreateBuildStyleAsset.CreateBuildingStyle();
					if(buildStyle != null)
					{
						Undo.RegisterCompleteObjectUndo(buildingBp, "Add Style to Blueprint");
						buildingBp.BuildingStyle = buildStyle;
						EditorUtility.SetDirty(buildingBp);
					}
				}
			}
			GUILayout.EndHorizontal();


			// Display the extra building styles that are in this building


			GUILayout.Label("NOTE: You are editing the global settings for this building style", EditorStyles.miniBoldLabel);

			if(buildingBp.BuildingStyle != null)
			{
				BuildingStylePanel.DisplayBuildingStyle(buildingBp.BuildingStyle);

			}

				
		}

		private void UpdateCustomRooms(BuildingBlueprint buildingBp)
		{
			overriddenRoomStyles.Clear();

			for(int f = 0; f < buildingBp.Floors.Count; f++)
			{
				FloorBlueprint floor = buildingBp.Floors[f];

				for(int r = 0; r < floor.RoomBlueprints.Count; r++)
				{
					RoomBlueprint room = floor.RoomBlueprints[r];

					if(room.OverrideRoomStyle != null)
					{
						if(overriddenRoomStyles.Contains(room.OverrideRoomStyle) == false)
						{
							overriddenRoomStyles.Add(room.OverrideRoomStyle);
						}
					}
				}
			}
		}

		private static void DisplayOverriddenRoomStyles(List<RoomStyle> roomStyles, BuildingStyle style)
		{
			GUILayout.Label("Custom Room Styles", EditorStyles.boldLabel);
			GUILayout.Label("Edit the styles attached to specific rooms in this blueprint", EditorStyles.miniBoldLabel);

			BuildingStylePanel.DisplayRoomStyle(ref roomStyles, style, false);

		}
	}
}
