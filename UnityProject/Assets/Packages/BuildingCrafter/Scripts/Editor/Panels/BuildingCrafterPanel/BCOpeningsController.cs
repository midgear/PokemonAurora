using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{

	public partial class BuildingCrafterPanel : Editor
	{
		private enum OpeningType
		{
			None = -1,
			Door = 0,
			Window = 1
		}

		// =============================================
		// DOOR / WINDOW Editing Properties
		// =============================================
		int editingIndex = -1;
		int lastEditingIndex = -1;
		OpeningType openingType = OpeningType.None;
		bool isEditing = false;

		// Laying the windows
		private bool openDoorOrWindow = false;
		private Vector3 startOpening;
		private Vector3 endOpening;

		// New Doors and Windows
//		private DoorInfo newDoor;
//		private WindowInfo newWindow;

		// USED TO LAY DOORS AND WINDOWS, FINDS THE RIGHT POINT AND TESTS TO ENSURE IT IS VALID
		private void LayingWindowAndDoors (Event currentEvent)
		{
			if(CheckForEscape(currentEvent, true))
			{
				Script.EditingState = EditingState.None;
				startPoint = Vector3.zero;
				openDoorOrWindow = false;
				return;
			}

			if(Script.CurrentFloorBlueprint == null)
				return;

			bool validPlacement = true;
			
			float floorHeight = 0;
			
			// ==========================================================
			// = Casting out the Raycast to figure out the floor layout =
			// ==========================================================
			Vector3 gridPoint = Vector3.zero;

			if(this.GetGridPoint(out gridPoint, true, currentEvent) == false)
				return;

			// The player can lay half meter windows that get downsized to about 30 cm openings
			// NOTE: Doors have to be at least a meter wide
			if(Script.EditingState == EditingState.LayWindows || openDoorOrWindow == false)
				gridPoint = new Vector3(Mathf.Round(gridPoint.x * 2) / 2, floorHeight, Mathf.Round(gridPoint.z * 2) / 2);
			else
			{
				gridPoint = new Vector3(Mathf.Round(gridPoint.x * 2) / 2, floorHeight, Mathf.Round(gridPoint.z * 2) / 2);
				if ((startOpening.x % 1) != 0)
				{
					if(gridPoint.x > startOpening.x && gridPoint.x % 1 == 0)
						gridPoint = new Vector3(Mathf.Round(gridPoint.x * 2) / 2 - 0.5f, floorHeight, Mathf.Round(gridPoint.z * 2) / 2);
					if(gridPoint.x < startOpening.x && gridPoint.x % 1 == 0)
						gridPoint = new Vector3(Mathf.Round(gridPoint.x * 2) / 2 + 0.5f, floorHeight, Mathf.Round(gridPoint.z * 2) / 2);
				}
				else if((startOpening.z % 1) != 0)
				{
					if(gridPoint.z > startOpening.z && gridPoint.z % 1 == 0)
						gridPoint = new Vector3(Mathf.Round(gridPoint.x * 2) / 2, floorHeight, Mathf.Round(gridPoint.z * 2) / 2 - 0.5f);
					if(gridPoint.z < startOpening.z && gridPoint.z % 1 == 0)
						gridPoint = new Vector3(Mathf.Round(gridPoint.x * 2) / 2, floorHeight, Mathf.Round(gridPoint.z * 2) / 2 + 0.5f);
				}
				else
				{
					gridPoint = new Vector3(Mathf.Round(gridPoint.x), floorHeight, Mathf.Round(gridPoint.z));
				}
			}

			validPlacement = false;
			
			// Tests if selecting a starting point
			if(openDoorOrWindow == false && Script.CurrentFloorBlueprint.RoomBlueprints != null)
			{
				for(int j = 0; j < Script.CurrentFloorBlueprint.RoomBlueprints.Count; j++)
				{
					// Creates the wall you will need to test against
					List<Vector3> walls = Script.CurrentFloorBlueprint.RoomBlueprints[j].PerimeterWalls;
					
					if(walls == null)
						return;
					
					List<int> indexes = BCUtils.BetweenTwoPointOnALine(gridPoint, walls);
					
					if(indexes.Count == 2)
					{
						if(walls[indexes[0]].x == walls[indexes[1]].x)
						{
							gridPoint = new Vector3(walls[indexes[0]].x, gridPoint.y, gridPoint.z);
							validPlacement = true;
						}
						if(walls[indexes[0]].z == walls[indexes[1]].z)
						{
							gridPoint = new Vector3(gridPoint.x, gridPoint.y, walls[indexes[0]].z);
							validPlacement = true;
						}
					}
					else if(indexes.Count == 3)
					{
						gridPoint = walls[indexes[0]]; // NOTE: the 0 index is the actual point that was detected as the join)
						validPlacement = true;
					}
				}
			}

			if(openDoorOrWindow == true)
			{
				validPlacement = BCUtils.TestValidRoomOpening(startOpening, gridPoint, Script.CurrentFloorBlueprint);
			}

			// Right click breakout WITHOUT drag
			if(TestRightQuickClick(currentEvent))
			{
				openDoorOrWindow = false;
			}
			
			// ============================
			// Code for laying out windows and allowing clicks
			if(validPlacement == true && Script.EditingState == EditingState.LayWindows)
			{
				if(openDoorOrWindow == false)
				{
					gridCursorColor = greenGridCursor;
					gridCursor = gridPoint + currentFloorHeight;

					if(TestMouseClick(currentEvent, 0))
					{
						openDoorOrWindow = true;
						// Find which walls the door could be on, and add that to a list
						
						startOpening = gridPoint;
					}
				}
				else if(openDoorOrWindow == true)
				{			
					Handles.DrawWireDisc(startOpening + currentFloorHeight, Vector3.up, 0.2f);
					
					// Creates the window to be displayed in place of the system
					Vector3[] window = BCMesh.WindowOutline(startOpening, gridPoint, Script.NewWindow, Vector3.zero);

					for(int i = 0; i < window.Length; i++)
					{
						window[i] += currentFloorHeight;
					}

					Handles.color = Color.green;
					Handles.DrawAAPolyLine(4f, window);
				}
				
				if(currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && openDoorOrWindow == true)
				{
					openDoorOrWindow = false;
					// Add a new window to this system
					if(startOpening != gridPoint)
					{
						Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Add Window");

						endOpening = gridPoint;

						WindowInfo newWindow = Script.NewWindow;
						newWindow.Start = startOpening;
						newWindow.End = endOpening;

						Script.CurrentFloorBlueprint.Windows.Add(newWindow);

						DrawNewWindowOutlines(currentFloorHeight);
					}
					EditorGUIUtility.hotControl = controlId;
					startOpening = Vector3.zero;
					endOpening = Vector3.zero;
					
					currentEvent.Use();
				}
			}
			
			// In case drawing of doors can't be done, then reset it
			
			if(validPlacement == false && (Script.EditingState == EditingState.LayDoors || Script.EditingState == EditingState.LayWindows))
			{
				gridCursorColor = invisibleCursor;

				if(TestMouseClick(currentEvent, 0) && openDoorOrWindow == true)
				{
					openDoorOrWindow = false;
					
					DrawNewDoorOutlines(currentFloorHeight);
					startOpening = Vector3.zero;
					endOpening = Vector3.zero;
				}
			}
			
			// ====================================
			// ====== Door placement ==============
			// ====================================
			if(validPlacement == true && Script.EditingState == EditingState.LayDoors)
			{
				if(openDoorOrWindow == false)
				{
					gridCursorColor = greenGridCursor;
					gridCursor = gridPoint + currentFloorHeight;
					
					if(TestMouseClick(currentEvent, 0))
					{
						Undo.RegisterCompleteObjectUndo(this, "Undo Start Door");

						openDoorOrWindow = true;
						startOpening = gridPoint;
					}
				}
				else if(openDoorOrWindow == true)
				{			
					// Creates the window to be displayed in place of the system
					Vector3[] door = BCMesh.DoorOutline(startOpening, gridPoint, Script.NewDoor, Vector3.zero);
					
					for(int i = 0; i < door.Length; i++)
					{
						door[i] += currentFloorHeight;
					}
					
					Handles.color = Color.green;
					Handles.DrawAAPolyLine(4, door);
				}
				
				if(TestMouseClick(currentEvent, 0) && openDoorOrWindow == true)
				{
					openDoorOrWindow = false;
					// Add a new window to this system
					if(startOpening != gridPoint)
					{
						endOpening = gridPoint;
						Script.NewDoor.Start = startOpening;
						Script.NewDoor.End = endOpening;
						Script.NewDoor.Direction = 1;

						Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Add Door");

						Script.CurrentFloorBlueprint.Doors.Add(Script.NewDoor);

						// To determine if a door is on a corner, then change to a type of door
						for(int j = 0; j < Script.CurrentFloorBlueprint.RoomBlueprints.Count; j++)
						{
							// Creates the wall you will need to test against
							List<Vector3> walls = Script.CurrentFloorBlueprint.RoomBlueprints[j].PerimeterWalls;
							
							if(walls == null)
								return;

							DoorInfo newestDoor = Script.CurrentFloorBlueprint.Doors[Script.CurrentFloorBlueprint.Doors.Count - 1];

							List<int> startIndexes = BCUtils.BetweenTwoPointOnALine(newestDoor.Start, walls);
							List<int> endIndexes = BCUtils.BetweenTwoPointOnALine(newestDoor.End, walls);
							if(startIndexes.Count == 3 || endIndexes.Count == 3)
							{
								newestDoor.DoorType = DoorTypeEnum.SkinnyOpen;
								Script.CurrentFloorBlueprint.Doors[Script.CurrentFloorBlueprint.Doors.Count - 1] = newestDoor;
								break;
							}
						}

						DrawNewDoorOutlines(currentFloorHeight);
					}
					startOpening = Vector3.zero;
					endOpening = Vector3.zero;
				}
			}

			// Does a click in case the user doesn't click right on the item
			if(TestMouseClick(currentEvent, 0))
			{

			}

			ResetClickUp(currentEvent);
		}

		private void DeleteWindowsAndDoors(Event currentEvent)
		{
			if(CheckForEscape(currentEvent, true))
			{
				return;
			}

			Vector3 gridPoint;
			if(GetGridPoint(out gridPoint, true, currentEvent) == false)
				return;

			gridCursorColor = Color.red;
			gridCursor = gridPoint + currentFloorHeight;
			
			List<DoorInfo> selectedDoors = new List<DoorInfo>();
			List<WindowInfo> selectedWindows = new List<WindowInfo>();
			List<Vector3> deleteOutlines = new List<Vector3>();
			
			for (int i = 0; i < Script.CurrentFloorBlueprint.Doors.Count; i++) 
			{
				var door = Script.CurrentFloorBlueprint.Doors [i];

				if (BCUtils.TestBetweenTwoPoints (gridPoint, door.Start, door.End)) {
					selectedDoors.Add (door);
					deleteOutlines.AddRange (GetHighlightOpening (door.Start, door.End, 0, door.DoorHeight));
				}
			}
			
			for (int i = 0; i < Script.CurrentFloorBlueprint.Windows.Count; i++) 
			{
				var window = Script.CurrentFloorBlueprint.Windows [i];

				if (BCUtils.TestBetweenTwoPoints (gridPoint, window.Start, window.End)) {
					selectedWindows.Add (window);
					deleteOutlines.AddRange (GetHighlightOpening (window.Start, window.End, window.BottomHeight, window.TopHeight));
				}
			}
			
			highlightOpeningColor = Color.red;
			highlightedOpening = deleteOutlines.ToArray<Vector3>();

			if(TestMouseClick(currentEvent, 0))
			{
				Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Delete Opening");

				foreach(var selectedDoor in selectedDoors)
					Script.CurrentFloorBlueprint.Doors.Remove(selectedDoor);
				
				foreach(var selectedWindow in selectedWindows)
					Script.CurrentFloorBlueprint.Windows.Remove(selectedWindow);
				
				DrawNewDoorOutlines(currentFloorHeight);
				DrawNewWindowOutlines(currentFloorHeight);

				EditorUtility.SetDirty(Script.BuildingBlueprint);
			}
			
			ResetClickUp(currentEvent);
		}

		void EyedropOpenings(Event currentEvent)
		{
			Vector3 gridPoint;
			bool properClick = GetGridPoint(out gridPoint, true, currentEvent);
			
			Vector3 precisePoint;
			bool properPoint = GetPrecisePoint(out precisePoint, currentEvent);
			
			if(properClick == false || properPoint == false)
			{
				gridCursorColor = invisibleCursor;
				return;
			}

			// Sets the selector to the precise point
			gridCursor = precisePoint + currentFloorHeight;
			gridCursorColor = greenGridCursor;
			
			bool foundWindow = false;
			bool foundDoor = false;
			int openingIndex = -1;

			// First checks for a window
			for(int i = 0; i < Script.CurrentFloorBlueprint.Windows.Count; i++)
			{
				WindowInfo window = Script.CurrentFloorBlueprint.Windows[i];
				
				if(BCUtils.TestBetweenTwoPoints(gridPoint, window.Start, window.End))
				{
					foundWindow = true;
					openingIndex = i;
					break;
				}
			}

			if(foundWindow == false)
			{
				// If a door is at the same spot, override and do the door instead
				for(int i = 0; i < Script.CurrentFloorBlueprint.Doors.Count; i++)
				{
					DoorInfo door = Script.CurrentFloorBlueprint.Doors[i];
					
					if(BCUtils.TestBetweenTwoPoints(gridPoint, door.Start, door.End))
					{
						foundDoor = true;
						openingIndex = i;
						break;
					}
				}
			}


			if(openingIndex < 0)
			{
				TestMouseClick(currentEvent, 0);
				ResetClickUp(currentEvent);
				ClearHighlightingOpening();
				return;
			}

			Color highlightColor = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.5f);

			// Display the window to be eyedroppered
			if(foundWindow)
			{
				WindowInfo window = Script.CurrentFloorBlueprint.Windows[openingIndex];
				SetHighlightOpening(highlightColor, window.Start, window.End, window.BottomHeight - 0.1f, window.TopHeight + 0.1f);
			}
			else if(foundDoor)
			{
				DoorInfo door = Script.CurrentFloorBlueprint.Doors[openingIndex];
				SetHighlightOpening(highlightColor, door.Start, door.End, 0, door.DoorHeight + 0.1f);
			}
			else
				ClearHighlightingOpening();

			// Tests if there is a click and opens up the window
			if(TestMouseClick(currentEvent, 0) && openingIndex > -1)
			{
				if(foundWindow)
					Script.NewWindow = Script.CurrentFloorBlueprint.Windows[openingIndex];
				else
					Script.NewDoor = Script.CurrentFloorBlueprint.Doors[openingIndex];
			}

			ResetClickUp(currentEvent);
		}

		void EditDoorWindowProperties (Event currentEvent)
		{
			if(Script.CurrentFloorBlueprint == null)
				return;

			if(editingIndex > -1 && isEditing == true)
			{
				if(openingType == OpeningType.Door)
					GUI.Window(1, editRect, EditDoorWinPanel, windowTitle);
				else if(openingType == OpeningType.Window)
					GUI.Window(2, editRect, EditWindowWinPanel, windowTitle);
			}

			Vector3 gridPoint;
			bool properClick = GetGridPoint(out gridPoint, true, currentEvent);

			Vector3 precisePoint;
			bool properPoint = GetPrecisePoint(out precisePoint, currentEvent);

			if(properClick == false || properPoint == false)
			{
				gridCursorColor = invisibleCursor;
				return;
			}
				

			// Sets the selector to the precise point
			gridCursor = precisePoint + currentFloorHeight;
			gridCursorColor = greenGridCursor;
			
			bool foundOpening = false;
			
			// Finds the editing index when looking around
			if(isEditing == false)
			{
				// First checks for a window
				for(int i = 0; i < Script.CurrentFloorBlueprint.Windows.Count; i++)
				{
					WindowInfo window = Script.CurrentFloorBlueprint.Windows[i];
					
					if(BCUtils.TestBetweenTwoPoints(gridPoint, window.Start, window.End))
					{
						editingIndex = i;
						openingType = OpeningType.Window;
						foundOpening = true;
						break;
					}
				}
				
				// If a door is at the same spot, override and do the door instead
				for(int i = 0; i < Script.CurrentFloorBlueprint.Doors.Count; i++)
				{
					DoorInfo door = Script.CurrentFloorBlueprint.Doors[i];
					
					if(BCUtils.TestBetweenTwoPoints(gridPoint, door.Start, door.End))
					{
						editingIndex = i;
						openingType = OpeningType.Door;;
						foundOpening = true;
						break;
					}
				}
				
				if(foundOpening == false)
				{
					editingIndex = -1;
					openingType = OpeningType.None;
				}
			}
			
			if(currentEvent.type == EventType.MouseUp && currentEvent.button == 1 && editingIndex > -1)
			{
				editingIndex = -1;
				openingType = OpeningType.None;
				isEditing = false;
			}
			
			
			if(editingIndex < 0 || openingType == OpeningType.None)
			{
				ResetClickUp(currentEvent);
				isEditing = false;
			}
			
			if(lastEditingIndex != editingIndex)
			{
				if(openingType == OpeningType.Door && (editingIndex >= Script.CurrentFloorBlueprint.Doors.Count || editingIndex < 0))
					openingType = OpeningType.None;
				if(openingType == OpeningType.Window && editingIndex >= Script.CurrentFloorBlueprint.Windows.Count || editingIndex < 0)
					openingType = OpeningType.None;

				highlightOpeningColor = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.2f);
				switch(openingType)
				{
				case(OpeningType.Door): // Door
					SetHighlightOpening(Script.CurrentFloorBlueprint.Doors[editingIndex].Start, Script.CurrentFloorBlueprint.Doors[editingIndex].End, 0, 3);
					break;
				case(OpeningType.Window): // Window
					SetHighlightOpening(Script.CurrentFloorBlueprint.Windows[editingIndex].Start, Script.CurrentFloorBlueprint.Windows[editingIndex].End, 0, 3f);
					break;
				default:
					ClearHighlightingOpening();
					break;
				}
				lastEditingIndex = editingIndex;
			}
			
			// Tests if there is a click and opens up the window
			if(TestMouseClick(currentEvent, 0) && editingIndex > -1 && isEditing == false)
			{
				editRect = new Rect(currentEvent.mousePosition.x, currentEvent.mousePosition.y, 200, 285);
				isEditing = true;
				if(openingType == OpeningType.Door)
					windowTitle = "Door " + editingIndex;
				else if(openingType == OpeningType.Window)
					windowTitle = "Window " + editingIndex;
			}
		}

		private void NewDoorPanel()
		{
			DoorInfo door = this.ShowDoorOptions(Script.NewDoor, false);

			if(door != Script.NewDoor)
			{
				Undo.RecordObject(this, "Change New Door Style");
				Script.NewDoor = door;
			}
		}
		
		private void EditDoorWinPanel(int i)
		{
			if(editingIndex < 0 || editingIndex >= Script.CurrentFloorBlueprint.Doors.Count)
			{
				editingIndex = -1;
				return;
			}

			DoorInfo doorInfo = this.ShowDoorOptions(Script.CurrentFloorBlueprint.Doors[editingIndex]);

			GUILayout.FlexibleSpace();

			Color backgroundOriginal = GUI.backgroundColor;
			GUI.backgroundColor = DeleteButtonColor;
			if(GUILayout.Button("Delete Door"))
			{
				Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Delete Door");
				Script.CurrentFloorBlueprint.Doors.RemoveAt(editingIndex);
				editingIndex = -1;
				DrawNewFloor();
				return;
			}
			GUI.backgroundColor = backgroundOriginal;

			if(doorInfo != Script.CurrentFloorBlueprint.Doors[editingIndex])
			{
				Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Modify Door Properties");
				Script.CurrentFloorBlueprint.Doors[editingIndex] = doorInfo;
				DrawNewFloor();
			}
		}

		private DoorInfo ShowDoorOptions(DoorInfo doorInfo, bool showSwitchOptions = true)
		{
			GUILayout.Label("Door Type");
			doorInfo.DoorType = (DoorTypeEnum)EditorGUILayout.EnumPopup(doorInfo.DoorType);
			
			if(doorInfo.DoorType == DoorTypeEnum.Standard
			   || doorInfo.DoorType == DoorTypeEnum.Heavy
			   || doorInfo.DoorType == DoorTypeEnum.Closet 
			   || doorInfo.DoorType == DoorTypeEnum.DoorToRoof)
			{
				// Only 2 meter and less doors can have a direction
				if((doorInfo.Start - doorInfo.End).magnitude < 2.5f)
				{
					doorInfo.isLockable = EditorGUILayout.Toggle("Is Lockable", doorInfo.isLockable);
					doorInfo.isAutolocking = EditorGUILayout.Toggle("Is Autolocking", doorInfo.isAutolocking);
					doorInfo.isFireDoor = EditorGUILayout.Toggle("Is FireDoor", doorInfo.isFireDoor);
					doorInfo.IsForcedPlain = EditorGUILayout.Toggle("Is Forced Plain", doorInfo.IsForcedPlain);
					doorInfo.IsStartOpen = EditorGUILayout.Toggle("Door Spawns Open", doorInfo.IsStartOpen);

					if(doorInfo.IsStartOpen)
					{
						EditorGUILayout.LabelField("Start Angle");
						doorInfo.StartOpenAngle = (float)EditorGUILayout.IntSlider(Mathf.RoundToInt(doorInfo.StartOpenAngle), 0, 90);
					}

//					To allow max angle to be greater than 90 degrees, have to move the door within the frame so it is offset depending on the direction
//					EditorGUILayout.LabelField("Max Open Angle");
//					int offset = Mathf.RoundToInt(doorInfo.MaxOpenAngleOffset) + 90;
//					int newAngle = EditorGUILayout.IntSlider(offset, 5, 180);
//					if(newAngle < doorInfo.StartOpenAngle)
//						newAngle = Mathf.RoundToInt(doorInfo.StartOpenAngle);
//					doorInfo.MaxOpenAngleOffset = newAngle - 90;

					if(showSwitchOptions && GUILayout.Button("Switch Door Direction") ) 
					{
						doorInfo.Direction = doorInfo.Direction * -1;
					}
				}
				
				if(doorInfo.IsDoubleDoor == false)
				{
					if((doorInfo.Start - doorInfo.End).magnitude == 1)
					{				
						if(showSwitchOptions && GUILayout.Button("Switch Anchor"))
						{
							Vector3 oldEnd = doorInfo.End;
							doorInfo.Direction = doorInfo.Direction * -1;
							doorInfo.End = doorInfo.Start;
							doorInfo.Start = oldEnd;
						}
					}
				}
			}

			return doorInfo;
		}

		private void NewWindowPanel()
		{
			WindowInfo windowInfo = this.ShowWindowOptions(Script.NewWindow);

			if(windowInfo != Script.NewWindow)
			{
				Undo.RecordObject(this, "Change New Window Style");
				Script.NewWindow = windowInfo;
			}
		}
		
		private void EditWindowWinPanel(int i)
		{
			if(editingIndex < 0 || editingIndex >= Script.CurrentFloorBlueprint.Windows.Count)
			{
				editingIndex = -1;
				return;
			}

			WindowInfo window = this.ShowWindowOptions(Script.CurrentFloorBlueprint.Windows[editingIndex]);

			GUILayout.FlexibleSpace();

			Color backgroundOriginal = GUI.backgroundColor;
			GUI.backgroundColor = DeleteButtonColor;
			if(GUILayout.Button("Delete Window"))
			{
				Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Delete Window");
				Script.CurrentFloorBlueprint.Windows.RemoveAt(editingIndex);
				editingIndex = -1;
				DrawNewFloor();
				return;
			}
			GUI.backgroundColor = backgroundOriginal;

			bool windowChanged = Script.CurrentFloorBlueprint.Windows[editingIndex] != window;

			if(windowChanged)
			{
				Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Modify Window");
				Script.CurrentFloorBlueprint.Windows[editingIndex] = window;
				DrawNewFloor();
			}
		}

		private WindowInfo ShowWindowOptions(WindowInfo window)
		{
			GUILayout.Label("Window Type");
			WindowTypeEnum newType = (WindowTypeEnum)EditorGUILayout.EnumPopup(window.WindowType);
			
			if(newType != WindowTypeEnum.Override)
				window.WindowType = newType;
			
			// Adds a range option for the windows
			float bottom = window.BottomHeight;
			float top = window.TopHeight;
			
			EditorGUILayout.BeginHorizontal();
			float maxHeight = 3;
			if(Script.CurrentFloorBlueprint != null)
				maxHeight = Script.CurrentFloorBlueprint.Height - 0.11f;
			EditorGUILayout.MinMaxSlider(ref bottom, ref top, 0.01f, maxHeight);
			bottom = EditorGUILayout.FloatField(bottom, GUILayout.MaxWidth(34f));
			top = EditorGUILayout.FloatField(top, GUILayout.MaxWidth(34f));
			EditorGUILayout.EndHorizontal();
			
			bottom = (float)System.Math.Round(bottom, 2);
			top = (float)System.Math.Round(top, 2);
			float height = top - bottom;
			
			// Ensure no values are outside the range
			top = Mathf.Clamp(top, 0.01f, Script.CurrentFloorBlueprint.Height - 0.11f);
			bottom = Mathf.Clamp(bottom, 0.01f, Script.CurrentFloorBlueprint.Height - 0.11f);
			
			if(WindowInfo.GetWindowType(bottom, top) == WindowTypeEnum.Override)
			{
				window.WindowType = WindowTypeEnum.Override;

				if(window.OverriddenTop != top && height < 0.25f)
					window.OverriddenTop = window.BottomHeight + 0.25f;
				else
					window.OverriddenTop = top;
				
				if(window.OverriddenBottom != bottom && height < 0.25f)
					window.OverriddenBottom = window.TopHeight - 0.25f;
				else
					window.OverriddenBottom = bottom;
			}
			
			window.IsWindowEmpty = EditorGUILayout.Toggle("Window Is Empty", window.IsWindowEmpty);

			GameObject windowPrefab = null;
			if(window.OverriddenWindowType != null)
				windowPrefab = window.OverriddenWindowType.gameObject;

			GUILayout.BeginHorizontal();

			GameObject windowType = null;

			if(window.IsWindowEmpty == false)
				windowType = (GameObject)EditorGUILayout.ObjectField(windowPrefab, typeof(GameObject), false);

			if(window.OverriddenWindowType != null && GUILayout.Button("x", GUILayout.MaxWidth(20), GUILayout.MaxHeight(14)) || window.IsWindowEmpty)
			{
				window.OverriddenWindowType = null;
				windowType = null;
			}
			GUILayout.EndHorizontal();

			if(windowType != null)
			{
				if(windowType.GetComponent<BCWindow>() == null)
					Debug.Log("Can't add " + windowType.name + " because it does not have the BCWindow component");
				else
					window.OverriddenWindowType = windowType.GetComponent<BCWindow>();
			}

			return window;
		}
	}
}