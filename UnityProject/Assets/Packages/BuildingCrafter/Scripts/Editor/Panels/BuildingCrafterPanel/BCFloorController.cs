using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace BuildingCrafter
{
	public partial class BuildingCrafterPanel : Editor 
	{
		#region Variables
		// Checkers
		/// <summary> Returns true if the building is new and has no floors within it </summary>
		bool buildingIsEmpty;

		Vector3 lastGridPoint = Vector3.zero;

		// Floor options
		float allCeilingHeight = 2.9f;
		bool highlightGenericRooms = false;

		// Room editing selection boxes
		int roomEditingType = -1;
		int doorEditingType = -1;
		int windowEditingType = -1;
		int stairEditingType = -1;
		int yardEditingType = -1;

		// Tiles of each floor option
		string[] floorsDisplay;
		Texture2D[] duplicateFloorsDisplay;
		Texture2D[] deleteFloorsDisplay;

		int maxButtons = 5;

		// Textures for laying items
		static GUIContent roomEditingLabel = new GUIContent("Rooms", "Add, remove and modify rooms on current floor.\n\n" +
		                                                    "(+) Add new rooms to the current floor\n" +
		                                                    "(-) Delete entire rooms from the current floor\n" +
		                                                    "(<-/->) Move a wall or corner along cartisian plane\n" +
		                                                    "(+/o) Add a point to a wall and expand newly split wall\n" +
		                                                    "(gear) Modify properities of a room on current floor");

		static GUIContent doorEditingLabel = new GUIContent("Doors", "Add doors to the current floor.\n\n" +
		                                                    "(+) Add a new door\n" +
		                                                    "(-) Delete a door\n" +
		                                                    "(gear) Modify the door properties");

		static GUIContent windowEditingLabel = new GUIContent("Windows", "Add windows to the current floor.\n\n" +
		                                                      "(+) Add a new window\n" +
		                                                      "(-) Delete a window\n" +
		                                                      "(gear) Modify the window properties");

		static GUIContent stairsEditingLabel = new GUIContent("Stairs", "Add stairs to the current floor.\n\n" +
		                                                      "(+) Add a new stair\n" +
		                                                      "(-) Delete a stair\n" +
		                                                      "(gear) Modify the window properties");

		static GUIContent yardEditingLabel = new GUIContent("Yards*", "Adds experimental yard sections to only the first floor.\n\n" +
		                                                    "(+) Add a new yard point\n" +
		                                                    "(-) Delete a yard\n" +
		                                                    "(gear) Modify the yard properties");

		// Various button options
		static Texture2D[] roomEditing = null;
		static Texture2D[] doorEditing = null;
		static Texture2D[] windowEditing = null;
		static Texture2D[] stairEditing = null;
		static Texture2D[] yardEditing = null;

		// Displaying floor options
		static Texture2D trashIcon = null;
		static Texture2D copyIcon = null;

		// Editing the Floor Properties
		int editingPlan = -1;
		bool editingRoom = false;
		bool editingYard = false;

		// Used to modify rooms between two place
		int selectedRoomIndex = -1;
		int selectedLineIndex = -1;
		bool grabbingRoomCorner = false;
		Vector3 splitPoint = Vector3.zero;
		Vector3 originalStart = Vector3.zero;
		Vector3 originalEnd = Vector3.zero;
		Vector3 wallDirectionAtSplit = Vector3.zero;

		// Used to update the blueprint center
		int pivotUpdate = -1;
		string[] updatePivot = new string[2] { "Set", "Stop" }; 

		// Generic rooms properties
		List<int> indexesOfGenericRooms = new List<int>();
		List<Vector3[]> genericRoomInset = new List<Vector3[]>();
		private static Color genericRoomColor = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, .3f );

		// All Labels /w tooltips
		GUIContent updatePivotPointLabel = new GUIContent("Update Pivot Point", "Every building has a pivot point that it can spin around and the parent's pivot point.");
		GUIContent updateCeilingHeightLabel = new GUIContent("Set Ceiling Height", "Update the ceiling height for all rooms here. You must select 'set' to update the floors ceilings and generate the building again.");

		#endregion

		/// <summary>
		/// Displays room and floor editor.
		/// </summary>
		private void DisplayFloorEditor()
		{
			if(Script == null)
			{
				ClearFloorOutlines();
				return;
			}

			if(Script != null && Script.FloorEditType != Script.LastFloorEditType || Script.BuildingBlueprint != null && Script.SelectedFloor >= Script.BuildingBlueprint.Floors.Count)
			{
				if(Script.SelectedFloor == -1)
				{
					if(Script.PreviousFloor > -1)
						Script.SelectedFloor = Script.PreviousFloor;
					else
						Script.SelectedFloor = Script.BuildingBlueprint.Floors.Count - 1;
				}

				if(Script.EditingState != EditingState.None && roomEditingType < 0 && doorEditingType < 0 && windowEditingType < 0 && stairEditingType < 0 && yardEditingType < 0)
					SetSelectionFromEditingState(Script.EditingState);

				// If the selected floor is greater than the current amount of floors then set it to zero
				if(Script.BuildingBlueprint.Floors != null)
				{
					if(Script.SelectedFloor >= Script.BuildingBlueprint.Floors.Count || Script.SelectedFloor < 0)
					{
						Script.LastSelectedFloor = -1;
						Script.SelectedFloor = 0;
					}

					// Check to make sure that the previous floor does not outpace any floor in the system
					if(Script.PreviousFloor >= Script.BuildingBlueprint.Floors.Count)
					{
						Script.LastSelectedFloor = -1;
						Script.PreviousFloor = Script.SelectedFloor;
					}
				}

				// Update the current height of the floor that is selected
				UpdateCurrentFloorHeight(Script.SelectedFloor);
				
				// Update the integer for the current floor
				Script.CurrentFloor = GetFloorIndex(Script.SelectedFloor);
				
				// Ensures that the player can't lay yards on the second floor
				if(Script.CurrentFloor > 0 && yardEditingType > 0)
					ResetAllSelections();
				
				// Sees if the building is empty and thus needs to find a spot in the real world to start
				CheckForBuildingBeingEmpty();
				
				// Update so this doesn't run constantly
				Script.LastFloorEditType = Script.FloorEditType;
			}
			
			// GUI
			DisplayFloorsPanel();
			if(Script.BuildingBlueprint.Floors != null && Script.BuildingBlueprint.Floors.Count > 0)
				DisplayRoomEditor();
			else
			{
				GUILayout.Label("Please add a floor to allow floor placement", EditorStyles.helpBox);
			}

			if(Script.CurrentFloor > -1)
			{
				// Update the floor to the current floor
				DrawNewFloor();
			}
		}

		/// <summary>
		/// Displays the editor for what floor you are on
		/// </summary>
		void DisplayFloorsPanel()
		{
			BuildingBlueprint buildingBp = Script.BuildingBlueprint;
			
			if(buildingBp == null)
				return;
			
			// Adding a floor to the top of this building
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			GUIContent addFloorButton = new GUIContent("Add Floor", "Add a new floor to the top of this building");

			// Allow a user to add a new floor
			if(GUILayout.Button(addFloorButton, GUILayout.Width(100), GUILayout.Height(20)))
			{
				UnityEngine.Object[] undoObjects = new UnityEngine.Object[2] { Script, Script.BuildingBlueprint };
				Undo.RegisterCompleteObjectUndo(undoObjects, "Add Floor");

				Script.BuildingBlueprint.Floors.Add(new FloorBlueprint());
				if(Script.BuildingBlueprint.Floors.Count == 1)
					Script.SelectedFloor = 0;
				else
					Script.SelectedFloor++;
				
				UpdateCurrentFloorHeight(Script.SelectedFloor);
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			// Display the floor selector, duplicator and deleter
			EditorGUILayout.BeginHorizontal();
			if(buildingBp.Floors != null)
			{
				// Changes the floor display string if the floors change
				if(floorsDisplay == null 
				   || duplicateFloorsDisplay == null
				   || deleteFloorsDisplay == null
				   || floorsDisplay.Length != buildingBp.Floors.Count)
				{
					floorsDisplay = new string[buildingBp.Floors.Count];
					for(int i = 0; i < floorsDisplay.Length; i++)
						floorsDisplay[i] = "Floor " + (floorsDisplay.Length - i).ToString();

					duplicateFloorsDisplay = new Texture2D[buildingBp.Floors.Count];
					for(int i = 0; i < duplicateFloorsDisplay.Length; i++)
						duplicateFloorsDisplay[i] = copyIcon;

					deleteFloorsDisplay = new Texture2D[buildingBp.Floors.Count];
					for(int i = 0; i < deleteFloorsDisplay.Length; i++)
						deleteFloorsDisplay[i] = trashIcon;
				}

				// Update the floor the player is currently looking at
				int newSelectedFloor = GUILayout.SelectionGrid(Script.SelectedFloor, floorsDisplay, 1);
				if(Script.LastSelectedFloor != newSelectedFloor)
				{
					UnityEngine.Object[] undoObjects = new UnityEngine.Object[2] { Script, Script.BuildingBlueprint };
					Undo.RegisterCompleteObjectUndo(undoObjects, "Duplicate Floor");
					Script.SelectedFloor = newSelectedFloor;
					Script.LastSelectedFloor = Script.SelectedFloor;
					UpdateCurrentFloorHeight(Script.SelectedFloor);
				}

				float selectionGridHeight = floorsDisplay.Length * 18f + (floorsDisplay.Length - 1) * 3f;

				// Duplicate a floor
				duplicateFloor = GUILayout.SelectionGrid(-1, duplicateFloorsDisplay, 1, GUILayout.Width(25), GUILayout.Height(selectionGridHeight));
				if(duplicateFloor > -1)
				{
					UnityEngine.Object[] undoObjects = new UnityEngine.Object[2] { Script, Script.BuildingBlueprint };
					Undo.RegisterCompleteObjectUndo(undoObjects, "Duplicate Floor");

					buildingBp.Floors.Insert(GetFloorIndex(duplicateFloor) + 1, BCUtils.DeepCopyFloor(buildingBp.Floors[GetFloorIndex(duplicateFloor)]));
					Script.SelectedFloor++;
					UpdateCurrentFloorHeight(Script.SelectedFloor);
					return;
				}

				// Delete a floor
				deleteFloor = GUILayout.SelectionGrid(-1, deleteFloorsDisplay, 1, GUILayout.Width(25), GUILayout.Height(selectionGridHeight));
				if(deleteFloor > -1)
				{
					UnityEngine.Object[] undoObjects = new UnityEngine.Object[2] { Script, Script.BuildingBlueprint };
					Undo.RegisterCompleteObjectUndo(undoObjects, "Add Floor");

					buildingBp.Floors.RemoveAt(GetFloorIndex(deleteFloor));
					
					deleteFloor = -1;
					Script.CurrentFloor -= 1;
					UpdateCurrentFloorHeight(Script.CurrentFloor);
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DisplayRoomEditor()
		{
			EditorGUILayout.Space();

			if(Script.EditingState != EditingState.None && roomEditingType < 0 && doorEditingType < 0 && windowEditingType < 0 && stairEditingType < 0 && yardEditingType < 0)
				SetSelectionFromEditingState(Script.EditingState);

			GUILayout.Label("Add New Floor Items", EditorStyles.helpBox);
			
			// Edit room floor
			int newRoomType = DisplayEditType(roomEditingLabel, roomEditing, roomEditingType);
			int newDoorType = DisplayEditType(doorEditingLabel, doorEditing, doorEditingType);
						
			if(Script.EditingState == EditingState.LayDoors || doorEditingType == 2) // HACK
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(" ");
				EditorGUILayout.BeginVertical();
				NewDoorPanel();
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
			}
			
			int newWindowType = DisplayEditType(windowEditingLabel, windowEditing, windowEditingType);
			if(Script.EditingState == EditingState.LayWindows || windowEditingType == 2) // HACK
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(" ");
				EditorGUILayout.BeginVertical();
				NewWindowPanel();
				EditorGUILayout.Space();
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
			}
			
			int newStairType = DisplayEditType(stairsEditingLabel, stairEditing, stairEditingType);
			
			// Special Case for the Yard (on the first floor)
			int newYardType = yardEditingType;

			if(Script.CurrentFloor == 0)
			{
				newYardType = DisplayEditType(yardEditingLabel, yardEditing, yardEditingType);
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("* Experimental", EditorStyles.miniLabel);
				GUILayout.EndHorizontal();
			}
			else
				newYardType = -1;
			
			// Goes through all selected types and updates what the user is editing currently
			SetEditingType(newRoomType, newDoorType, newWindowType, newStairType, newYardType);
			
			EditorGUILayout.Separator();
			
			ShowFloorEditingOptions();

			EditorGUILayout.Separator();

			GUILayout.Label("Live View", EditorStyles.helpBox);
			GUILayout.BeginHorizontal();
			DisplayUndoableToggle(new GUIContent("Show Live View *", "Enables live view on this building, which renders a building while editing"), 
			                      ref Script.BuildingBlueprint.LiveViewEnabled, 
			                      "Change Live View", 
			                      Script.BuildingBlueprint);
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Quick Generation", GUILayout.Height(14f)))
			{
				BCGenerator.GenerateFullBuilding(Script.BuildingBlueprint);
				HideFloorsNotInUse(Script.BuildingBlueprint);
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label("* Experimental, please use high-performance computer.", EditorStyles.miniLabel);
			GUILayout.EndHorizontal();
		}

		private void ShowFloorEditingOptions()
		{
			// =============== options for the entire floor ====================
			GUILayout.Label("Floor Update Options", EditorStyles.helpBox);

			// Sets the floor height
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(updateCeilingHeightLabel);
			allCeilingHeight = (float)System.Math.Round(GUILayout.HorizontalSlider(allCeilingHeight, 2.0f, 2.9f), 1);
			EditorGUILayout.LabelField(allCeilingHeight.ToString() + "m", GUILayout.Width(30));
			if(GUILayout.Button("Set", GUILayout.Width(30f), GUILayout.Height(14f)))
			{
				if(allCeilingHeight > 2 && allCeilingHeight < 2.90001)
				{
					Undo.RecordObject(Script.BuildingBlueprint, "Change All Ceiling Heights");
					SetCeilingHeightForFloor(Script.CurrentFloorBlueprint, allCeilingHeight);
					Debug.Log("BUILDING CRAFTER: All ceilings on floor " + (Script.CurrentFloor + 1) + " set to " + allCeilingHeight + " meters high");
				}
			}
			EditorGUILayout.EndHorizontal();

			// Highlights only the generic rooms so that the player editor can fix any generic rooms
			this.highlightGenericRooms = EditorGUILayout.Toggle("Show Generic Rooms", this.highlightGenericRooms);

			if(this.highlightGenericRooms)
			{
				HighlightGenericRooms();
			}

			// Update building pivot
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(updatePivotPointLabel);
			pivotUpdate = GUILayout.SelectionGrid(pivotUpdate, updatePivot, 2);
			if(pivotUpdate == 1)
			{
				pivotUpdate = -1;
				Script.EditingState = EditingState.None;
			}
			else if(pivotUpdate == 0)
			{
				// Disables all other editing
				ResetAllSelections();
				Script.EditingState = EditingState.UpdatingPivot;
			}

			EditorGUILayout.EndHorizontal();

		}

		private void SetEditingType(int newRoomType, int newDoorType, int newWindowType, int newStairType, int newYardType)
		{
			// Set the selection grid so it doesn't look like you are editing two things at once
			if(newRoomType > -1)
			{
				ResetAllSelections();
				roomEditingType = newRoomType;
			}
			if(newDoorType > -1)
			{
				ResetAllSelections();
				doorEditingType = newDoorType;
			}
			if(newWindowType > -1)
			{
				ResetAllSelections();
				windowEditingType = newWindowType;
			}
			if(newStairType > -1)
			{
				ResetAllSelections();
				stairEditingType = newStairType;
			}
			if(newYardType > -1)
			{
				ResetAllSelections();
				yardEditingType = newYardType;
			}

			// Resets anything else because the user has selected something
			if(newRoomType > -1 
			   || newDoorType > -1
			   || newWindowType > -1 
			   || newStairType > -1 
			   || newYardType > -1 )
			{
				pivotUpdate = -1;
			}
			
			// Set the editing type
			if(roomEditingType > -1)
			{
				switch(roomEditingType)
				{
				case 0:
					Script.EditingState = EditingState.LayRoomFloors;
					break;
				case 1:
					Script.EditingState = EditingState.DeleteRooms;
					break;
				case 2:
					Script.EditingState = EditingState.ModifyFloor;
					break;
				case 3:
					Script.EditingState = EditingState.AddingFloorPoints;
					break;
				case 4:
					Script.EditingState = EditingState.EditFloorProperties;
					break;
				}
			}
			
			if(doorEditingType > -1)
			{
				switch(doorEditingType)
				{
				case 0:
					Script.EditingState = EditingState.LayDoors;
					break;
				case 1:
					Script.EditingState = EditingState.DeleteDoorsAndWindows;
					break;
				case 2:
					Script.EditingState = EditingState.EyedropDoorWindows;
					break;
				case 3:
					Script.EditingState = EditingState.EditDoorWindowProperties;
					break;
				}
			}
			
			if(windowEditingType > -1)
			{
				switch(windowEditingType)
				{
				case 0:
					Script.EditingState = EditingState.LayWindows;
					break;
				case 1:
					Script.EditingState = EditingState.DeleteDoorsAndWindows;
					break;
				case 2:
					Script.EditingState = EditingState.EyedropDoorWindows;
					break;
				case 3:
					Script.EditingState = EditingState.EditDoorWindowProperties;
					break;
				}
			}
			
			if(stairEditingType > -1)
			{
				switch(stairEditingType)
				{
				case 0:
					Script.EditingState = EditingState.LayStairs;
					break;
				case 1:
					Script.EditingState = EditingState.DeleteStairs;
					break;
				}
			}
			
			if(yardEditingType > -1)
			{
				switch(yardEditingType)
				{
				case 0:
					Script.EditingState = EditingState.LayYard;
					break;
				case 1:
					Script.EditingState = EditingState.DeleteYard;
					break;
				case 2:
					Script.EditingState = EditingState.EditFloorProperties;
					break;
				}
			}
		}

		private void SetSelectionFromEditingState(EditingState currentEditingState)
		{
			ResetAllSelections();

			switch(Script.EditingState)
			{
			// ======= FLOORS ==========
			case(EditingState.LayRoomFloors):
				roomEditingType = 0;
				break;

			case(EditingState.DeleteRooms):
				roomEditingType = 1;
				break;

			case(EditingState.ModifyFloor):
				roomEditingType = 2;
				break;
				
			case(EditingState.AddingFloorPoints):
				roomEditingType = 3;
				break;
				
			case(EditingState.EditFloorProperties):
				roomEditingType = 4;
				break;

			// == DOORS AND WINDOWS ==

			case(EditingState.DeleteDoorsAndWindows):
				doorEditingType = 1;
				break;

			case(EditingState.EyedropDoorWindows):
				doorEditingType = 2;
				break;

			case(EditingState.EditDoorWindowProperties):
				doorEditingType = 3;
				break;

				
			// ======= WINDOWS ==========
			
			case(EditingState.LayWindows):
				windowEditingType = 0;
				break;

			// ======= DOORS ==========

			case(EditingState.LayDoors):
				doorEditingType = 0;
				break;

			// ======= STAIRS ==========

			case(EditingState.LayStairs):
				stairEditingType = 0;
				break;

			case(EditingState.DeleteStairs):
				stairEditingType = 1;
				break;

			// ======= YARDS ==========
			case(EditingState.LayYard):
				yardEditingType = 0;
				break;

			case(EditingState.DeleteYard):
				yardEditingType = 1;
				break;

			// ======= ROOFS ========
				
			case(EditingState.LayRoof):
				roofEditType = 0;
				break;
				
			case(EditingState.DeleteRoof):
				roofEditType = 1;
				break;
				
			case(EditingState.EditRoofProperties):
				roofEditType = 2;
				break;

//			case(EditingState.UpdatingPivot):
//				EditPivot(currentEvent);
//				break;
			}
		}

		/// <summary>
		/// Returns the new selection position ONLY if it is different
		/// </summary>
		/// <returns>-1 unless the type has changed</returns>
	//	private int DisplayEditType(string title, string[] buttons, int editingType)
	//	{
	//		int newType = -1;
	//
	//		GUILayout.BeginHorizontal();
	//		EditorGUILayout.PrefixLabel(title);
	//		newType = GUILayout.SelectionGrid(editingType, buttons, maxButtons, GUILayout.Height(20f));
	//		GUILayout.EndHorizontal();
	//		GUILayout.Space(5f);
	//
	//		if(newType == editingType)
	//			return -1;
	//
	//		return newType;
	//	}

		/// <summary>
		/// Returns the new selection position ONLY if it is different
		/// </summary>
		/// <returns>-1 unless the type has changed</returns>
		private int DisplayEditType(GUIContent title, Texture2D[] buttons, int editingType, bool experimental = false)
		{
			int newType = -1;
			
			GUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(title);
			newType = GUILayout.SelectionGrid(editingType, buttons, maxButtons, GUILayout.Height(30f), GUILayout.MinWidth(20f));
			GUILayout.EndHorizontal();
			GUILayout.Space(5f);
			
			if(newType == editingType)
				return -1;
			
			return newType;
		}


		private void ResetAllSelections()
		{
			this.roomEditingType = -1;
			this.doorEditingType = -1;
			this.windowEditingType = -1;
			this.stairEditingType = -1;
			this.yardEditingType = -1;
		}

		private void CheckForBuildingBeingEmpty()
		{
			if(Script.CurrentWallPath.Count > 0)
			{
				buildingIsEmpty = false;
				return;
			}

			for(int i = 0; i < Script.BuildingBlueprint.Floors.Count; i++)
			{
				for(int j = 0; j < Script.BuildingBlueprint.Floors[i].RoomBlueprints.Count; j++)
				{
					if(Script.BuildingBlueprint.Floors[i].RoomBlueprints.Count > 0)
					{
						buildingIsEmpty = false;
						return;
					}
				}
			}
			buildingIsEmpty = true;
		}


		/// <summary>
		/// Draws the path of the currently laid wall
		/// </summary>
		void DrawCurrentWallPath()
		{
			if(Script.CurrentWallPath == null || Script.CurrentWallPath.Count < 1)
				return;

			Handles.color = blueprintColor;

			if(Script.EditingState == EditingState.LayYard)
				Handles.color = yardColor;

			Vector3[] currentWallPath = Script.CurrentWallPath.ToArray<Vector3>();
			for(int i = 0; i < currentWallPath.Length; i++)
				currentWallPath[i] += currentFloorHeight;

			Handles.DrawAAPolyLine(10, currentWallPath);

			if(Script.CurrentWallPath.Count > 0)
			{
				for(int i = 0; i < Script.CurrentWallPath.Count; i++)
				{
					if(i == Script.CurrentWallPath.Count - 1 && i > 1)
					{
						Handles.DrawSolidDisc(Script.CurrentWallPath[i] + currentFloorHeight, Vector3.up, 0.15f);
	//					Handles.SphereCap(0, Script.CurrentWallPath[i] + currentFloorHeight + floorHeight * Vector3.up, Quaternion.identity, 0.25f);
						continue;
					}
					
					if(i == 0)
					{
						Handles.DrawSolidDisc(Script.CurrentWallPath[i] + currentFloorHeight, Vector3.up, 0.15f);
	//					Handles.SphereCap(0, Script.CurrentWallPath[i] + currentFloorHeight + floorHeight * Vector3.up, Quaternion.identity, 0.25f);
						continue;
					}
					Handles.DrawSolidDisc(Script.CurrentWallPath[i] + currentFloorHeight, Vector3.up, 0.15f);
	//				Handles.SphereCap(0, Script.CurrentWallPath[i] + currentFloorHeight + floorHeight * Vector3.up, Quaternion.identity, 0.25f);
				}
			}

			if(Script.EditingState == EditingState.LayRoomFloors || Script.EditingState == EditingState.LayYard)
			{
				if(Script.CurrentWallPath.Count > 0)
				{
					Handles.color = gridCursorColor;
					Handles.DrawAAPolyLine(10, currentWallPath.Last(), gridCursor);
				}
			}
		}

		/// <summary>
		/// Draws a highlighted path for the player
		/// </summary>
		void DrawSelectedPath()
		{
			Handles.color = gridCursorColor;

			Vector3[] currentWallPath = Script.SelectedPath.ToArray<Vector3>();
			for(int i = 0; i < currentWallPath.Length; i++)
				currentWallPath[i] += currentFloorHeight;
			
			Handles.DrawAAPolyLine(10, currentWallPath);
		}

		void PlaceNewWallPoint(Vector3 gridPoint)
		{
			Script.CurrentWallPath.Add(new Vector3(gridPoint.x, 0, gridPoint.z));
		}

		private void DragBuildingCorners(Event currentEvent)
		{
			Vector3 gridPoint;
			if(GetGridPoint(out gridPoint, false, currentEvent) == false)
				return;

			Vector3 precisePoint;
			GetPrecisePoint(out precisePoint, currentEvent);

			// Set the cursor to the point
			gridCursor = gridPoint + currentFloorHeight;

			if(selectedRoomIndex < 0 && selectedLineIndex < 0)
			{
				// By default, the cursor is red
				gridCursorColor = redGridCursor;

				int roomIndex = -1;
				RoomBlueprint roomBp = BCUtils.GetRoomFromPoint(precisePoint, Script.BuildingBlueprint, Script.CurrentFloor, out roomIndex);

				if(roomBp == null)
				{
					TestMouseClick(currentEvent);
					return;
				}

				// We've found a room, set 
				int intersectIndex = BCUtils.IsPointOnCornerPoint(gridPoint, roomBp.PerimeterWalls);

				if(intersectIndex < 0)
				{
					TestMouseClick(currentEvent);
					return;
				}

				gridCursorColor = greenGridCursor;

				if(TestMouseClick(currentEvent))
				{
					selectedRoomIndex = roomIndex;
					selectedLineIndex = intersectIndex;
				}
			}
			else
			{
				if(selectedRoomIndex > -1 && selectedLineIndex > -1)
				{
					if(currentEvent.type == EventType.MouseDrag)
					{
						EditorGUIUtility.hotControl = controlId;

						gridCursorColor = greenGridCursor;
						
						RoomBlueprint roomBp = Script.CurrentFloorBlueprint.RoomBlueprints[selectedRoomIndex];

						Vector3 prevPoint = roomBp.PerimeterWalls[BCUtils.GetIndexAtPlus(selectedLineIndex, -1, roomBp.PerimeterWalls)];
						Script.SelectedPath = new List<Vector3>() { prevPoint, roomBp.PerimeterWalls[selectedLineIndex], roomBp.PerimeterWalls[selectedLineIndex + 1]};

						int prevIndex = BCUtils.GetIndexAtPlus(selectedLineIndex, -1, roomBp.PerimeterWalls);
						int thisIndex = BCUtils.GetIndexAtPlus(selectedLineIndex, 0, roomBp.PerimeterWalls);
						int nextIndex = BCUtils.GetIndexAtPlus(selectedLineIndex, 1, roomBp.PerimeterWalls);

						Vector3 prevPos = roomBp.PerimeterWalls[prevIndex];
						Vector3 thisPos = roomBp.PerimeterWalls[thisIndex];
						Vector3 nextPos = roomBp.PerimeterWalls[nextIndex];

						Vector3 nextDirection = (thisPos - nextPos).normalized;
						Vector3 prevDirection = (thisPos - prevPos).normalized;

						// Must test to see if the new point has cross over 
						Vector3 newPrevPos = prevPos;
						Vector3 newThisPos = gridPoint;
						Vector3 newNextPos = nextPos;

						// Set the prev point
						if(prevDirection.sqrMagnitude < 0.1f)
						{
							
						}
						else if(prevDirection.z == 0)
						{
							newPrevPos = new Vector3(prevPos.x, 0, gridPoint.z);
						}
						else if(prevDirection.x == 0)
						{
							newPrevPos = new Vector3(gridPoint.x, 0, prevPos.z);
						}

						// Set the next point
						if(nextDirection.sqrMagnitude < 0.1f)
						{
							
						}
						else if(nextDirection.z == 0)
						{
							newNextPos = new Vector3(nextPos.x, 0, gridPoint.z);
						}
						else if(nextDirection.x == 0)
						{
							newNextPos = new Vector3(gridPoint.x, 0, nextPos.z);
						}

						// TODO: Add ability to drag 3 points on top of each other
						if((newPrevPos - newThisPos).sqrMagnitude < 0.01 || (newNextPos - newThisPos).sqrMagnitude < 0.01)
						{

						}
						else
						{
							BCUtils.SetPointOnWall(newPrevPos, prevIndex, roomBp);
							BCUtils.SetPointOnWall(newThisPos, thisIndex, roomBp);
							BCUtils.SetPointOnWall(newNextPos, nextIndex, roomBp);
						}

						currentEvent.Use();
					}
				}
			}

			if(ResetClickUp(currentEvent))
			{
				RoomBlueprint roomBp = Script.CurrentFloorBlueprint.RoomBlueprints[selectedRoomIndex];
				BCUtils.CollapseWallLines(ref roomBp);
				selectedRoomIndex = -1;
				selectedLineIndex = -1;
				grabbingRoomCorner = false;
			}
		}

		void EditFloorProperties (Event currentEvent)
		{
			if(editingPlan > -1 && editingRoom)
			{
				GUI.Window(1, editRect, EditFloorWindow, windowTitle);
			}
			else if(editingPlan > -1 && editingYard)
			{
				GUI.Window(1, editRect, EditYardWindow, windowTitle);
			}

			Vector3 precisePos;
			bool point = GetPrecisePoint(out precisePos, currentEvent);

			if(point == false)
				return;
			
			// Sets the selector to the precise point
			gridCursor = precisePos + currentFloorHeight;

			// Tests if there is a click and opens up the window
			if(TestMouseClick(currentEvent, 0) && editingPlan < 0)
			{
				for(int i = 0; i < Script.CurrentFloorBlueprint.RoomBlueprints.Count; i++)
				{
					if(BCUtils.PointInPolygonXZ(precisePos, Script.CurrentFloorBlueprint.RoomBlueprints[i].PerimeterWalls.ToArray()))
					{
						editingPlan = i;
						windowTitle = "Room " + i.ToString();
						editingRoom = true;
						break;
					}
				}

				// FOR MODIFYING YARD LAYOUTS
				if(Script.CurrentFloor == 0)
				{
					for(int i = 0; i < Script.BuildingBlueprint.Floors[0].YardLayouts.Count; i++)
					{
						if(BCUtils.PointInPolygonXZ(precisePos, Script.BuildingBlueprint.Floors[0].YardLayouts[i].PerimeterWalls.ToArray()))
						{
							editingPlan = i;
							windowTitle = "Yard " + i.ToString();
							editingYard = true;
							break;
						}
					}
				}

				editRect = new Rect(currentEvent.mousePosition.x, currentEvent.mousePosition.y, 200, 200);
			}
			
			if(currentEvent.type == EventType.MouseUp && currentEvent.button == 1 && editingPlan > -1)
			{
				editingPlan = -1;
				editingRoom = false;
				editingYard = false;
			}
				
			
			if(editingPlan < 0)
			{
				editingRoom = false;
				editingYard = false;
				ResetClickUp(currentEvent);
			}
			

		}

		private void EditFloorWindow(int i)
		{
			if(editingPlan >= Script.CurrentFloorBlueprint.RoomBlueprints.Count || editingPlan < 0)
			{
				editingRoom = false;
				editingPlan = -1;
				return;
			}

			RoomBlueprint roomBp = Script.CurrentFloorBlueprint.RoomBlueprints[editingPlan];

			GUILayout.Label("Ceiling Height");
			
			GUILayout.BeginHorizontal();
			float newCeilingHeight = (float)System.Math.Round(GUILayout.HorizontalSlider(roomBp.CeilingHeight, 2.0f, 2.9f), 1);
			EditorGUILayout.LabelField(roomBp.CeilingHeight.ToString(), GUILayout.Width(30));
			GUILayout.EndHorizontal();

			if(newCeilingHeight != roomBp.CeilingHeight)
			{
				Undo.RecordObject(Script.BuildingBlueprint, "Ceiling Height Change");
				roomBp.CeilingHeight = newCeilingHeight;
				EditorUtility.SetDirty(Script.BuildingBlueprint);
			}
			
			GUILayout.Label("Room Type");
			RoomType newRoomType = (RoomType)EditorGUILayout.EnumPopup(roomBp.RoomType);

			if(newRoomType != roomBp.RoomType)	
			{
				Undo.RecordObject(Script.BuildingBlueprint, "Change Room Type");
				roomBp.RoomType = newRoomType;
				if(roomBp.RoomType != RoomType.CustomType && roomBp.OverrideRoomStyle != null)
					roomBp.OverrideRoomStyle = null;

				EditorUtility.SetDirty(Script.BuildingBlueprint);
			}

			GUILayout.Label("Override Room Type");
			EditorGUILayout.BeginHorizontal();
			RoomStyle newRoomStyle = (RoomStyle)EditorGUILayout.ObjectField(roomBp.OverrideRoomStyle, typeof(RoomStyle), false);
			if(roomBp.OverrideRoomStyle != null)
			{
				if(GUILayout.Button("X"))
				{
					Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Remove Override Style");
					roomBp.OverrideRoomStyle = null;
					roomBp.RoomType = RoomType.Generic;

					EditorUtility.SetDirty(Script.BuildingBlueprint);
					return; // Makes sure that this field isn't set again
				}
			}
			else
			{
				if(GUILayout.Button("Create", GUILayout.Width(54), GUILayout.Height(14)))
				{
					RoomStyle roomStyle = CreateBuildStyleAsset.CreateRoomStyle();
					if(roomStyle != null)
						roomBp.OverrideRoomStyle = roomStyle;
				}
			}

			if(newRoomStyle != roomBp.OverrideRoomStyle)
			{
				Undo.RecordObject(Script.BuildingBlueprint, "Change Override Style");
				roomBp.OverrideRoomStyle = newRoomStyle;
				if(roomBp.OverrideRoomStyle != null && roomBp.RoomType != RoomType.CustomType)
					roomBp.RoomType = RoomType.CustomType;

				EditorUtility.SetDirty(Script.BuildingBlueprint);
			}

			EditorGUILayout.EndHorizontal();

			GUILayout.FlexibleSpace();

			Color backgroundOriginal = GUI.backgroundColor;
			GUI.backgroundColor = DeleteButtonColor;
			if(GUILayout.Button("Delete Room"))
			{
				Object[] undoers = new Object[2] {Script.BuildingBlueprint, Script};

				Undo.RegisterCompleteObjectUndo(undoers, "Delete Room");
				Script.CurrentFloorBlueprint.RoomBlueprints.RemoveAt(editingPlan);
				editingPlan = -1;
				editingRoom = false;
				DrawNewFloor();
				
				EditorUtility.SetDirty(Script.BuildingBlueprint);

				return;
			}
			GUI.backgroundColor = backgroundOriginal;
		}

		private void EditYardWindow(int i)
		{
			YardLayout yardLayout = Script.BuildingBlueprint.Floors[0].YardLayouts[editingPlan];

			GUILayout.Label("Yard Type", GUILayout.Width(100));
			YardTypeEmum newYardType = (YardTypeEmum)EditorGUILayout.EnumPopup(yardLayout.YardType);

			if(newYardType != yardLayout.YardType)
			{
				Undo.RecordObject(Script.BuildingBlueprint, "Change Yard Type");
				yardLayout.YardType = newYardType;

				EditorUtility.SetDirty(Script.BuildingBlueprint);
			}

			GUILayout.FlexibleSpace();

			Color backgroundOriginal = GUI.backgroundColor;
			GUI.backgroundColor = DeleteButtonColor;
			if(GUILayout.Button("Delete Yard Tile"))
			{
				Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Delete Yard Tile");
				Script.BuildingBlueprint.Floors[0].YardLayouts.RemoveAt(editingPlan);
				editingPlan = -1;

				DrawNewFloor();

				return;
			}

			GUI.backgroundColor = backgroundOriginal;
		}


		int WallStartGroup;

		
		/// <summary>
		/// NOTE: This both lays walls and the yard. When it completes a floor, then depending on the editing state we create a new item
		/// </summary>
		/// <param name="currentEvent">Current event.</param>
		private void LayingWalls(Event currentEvent)
		{
			// Check to see if we want to leave this state
			if(CheckForEscape(currentEvent, true) == true)
			{
				Undo.RecordObject(Script, "Exit Laying Walls");
				Script.CurrentWallPath.Clear();
				return;
			}

			CheckForBuildingBeingEmpty();
			
			if(buildingIsEmpty == true)
			{
				if(LayFirstWallBasedOnWorld() == true)
					return;
			}
			
			// Calculated last points for the wall currently being worked on
			Vector3 lastPoint = new Vector3();
			if(Script.CurrentWallPath.Count > 0)
				lastPoint = Script.CurrentWallPath[Script.CurrentWallPath.Count - 1];
			
			Vector3 lastLastPoint = new Vector3();
			if(Script.CurrentWallPath.Count > 1)
				lastLastPoint = Script.CurrentWallPath[Script.CurrentWallPath.Count - 2];
			
			// Used to test if we are closing the loop
			Vector3 firstPoint = new Vector3();
			if(Script.CurrentWallPath.Count > 0)
				firstPoint = Script.CurrentWallPath[0];
			
			// NOTE: The floorheight is always the zero y of the Building Crafter GameObject
			float floorHeight = Script.transform.position.y;
			
			// ==========================================================
			// = Casting out the Raycast to figure out the floor layout =
			// ==========================================================
			Vector3 gridPoint = new Vector3(); // The point where the player is aiming
			
			bool working = GetGridPoint(out gridPoint, false, currentEvent);
			
			if(working == false)
				return;
			
			bool hasHitSomething = false;
						
			if(Script.CurrentWallPath.Count == 0)
			{
				gridPoint = new Vector3(Mathf.Round(gridPoint.x), floorHeight, Mathf.Round(gridPoint.z));
				hasHitSomething = true;
			}
			else
			{
				Vector3 actualGridPoint = new Vector3(Mathf.Round(gridPoint.x), floorHeight, Mathf.Round(gridPoint.z));
				int xDiff = (int)(actualGridPoint.x - lastPoint.x);
				int zDiff = (int)(actualGridPoint.z - lastPoint.z);
				
				hasHitSomething = true;
				
				if(Mathf.Abs(xDiff) > Mathf.Abs(zDiff))
					gridPoint = new Vector3(actualGridPoint.x, floorHeight, lastPoint.z);
				else
					gridPoint = new Vector3(lastPoint.x, floorHeight, actualGridPoint.z);
			}
			
			if(hasHitSomething == false)
				return;
			
			bool canPlace = true;
			bool extendPoint = false; // Used to calculate if a wall is along the same axis
			
			// Tests for the starting point to see if it lines up
			if(Script.CurrentWallPath.Count < 1)
			{
				// Test in all directions to see if there is an opening
				bool openSpaceIsPresent = false;
			
				if(BCUtils.IsPointOnlyInsideAnyRoom(gridPoint + (Vector3.left + Vector3.forward) * 0.1f, Script.CurrentFloorBlueprint) == false)
					openSpaceIsPresent = true;
				
				if(BCUtils.IsPointOnlyInsideAnyRoom(gridPoint + (Vector3.left + Vector3.back) * 0.1f, Script.CurrentFloorBlueprint) == false)
					openSpaceIsPresent = true;
				
				if(BCUtils.IsPointOnlyInsideAnyRoom(gridPoint + (Vector3.right + Vector3.forward) * 0.1f, Script.CurrentFloorBlueprint) == false)
					openSpaceIsPresent = true;
				
				if(BCUtils.IsPointOnlyInsideAnyRoom(gridPoint + (Vector3.right + Vector3.back) * 0.1f, Script.CurrentFloorBlueprint) == false)
					openSpaceIsPresent = true;
			
				if(openSpaceIsPresent == false)
					canPlace = false;

			}
			else
			{
				if(Script.CurrentWallPath[Script.CurrentWallPath.Count - 1] == gridPoint)
					canPlace = false;

				// Find if the point would overlap the current line being drawn
				if(Script.CurrentWallPath.Count > 0)
				{
					float length = (lastPoint - gridPoint).magnitude;
					Vector3 direc = (lastPoint - gridPoint).normalized;

					for(float f = 0.5f; f < length; f += 0.5f)
					{
						Vector3 testPoint = lastPoint - direc * f;
						
						int wallsTouching = 0;
						
						if(Script.CurrentFloorBlueprint.RoomBlueprints == null)
							continue;
						
						for(int i = 0; i < Script.CurrentFloorBlueprint.RoomBlueprints.Count; i++)
						{
							if(Script.CurrentFloorBlueprint.RoomBlueprints[i].PerimeterWalls == null || Script.CurrentFloorBlueprint.RoomBlueprints[i].PerimeterWalls.Count < 4)
								continue;
							
							RoomBlueprint testRoom =  Script.CurrentFloorBlueprint.RoomBlueprints[i];
							
							if(BCUtils.IsPointOnlyInsideARoom(testPoint, testRoom) == true)
							{
								canPlace = false;
								break;
							}
							if(BCUtils.IsPointAlongAWall(testPoint, testRoom))
								wallsTouching++;

							if(wallsTouching > 1)
							{
								int touchType = BCUtils.GetOutsetTypeFromManyRooms(testPoint, Script.CurrentFloorBlueprint);

								if(touchType == 3)
								{
									canPlace = false;
									break;
								}
							}
						}
						if(canPlace == false)
							break;
					}
				}
				
				if(Script.CurrentWallPath.Count > 1)
				{
					// Stops the wall layer from placing beyond the end of the start point
					if((firstPoint.z == gridPoint.z || firstPoint.x == gridPoint.x) && Script.CurrentWallPath.Count > 3)
					{
						Vector3 direction = (firstPoint - gridPoint).normalized;
						Vector3 directionOfLastPoint = (lastPoint - gridPoint).normalized;
						Vector3 directionToComplete = (lastPoint - firstPoint).normalized;
						
						float difference = (direction - directionOfLastPoint).magnitude;
						
						if(difference == 0 && directionToComplete == directionOfLastPoint)
							canPlace = false;
					}
					
					// Deals with going back on a wall already set up
					if(lastPoint.z == gridPoint.z)
					{
						if(gridPoint.x > lastPoint.x && lastPoint.x < lastLastPoint.x)
							canPlace = false;
						else if(gridPoint.x < lastPoint.x && lastPoint.x > lastLastPoint.x)
							canPlace = false;
						// Deals with extending points
						else if(gridPoint.x > lastPoint.x && lastPoint.x > lastLastPoint.x)
							extendPoint = true;
						else if(gridPoint.x < lastPoint.x && lastPoint.x < lastLastPoint.x)
							extendPoint = true;
					}
					else if(lastPoint.x == gridPoint.x)
					{
						if(gridPoint.z > lastPoint.z && lastPoint.z < lastLastPoint.z)
							canPlace = false;
						else if(gridPoint.z < lastPoint.z && lastPoint.z > lastLastPoint.z)
							canPlace = false;
						// Deals with extending points
						else if(gridPoint.z > lastPoint.z && lastPoint.z > lastLastPoint.z)
							extendPoint = true;
						else if(gridPoint.z < lastPoint.z && lastPoint.z < lastLastPoint.z)
							extendPoint = true;
					}
					
					// TODO: Deal with walls crossing over each other
				}
			}
			
			// Sets the grid up correctly with the right color
			if(canPlace == false)
				gridCursorColor = redGridCursor;
			else
				gridCursorColor = greenGridCursor;

			// HACK: This should not be in here, should be 
			if(canPlace && Script.BuildingBlueprint.LiveViewEnabled && gridPoint != lastGridPoint)
			{
				DrawPreviewOfNewRoom(gridPoint);
				lastGridPoint = gridPoint;
			}

			gridCursor = gridPoint + currentFloorHeight;
			
			// NOTE NOTE NOTE: THERE IS SOME CODE IN HERE ABOUT LAYING ROOM OR LAYING YARD
			if(Script.EditingState == EditingState.LayYard)
				Handles.color = yardColor;
			else
				Handles.color = blueprintColor;

			if(Script.CurrentWallPath.Count == 0)
				WallStartGroup = Undo.GetCurrentGroup();

			if(TestMouseClick(currentEvent, 0) && (Script.EditingState == EditingState.LayRoomFloors || Script.EditingState == EditingState.LayYard))
			{
				if(canPlace == true)
				{
					Undo.RegisterCompleteObjectUndo(Script, "Wall Path Add");
					
					if(extendPoint == true)
					{
						Script.CurrentWallPath[Script.CurrentWallPath.Count - 1] = gridPoint;
					}
					else
					{
						Script.CurrentWallPath.Add(gridPoint);
					}

				}
				
				if(Script.CurrentWallPath.Count > 3 && gridPoint == Script.CurrentWallPath[0] && canPlace)
				{
					// Remove a midpoint starting or ending
					if(Script.CurrentWallPath[1].x == gridPoint.x && gridPoint.x == Script.CurrentWallPath[Script.CurrentWallPath.Count - 2].x)
					{
						Script.CurrentWallPath[Script.CurrentWallPath.Count - 1] = Script.CurrentWallPath[1];
						Script.CurrentWallPath.RemoveAt(0);
					}
					else if(Script.CurrentWallPath[1].z == gridPoint.z && gridPoint.z == Script.CurrentWallPath[Script.CurrentWallPath.Count - 2].z)
					{
						Script.CurrentWallPath[Script.CurrentWallPath.Count - 1] = Script.CurrentWallPath[1];
						Script.CurrentWallPath.RemoveAt(0);
					}
					
					// This temp path checks to see if the path is clockwise or counter clockwise. Reverses it if it is
					List<Vector3> tempPath = Script.CurrentWallPath;
					if(BCUtils.IsClockwisePolygon(tempPath.ToArray()) == false)
						tempPath.Reverse();
					
					// =================== IF THE USER IS LAYING FLOORS, ADD A FLOOR =========================
					if(Script.EditingState == EditingState.LayRoomFloors)
					{

						Object[] objects = new Object[2] { Script.BuildingBlueprint, Script };

						Undo.RegisterCompleteObjectUndo(objects, "Add Room");

						// Updates the Building Blueprint center if certain conditions are met
						if(Script.CurrentFloor == 0 && Script.CurrentFloorBlueprint.RoomBlueprints.Count == 0 && Script.BuildingBlueprint.Floors.Count == 1)
						{
							// Finds the current height of the object
							float yPos = Script.BuildingBlueprint.Transform.position.y;

							BCGenerator.DestroyGeneratedBuilding(Script.BuildingBlueprint);
							Bounds bound = new Bounds(Script.CurrentWallPath[0], Vector3.zero);
							for(int i = 0; i < Script.CurrentWallPath.Count; i++)
								bound.Encapsulate(Script.CurrentWallPath[i]);
							
							Script.BuildingBlueprint.transform.position = bound.center - bound.extents;
							Script.BuildingBlueprint.LastGeneratedPosition = Script.BuildingBlueprint.Transform.position;

							// Sets this to the world height that it was at when that new point was laid
							Script.BuildingBlueprint.transform.position += new Vector3(0, yPos, 0);
						}

						Undo.CollapseUndoOperations(WallStartGroup - 1); // -1 means that it collapses till before the first point was laid

						// Adds a new roomblueprint to the floor blueprint and then sets the perimtere walls
						int indexOfNewRoom = Script.CurrentFloorBlueprint.RoomBlueprints.Count;
						Script.CurrentFloorBlueprint.RoomBlueprints.Add(new RoomBlueprint());
						
						RoomBlueprint roomBp = Script.CurrentFloorBlueprint.RoomBlueprints[indexOfNewRoom];
						roomBp.SetPerimeterWalls(tempPath.ToList<Vector3>());
					}
					
					// =================== IF THE USER IS LAYING A YARD, ADD A YARD =========================
					if(Script.EditingState == EditingState.LayYard)
					{
						Object[] objects = new Object[2] { Script.BuildingBlueprint, Script };
						
						Undo.RegisterCompleteObjectUndo(objects, "Add Room");
						Undo.CollapseUndoOperations(WallStartGroup - 1); // -1 means that it collapses till before the first point was laid

						// Adds a new roomblueprint to the floor blueprint and then sets the perimtere walls
						int newYardIndex = Script.CurrentFloorBlueprint.YardLayouts.Count;
						Script.CurrentFloorBlueprint.YardLayouts.Add(new YardLayout());
						
						YardLayout yardLayout = Script.CurrentFloorBlueprint.YardLayouts[newYardIndex];
						yardLayout.SetPerimeterWalls(tempPath.ToList<Vector3>());
					}

					Script.CurrentWallPath.Clear();
				}
				else
				{
					ResetClickUp(currentEvent);
				}
			}
			
			if(ResetClickUp(currentEvent))
			{
				// Clean up and remove any rooms that have walls of less than 4
				
				int wallBreaker = 0;
				while(wallBreaker < 100)
				{
					if(Script.CurrentFloorBlueprint.RoomBlueprints == null)
						break;

					wallBreaker++;
					
					int clearIndex = -1;
					
					for(int i = 0; i < Script.CurrentFloorBlueprint.RoomBlueprints.Count; i++)
					{
						if(Script.CurrentFloorBlueprint.RoomBlueprints[i].PerimeterWalls.Count < 4)
						{
							clearIndex = i;
							break;
						}
						
					}
					
					if(clearIndex < 0)
						break;
					
					Debug.Log("Room has been cleared because of no walls" + clearIndex);
					Script.CurrentFloorBlueprint.RoomBlueprints.RemoveAt(clearIndex);
				}
			}
		}

		
		/// <summary>
		/// Uses raycasts to the world to find where to place the first point
		/// </summary>
		bool LayFirstWallBasedOnWorld ()
		{
			Vector3 gridPoint = Vector3.zero;
			
			// First get the point by raycasting against the world
			if(GetWorldPoint(Event.current, out gridPoint) == false)
			{
				this.gridCursorColor = new Color(0, 0, 0, 0);
				return false;
			}
			
			// Now raycast down dot make sure that the selection point is hitting something
			Ray ray = new Ray(gridPoint + Vector3.up * 0.5f, Vector3.down);
			if(Physics.Raycast(ray, 1) == false)
			{
				this.gridCursorColor = new Color(0, 0, 0, 0);
				return false;
			}
			
			// If it does hit something, update the system
			this.gridCursorColor = Color.green;
			this.gridCursor = gridPoint;
			
			if(TestMouseClick(Event.current, 0))
			{
				Script.BuildingBlueprint.Transform.position = this.gridCursor;
				Script.BuildingBlueprint.LastGeneratedPosition = this.gridCursor;
				UpdateCurrentFloorHeight(0);
				PlaceNewWallPoint(gridPoint);
				CheckForBuildingBeingEmpty();
			}
			
			ResetClickUp(Event.current);
			
			return true;
		}

		
		/// <summary>
		/// Deletes a yard layout
		/// </summary>
		/// <param name="currentEvent">Current event.</param>
		void DeletingYards (Event currentEvent)
		{
			ResetClickUp(currentEvent);
			
			Vector3 gridPoint;
			bool pointFound = GetPrecisePoint(out gridPoint, currentEvent);

			if(pointFound == false)
				return;
			
			gridCursor = gridPoint + currentFloorHeight;
			
			int removeIndex = -1;
			
			for(int i = 0; i < Script.CurrentFloorBlueprint.YardLayouts.Count; i++)
			{
				Vector3[] walls = Script.CurrentFloorBlueprint.YardLayouts[i].PerimeterWalls.ToArray();
				
				for(int index = 0; index < walls.Length; index++)
					walls[index] += currentFloorHeight;
				
				convexOutline.Clear();
				
				if(BCUtils.PointInPolygonXZ(gridPoint, walls))
				{
					convexColor = convexDeleteColor;
					
					MeshInfo meshInfo = BCMesh.GenerateGenericMeshInfo(walls);
					for(int n = 0; n < meshInfo.Triangles.Length; n += 3)
					{
						convexOutline.Add(new Vector3[3] { meshInfo.Vertices[meshInfo.Triangles[n]], meshInfo.Vertices[meshInfo.Triangles[n + 1]], meshInfo.Vertices[meshInfo.Triangles[n + 2]]});
					}
					
					if(TestMouseClick(currentEvent))
					{
						Undo.RegisterFullObjectHierarchyUndo(Script.BuildingBlueprint, "Delete Yard");
						removeIndex = i;
					}
					break;
				}
				
			}
			
			if(pointFound)
			{
				TestMouseClick(currentEvent);
				// Use up the click if the player misses hitting a room
			}
			
			if(removeIndex > -1)
			{
				Script.CurrentFloorBlueprint.YardLayouts.RemoveAt(removeIndex);
				DrawNewFloor();
				if(Script.CurrentFloorBlueprint.YardLayouts.Count < 1)
					this.convexOutline.Clear();
			}
		}

		/// <summary>
		/// Allows player to remove rooms by clicking on them
		/// </summary>
		void DeletingRooms (Event currentEvent)
		{
			ResetClickUp(currentEvent);

			Vector3 gridPoint;
			bool pointFound = GetPrecisePoint(out gridPoint, currentEvent);

			if(pointFound == false)
			{
				return;
			}
			
			gridCursor = gridPoint + currentFloorHeight;
			
			int removeIndex = -1;

			if(Script.CurrentFloorBlueprint == null || Script.CurrentFloorBlueprint.RoomBlueprints == null)
				return;
			
			for(int i = 0; i < Script.CurrentFloorBlueprint.RoomBlueprints.Count; i++)
			{
				Vector3[] walls = Script.CurrentFloorBlueprint.RoomBlueprints[i].PerimeterWalls.ToArray();
				
				for(int index = 0; index < walls.Length; index++)
					walls[index] += currentFloorHeight;
				
				convexOutline.Clear();
				
				if(BCUtils.PointInPolygonXZ(gridPoint, walls))
				{
					convexColor = convexDeleteColor;
					
					MeshInfo meshInfo = BCMesh.GenerateGenericMeshInfo(walls);
					for(int n = 0; n < meshInfo.Triangles.Length; n += 3)
					{
						convexOutline.Add(new Vector3[3] { meshInfo.Vertices[meshInfo.Triangles[n]], meshInfo.Vertices[meshInfo.Triangles[n + 1]], meshInfo.Vertices[meshInfo.Triangles[n + 2]]});
					}
					
					
					if(TestMouseClick(currentEvent))
					{
						Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Delete Room");
						removeIndex = i;
					}
					break;
				}
				
			}
			
			if(pointFound)
			{
				TestMouseClick(currentEvent);
				// Use up the click if the player misses hitting a room
			}
			
			if(removeIndex > -1)
			{
				Script.CurrentFloorBlueprint.RoomBlueprints.RemoveAt(removeIndex);
				DrawNewFloor();
				if(Script.CurrentFloorBlueprint.YardLayouts.Count < 1)
					this.convexOutline.Clear();
			}
		}

		/// <summary>
		/// Allows players to move room walls by clicking and dragging
		/// </summary>
		private void ModifyingFloors (Event currentEvent)
		{
			if(Script.CurrentFloorBlueprint == null)
				return;
			
			if(Script.CurrentFloorBlueprint.RoomBlueprints.Count < 1)
				return;
			
			// Select a room to modify
			RoomBlueprint roomBp = null;
			
			// Gets the points in the world, if false, returns;
			Vector3 gridPoint;
			if(GetGridPoint(out gridPoint, true, currentEvent) == false)
				return;
			
			Vector3 precisePoint;
			if(GetPrecisePoint(out precisePoint, currentEvent) == false)
				return;

			if(grabbingRoomCorner == true)
			{
				DragBuildingCorners(currentEvent);
			}

			// Sees if the player is dragging
			
			int roomIndex = -1;
			
			for(int i = 0; i < Script.CurrentFloorBlueprint.RoomBlueprints.Count; i++)
			{
				roomBp = Script.CurrentFloorBlueprint.RoomBlueprints[i];
				if(BCUtils.PointInPolygonXZ(precisePoint, roomBp.PerimeterWalls.ToArray<Vector3>()))
				{
					roomIndex = i;
					break;
				}
			}
			
			int wallPointIndex = -1;
			
			if(selectedRoomIndex > -1)
				roomIndex = selectedRoomIndex;
			
			if(roomIndex < 0)
			{
				TestMouseClick(currentEvent);
				Script.SelectedPath.Clear();
				return;
			}
			
			
			roomBp = Script.CurrentFloorBlueprint.RoomBlueprints[roomIndex];
			
			wallPointIndex = BCUtils.GetIndexOfWall(gridPoint, roomBp);

			if(TestMouseClick(currentEvent))
			{
				Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Move Room Wall");

				// The second we click, we now should be tracking what room Id is being tracked
				selectedRoomIndex = roomIndex;
				selectedLineIndex = wallPointIndex;

				if(BCUtils.IsPointOnCornerPoint(gridPoint, roomBp.PerimeterWalls) > -1)
				{
					grabbingRoomCorner = true;
				}
			}

			if(ResetClickUp(currentEvent))
			{
				if(selectedRoomIndex > -1)
				{
					roomBp = Script.CurrentFloorBlueprint.RoomBlueprints[selectedRoomIndex];
					BCUtils.CollapseWallLines(ref roomBp);
				}
				
				selectedRoomIndex = -1;
				selectedLineIndex = -1;
				
			}
			
			// If something has been selected, then it is set to the selection automatically
			if(selectedRoomIndex > -1)
				roomIndex = selectedRoomIndex;
			
			if(selectedLineIndex > -1)
				wallPointIndex = selectedLineIndex;
			
			// Checks to see if a wall was found, if not return
			if(wallPointIndex < 0 || roomIndex < 0)
			{
				Script.SelectedPath.Clear();
				return;
			}

			if(wallPointIndex < roomBp.PerimeterWalls.Count - 1)
			{
				gridCursorColor = greenGridCursor;
				if(BCUtils.IsPointOnCornerPoint(gridPoint, roomBp.PerimeterWalls) > -1)
				{
					Vector3 prevPoint = roomBp.PerimeterWalls[BCUtils.GetIndexAtPlus(wallPointIndex, -1, roomBp.PerimeterWalls)];
					Script.SelectedPath = new List<Vector3>() { prevPoint, roomBp.PerimeterWalls[wallPointIndex], roomBp.PerimeterWalls[wallPointIndex + 1]};
				}
				else
				{
					Script.SelectedPath = new List<Vector3>() { roomBp.PerimeterWalls[wallPointIndex], roomBp.PerimeterWalls[wallPointIndex + 1]};
				}
			}
			
			
			if(currentEvent.type == EventType.MouseDrag)
			{
				Vector3 selectedLine = (roomBp.PerimeterWalls[wallPointIndex]) - (roomBp.PerimeterWalls[wallPointIndex + 1]);
				Vector3 direction = selectedLine.normalized;
				
				GetGridPoint(out gridPoint, false, currentEvent);
				
				if(direction.x == 0)
				{
					roomBp.PerimeterWalls[wallPointIndex] = new Vector3(gridPoint.x, roomBp.PerimeterWalls[wallPointIndex].y, roomBp.PerimeterWalls[wallPointIndex].z);
					roomBp.PerimeterWalls[wallPointIndex + 1] = new Vector3(gridPoint.x, roomBp.PerimeterWalls[wallPointIndex + 1].y, roomBp.PerimeterWalls[wallPointIndex + 1].z);
					if(wallPointIndex == 0)
						roomBp.PerimeterWalls[roomBp.PerimeterWalls.Count - 1] = roomBp.PerimeterWalls[wallPointIndex];
					else if(wallPointIndex == roomBp.PerimeterWalls.Count - 2)
						roomBp.PerimeterWalls[0] = roomBp.PerimeterWalls[wallPointIndex + 1];
					
				}
				else if(direction.z == 0)
				{
					roomBp.PerimeterWalls[wallPointIndex] = new Vector3(roomBp.PerimeterWalls[wallPointIndex].x, roomBp.PerimeterWalls[wallPointIndex].y, gridPoint.z);
					roomBp.PerimeterWalls[wallPointIndex + 1] = new Vector3(roomBp.PerimeterWalls[wallPointIndex + 1].x, roomBp.PerimeterWalls[wallPointIndex + 1].y, gridPoint.z);
					if(wallPointIndex == 0)
						roomBp.PerimeterWalls[roomBp.PerimeterWalls.Count - 1] = roomBp.PerimeterWalls[wallPointIndex];
					else if(wallPointIndex == roomBp.PerimeterWalls.Count - 2)
						roomBp.PerimeterWalls[0] = roomBp.PerimeterWalls[wallPointIndex + 1];
				}
			}
		}

		/// <summary>
		/// Allows players to add points along a flat wall
		/// </summary>
		void AddingFloorPoints(Event currentEvent)
		{
			if(Script.CurrentFloorBlueprint.RoomBlueprints.Count < 1)
				return;
			
			Vector3 gridPoint;
			if(GetGridPoint(out gridPoint, true, currentEvent) == false)
				return;
			
			Vector3 precisePoint;
			if(GetPrecisePoint(out precisePoint, currentEvent) == false)
				return;
			
			// Room we are working with
			RoomBlueprint roomBp = null;
			
			// SUPER HACK
			if(grabbingRoomCorner == true)
			{
				DragBuildingCorners(currentEvent);
				return;
			}
			
			if(selectedRoomIndex < 0 && selectedLineIndex < 0)
			{
				// Figure out that the user has selected a corner
				
				// If the player is not dragging, then we figure out where the player is dragging around
				
				// Finds the room to edit
				int roomIndex = -1;
				for(int i = 0; i < Script.CurrentFloorBlueprint.RoomBlueprints.Count; i++)
				{
					RoomBlueprint room = Script.CurrentFloorBlueprint.RoomBlueprints[i];
					if(BCUtils.PointInPolygonXZ(precisePoint, room.PerimeterWalls.ToArray<Vector3>()))
					{
						roomIndex = i;
						break;
					}
				}

				// If the player clicks on nothing then do not deselect this
				if(roomIndex < 0)
				{
					TestMouseClick(currentEvent);
					gridCursorColor = DeleteButtonColor;
					gridCursor = precisePoint + currentFloorHeight;
					Script.SelectedPath.Clear();
					return;
				}
				
				// Finds the room in question
				roomBp = Script.CurrentFloorBlueprint.RoomBlueprints[roomIndex];
				
				int wallPointIndex = BCUtils.GetIndexOfWall(gridPoint, roomBp);
				
				// Checks to see if a wall was found, if not return
				if(wallPointIndex < 0 || roomIndex < 0)
				{
					TestMouseClick(currentEvent);
					gridCursorColor = DeleteButtonColor;
					gridCursor = precisePoint + currentFloorHeight;
					Script.SelectedPath.Clear();
					return;
				}
				
				GetGridPoint(out gridPoint, false, currentEvent);
				
				if(wallPointIndex < roomBp.PerimeterWalls.Count - 1)
				{
					gridCursorColor = Color.green;
					gridCursor = gridPoint + currentFloorHeight;
					
					int closestPoint = BCUtils.IsPointOnCornerPoint(gridPoint, roomBp.PerimeterWalls);
					
					if(closestPoint > -1)
					{
						Vector3 prev = roomBp.PerimeterWalls[BCUtils.GetIndexAtPlus(closestPoint, -1, roomBp.PerimeterWalls)];
						Script.SelectedPath = new List<Vector3>() { prev, roomBp.PerimeterWalls[closestPoint], roomBp.PerimeterWalls[closestPoint + 1] };
					}
					else
					{
						Script.SelectedPath = new List<Vector3>() { roomBp.PerimeterWalls[wallPointIndex], roomBp.PerimeterWalls[wallPointIndex + 1] };
					}
				}
				
				if(TestMouseClick(currentEvent, 0))
				{
					// Test to see if on a corner
					if(BCUtils.IsPointOnCornerPoint(gridPoint, roomBp.PerimeterWalls) > -1)
					{
						selectedRoomIndex = roomIndex;
						selectedLineIndex = wallPointIndex;
						grabbingRoomCorner = true;
						Script.SelectedPath.Clear();
						return;
					}

					
					// The second we click, we now should be tracking what room Id is being tracked
					selectedRoomIndex = roomIndex;
					selectedLineIndex = wallPointIndex;
					originalStart = roomBp.PerimeterWalls[selectedLineIndex];
					originalEnd = roomBp.PerimeterWalls[selectedLineIndex + 1];
					splitPoint = gridPoint;
					wallDirectionAtSplit = (roomBp.PerimeterWalls[wallPointIndex + 1] - roomBp.PerimeterWalls[wallPointIndex]).normalized;

					Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Add Wall Point");

					// Insert the new items
					Script.CurrentFloorBlueprint.RoomBlueprints[selectedRoomIndex].PerimeterWalls.Insert(selectedLineIndex + 1, gridPoint);
					Script.CurrentFloorBlueprint.RoomBlueprints[selectedRoomIndex].PerimeterWalls.Insert(selectedLineIndex + 1, gridPoint);
				}
			}
			else if(selectedRoomIndex > -1 && selectedLineIndex > -1)
			{
				roomBp = Script.CurrentFloorBlueprint.RoomBlueprints[selectedRoomIndex];
				
				if(currentEvent.type == EventType.MouseDrag)
				{
					GetGridPoint(out gridPoint, false, currentEvent);

					// Figures out if the cursor is in front of or behind the wall			
					Vector2 precise2d = new Vector2(precisePoint.x, precisePoint.z);
					Vector2 split2d = new Vector2(splitPoint.x, splitPoint.z);
					
					Vector2 direction2d = (precise2d - split2d).normalized;
					Vector2 wallDir2d = new Vector2(wallDirectionAtSplit.x, wallDirectionAtSplit.z);
					
					float dot = Vector2.Dot(direction2d, wallDir2d);
					
					int startPoint = BCUtils.GetIndexAtPlus(selectedLineIndex, 0, roomBp.PerimeterWalls);
					int firstPoint = BCUtils.GetIndexAtPlus(selectedLineIndex, 1, roomBp.PerimeterWalls);
					int secondPoint = BCUtils.GetIndexAtPlus(selectedLineIndex, 2, roomBp.PerimeterWalls);
					int endPoint = BCUtils.GetIndexAtPlus(selectedLineIndex, 3, roomBp.PerimeterWalls);
					
					bool startPointIsZero = false;
					bool endPointIsZero = false;
					
					if(startPoint == 0)
						startPointIsZero = true;
					if(endPoint == 0)
						endPointIsZero = true;
					
					if(wallDir2d.x == 0)
					{
						if(dot < 0)
						{
							roomBp.PerimeterWalls[startPoint] = new Vector3(gridPoint.x, 0, roomBp.PerimeterWalls[startPoint].z);
							roomBp.PerimeterWalls[firstPoint] = new Vector3(gridPoint.x, 0, roomBp.PerimeterWalls[firstPoint].z);
							roomBp.PerimeterWalls[secondPoint] = splitPoint;
							roomBp.PerimeterWalls[endPoint] = originalEnd;
							
							Script.SelectedPath = new List<Vector3>() { roomBp.PerimeterWalls[startPoint], roomBp.PerimeterWalls[firstPoint]};
							
							// Special Cases
							if(startPointIsZero)
								roomBp.PerimeterWalls[roomBp.PerimeterWalls.Count - 1] = new Vector3(gridPoint.x, 0, roomBp.PerimeterWalls[startPoint].z);
							if(endPointIsZero)
								roomBp.PerimeterWalls[roomBp.PerimeterWalls.Count - 1] = originalEnd;
							
						}
						else
						{
							roomBp.PerimeterWalls[startPoint] = originalStart;
							roomBp.PerimeterWalls[firstPoint] = splitPoint;
							roomBp.PerimeterWalls[secondPoint] = new Vector3(gridPoint.x, 0, roomBp.PerimeterWalls[secondPoint].z);
							roomBp.PerimeterWalls[endPoint] = new Vector3(gridPoint.x, 0, roomBp.PerimeterWalls[endPoint].z);
							
							Script.SelectedPath = new List<Vector3>() { roomBp.PerimeterWalls[secondPoint], roomBp.PerimeterWalls[endPoint]};
							
							// Special Cases
							if(startPointIsZero)
								roomBp.PerimeterWalls[roomBp.PerimeterWalls.Count - 1] = originalStart;
							if(endPointIsZero)
								roomBp.PerimeterWalls[roomBp.PerimeterWalls.Count - 1] = new Vector3(gridPoint.x, 0, roomBp.PerimeterWalls[endPoint].z);
						}
					}
					else if(wallDir2d.y == 0)
					{
						if(dot < 0)
						{
							roomBp.PerimeterWalls[startPoint] = new Vector3(roomBp.PerimeterWalls[startPoint].x, 0, gridPoint.z);
							roomBp.PerimeterWalls[firstPoint] = new Vector3(roomBp.PerimeterWalls[firstPoint].x, 0, gridPoint.z);
							roomBp.PerimeterWalls[secondPoint] = splitPoint;
							roomBp.PerimeterWalls[endPoint] = originalEnd;
							
							Script.SelectedPath = new List<Vector3>() { roomBp.PerimeterWalls[startPoint], roomBp.PerimeterWalls[firstPoint]};
							
							// Special Cases
							if(startPointIsZero)
								roomBp.PerimeterWalls[roomBp.PerimeterWalls.Count - 1] = new Vector3(roomBp.PerimeterWalls[startPoint].x, 0, gridPoint.z);
							if(endPointIsZero)
								roomBp.PerimeterWalls[roomBp.PerimeterWalls.Count - 1] = originalEnd;
						}
						else
						{
							roomBp.PerimeterWalls[startPoint] = originalStart;
							roomBp.PerimeterWalls[firstPoint] = splitPoint;
							roomBp.PerimeterWalls[secondPoint] = new Vector3(roomBp.PerimeterWalls[secondPoint].x, 0, gridPoint.z);
							roomBp.PerimeterWalls[endPoint] = new Vector3(roomBp.PerimeterWalls[endPoint].x, 0, gridPoint.z);
							
							Script.SelectedPath = new List<Vector3>() { roomBp.PerimeterWalls[secondPoint], roomBp.PerimeterWalls[endPoint]};
							
							// Special Cases
							if(startPointIsZero)
								roomBp.PerimeterWalls[roomBp.PerimeterWalls.Count - 1] = originalStart;
							if(endPointIsZero)
								roomBp.PerimeterWalls[roomBp.PerimeterWalls.Count - 1] = new Vector3(roomBp.PerimeterWalls[endPoint].x, 0, gridPoint.z);
						}
					}
				}
				
				if(ResetClickUp(currentEvent))
				{
					selectedRoomIndex = -1;
					selectedLineIndex = -1;
					BCUtils.CollapseWallLines(ref roomBp);
				}
				
				return;
			}
			else
			{
				if(ResetClickUp(currentEvent))
				{
					selectedRoomIndex = -1;
					selectedLineIndex = -1;
					BCUtils.CollapseWallLines(ref roomBp);
				}
			}
			
			return;
		}
		
		// TODO: Combine Deleting Rooms and Deleting Yards into one general deleting any boxes thing
		// ============================ OPTIONS FOR EVERYTHING ON THE FLOOR ==================================

		#region New Region
		
		/// <summary>
		/// Sets the ceiling height for floor.
		/// </summary>
		private static void SetCeilingHeightForFloor (FloorBlueprint currentFloorBlueprint, float allCeilingHeightInt)
		{
			if(allCeilingHeightInt < 2 || allCeilingHeightInt > 2.90001)
				return;

			for(int i = 0; i < currentFloorBlueprint.RoomBlueprints.Count; i++)
			{
				currentFloorBlueprint.RoomBlueprints[i].CeilingHeight = allCeilingHeightInt;
			}
		}


		void HighlightGenericRooms ()
		{
			indexesOfGenericRooms.Clear();
			genericRoomInset.Clear();

			for(int i = 0; i < Script.CurrentFloorBlueprint.RoomBlueprints.Count; i++)
			{
				if(Script.CurrentFloorBlueprint.RoomBlueprints[i].RoomType == RoomType.Generic)
					indexesOfGenericRooms.Add(i);
			}
			
			Color filledColor = blueprintColor;
			filledColor.a = 1;

			for(int i = 0; i < indexesOfGenericRooms.Count; i++)
			{
				RoomBlueprint roomBp = Script.CurrentFloorBlueprint.RoomBlueprints[indexesOfGenericRooms[i]];

				MeshInfo meshInfo = BCMesh.GenerateGenericMeshInfo(roomBp.PerimeterWalls);
				
				for(int j = 0; j < meshInfo.Triangles.Length - 1; j += 3)
				{
					int tri1 = meshInfo.Triangles[j];
					int tri2 = meshInfo.Triangles[j + 1];
					int tri3 = meshInfo.Triangles[j + 2];
					
					Vector3 p1 = meshInfo.Vertices[tri1] + currentFloorHeight;
					Vector3 p2 = meshInfo.Vertices[tri2] + currentFloorHeight;
					Vector3 p3 = meshInfo.Vertices[tri3] + currentFloorHeight;

					genericRoomInset.Add(new Vector3[3] { p1, p2, p3 });
				}
			}
		}

		#endregion

	}
}
