using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace BuildingCrafter
{

	[CustomEditor(typeof(BuildingCrafterGenerator))]
	public partial class BuildingCrafterPanel : Editor 
	{
		#region Variables for cursor

		private Vector3 gridCursor;
		private Color gridCursorColor = Color.green;

		private readonly static Color greenGridCursor = Color.green;
		private readonly static Color redGridCursor = Color.red;
		private readonly static Color invisibleCursor = new Color(0, 0, 0 , 0);
		public readonly static Color DeleteButtonColor = new Color(0.906f, 0.298f, 0.0235f);

		#endregion

		#region Colors for displaying handles

		private static Color blueprintColor = new Color(0.28f, 0.49f, 0.796f);
//		private static Color blueprintColorHalf = new Color(0.28f * 2, 0.49f * 2, 0.796f * 2);
		private static Color blueprintColorTrans = new Color(0.28f, 0.49f, 0.796f, 0.2f);
		
		private static Color yardColor = new Color(0.1529f, 0.682f, 0.376f);
		private static Color yardColorTrans = new Color(0.1529f, 0.682f, 0.376f, 0.1f);

		private readonly static Color convexDeleteColor = Color.red;
		private readonly static Color convexGreenColor = Color.green;

		private Color convexColor = Color.green;
		private List<Vector3[]> convexOutline = new List<Vector3[]>();
		
		private Color highlightOpeningColor = Color.green;

		#endregion

		bool generationFoldout 
		{
			get
			{
				if(Script.BuildingBlueprint == null)
					return false;
				else
					return Script.BuildingBlueprint.ShowGenerationOptions;
			}
			set
			{
				if(Script.BuildingBlueprint != null)
					Script.BuildingBlueprint.ShowGenerationOptions = value;
			}
		}

		bool atlasFoldout 
		{
			get
			{
				if(Script.BuildingBlueprint == null)
					return false;
				else
					return Script.BuildingBlueprint.ShowAtlasOptions;
			}
			set
			{
				if(Script.BuildingBlueprint != null)
					Script.BuildingBlueprint.ShowAtlasOptions = value;
			}
		}

		string[] editSystem = new string[4] { "Floors", "Roof", "Exterior", "Style" };

		// Tooltips and labels
		GUIContent wallCappersGenLabel = 		new GUIContent("Generate Wall Cappers", "Adds 'tops' to each floor for 3rd person views.");
		GUIContent windowGenerationLabel = 		new GUIContent("Create Static Windows", "To render Unity's GI correctly, windows can't be static.");
		GUIContent generationOptionsLabel = 	new GUIContent("Generate Options", "Options upon generation that will execute when 'Generate All' or others is selected.");
		GUIContent generateLODsToggle = 		new GUIContent("Generate LODs", "Building Crafter can have automatic LODs set up to reduce the number of draws needed to render a building.");
		GUIContent generateFBXAssetLabel = 		new GUIContent("Always Generate FBX", "Every time this mesh is generated, it will delete and create a FBX version of this at the assigned point.");
		GUIContent pathToExportedFbxLabel = 	new GUIContent("Exported FBX Path", "The path that the exported FBX will write to. \n\nWARNING: Will overwrite existing FBX file.");
		GUIContent copyMaterialsLabel = 		new GUIContent("Copy Materials to FBX", "When selected, when the object is saved to FBX, it will copy new Materials to the /Materials path next to the new FBX file.");
		GUIContent copyTexturesLabel = 			new GUIContent("Copy Textures to FBX", "When selected, when the object is saved to FBX, it will copy new Textures to the /Textures path next to the new FBX file.");
		GUIContent createBrokeWindowsLabel = 	new GUIContent("Create Broken Windows", "With this enabled, broken windows will be generated and hidden by Building Crafter");
		GUIContent generateAllLabel = 			new GUIContent("Generate All", "Select to delete the entire old building and regenerate it from the building blueprint");
		GUIContent useAtlasLabel =				new GUIContent("Use Atlas", "Use Atlases for this building. Combines textures to improve performance");
		GUIContent exportFBXFile =	 			new GUIContent("Export FBX", "Export currently generated FBX to new file. Copy textures / copy materials settings are located under generate settings");

		public override void OnInspectorGUI ()
		{
			serializedObject.Update();

			BuildingBlueprint newBpField = (BuildingBlueprint)EditorGUILayout.ObjectField("Building Blueprint", Script.BuildingBlueprint, typeof(BuildingBlueprint), true);

			if(newBpField != Script.BuildingBlueprint)
			{
				Object[] undoers = null;

				if(Script.BuildingBlueprint != null)
					undoers = new Object[2] {Script, Script.BuildingBlueprint};
				else
					undoers = new Object[1] { Script }; 

				Undo.RegisterCompleteObjectUndo(undoers, "Change Blueprint");

				ResetSelectedBuilding();
				Script.BuildingBlueprint = newBpField;
				this.OnEnable();
			}

//			if(buildingBlueprintGameObject.objectReferenceValue == null)
//			{
//				EditorGUILayout.LabelField("Drag a blueprint gameObject to begin");
//			}
			
			if(GUILayout.Button("Create new building blueprint", GUILayout.Height(30f)))
			{
				OnDisableLiveView();

				GameObject newBp = BCMesh.GenerateEmptyGameObject("Create Empty Building Blueprint");
				BuildingBlueprint buildingBp = newBp.AddComponent<BuildingBlueprint>();
				newBp.name = "NewBuilding";

				BuildingStyle genericBuildingStylePrefab = ((BuildingStyle)Resources.Load("BuildingStyles/GenericBuildingStyle"));
				if(genericBuildingStylePrefab != null)
				{
					buildingBp.BuildingStyle = Resources.Load("BuildingStyles/GenericBuildingStyle") as BuildingStyle;
				}
				else
					Debug.LogError("Generic Building Style does not exist");

				Script.BuildingBlueprint = buildingBp;
				Script.BuildingBlueprint.Floors.Add(new FloorBlueprint());
				Script.FloorEditType = EditFloorType.Floor;
				Script.SelectedFloor = 0;
				UpdateCurrentFloorHeight(Script.SelectedFloor);
			}

			if(buildingBlueprintGameObject.objectReferenceValue == null || Script.BuildingBlueprint == null)
			{
				serializedObject.ApplyModifiedProperties();
				return;
			}

			GUILayout.BeginHorizontal();
			bool exportJson = GUILayout.Button("Export JSON");
			bool importJson = GUILayout.Button("Import JSON");
			bool exportFBX = GUILayout.Button(exportFBXFile);
			GUILayout.EndHorizontal();

			if(buildingBlueprintObject != null)
				buildingBlueprintObject.Update();

			GUILayout.Space(7);

			if(Script.BuildingBlueprint != null)
				Script.BuildingBlueprint.name = EditorGUILayout.TextField("Building Name", Script.BuildingBlueprint.name);


			GUILayout.Space(7);

			EditFloorType newType = (EditFloorType)GUILayout.SelectionGrid((int)Script.FloorEditType, editSystem, 4);

			if(newType != Script.FloorEditType)
			{
				Undo.RecordObject(Script, "Change Edit Type");
				Script.FloorEditType = newType;
			}

			EditorGUILayout.Separator();

			// Shows the type of editing type you are doing
			switch(Script.FloorEditType)
			{
			case(EditFloorType.Floor):
				DisplayFloorEditor();
				break;
			case(EditFloorType.Roof):
				DisplayRoofEditor();
				break;
			case(EditFloorType.Exterior):
				DisplayExteriorEditor();
				break;
			case(EditFloorType.Style):
				DisplayStyleEditor();				
				if(Event.current.type == EventType.Repaint) // Required to ensure hovering works. May want to move this to OnSceneView so it happens constantly
					EditorUtility.SetDirty(Script);
				break;
			}
			// End the show of the type

			Script.FloorEditType = Script.LastFloorEditType;

			GUILayout.Space(5);

			if(Script.BuildingBlueprint.Floors == null || Script.BuildingBlueprint.Floors.Count < 1)
				GUI.enabled = false;

//			if(false)
//			{
//				if(GUILayout.Button("Test With Outlines", GUILayout.Height(20)))
//				{
//					List<Vector3[]> allRoomOutlines = new List<Vector3[]>();
//
//					for(int i = 0; i < Script.CurrentFloorBlueprint.RoomBlueprints.Count; i++)
//					{
//						allRoomOutlines.Add(Script.CurrentFloorBlueprint.RoomBlueprints[i].PerimeterWalls.ToArray<Vector3>());
//					}
//					showPoints = true;
//					showLines = true;
//
//					TempDisplayOutline = BCUtils.CombineVectors(allRoomOutlines);
//				}
//			}

			EditorGUILayout.BeginHorizontal();

			Color backgroundOriginal = GUI.backgroundColor;
			GUI.backgroundColor = Color.green;
			bool clickedGenerateAll = GUILayout.Button(generateAllLabel, GUILayout.Height(30));
			GUI.backgroundColor = backgroundOriginal;

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(14f);

			generationFoldout = EditorGUILayout.Foldout(generationFoldout, generationOptionsLabel);
			EditorGUILayout.EndHorizontal();

			if(generationFoldout)
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(24f);
				EditorGUILayout.BeginVertical();

				// Generate Static Windows
				DisplayUndoableToggleLeft(wallCappersGenLabel, ref Script.BuildingBlueprint.GenerateCappers, "Add Wall Cappers To Each Floor", Script.BuildingBlueprint);
				DisplayUndoableToggleLeft(windowGenerationLabel, ref Script.BuildingBlueprint.WindowsGenerateAsStatic, "Set Windows Static Upon Generation", Script.BuildingBlueprint);
				DisplayUndoableToggleLeft(createBrokeWindowsLabel, ref Script.BuildingBlueprint.GenerateBrokenGlass, "Set Broken Glass Generation", Script.BuildingBlueprint);
				DisplayUndoableToggleLeft(generateLODsToggle, ref Script.BuildingBlueprint.GenerateLOD, "Change LOD Generation Toggle", Script.BuildingBlueprint);

				// FBX Stuff
				DisplayUndoableToggleLeft(generateFBXAssetLabel, ref Script.BuildingBlueprint.GenerateFBXAssetsAndPrefab, "Toggle FBX Export", Script.BuildingBlueprint);

				if(Script.BuildingBlueprint.GenerateFBXAssetsAndPrefab)
				{
					DisplayUndoablePath(pathToExportedFbxLabel, ref Script.BuildingBlueprint.ExportedMeshPath, "fbx", "Change Mesh Export", Script.BuildingBlueprint);
				}

				DisplayUndoableToggleLeft(copyMaterialsLabel, ref Script.BuildingBlueprint.ExportMaterials, "Change Copy Materials", Script.BuildingBlueprint);
				if(Script.BuildingBlueprint.ExportMaterials == true)
					DisplayUndoableToggleLeft(copyTexturesLabel, ref Script.BuildingBlueprint.ExportTextures, "Change Copy Textures", Script.BuildingBlueprint);
				else if(Script.BuildingBlueprint.ExportMaterials == false && Script.BuildingBlueprint.ExportTextures == true) // Never allow textures to be generated without materials
				{
					int currentGroup = Undo.GetCurrentGroup();
					Undo.RegisterCompleteObjectUndo(Script.BuildingBlueprint, "Change Copy Textures and Materials");
					Script.BuildingBlueprint.ExportTextures = false;
					Undo.CollapseUndoOperations(currentGroup);
				}

				DisplayUndoableToggleLeft(useAtlasLabel, ref Script.BuildingBlueprint.UseAtlases, "Use Atlas", Script.BuildingBlueprint);

				// Atlases always save in a subfolder by the BuildingStyle. May never need this.
//				bool updateAtlasSaveLocation = GUILayout.Button("Change Atlas Save Location");
//				if(updateAtlasSaveLocation)
//				{
//					BCFiles.CreateBuildingCrafterAssetDirectories();
//
//					string currentAtlasFolder = Script.BuildingBlueprint.AtlasParentFolder;
//					if(string.IsNullOrEmpty(currentAtlasFolder))
//					{
//						currentAtlasFolder = Application.dataPath + BCFiles.Atlases;
//						Script.BuildingBlueprint.AtlasParentFolder = currentAtlasFolder;
//					}
//
//					string newPath = EditorUtility.OpenFolderPanel("Update Atlas Location", Script.BuildingBlueprint.AtlasParentFolder, "");
//					if(string.IsNullOrEmpty(newPath) == false)
//					{
//						Undo.RecordObject(Script.BuildingBlueprint, "Change Atlas Location");
//						Script.BuildingBlueprint.AtlasParentFolder = newPath;
//					}
//				}

				GUILayout.FlexibleSpace();
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
			}

			GUILayout.Space(2f);

			// End Atlas Options

			if(clickedGenerateAll)
			{
				TempDisplayPoints = new List<Vector3>();
				TempDisplayOutline = new List<Vector3[]>();

				if(Script.EditingState == EditingState.UpdatingPivot)
					Script.EditingState = EditingState.None;

				BCGenerator.GenerateFullBuilding(Script.BuildingBlueprint);

				if(Script.BuildingBlueprint.GenerateFBXAssetsAndPrefab)
					exportFBX = true;				
			}

			if(exportJson)
			{
				BCJsonExporterImporterMenu.ExportToJson(Script.BuildingBlueprint);
			}

			if(importJson)
			{
				BCJsonExporterImporterMenu.ImportJson(ref Script.BuildingBlueprint);
			}

			if(exportFBX)
			{
				BuildingBlueprint buildingBp = Script.BuildingBlueprint;

				if(Script.BuildingBlueprint.GetComponentInChildren<MeshFilter>() == null)
					Debug.LogError("Building Crafter: Building has no geometry generated. Generate the building first before export");
				else
				{
					string savedPath = UnityFBXExporter.ExporterMenu.ExportGameObject(buildingBp.gameObject, buildingBp.ExportMaterials, buildingBp.ExportTextures, buildingBp.ExportedMeshPath);
					if(savedPath != null)
					{
						int savedPathIndex = savedPath.LastIndexOf("/Assets");
						if(savedPathIndex > -1)
							Script.BuildingBlueprint.ExportedMeshPath = savedPath.Remove(0, savedPathIndex);
						else if(savedPath.Length > 0 && savedPath.IndexOf("Assets") == 0)
							Script.BuildingBlueprint.ExportedMeshPath = "/" + savedPath;
					}
				}
			}

			GUILayout.BeginHorizontal();

			if(GUILayout.Button("Gen. Floors", GUILayout.Height(30)))
			{
				BCGenerator.GenerateOnlyInteriors(Script.BuildingBlueprint);
			}

			if(GUILayout.Button("Gen. Walls", GUILayout.Height(30)))
			{
				BCGenerator.GenerateOutsideWalls(Script.BuildingBlueprint, false);
			}

			if(GUILayout.Button("Gen. Roofs", GUILayout.Height(30)))
			{
				BCGenerator.GenerateRoofs(Script.BuildingBlueprint, false);
			}

			GUILayout.EndHorizontal();

			GUILayout.Space(5);

			backgroundOriginal = GUI.backgroundColor;
			GUI.backgroundColor = DeleteButtonColor;
			if(GUILayout.Button("Delete Generate Rooms", GUILayout.Height(20)))
			{
				BCGenerator.DestroyGeneratedBuilding(Script.BuildingBlueprint);
			}
			GUI.backgroundColor = backgroundOriginal;

			if(GUI.enabled == false)
				GUI.enabled = true;

			EditorGUILayout.Separator();

			serializedObject.ApplyModifiedProperties();

			if(buildingBlueprintObject != null)
				buildingBlueprintObject.ApplyModifiedProperties();
		}

		void OnSceneGUI()
		{
			if(Script.BuildingBlueprint == null)
				return;

			Event currentEvent = Event.current;

			// Draw all base items before drawing the stuff on top of it
			if(Script.EditingState != Script.LastEditingState)
				Script.LastEditingState = Script.EditingState;

			DisplayYard();
			DisplayCurrentFloorPlanWalls();
			DisplayStairs();

			DisplayWallPointIndexes();

			currentFloorHeight = Vector3.up * Script.CurrentFloor * 3 + Script.BuildingBlueprint.BlueprintGroundHeight;

			if(Script.BuildingBlueprint.transform.childCount < 1 || Script.BuildingBlueprint.BuildingRotation != new Quaternion())
			{
				DisplayWalls();
				DrawGenericRoomOutlines();
			}
			
			if(Script.FloorEditType != EditFloorType.Roof)
			{
				DisplayDoors();
				DisplayWindows();
			}

			DisplayGridCursor();
			DisplayConvexOutline();
			DisplayHighlightedOpening();
			DisplayOutsideWallOutlines();
			
			if(Script.FloorEditType == EditFloorType.Roof)
				DisplayRoofOutline();

			DisplayRoofPerimeter();

			// TODO: Redorder this to the same as the UI
			switch(Script.EditingState)
			{
			case(EditingState.LayRoomFloors):
				LayingWalls(currentEvent);
				break;

			case(EditingState.LayYard):
				LayingWalls(currentEvent);
				break;
				
			case(EditingState.LayWindows):
				LayingWindowAndDoors(currentEvent);
				break;
				
			case(EditingState.LayDoors):
				LayingWindowAndDoors(currentEvent);
				break;
				
			case(EditingState.LayStairs):
				LayingStairs(currentEvent);
				break;
				
			case(EditingState.DeleteStairs):
				DeletingStairs(currentEvent);
				break;
				
			case(EditingState.DeleteDoorsAndWindows):
				DeleteWindowsAndDoors(Event.current);
				break;
				
			case(EditingState.DeleteRooms):
				DeletingRooms(currentEvent);
				break;

			case(EditingState.DeleteYard):
				DeletingYards(currentEvent);
				break;
				
			case(EditingState.ModifyFloor):
				ModifyingFloors(currentEvent);
				break;
				
			case(EditingState.AddingFloorPoints):
				AddingFloorPoints(currentEvent);
				break;
				
			case(EditingState.EditFloorProperties):
				EditFloorProperties(currentEvent);
				break;
				
			case(EditingState.EditDoorWindowProperties):
				EditDoorWindowProperties(currentEvent);		
				break;
				
			case(EditingState.LayRoof):
				LaySlantedRoofBoxes(currentEvent);
				break;

			case(EditingState.DeleteRoof):
				DeleteSlantedRoofBoxes(currentEvent);
				break;

			case(EditingState.EditRoofProperties):
				EditSlantedRoofProperties(currentEvent);
				break;

			case(EditingState.UpdatingPivot):
				EditPivot(currentEvent);
				break;

			case(EditingState.EyedropDoorWindows):
				EyedropOpenings(currentEvent);
				break;
			}

			// Draws the new wall path on top of everything except a selected path
			DrawCurrentWallPath();
			DrawSelectedPath();

			Handles.color = blueprintColor;
			Handles.DrawSolidDisc(Script.BuildingBlueprint.Transform.position, Vector3.up, 0.1f);
			Handles.Label(Script.BuildingBlueprint.Transform.position, "BP Center");
	//		DisplayOverHangs();

			if(Script.BuildingBlueprint.LiveViewEnabled)
				ShowLiveView();

			// Temp Testing Stuff
			DrawDebugLinesAndPoints();
			// End of Testing Stuff
		}

		private void ResetSelectedBuilding()
		{
			editingIndex = -1;
			lastEditingIndex = -1;
			editingPlan = -1;
			editingRoom = false;
			editingYard = false;
			roofEditType = -1;
		}

		public static void DisplayUndoableToggle(GUIContent labelTitle, ref bool toggleInQuestion, string undoMessage, Object objectToUndo)
		{
			bool changedToggle = EditorGUILayout.Toggle(labelTitle, toggleInQuestion);
			
			if(changedToggle != toggleInQuestion)
			{
				Undo.RegisterCompleteObjectUndo(objectToUndo, undoMessage);
				toggleInQuestion = changedToggle;
			}
		}

		public static void DisplayUndoableToggleLeft(GUIContent labelTitle, ref bool toggleInQuestion, string undoMessage, Object objectToUndo)
		{
			bool changedToggle = EditorGUILayout.ToggleLeft(labelTitle, toggleInQuestion);

			if(changedToggle != toggleInQuestion)
			{
				Undo.RegisterCompleteObjectUndo(objectToUndo, undoMessage);
				toggleInQuestion = changedToggle;
			}
		}

		public static void DisplayUndoableString(GUIContent labelTitle, ref string modifiableString, string undoMessage, Object objectToUndo)
		{
			string changedString = EditorGUILayout.TextField(labelTitle, modifiableString);
			
			if(changedString != modifiableString)
			{
				Undo.RegisterCompleteObjectUndo(objectToUndo, undoMessage);
				modifiableString = changedString;
			}
		}

		public static void DisplayUndoablePath(GUIContent labelTitle, ref string modifiablePathString, string extension, string undoMessage, Object objectToUndo)
		{
			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.PrefixLabel(labelTitle);

			bool oldEnabled = GUI.enabled;
			GUI.enabled = false;
			EditorGUILayout.TextField(modifiablePathString, GUILayout.MinWidth(20f));
			GUI.enabled = oldEnabled;

			string changedString = modifiablePathString;

			if(GUILayout.Button("Set Path", GUILayout.Width(60), GUILayout.Height(14f)))
			{
				string pathFolder = Application.dataPath;
				pathFolder = pathFolder.Remove(pathFolder.LastIndexOf("/Assets"), pathFolder.Length - pathFolder.LastIndexOf("/Assets")) + modifiablePathString;
				pathFolder = pathFolder.Remove(pathFolder.LastIndexOf('/'), pathFolder.Length - pathFolder.LastIndexOf('/'));

				string fileName = modifiablePathString.Remove(0, modifiablePathString.LastIndexOf('/') + 1);

				int lastIndexOfSlash = fileName.LastIndexOf('.');
				if(lastIndexOfSlash > -1)
				{
					fileName = fileName.Remove(lastIndexOfSlash, fileName.Length - lastIndexOfSlash);
				}

				if(System.IO.Directory.Exists(pathFolder))
					changedString = EditorUtility.SaveFilePanel("Update path with ." + extension, pathFolder, fileName, extension);
				else
					changedString = EditorUtility.SaveFilePanelInProject("Update path with ." + extension, fileName, "fbx", "");

				int assetFolderIndex = changedString.LastIndexOf("/Assets");
				if(assetFolderIndex > -1)
				{
					changedString = changedString.Remove(0, assetFolderIndex);
				}
				else if(string.IsNullOrEmpty(changedString) == false && changedString[0] != '/' && changedString.IndexOf("Assets") == 0)
				{
					changedString = "/" + changedString;
				}
			}
			EditorGUILayout.EndHorizontal();

			if(changedString != modifiablePathString && string.IsNullOrEmpty(changedString) == false)
			{
				Undo.RegisterCompleteObjectUndo(objectToUndo, undoMessage);
				modifiablePathString = changedString;
			}
		}

		private void DisplayGridCursor()
		{
			Handles.color = this.gridCursorColor;
			Handles.DrawWireDisc(gridCursor, Vector3.up, 0.25f);
		}
		
		private void DisplayConvexOutline()
		{
			if(convexOutline == null)
				return;
			
			Handles.color = convexColor;
			
			for(int i = 0; i < convexOutline.Count; i++)
			{
				Handles.DrawAAConvexPolygon(convexOutline[i]);
			}
		}
		
		void DisplayHighlightedOpening ()
		{
			if(highlightedOpening == null)
				return;
			
			for(int i = 0; i < highlightedOpening.Length; i += 4)
			{
				Vector3[] outline = new Vector3[4] { highlightedOpening[i], highlightedOpening[i + 1], highlightedOpening[i + 2], highlightedOpening[i + 3], };
				Handles.color = highlightOpeningColor;
				Handles.DrawSolidRectangleWithOutline(outline, highlightOpeningColor, Color.black);
			}
			
		}

		/// <summary>
		/// Draws the current floor wall plans every frame
		/// </summary>
		private void DisplayCurrentFloorPlanWalls()
		{
			if(currentFloorFillInset == null || currentFloorOutline == null)
				return;
			
			Handles.color = blueprintColor;
			
			for(int i = 0; i < currentFloorOutline.Count; i++)
			{
				if(currentFloorOutline[i] != null)
				{
					Handles.DrawAAPolyLine(5, currentFloorOutline[i]);
				}
			}

			if(Script.CurrentFloorBlueprint == null)
				return;
			
			if(Script.CurrentFloorBlueprint.RoomBlueprints != null)
			{
				for(int j = 0; j < Script.CurrentFloorBlueprint.RoomBlueprints.Count; j++)
				{
					if(Script.CurrentFloorBlueprint.RoomBlueprints[j] == null || Script.CurrentFloorBlueprint.RoomBlueprints[j].PerimeterWalls == null)
						continue;
					
	//				for(int i = 0; i < Script.CurrentFloorBlueprint.RoomBlueprints[j].PerimeterWalls.Count; i++)
	//				{
	//					if(i == 0)
	//						Handles.Label(Script.CurrentFloorBlueprint.RoomBlueprints[j].PerimeterWalls[i] + Vector3.up * 1f + currentFloorHeight, i.ToString());
	//					else
	//						Handles.Label(Script.CurrentFloorBlueprint.RoomBlueprints[j].PerimeterWalls[i] + Vector3.up * 1.2f + currentFloorHeight, i.ToString());
	//				}
				}
			}
			
			Handles.color = blueprintColorTrans;
			
			for(int i = 0; i < currentFloorFillInset.Count; i++)
			{
				Handles.DrawAAConvexPolygon(currentFloorFillInset[i]);
				//			Handles.DrawAAConvexPolygon(meshInfo.Vertices[meshInfo.Triangles[i * 3]], meshInfo.Vertices[meshInfo.Triangles[i * 3 + 1]], meshInfo.Vertices[meshInfo.Triangles[i * 3 + 2]]);
			}
		}

		private void DisplayYard()
		{
			if(currentYardOutline == null || currentYardFillInset == null)
				return;
			
			Handles.color = yardColor;
			
			for(int i = 0; i < currentYardOutline.Count; i++)
			{
				if(currentYardOutline[i] != null)
				{
					Handles.DrawAAPolyLine(5, currentYardOutline[i]);
				}
			}

			Handles.color = yardColorTrans;
			
			for(int i = 0; i < currentYardFillInset.Count; i++)
			{
				Handles.DrawAAConvexPolygon(currentYardFillInset[i]);
			}
		}

		private void DisplayWalls()
		{
			if(Script.BuildingBlueprint.LiveViewEnabled == true)
				return;

			if(Script.RoomsBelowCurrentFloor.Count > 0)
			{
				for(int i = 0; i < Script.RoomsBelowCurrentFloor.Count; i++)
				{
					Handles.color = Color.gray;
					Handles.DrawAAPolyLine(4, Script.RoomsBelowCurrentFloor[i]);
				}
			}
			
			if(Script.RoomOutlineBelow.Count > 0)
			{
				for(int i = 0; i < Script.RoomOutlineBelow.Count; i++)
				{
					Handles.color = Color.gray;
					Handles.DrawAAConvexPolygon(Script.RoomOutlineBelow[i]);
				}
			}
		}

		void DrawGenericRoomOutlines ()
		{
			if(highlightGenericRooms && genericRoomInset != null)
			{
				Handles.color = genericRoomColor;
				
				for(int i = 0; i < genericRoomInset.Count; i++)
				{
					Handles.DrawAAConvexPolygon(genericRoomInset[i]);
				}
			}
		}

		/// <summary>
		/// Draws a grid for the ease of use for the user
		/// </summary>
		private void DisplayGridAtFloorHeight()
		{
			// Draw a big grid for ease of use
			//		for(int i = -50; i <= 50; i += 5)
			//		{
			//			Handles.color = Color.blue;
			//			Handles.DrawAAPolyLine(1, new Vector3[2] { new Vector3(i, currentFloorHeight.y, -50f), new Vector3(i, currentFloorHeight.y, 50f)});
			//			Handles.DrawAAPolyLine(1, new Vector3[2] { new Vector3(-50f, currentFloorHeight.y, i), new Vector3(50f, currentFloorHeight.y, i)});
			//		}
		}

		private void DisplayRoofPerimeter()
		{
			if(FloorOutline == null)
				return;
			
			Handles.DrawAAPolyLine(10, FloorOutline);
			
			for(int i = 0; i < FloorOutline.Length; i++)
			{
				if(i == 0)
					Handles.Label(FloorOutline[i] + Vector3.up * 1f + currentFloorHeight, i.ToString());
				else
					Handles.Label(FloorOutline[i] + Vector3.up * 1.2f + currentFloorHeight, i.ToString());
			}
		}
		
		private void DisplayOutsideWallOutlines()
		{
	//		if(roofPointOverlay == null)
	//			return;
	//		
	//		for(int i = 0; i < roofPointOverlay.Count; i++)
	//		{
	//			
	//			Handles.color = Color.magenta;
	//			Handles.DrawAAPolyLine(10, roofPointOverlay[i]);
	//		}
		}

		private void DisplayWindows()
		{
			if(Script.CurrentFloorBlueprint == null)
				return;

			for(int i = 0; i < Script.WindowDisplays.Count; i++)
			{
				Handles.color = Color.black;
				Handles.DrawAAPolyLine(4, Script.WindowDisplays[i]);
			}
		}

		private void DisplayDoors()
		{
			if(Script.CurrentFloorBlueprint == null)
				return;

			if(Script.FloorEditType != EditFloorType.Floor)
				return;

			for (int i = 0; i < Script.CurrentFloorBlueprint.Doors.Count; i++) 
			{
				var door = Script.CurrentFloorBlueprint.Doors[i];
				
				// these type of doors do not have any door inside them
				if(door.DoorType == DoorTypeEnum.Open 
				   || door.DoorType == DoorTypeEnum.Closet 
				   || door.DoorType == DoorTypeEnum.SkinnyOpen 
				   || door.DoorType == DoorTypeEnum.TallOpen)
					continue;
				
				Vector3 length = (door.End - door.Start);
				if(length.magnitude > 2.5)
					continue;

				Vector3 direction = length.normalized;
				
				Handles.color = blueprintColor;
				Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, 0.4f);
				Handles.DrawSolidArc(door.Start + currentFloorHeight, Vector3.up, direction, door.Direction * (door.MaxOpenAngleOffset + 90), 1);
				if(door.IsDoubleDoor)
					Handles.DrawSolidArc(door.End + currentFloorHeight, Vector3.up, -direction, -door.Direction * (door.MaxOpenAngleOffset + 90), 1);
			}

			// Draws all the doors with the proper walls and inset correctly
			for(int i = 0; i < Script.DoorDisplays.Count; i++)
			{
				Handles.color = Color.black;
				Handles.DrawAAPolyLine(4, Script.DoorDisplays[i]);
			}
		}
		
		void DisplayStairs ()
		{
			for (int i = 0; i < Script.StairsDisplay.ToArray().Length; i++) 
			{
				var stairOutline = Script.StairsDisplay.ToArray() [i];
				Handles.color = Color.black;
				Handles.DrawAAPolyLine (4, stairOutline);
			}
		}

		void DisplayWallPointIndexes()
		{
			if(Script.ShowIndexPoints == false)
				return;

			if(Script.CurrentFloorBlueprint == null || Script.CurrentFloorBlueprint.RoomBlueprints == null)
				return;

			for(int roomIndex = 0; roomIndex < Script.CurrentFloorBlueprint.RoomBlueprints.Count; roomIndex++)
			{
				for(int i = 0; i < Script.CurrentFloorBlueprint.RoomBlueprints[roomIndex].PerimeterWalls.Count; i++)
				{
					Vector3 wallPoint = Script.CurrentFloorBlueprint.RoomBlueprints[roomIndex].PerimeterWalls[i];
					wallPoint += Script.BuildingBlueprint.BlueprintGroundHeight;
					wallPoint += Script.CurrentFloor * 3f * Vector3.up;

					Handles.color = Color.black;
					Handles.Label(wallPoint, i.ToString());
				}
			}
		}
	}
}
