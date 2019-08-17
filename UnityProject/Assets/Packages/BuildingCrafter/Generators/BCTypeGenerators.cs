using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

namespace BuildingCrafter
{
	public static partial class BCGenerator
	{
		public static void GenerateBuilding(BuildingBlueprint buildingBp, 
		                                        bool destroy = true, 
		                                        bool interiors = true, 
		                                    	bool outside = true,
		                                    	bool roofs = true,
		                                        bool yards = true,
												bool randomizeRoomsAndWalls = false)
		{
			// To figure out how long a generation took
			int tickCount = System.Environment.TickCount;

			if(buildingBp == null)
				return;

			if(buildingBp.BuildingStyle == null)
			{
				buildingBp.BuildingStyle = BCGenerator.GetDefaultBuildingStyle();
				
				if(buildingBp.BuildingStyle == null)
				{
					Debug.LogError("No building styles exist, please create one from Building Crafter Panel");
					return;
				}
			}

			if(CheckBuildingStyleForProperInfo(buildingBp.BuildingStyle) == false)
			{
				return;
			}

			// Temporarily set the building position base height to 0;
			float buildingGroundHeight = buildingBp.Transform.position.y;

			// Set the building blueprint position and round it
			buildingBp.transform.position = new Vector3(Mathf.Round(buildingBp.Transform.position.x), 0, Mathf.Round(buildingBp.Transform.position.z));
			// Round the building position to make sure it is not offset

			// Check to see if the LastGeneatedPosition is blank. If it is, then shift the last generated point to the Blueprint Center
			// This makes sure buildings that have been generated weird won't go all over the place
			if(buildingBp.LastGeneratedPosition == Vector3.zero)
				buildingBp.LastGeneratedPosition = buildingBp.BlueprintXZCenter;

			// Offset the entire building compared to the last position
			BCUtils.ShiftBlueprintCenter(buildingBp, buildingBp.LastGeneratedPosition, buildingBp.transform.position);
			buildingBp.LastGeneratedPosition = buildingBp.transform.position;

			// Update the blueprint to ensure it is valid before generating it
			BCUtils.UpdateBlueprintCentre(buildingBp, buildingBp.transform.position);
			BCGenerator.CleanNullAndShortPerimeterWalls(buildingBp);
			BCGenerator.CalculatePartyWalls(buildingBp);

			// Destroy the old building
			if(destroy)
				BCGenerator.DestroyGeneratedBuilding(buildingBp);

			if(randomizeRoomsAndWalls)
			{
				buildingBp.LastFancySideIndex = Random.Range(0, buildingBp.BuildingStyle.FancySidings.Length);
				// TODO - set up room randomization
			}

			// Generate Each part of the building
			if(interiors)
				BCGenerator.GenerateInteriors(buildingBp);
			if(outside || roofs)
				BCWallRoofGenerator.GenerateWallsAndRoofs(buildingBp, outside, roofs, buildingBp.GenerateCappers);
			if(yards)
				BCGenerator.GenerateYardLayouts(buildingBp);

			UpdateBuildingGroundFloorHeight(buildingBp, buildingGroundHeight);

			// Static entire building for speed purposes
			List<GameObject> entireBuilding = BCUtils.GetChildren(buildingBp.gameObject);
			
			for(int i = 0; i < entireBuilding.Count; i++)
				entireBuilding[i].isStatic = true;

			// Set the doors in the building to non-static 
			DoorOpener[] doorOpeners = buildingBp.gameObject.GetComponentsInChildren<DoorOpener>();
			for(int index = 0; index < doorOpeners.Length; index++)
			{
				List<GameObject> childrenAndParent = BuildingCrafter.BCUtils.GetChildren(doorOpeners[index].gameObject);
				for(int i = 0; i < childrenAndParent.Count; i++)
					childrenAndParent[i].isStatic = false;
			}

			WindowHolder[] windows = buildingBp.gameObject.GetComponentsInChildren<WindowHolder>();
			
			for(int index = 0; index < windows.Length; index++)
			{
				List<GameObject> childrenAndParent = BuildingCrafter.BCUtils.GetChildren(windows[index].gameObject);
				
				for(int i = 0; i < childrenAndParent.Count; i++)
				{
					if(buildingBp.WindowsGenerateAsStatic != true)
						childrenAndParent[i].isStatic = false;

					childrenAndParent[i].layer = 1; // Sets windows to the transparent layer
				}
			}

			if(buildingBp.UseAtlases)
			{
				#if UNITY_EDITOR
				// TODO - Allow the user to generate the atlases right here
				if(buildingBp.BuildingStyle.IsAtlased == false)
				{	
					EditorUtility.DisplayDialog("Warning: Building Style has no atlas generated", 
						"The Building Style (" + buildingBp.BuildingStyle.name + 
						") has no atlas generated or a broken atlas. Please generate it to allow atlasing on this building. " +
						"Go to the Building Style, scroll to the bottom and click 'Generate Atlases' or 'Update Atlases'. " +
						"This building will generate without an atlas."
						, "Okay");
				}
				#endif

				if(buildingBp.BuildingStyle.IsAtlased)
					BCAtlas.AtlasGameObject(buildingBp.gameObject, buildingBp.BuildingStyle.AtlasMaterials);
			}

			// Display the generation time taken
//			Debug.Log("Building generated in " + (System.Environment.TickCount - tickCount));
		}

		public static void GenerateFullBuilding(BuildingBlueprint buildingBp)
		{
			GenerateBuilding(buildingBp, true, true, true, true, true, true);
		}

		public static void GenerateOnlyInteriors(BuildingBlueprint buildingBp)
		{
			GenerateBuilding(buildingBp, true, true, false, false, false);
		}

		public static void GenerateRoofs(BuildingBlueprint buildingBp, bool destroy)
		{
#if UNITY_EDITOR
			Undo.RegisterFullObjectHierarchyUndo(buildingBp, "Generate Roofs");
#endif
			GenerateBuilding(buildingBp, destroy, false, false, true, false);
		}

		public static void GenerateOutsideWalls(BuildingBlueprint buildingBp, bool destroy)
		{
#if UNITY_EDITOR
			Undo.RegisterFullObjectHierarchyUndo(buildingBp, "Generate Walls");
#endif
			GenerateBuilding(buildingBp, destroy, false, true, false, false);
		}

		// Update the building floor level
		private static void UpdateBuildingGroundFloorHeight(BuildingBlueprint buildingBp, float buildingFloorLevel)
		{
			buildingBp.Transform.position += Vector3.up * buildingFloorLevel;
		}

		/// <summary>
		/// Checks to make sure the Building Style provided is proper
		/// </summary>
		public static bool CheckBuildingStyleForProperInfo(BuildingStyle buildingStyle)
		{
			if(buildingStyle == null)
			{
				Debug.LogError("There is no building style attached to this building");
				return false;
			}

			if(buildingStyle.BaseWindow == null)
			{
				GameObject baseWindow = Resources.Load<GameObject>("StandardBaseWindow") as GameObject;

				if(baseWindow != null)
				{
					Debug.Log("This building style (" +  buildingStyle.name + ") has no base window. Added the default window type.");
					buildingStyle.BaseWindow = baseWindow;
				}
				else
				{
					Debug.LogError("This building style (" +  buildingStyle.name + ") has no base window and there is no default base window type in a resources folder.");
					return false;
				}
			}
				
			return true;
		}
	}
}