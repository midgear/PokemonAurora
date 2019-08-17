using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using LibTessDotNet;

namespace BuildingCrafter
{
	public partial class BuildingCrafterPanel : Editor 
	{
		public BuildingCrafterGenerator Script;

		private SerializedProperty buildingBlueprintGameObject;
		private SerializedProperty buildingName;

		private SerializedObject buildingBlueprintObject;

		private int controlId = 0;

		// For editing windows
		private Rect editRect;
		private string windowTitle = "";

		// For floors and other info
		private Vector3 currentFloorHeight;

		// For deleting floors or duplicating them
		int deleteFloor = -1;
		int duplicateFloor = -1;

		void OnEnable()
		{
			InitializeFieldsForBCPanel();

			Script = (BuildingCrafterGenerator)target;
			buildingBlueprintGameObject = serializedObject.FindProperty("BuildingBlueprint");

			Script.LastSelectedFloor = -1;
			UpdateCurrentFloorHeight(Script.SelectedFloor);

			// Ensures the building crafter is rounded for its position
			Vector3 crafterPosition = Script.gameObject.transform.position;

			Script.gameObject.transform.position = new Vector3(Mathf.Round(crafterPosition.x), 0, Mathf.Round(crafterPosition.z));

			if(Script.BuildingBlueprint != null)
			{
				// Check to see if the Blueprint has old validation errors
				TestForDataModelUpdate(Script.BuildingBlueprint);

				Script.LastSelectedFloor = -1;
				UpdateCurrentFloorHeight(Script.SelectedFloor);

				DrawNewFloor();

				BCGenerator.CleanNullAndShortPerimeterWalls(Script.BuildingBlueprint);
				UpdateRoofOutlines(Script.BuildingBlueprint.Floors.Count * Vector3.up * 3);
				buildingBlueprintGameObject = serializedObject.FindProperty("BuildingBlueprint");

				// TODO: Stop an error from happening when dragging this onto the serialized property
				buildingBlueprintObject = new SerializedObject(buildingBlueprintGameObject.objectReferenceValue);

				// Ensure that old data is updated
				for(int i = 0; i < Script.BuildingBlueprint.Floors.Count; i++)
				{
					for(int j = 0; j < Script.BuildingBlueprint.Floors[i].Doors.Count; j++)
					{
						var door = Script.BuildingBlueprint.Floors[i].Doors[j];
						
						if(door.Direction == 0)
						{
							door.Direction = 1;
							Script.BuildingBlueprint.Floors[i].Doors[j] = door;
						}
					}
				}

				BCUtils.UpdateBlueprintCentre(Script.BuildingBlueprint, Script.BuildingBlueprint.transform.position);

				// Checks to see if this should be generated on enable
				GenerateOnEnable();

				if(Script.BuildingBlueprint.LiveViewEnabled && Application.isPlaying == false)
				{
					HideFloorsNotInUse(Script.BuildingBlueprint);
				}
			}
			else
			{
				ClearFloorOutlines();
			}
		}

		void OnDisable()
		{

			OnDisableLiveView();
		}

		private static void InitializeFieldsForBCPanel()
		{
			GUIUtility.GetControlID(FocusType.Passive);

			roomEditing = new Texture2D[] 
			{ Resources.Load<Texture2D>("BCIcons-plus"), 
				Resources.Load<Texture2D>("BCIcons-minus"),
				Resources.Load<Texture2D>("BCIcons-move-wall"), 
				Resources.Load<Texture2D>("BCIcons-plus-wall"), 
				Resources.Load<Texture2D>("BCIcons-gear")};

			doorEditing = new Texture2D[] 
			{ Resources.Load<Texture2D>("BCIcons-plus"), 
				Resources.Load<Texture2D>("BCIcons-minus"),
				Resources.Load<Texture2D>("BCIcons-eyedropper"),
				Resources.Load<Texture2D>("BCIcons-gear")};

			windowEditing = new Texture2D[] 
			{ Resources.Load<Texture2D>("BCIcons-plus"), 
				Resources.Load<Texture2D>("BCIcons-minus"),
				Resources.Load<Texture2D>("BCIcons-eyedropper"),
				Resources.Load<Texture2D>("BCIcons-gear")};

			stairEditing = new Texture2D[] 
			{ Resources.Load<Texture2D>("BCIcons-plus"), 
				Resources.Load<Texture2D>("BCIcons-minus")};

			yardEditing = new Texture2D[] 
			{ Resources.Load<Texture2D>("BCIcons-plus"), 
				Resources.Load<Texture2D>("BCIcons-minus"),
				Resources.Load<Texture2D>("BCIcons-gear")};

			roofEditing = new Texture2D[] {
				Resources.Load<Texture2D>("BCIcons-plus"),
				Resources.Load<Texture2D>("BCIcons-minus"),
				Resources.Load<Texture2D>("BCIcons-gear")};

			trashIcon = Resources.Load<Texture2D>("BCIcons-trash");
			copyIcon = Resources.Load<Texture2D>("BCIcons-copy");
		}

