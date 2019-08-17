using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{
	[CustomEditor(typeof(BuildingStyle))]
	public class BuildingStylePanel : Editor 
	{
		public BuildingStyle Script;

		public static float buttonWidthWide = 22;
		public static float buttonSuperWide = 50;

		private static string editingStyle = "";
		private static int editingIndex = -1;

		// For the Crown Editors
		static bool vector2ControlsEnabled = false;
		static Vector2ArrayDisplayStyle CrownDisplayStyle;
		static Vector2ArrayDisplayControls FancyCrownControls;
		static Vector2ArrayDisplayControls PlainCrownControls;

//		private static GUIContent baseWindowLabel =		 new GUIContent("Base Window", "Set the base window that will fill all window frames");

		void OnEnable()
		{
			Script = (BuildingStyle)target;
		}

		static void EnableCrownDisplays()
		{
			if(vector2ControlsEnabled == false)
			{
				CrownDisplayStyle = new Vector2ArrayDisplayStyle(-1, 1, -1, 1, 100f);
				CrownDisplayStyle.ExtraQuadLabel = "Building Profile";
				
				CrownDisplayStyle.ExtraQuadPoint = new Vector2[4] {
					new Vector2(0, 0), 
					new Vector2(CrownDisplayStyle.XMin, 0), 
					new Vector2(CrownDisplayStyle.XMin, CrownDisplayStyle.YMin), 
					new Vector2(0, CrownDisplayStyle.YMin)};
				
				FancyCrownControls = new Vector2ArrayDisplayControls();
				PlainCrownControls = new Vector2ArrayDisplayControls();

				vector2ControlsEnabled = true;
			}
		}


		public override void OnInspectorGUI ()
		{
			serializedObject.Update();

			if(GUILayout.Button("Duplicate Style"))
			{
				DuplicateStyle(Script);
			}

			DisplayBuildingStyle(Script);

			if(Event.current.type == EventType.Repaint)
				EditorUtility.SetDirty( Script );

			serializedObject.ApplyModifiedProperties();
		}

		public static void DisplayBuildingStyle(BuildingStyle style)
		{
//			bool createAtlas = GUILayout.Button("Create Building Atlases");

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Materials", EditorStyles.boldLabel);
			if(GUILayout.Button("Copy Material Styles", EditorStyles.miniButton, GUILayout.Width(110)))
			{
				string copyPath = EditorUtility.OpenFilePanel("Copy Material Styles from Building Style" , BCFiles.BuildingStyles, "asset");

				if(copyPath != null && copyPath.Length > 0)
				{
					CopyMaterialsFromStyle(copyPath, style);
				}
			}
			EditorGUILayout.EndHorizontal();

			DisplayMaterialArray(ref style.FancySidings, style, "Sidings");
			
			GUILayout.Label("Single Materials", EditorStyles.helpBox);
			DisplaySingleMaterials(ref style.PlainSiding, style, "Plain Siding");
			DisplaySingleMaterials(ref style.Window, style, "Window Material");
			DisplaySingleMaterials(ref style.DoorWindowFrames, style, "Door & Window Frame Materials");
			DisplaySingleMaterials(ref style.Rooftop, style, "Rooftop");
			DisplaySingleMaterials(ref style.Grass, style, "Plain Grass Yard");
			DisplaySingleMaterials(ref style.Concrete, style, "Concrete Yard");
			DisplaySingleMaterials(ref style.DirtPath, style, "Dirt Path Yard");
			EditorGUILayout.Separator();

			EnableCrownDisplays();

			GUILayout.Label("Crowns", EditorStyles.helpBox);
			style.FancyCrown = Vector2ArrayEditor.DisplayVectors2Array(style.FancyCrown, FancyCrownControls, style, "Fancy Crown", CrownDisplayStyle);
			style.PlainCrown = Vector2ArrayEditor.DisplayVectors2Array(style.PlainCrown, PlainCrownControls, style, "Plain Crown", CrownDisplayStyle);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("GameObject Templates", EditorStyles.boldLabel);
			if(GUILayout.Button("Copy Prefab Styles", EditorStyles.miniButton, GUILayout.Width(110)))
			{
				string copyPath = EditorUtility.OpenFilePanel("Copy GameObject Prefabs from Building Style" , BCFiles.BuildingStyles, "asset");
				
				if(copyPath != null && copyPath.Length > 0)
				{
					CopyGeneralPrefabsFromStyle(copyPath, style);
				}
			}
			EditorGUILayout.EndHorizontal();

			DisplayGameObject(ref style.OutsideFancyDoor, style, "Outside Fancy Door");
			DisplayGameObject(ref style.OutsidePlainDoor, style, "Outside Plain Door");
			DisplayGameObject(ref style.StandardDoor, style, "Standard Door");
			DisplayGameObject(ref style.HeavyDoor, style, "Heavy Door");
			DisplayGameObject(ref style.TwoByFourStairs, style, "2x4 Stairs");
			DisplayGameObject(ref style.StairsToRoof, style, "Stairs-To-Roof");

			EditorGUILayout.Separator();

			// Checks if the base window is empty If it is empty, then fill it with the base window
			DisplayWindows(ref style.BaseWindow, ref style.FancyWindowTypes, ref style.FancyWindows, style);
//			DisplayWindow(baseWindowLabel, ref style.BaseWindow, style);
//			DisplayWindowList(ref style.FancyWindows, ref style.FancyWindowTypes, style);

			EditorGUILayout.Separator();
			
			GUILayout.Label("Room Styles", EditorStyles.boldLabel);
			DisplayRoomStyle(ref style.GeneralRoomStyle, style, "General Room");
			DisplayRoomStyle(ref style.LivingRoomStyle, style, "Living Room");
			DisplayRoomStyle(ref style.BedroomStyle, style, "Bedroom");
			DisplayRoomStyle(ref style.ClosetStyle, style, "Closet");
			DisplayRoomStyle(ref style.HallwaysStyle, style, "Hallway");
			DisplayRoomStyle(ref style.KitchenStyle, style, "Kitchen");
			DisplayRoomStyle(ref style.DiningStyle, style, "Dining Room");
			DisplayRoomStyle(ref style.BathroomStyle, style, "Bathroom Room");
			DisplayRoomStyle(ref style.KidsStyle, style, "Kids Room");
			DisplayRoomStyle(ref style.UtilityStyle, style, "Utility Room");
			DisplayRoomStyle(ref style.PatioStyle, style, "Patio Room");
			DisplayRoomStyle(ref style.GarageStyle, style, "Garage Room");
			DisplayRoomStyle(ref style.OfficeStyle, style, "Office Room");
			DisplayRoomStyle(ref style.StoreStyle, style, "Store Room");
			DisplayRoomStyle(ref style.StoreBackroomStyle, style, "StoreBackroom Room");
			EditorGUILayout.Separator();

			GUILayout.Label("Atlases", EditorStyles.boldLabel);
			BCAtlas.Size newSize = (BCAtlas.Size)EditorGUILayout.EnumPopup("Atlas Texture Size", style.AtlasSize);
			if(newSize != style.AtlasSize)
			{
				Undo.RecordObject(style, "Atlas Size Update");
				style.AtlasSize = newSize;
			}

			bool createAtlas = false;

			if(style.AtlasMaterials != null && style.AtlasMaterials.Length > 0)
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("Generated Atlases", EditorStyles.helpBox);
				if(GUILayout.Button("x", GUILayout.Width(buttonSuperWide)))
				{
					Undo.RecordObject(style, "Remove Atlases");
					style.AtlasMaterials = new BCAtlas[0];
					EditorUtility.SetDirty(style);
					return;
				}
				EditorGUILayout.EndHorizontal();
				for(int i = 0; i < style.AtlasMaterials.Length; i++)
				{
					GUILayout.Label(style.AtlasMaterials[i].AtlasName);
				}

				createAtlas = GUILayout.Button("Update Building Atlases");
			}
			else
			{
				GUILayout.Label("Atlas Group has not been generated");
				createAtlas = GUILayout.Button("Create Building Atlases");
			}

			if(createAtlas)
			{
				Undo.RecordObject(style, "Update Atlases");
				BCAtlas[] bcAtlas = BCAtlasGenerator.AtlasBuildingStyle(style);
				style.AtlasMaterials = bcAtlas;
			}

			if(GUI.changed)
			{
				style.ValidateThisBuildingStyle();
				EditorUtility.SetDirty(style);
			}	
		}

		static void CopyMaterialsFromStyle (string copyPath, BuildingStyle buildingStyleToReplace)
		{
			int indexOfAssets = copyPath.IndexOf("Asset");
			copyPath = copyPath.Remove(0, indexOfAssets);
			
			BuildingStyle buildingStyle = AssetDatabase.LoadAssetAtPath(copyPath, typeof(BuildingStyle)) as BuildingStyle;

			if(buildingStyle != null)
			{
				Undo.RecordObject(buildingStyleToReplace, "Copy Over Materials");
				
				buildingStyleToReplace.FancySidings = new Material[buildingStyle.FancySidings.Length];
				
				for(int i = 0; i < buildingStyle.FancySidings.Length; i++)
				{
					buildingStyleToReplace.FancySidings[i] = buildingStyle.FancySidings[i];
				}
				
				buildingStyleToReplace.PlainSiding = buildingStyle.PlainSiding;
				buildingStyleToReplace.Window = buildingStyle.Window;
				buildingStyleToReplace.DoorWindowFrames = buildingStyle.DoorWindowFrames;
				buildingStyleToReplace.Rooftop = buildingStyle.Rooftop;
				buildingStyleToReplace.Grass = buildingStyle.Grass;
				buildingStyleToReplace.Concrete = buildingStyle.Concrete;
				buildingStyleToReplace.DirtPath = buildingStyle.DirtPath;
			}

		}

		static void CopyGeneralPrefabsFromStyle (string copyPath, BuildingStyle buildingStyleToReplace)
		{
			int indexOfAssets = copyPath.IndexOf("Asset");
			copyPath = copyPath.Remove(0, indexOfAssets);
			
			BuildingStyle buildingStyle = AssetDatabase.LoadAssetAtPath(copyPath, typeof(BuildingStyle)) as BuildingStyle;

			if(buildingStyle != null)
			{
				Undo.RecordObject(buildingStyleToReplace, "Copy Over Prefabs");

				buildingStyleToReplace.OutsideFancyDoor = buildingStyle.OutsideFancyDoor;
				buildingStyleToReplace.OutsidePlainDoor = buildingStyle.OutsidePlainDoor;
				buildingStyleToReplace.StandardDoor = buildingStyle.StandardDoor;
				buildingStyleToReplace.HeavyDoor = buildingStyle.HeavyDoor;
				buildingStyleToReplace.TwoByFourStairs = buildingStyle.TwoByFourStairs;
				buildingStyleToReplace.StairsToRoof = buildingStyle.StairsToRoof;
				buildingStyleToReplace.BaseWindow = buildingStyle.BaseWindow;
				buildingStyleToReplace.FancyWindows = buildingStyle.FancyWindows;
				buildingStyleToReplace.FancyWindowTypes = buildingStyle.FancyWindowTypes;
			}
		}

		static void DisplaySingleMaterials (ref Material material, BuildingStyle buildingStyle, string name)
		{
			Material newMat = (Material)EditorGUILayout.ObjectField(name, material, typeof(Material), false);

			if(material == null && newMat != null
			   || newMat == null && material == null
			   || material != newMat)
			{
				Undo.RecordObject(buildingStyle, "Change Material");
				material = newMat;
				EditorUtility.SetDirty(buildingStyle);
			}
		}

		static void DisplayGameObject (ref GameObject obj, BuildingStyle buildingStyle, string name)
		{
			GameObject newObj = (GameObject)EditorGUILayout.ObjectField(name, obj, typeof(GameObject), false);

			if(obj != null && newObj == null 
			   || obj == null && newObj != null
			   || obj != newObj)
			{
				Undo.RecordObject(buildingStyle, "Change Prefab Type");
				obj = newObj;
				EditorUtility.SetDirty(buildingStyle);
			}
		}

		static void DisplayWindows (ref GameObject baseWindow, ref List<WindowTypeEnum> fancyWindowTypes, ref List<GameObject> fancyWindows, BuildingStyle style)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(new GUIContent("Window Types", "Add what type of windows will appear in your building."), EditorStyles.boldLabel);
//			bool createWindowPrefab = GUILayout.Button("Create Window Prefab", EditorStyles.miniButton, GUILayout.Width(130), GUILayout.Height(14f));
			GUILayout.EndHorizontal();

//			if(createWindowPrefab)
			{
				// Click on the create window prefab
				// It opens up a view for you to select the fbx file or the prefab file
				// Check for it to be a fbx or a prefab
				// Create in the scene and add the BCWindow component
				// Turn on live view for the window
				// Create a new prefab, adding it to the BuildingCrafterAssets folder in "Windows" subfolder
			}

			EditorGUILayout.Separator();

			// 2. Display the windows in order with a little plus option
			DisplayWindowList(ref fancyWindows, ref fancyWindowTypes, style);

			DisplayWindow(new GUIContent("Base Window", "The window that can fit into any space in the building."), ref baseWindow, style);
		}

		static void DisplayWindow (GUIContent name, ref GameObject obj, BuildingStyle buildingStyle)
		{
			GameObject newObj = (GameObject)EditorGUILayout.ObjectField(name, obj, typeof(GameObject), false);

			if(newObj != null && newObj.GetComponent<BCWindow>() == null)
			{
				Debug.LogError("Window does not have BC Window Component attached, will not work. Please add this first");
				obj = null;
				return;
			}
				

			if(obj != null && newObj == null 
			   || obj == null && newObj != null
			   || obj != newObj)
			{
				Undo.RecordObject(buildingStyle, "Change Prefab Type");
				obj = newObj;
				EditorUtility.SetDirty(buildingStyle);
			}
		}

		static void DisplayWindow (GUIContent name, ref GameObject obj, ref WindowTypeEnum windowType, BuildingStyle buildingStyle)
		{
			EditorGUILayout.BeginHorizontal();
			GameObject newObj = (GameObject)EditorGUILayout.ObjectField(name, obj, typeof(GameObject), false);

			WindowTypeEnum newType = (WindowTypeEnum)EditorGUILayout.EnumMaskField(windowType, GUILayout.MaxWidth(70));

			if(newType != windowType)
			{
				Undo.RecordObject(buildingStyle, "Change Window Flag");
				windowType = newType;
				EditorUtility.SetDirty(buildingStyle);
			}

			EditorGUILayout.EndHorizontal();

			if(newObj != null && newObj.GetComponent<BCWindow>() == null)
			{
				Debug.LogError("Window does not have BC Window Component attached, will not work. Please add this first");
				obj = null;
				return;
			}
			
			if(obj != null && newObj == null 
			   || obj == null && newObj != null
			   || obj != newObj)
			{
				Undo.RecordObject(buildingStyle, "Change Prefab Type");
				obj = newObj;
				EditorUtility.SetDirty(buildingStyle);
			}

		}

		static void DisplayWindowList (ref List<GameObject> objs, ref List<WindowTypeEnum> windowTypes, BuildingStyle buildingStyle)
		{

			// Ensure window types and objs are always aligned
			if(windowTypes.Count > objs.Count)
			{
				while(windowTypes.Count > objs.Count)
					windowTypes.RemoveAt(windowTypes.Count - 1);
			}
			else if(windowTypes.Count < objs.Count)
			{
				while(windowTypes.Count < objs.Count)
					windowTypes.Add(WindowTypeEnum.Standard);
			}

			for(int i = 0; i < objs.Count; i++)
			{
				GUIContent label = new GUIContent("Fancy Window " + i, "Ordered windows will fall through the array to find a window that matches and will fit.");

				EditorGUILayout.BeginHorizontal();
				GameObject newObj = (GameObject)EditorGUILayout.ObjectField(label, objs[i], typeof(GameObject), false);
				
				WindowTypeEnum newType = (WindowTypeEnum)EditorGUILayout.EnumMaskField(windowTypes[i], GUILayout.MaxWidth(70));
				
				if(newType != windowTypes[i])
				{
					Undo.RecordObject(buildingStyle, "Change Window Flag");
					windowTypes[i] = newType;
					EditorUtility.SetDirty(buildingStyle);
				}

				if(GUILayout.Button("x", GUILayout.Width(buttonWidthWide), GUILayout.Height(14f)))
				{
					Undo.RecordObject(buildingStyle, "Remove Style Material");
					objs.RemoveAt(i);
					windowTypes.RemoveAt(i);
					EditorUtility.SetDirty(buildingStyle);
					return;
				}

				EditorGUILayout.EndHorizontal();
				
				if(newObj != null && newObj.GetComponent<BCWindow>() == null)
				{
					Debug.LogError("Window does not have BC Window Component attached, will not work. Please add this first");
					objs[i] = null;
					return;
				}
				
				if(objs[i] != null && newObj == null 
				   || objs[i] == null && newObj != null
				   || objs[i] != newObj)
				{
					Undo.RecordObject(buildingStyle, "Change Prefab Type");
					objs[i] = newObj;
					EditorUtility.SetDirty(buildingStyle);
				}
			}
			
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if(GUILayout.Button("+", GUILayout.Width(30)))
			{
				Undo.RecordObject(buildingStyle, "Add New Fancy Window");

				if(objs.Count > 0)
				{
					objs.Add(objs.Last());
					windowTypes.Add(windowTypes.Last());
				}
				else
				{
					objs.Add(null);

					WindowTypeEnum windowType = WindowTypeEnum.Standard;
					windowType |= ~WindowTypeEnum.Standard;

					windowTypes.Add(windowType);
				}

				EditorUtility.SetDirty(buildingStyle);
			}
			GUILayout.FlexibleSpace();

			GUILayout.EndHorizontal();
		}

		private static void DisplayRoomStyle(ref List<RoomStyle> roomStyles, BuildingStyle buildingStyle, string name)
		{
			GUILayout.Label(name + " (Room Style)", EditorStyles.helpBox);

			DisplayRoomStyle(ref roomStyles, buildingStyle);

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if(GUILayout.Button("Create", GUILayout.Width(buttonSuperWide)))
			{
				RoomStyle roomStyle = CreateBuildStyleAsset.CreateRoomStyle();
				if(roomStyle != null)
					roomStyles.Add(roomStyle);
			}
			if(GUILayout.Button("+", GUILayout.Width(buttonSuperWide)))
			{
				Undo.RecordObject(buildingStyle, "Add Style");
				if(roomStyles.Count > 1)
					roomStyles.Add(roomStyles[roomStyles.Count - 1]);
				else
					roomStyles.Add(null);

				EditorUtility.SetDirty(buildingStyle);
			}

			GUILayout.EndHorizontal();
		}

		
		public static void DisplayRoomStyle(ref List<RoomStyle> roomStyles, BuildingStyle buildingStyle, bool canRemoveStyles = true)
		{
			if(roomStyles != null)
			{
				for(int i = 0; i < roomStyles.Count; i++)
				{
					RoomStyle roomStyle = roomStyles[i];
					
					GUILayout.BeginHorizontal();
					if(roomStyle != null)
					{
						if(editingStyle == roomStyles[i].name && editingIndex == i && GUILayout.Button("v", GUILayout.Width(buttonWidthWide)))
						{
							Undo.RecordObject(buildingStyle, "Collapse Style");
							editingStyle = "";
							editingIndex = -1;
						}
						else if((editingStyle != roomStyles[i].name || editingIndex != i) && GUILayout.Button(">", GUILayout.Width(buttonWidthWide)))
						{
							Undo.RecordObject(buildingStyle, "Expand Style");
							editingStyle = roomStyles[i].name;
							editingIndex = i;
						}
					}
					else
						GUILayout.Label("", GUILayout.Width(buttonWidthWide));
					
					RoomStyle newRoomStyle = (RoomStyle)EditorGUILayout.ObjectField("Room Style", roomStyle, typeof(RoomStyle), false);
					
					if(newRoomStyle != null && roomStyle == null
					   || newRoomStyle == null && roomStyle != null 
					   || roomStyle != newRoomStyle)
					{
						Undo.RecordObject(buildingStyle, "Change Room Style");
						roomStyles[i] = newRoomStyle;
						EditorUtility.SetDirty(buildingStyle);
					}
					
					if(canRemoveStyles == true && GUILayout.Button("x", GUILayout.Width(buttonWidthWide)))
					{
						Undo.RecordObject(buildingStyle, "Remove Room Style");
						roomStyles.RemoveAt(i);
						EditorUtility.SetDirty(buildingStyle);
						return;
					}
					GUILayout.EndHorizontal();
					
					if(roomStyles[i] != null && editingStyle == roomStyles[i].name && editingIndex == i)
					{
						GUILayout.BeginHorizontal();
						
						GUILayout.BeginVertical(GUILayout.Width(50f));
						GUILayout.FlexibleSpace();
						GUILayout.EndVertical();
						
						GUILayout.BeginVertical();
						
						RoomStylePanel.DisplayRoomStyleAndFiller(roomStyle);
						
						//					RoomStylePanel.DisplayRoomStyle(roomStyle.RoomMaterials, "Edit Wall Coverings");
						//					RoomStylePanel.DisplayRoomFillers(ref roomStyle.RoomFiller, "Edit Room Filler");
						
						GUILayout.EndVertical();
						
						GUILayout.EndVertical();
						
						EditorGUILayout.Separator();
						EditorGUILayout.Separator();
						EditorGUILayout.Separator();
					}
				}
			}
		}

		private static void DisplayMaterialArray(ref Material[] materials, BuildingStyle style, string name, bool allowAddingMaterials = true, bool allowDestroyMaterials = true)
		{
			GUILayout.Label(name, EditorStyles.helpBox);

			if(materials == null)
				materials = new Material[0];

			for(int i = 0; i < materials.Length; i++)
			{
				Material material = materials[i];
				
				GUILayout.BeginHorizontal();
				Material newMat = (Material)EditorGUILayout.ObjectField("Material", material, typeof(Material), false);

				if(newMat != null)
				{
					if(material == null || newMat != material)
					{
						Undo.RecordObject(style, "Change Room Style");
						materials[i] = newMat;
						EditorUtility.SetDirty(style);
					}
				}

				if(allowDestroyMaterials)
				{
					if(GUILayout.Button("x", GUILayout.Width(buttonWidthWide)))
					{
						Undo.RecordObject(style, "Remove Style Material");
						List<Material> newMaterials = materials.ToList<Material>();
						newMaterials.RemoveAt(i);
						materials = newMaterials.ToArray<Material>();
						EditorUtility.SetDirty(style);
						return;
					}
				}
				GUILayout.EndHorizontal();
			}
			
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if(allowAddingMaterials == true)
			{
				if(GUILayout.Button("+", GUILayout.Width(buttonSuperWide)))
				{
					List<Material> newMaterials = materials.ToList<Material>();

					Undo.RecordObject(style, "Add Style Material");

					if(materials.Length > 1)
						newMaterials.Add(materials[materials.Length - 1]);
					else
						newMaterials.Add(null);

					materials = newMaterials.ToArray<Material>();
					EditorUtility.SetDirty(style);
				}
			}
			
			GUILayout.EndHorizontal();
		}

