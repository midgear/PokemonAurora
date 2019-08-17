using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{

	public partial class BuildingCrafterPanel : Editor 
	{
		// New control methods
		static Texture2D[] roofEditing = null;

		int roofEditType = -1;

		// Laying a Roof
		bool layRoofBox = false;
		Vector3 startPoint = Vector3.zero;
		
		// Roof outlines
		private List<Vector3[]> roofBases = new List<Vector3[]>();
		
		private List<Vector3[]> roofLefts = new List<Vector3[]>();
		private List<Vector3[]> roofRights = new List<Vector3[]>();
		private List<Vector3[]> roofFronts = new List<Vector3[]>();
		private List<Vector3[]> roofBacks = new List<Vector3[]>();

		private void DisplayRoofEditor()
		{
			// If the system has changed whatever editing state is going on, then update
			if(Script.FloorEditType != Script.LastFloorEditType)
			{
				Script.PreviousFloor = Script.SelectedFloor;
				ClearFloorOutlines();

				Script.SelectedFloor = -1;
				if(Script.BuildingBlueprint.Floors.Count > 0)
				{
					currentFloorHeight = Vector3.up * Script.BuildingBlueprint.Floors.Count * 3 + Script.BuildingBlueprint.BlueprintGroundHeight;
					UpdateRoofOutlines(currentFloorHeight);
					DrawGreyFloorOutline(Script.BuildingBlueprint.Floors.Count - 1);
				}

				editingIndex = -1;

				Script.LastFloorEditType = Script.FloorEditType;
				Script.EditingState = EditingState.None;
			}

			GUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Roof");

			int newEditType = GUILayout.SelectionGrid(roofEditType, roofEditing, maxButtons, GUILayout.Height(30f), GUILayout.MinWidth(20f));
			if(newEditType != roofEditType)
			{
				roofEditType = newEditType;
			}

			GUILayout.EndHorizontal();
			GUILayout.Space(5f);

			if(Script.EditingState != EditingState.None && roofEditType < 0)
				SetSelectionFromEditingState(Script.EditingState);

			switch(roofEditType)
			{
			case 0:
				Script.EditingState = EditingState.LayRoof;
				break;
			case 1:
				Script.EditingState = EditingState.DeleteRoof;
				break;
			case 2:
				Script.EditingState = EditingState.EditRoofProperties;
				break;
			}

			Color backgroundOriginal = GUI.backgroundColor;
			GUI.backgroundColor = DeleteButtonColor;
			if(GUILayout.Button("Delete All Roofs", GUILayout.Height(20)))
			{
				Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Delete All Roofs");
				Script.BuildingBlueprint.RoofInfos.Clear();
				currentFloorHeight = Vector3.up * Script.BuildingBlueprint.Floors.Count * 3 + Script.BuildingBlueprint.BlueprintGroundHeight;
				UpdateRoofOutlines(currentFloorHeight);
				editingIndex = -1;
			}
			GUI.backgroundColor = backgroundOriginal;

			UpdateRoofOutlines(currentFloorHeight);
		}


		
		void LaySlantedRoofBoxes(Event currentEvent)
		{
			Vector3 gridPoint;

			bool pointGood = GetGridPoint(out gridPoint, true, currentEvent);

			if(pointGood == false)
			{
				return;
			}

			convexOutline.Clear();
			
			// Lay out some grids from point to point
			if(layRoofBox == false)
			{	
				gridCursor = gridPoint + currentFloorHeight;
				gridCursorColor = greenGridCursor;

				if(TestMouseClick(currentEvent))
				{
					startPoint = gridPoint;
					layRoofBox = true;
				}	
			}
			else
			{
				gridCursorColor = invisibleCursor;

				Vector3[] box = new Vector3[4] { startPoint, new Vector3(startPoint.x, 0, gridPoint.z), gridPoint,  new Vector3(gridPoint.x, 0, startPoint.z)};
				
				for(int i = 0; i < box.Length; i++)
				{
					box[i] += currentFloorHeight;
				}

				convexColor = Color.grey;
				
				Handles.DrawAAPolyLine(4, box);
				Handles.DrawAAPolyLine(4, box[0], box[3]);
				convexOutline = new List<Vector3[]> { box };

				if(TestMouseClick(currentEvent))
				{
					Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Create Roof");

					// Give all the information to the Roof infos area here. Includes a default direction (longer) and the middle of the roof is calculated
					bool direction = false;
					
					float width = Mathf.Abs(box[1].x - box[2].x);
					float length = Mathf.Abs(box[0].z - box[1].z);
					
					if(width == 0 || length == 0)
					{
						layRoofBox = false;
					}
					else
					{
						if(length < width)
							direction = true;
						
						Script.BuildingBlueprint.RoofInfos.Add(new RoofInfo(startPoint, gridPoint, direction, false, false));
						startPoint = Vector3.zero;
						layRoofBox = false;
						Script.BuildingBlueprint.RoofInfos.Last().UpdateBaseOutline();
					}
					
					UpdateRoofOutlines(currentFloorHeight);
				}
			}
			
			ResetClickUp(currentEvent);
			
		}
		
		private void DeleteSlantedRoofBoxes(Event currentEvent)
		{
			Vector3 precisePoint;

			if(GetPrecisePoint(out precisePoint, currentEvent) == false)
				return;

			// Test against the roof boxes to see if they are within the polygone
			int hitIndex = SelectBox(precisePoint, Script.BuildingBlueprint.RoofInfos);

			if(hitIndex > -1)
			{
				HighlightRoof(hitIndex, DeleteButtonColor);

				if(TestMouseClick(currentEvent))
				{
					Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Delete Roof");
					Script.BuildingBlueprint.RoofInfos.RemoveAt(hitIndex);
					UpdateRoofOutlines(Script.BuildingBlueprint.Floors.Count * Vector3.up * 3f);
				}
			}
			else
			{
				convexOutline.Clear();
			}

			ResetClickUp(currentEvent);
		}

		private void EditSlantedRoofProperties(Event currentEvent)
		{
			ResetClickUp(currentEvent);

			Vector3 precisePoint;
			if(GetPrecisePoint(out precisePoint, currentEvent) == false)
				return;

			if(editingIndex < 0)
			{
				int hitIndex = SelectBox(precisePoint, Script.BuildingBlueprint.RoofInfos);

				if(hitIndex > -1)
				{
					HighlightRoof(hitIndex, convexGreenColor);
					
					if(TestMouseClick(currentEvent, 0) && hitIndex > -1)
					{
						editRect = new Rect(currentEvent.mousePosition.x, currentEvent.mousePosition.y, 200, 200);
						editingIndex = hitIndex;
						windowTitle = "Roof " + editingIndex;
					}
				}
				else
				{
					TestMouseClick(currentEvent);
					convexOutline.Clear();
				}
					
			}
			else
			{
				if(TestMouseClick(currentEvent, 1) && editingIndex > -1)
				{
					editingIndex = -1;
				}

				if(editingIndex > -1)
				{
					GUI.Window(1, editRect, EditRoofWinPanel, windowTitle);
				}

				if(TestMouseClick(currentEvent))
				{
					editingIndex = -1;
				}
			}

			if(editingIndex < 0)
				ResetClickUp(currentEvent);


		}

		private void EditRoofWinPanel(int i)
		{
			RoofInfo roofInfo = Script.BuildingBlueprint.RoofInfos[editingIndex];
			roofInfo.IsRoofEndSlanted = EditorGUILayout.Toggle("Is End Slanted", roofInfo.IsRoofEndSlanted);
			roofInfo.IsRoofStartSlanted = EditorGUILayout.Toggle("Is Start Slanted", roofInfo.IsRoofStartSlanted);

			if(GUILayout.Button("SwitchRoofDirection"))
			{
				Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Switch Roof Direction");

				roofInfo.IsRoofDirectionZ = !roofInfo.IsRoofDirectionZ;
			}

			GUILayout.FlexibleSpace();

			Color backgroundOriginal = GUI.backgroundColor;
			GUI.backgroundColor = DeleteButtonColor;
			if(GUILayout.Button("Delete Roof"))
			{
				Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Delete Roof");

				Script.BuildingBlueprint.RoofInfos.RemoveAt(editingIndex);
				editingIndex = -1;
				UpdateRoofOutlines(currentFloorHeight);
				return;
			}
			GUI.backgroundColor = backgroundOriginal;

			if(Script.BuildingBlueprint.RoofInfos[editingIndex].IsRoofStartSlanted != roofInfo.IsRoofStartSlanted
			   || Script.BuildingBlueprint.RoofInfos[editingIndex].IsRoofEndSlanted != roofInfo.IsRoofEndSlanted)
			{
				Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Modify Roof");
			}

			Script.BuildingBlueprint.RoofInfos[editingIndex] = roofInfo;
			UpdateRoofOutlines(Script.BuildingBlueprint.Floors.Count * Vector3.up * 3);
		}


		// HELPERS

		/// <summary>
		/// Returns the index in the roof infos of the box
		/// </summary>
		/// <returns>The box.</returns>
		/// <param name="cursorPoint">Cursor point.</param>
		/// <param name="roofInfos">Roof infos.</param>
		private int SelectBox(Vector3 cursorPoint, List<RoofInfo> roofInfos)
		{
			for(int roofIndex = 0; roofIndex < roofInfos.Count; roofIndex++)
			{
				if(BCUtils.PointInPolygonXZ(cursorPoint, roofInfos[roofIndex].RoofBaseOutline) == true)
					return roofIndex;
			}
			return -1;
		}

		private void HighlightRoof(int hitIndex, Color color)
		{
			if(hitIndex < 0 || hitIndex > Script.BuildingBlueprint.RoofInfos.Count - 1)
				return;

			List<Vector3[]> convex = new List<Vector3[]>();
			convex.Add(Script.BuildingBlueprint.RoofInfos[hitIndex].LeftRoof.ToArray<Vector3>());
			convex.Add(Script.BuildingBlueprint.RoofInfos[hitIndex].RightRoof.ToArray<Vector3>());
			convex.Add(Script.BuildingBlueprint.RoofInfos[hitIndex].FrontRoof.ToArray<Vector3>());
			convex.Add(Script.BuildingBlueprint.RoofInfos[hitIndex].BackRoof.ToArray<Vector3>());
			for(int i = 0; i < convex.Count; i++)
			{
				for(int n = 0; n < convex[i].Length; n++)
					convex[i][n] += Vector3.up * 3 * (Script.BuildingBlueprint.Floors.Count);
			}
			
			convexColor = color;
			convexOutline = convex;
		}

		void UpdateRoofOutlines (Vector3 floorHeight)
		{
			roofBases.Clear();
			roofFronts.Clear();
			roofBacks.Clear();
			roofLefts.Clear();
			roofRights.Clear();

			if(Script.BuildingBlueprint.RoofInfos == null)
				return;

			int floorNumbers = Script.BuildingBlueprint.Floors.Count;

			floorHeight = Vector3.up * 3 * floorNumbers;

			for(int i = 0; i < Script.BuildingBlueprint.RoofInfos.Count; i++)
			{
				RoofInfo roofInfo = Script.BuildingBlueprint.RoofInfos[i];
				
				roofBases.Add(new Vector3[5] { 
					roofInfo.BackLeftCorner + floorHeight + (Vector3.left - Vector3.back) * 0.2f,
					roofInfo.BackRightCorner + floorHeight + (Vector3.right - Vector3.back) * 0.2f, 
					roofInfo.FrontRightCorner + floorHeight + (Vector3.right - Vector3.forward) * 0.2f, 
					roofInfo.FrontLeftCorner + floorHeight + (Vector3.left - Vector3.forward) * 0.2f, 
					roofInfo.BackLeftCorner + floorHeight + (Vector3.left - Vector3.back) * 0.2f});
				
				Vector3[] tempLeft = roofInfo.LeftRoof;
				Vector3[] tempRight = roofInfo.RightRoof;
				Vector3[] tempBack = roofInfo.BackRoof;
				Vector3[] tempFront = roofInfo.FrontRoof;
				
				for(int n = 0; n < tempFront.Length; n++)
					tempFront[n] += floorHeight;
				
				for(int n = 0; n < tempBack.Length; n++)
					tempBack[n] += floorHeight;
				
				for(int n = 0; n < tempRight.Length; n++)
					tempRight[n] += floorHeight;
				
				for(int n = 0; n < tempLeft.Length; n++)
					tempLeft[n] += floorHeight;
				
				roofLefts.Add(tempLeft);
				roofRights.Add(tempRight);
				roofFronts.Add(tempFront);
				roofBacks.Add(tempBack);
			}
		}
	}
}