		public void TestForDataModelUpdate(BuildingBlueprint buildingBp)
		{
			bool windowsNeedUpdating = BCValidator.AreWindowsFromPreBC0p8(buildingBp);

			if(windowsNeedUpdating)
			{
				string title = "Windows are out of date";
				string message = "Prior to v0.8, windows did not store their type in flags. This building needs to be updated. Would you like to update?";

				if(EditorUtility.DisplayDialog(title, message, "Update", "Don't Update"))
				{
					BCValidator.UpdateWindowsFromOlderVersion(buildingBp);
					EditorUtility.DisplayDialog("Windows are updated", "Certain buildings may not update exactly right so do a quick glance over all your buildings. You can undo the update you just made.", "Got it");
				}
			}

			bool doorsNeedUpdating = BCValidator.AreDoorFromPreBC0p8(buildingBp);
			
			if(doorsNeedUpdating)
			{
				string title = "Doors are out of date";
				string message = "Prior to v0.8, doors did not store their type in flags. This building needs to be updated. Would you like to update?";
				
				if(EditorUtility.DisplayDialog(title, message, "Update", "Don't Update"))
				{
					BCValidator.UpdateDoorsFromOlderVersion(buildingBp);
					EditorUtility.DisplayDialog("Doors are updated", "Certain buildings may not update exactly right so do a quick glance over all your buildings. You can undo the update you just made.", "Got it");
				}
			}
		}

		/// <summary>
		/// Draws whatever floor being currently viewed
		/// </summary>
		private void DrawNewFloor()
		{
			DrawFloor(Script.CurrentFloor);
		}

		void UpdateCurrentFloorHeight(int selection)
		{
			if(selection == -1 || Script == null || Script.BuildingBlueprint == null)
			{
				Script.CurrentFloor = -1;
			}
			else
			{
				Script.CurrentFloor = GetFloorIndex(Script.SelectedFloor); // Sets up the current floor to edit and stuff
				currentFloorHeight = Vector3.up * Script.CurrentFloor * 3 + Script.BuildingBlueprint.BlueprintGroundHeight;
			}

			Script.LastSelectedFloor = Script.SelectedFloor;
		}

		private Vector3[] GetHighlightOpening(Vector3 start, Vector3 end, float bottom, float top)
		{
			Vector3 direction = (end - start).normalized;
			Vector3 rightAngleDirection = direction;
			if(direction.x == 0)
				rightAngleDirection = new Vector3(1, 0, 0);
			if(direction.z == 0)
				rightAngleDirection = new Vector3(0, 0, 1);

			Vector3 pStartLeft = start + rightAngleDirection * 0.1f;
			Vector3 pStartRight = start - rightAngleDirection * 0.1f;
			Vector3 pEndLeft = end + rightAngleDirection * 0.1f;
			Vector3 pEndRight = end - rightAngleDirection * 0.1f;

			Vector3 b = bottom * Vector3.up + currentFloorHeight;
			Vector3 t = top * Vector3.up + currentFloorHeight;

			Vector3[] newOutline = new Vector3[16]
			{
				pStartLeft + b, pStartRight + b, pStartRight + t, pStartLeft + t,
				pStartLeft + b, pStartLeft + t, pEndLeft + t, pEndLeft + b,
				pStartRight + b, pStartRight + t, pEndRight + t, pEndRight + b,
				pEndLeft + b, pEndRight + b, pEndRight + t, pEndLeft + t,
			};

			return newOutline;
		}

		private void SetHighlightOpening(Vector3 start, Vector3 end, float bottom, float top)
		{
			highlightedOpening = GetHighlightOpening(start, end, bottom, top);
		}

		private void SetHighlightOpening(Color color, Vector3 start, Vector3 end, float bottom, float top)
		{
			highlightOpeningColor = color;
			highlightedOpening = GetHighlightOpening(start, end, bottom, top);
		}

		private void ClearHighlightingOpening()
		{
			highlightedOpening = new Vector3[0];
		}

		private static bool BlueprintContainsFloor(BuildingBlueprint buildingBp, FloorBlueprint floorBp)
		{
			// Test to see if the currently selected floor is within the building. If it is not, then update
			for(int i = 0; i < buildingBp.Floors.Count; i++)
			{
				if(buildingBp.Floors[i] == floorBp)
					return true;
			}

			return false;
		}
	}
}