//		private static void DisplayMaterialArray(ref BCAtlas[] atlases, BuildingStyle style, string name, bool allowAddingAtlases = true, bool allowDestroyAtlases = true)
//		{
//			GUILayout.Label(name, EditorStyles.helpBox);
//
//			if(atlases == null)
//				atlases = new BCAtlas[0];
//
//			for(int i = 0; i < atlases.Length; i++)
//			{
//				Material atlas = atlases[i];
//
//				GUILayout.BeginHorizontal();
//				Material newMat = (Material)EditorGUILayout.ObjectField("Material", atlas, typeof(BCAtlas), true);
//
//				if(newMat != null)
//				{
//					if(atlas == null || newMat != atlas)
//					{
//						Undo.RecordObject(style, "Change Room Style");
//						atlas[i] = newMat;
//						EditorUtility.SetDirty(style);
//					}
//				}
//
//				if(allowDestroyMaterials)
//				{
//					if(GUILayout.Button("x", GUILayout.Width(buttonWidthWide)))
//					{
//						Undo.RecordObject(style, "Remove Style Material");
//						List<Material> newMaterials = materials.ToList<Material>();
//						newMaterials.RemoveAt(i);
//						materials = newMaterials.ToArray<Material>();
//						EditorUtility.SetDirty(style);
//						return;
//					}
//				}
//				GUILayout.EndHorizontal();
//			}
//
//			GUILayout.BeginHorizontal();
//			GUILayout.FlexibleSpace();
//
//			if(allowAddingMaterials == true)
//			{
//				if(GUILayout.Button("+", GUILayout.Width(buttonSuperWide)))
//				{
//					List<Material> newMaterials = materials.ToList<Material>();
//
//					Undo.RecordObject(style, "Add Style Material");
//
//					if(materials.Length > 1)
//						newMaterials.Add(materials[materials.Length - 1]);
//					else
//						newMaterials.Add(null);
//
//					materials = newMaterials.ToArray<Material>();
//					EditorUtility.SetDirty(style);
//				}
//			}
//
//			GUILayout.EndHorizontal();
//		}

		private static void DuplicateStyle (BuildingStyle script)
		{
			string path = AssetDatabase.GetAssetPath(script);

			BuildingStyle newAsset = BuildingStyle.DuplicateThisStyle(script);

			int index = path.LastIndexOf('/');
			path = path.Remove(index, path.Length - index);

			AssetDatabase.CreateAsset(newAsset, path + "/" + script.name + " copy.asset");
			AssetDatabase.SaveAssets();
			Selection.activeObject = newAsset;

			EditorUtility.SetDirty(newAsset);
		}
	}
}