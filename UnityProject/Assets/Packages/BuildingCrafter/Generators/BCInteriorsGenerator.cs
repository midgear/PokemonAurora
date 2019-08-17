using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibTessDotNet;
using UnityMesh = UnityEngine.Mesh;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BuildingCrafter
{
	public static partial class BCGenerator
	{
		/// <summary>
		/// Generates all interiors for a building
		/// </summary>
		/// <param name="buildingBp">Building bp.</param>
		public static bool GenerateInteriors (BuildingBlueprint buildingBp)
		{
			// Check to see if these interiors are valid
			if(buildingBp.BuildingStyle == null)
			{
				buildingBp.BuildingStyle = GetDefaultBuildingStyle();
				
				if(buildingBp.BuildingStyle == null)
				{
					Debug.LogError("No building styles exist, please create one from Building Crafter Panel");
					return false;
				}
			}

			if(buildingBp.Floors.Count < 1 || buildingBp.Floors[0].RoomBlueprints.Count < 1)
			{
				Debug.Log("Building Blueprint is empty, can't generate building.");
				return false;
			}


			if(buildingBp.Floors[0].RoomBlueprints[0].PerimeterWalls.Count < 4)
			{
				buildingBp.Floors[0].RoomBlueprints.RemoveAt(0);
				Debug.Log("Found a blank wall in first room and removed the wall");
				return false;
			}

			// Generate each floor on plane zero
			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				BCGenerator.GenerateFloor(buildingBp, floorIndex);
			}

			// Set the entire building to static
			List<GameObject> entireBuilding = BCUtils.GetChildren(buildingBp.gameObject);
			
			for(int i = 0; i < entireBuilding.Count; i++)
				entireBuilding[i].isStatic = true;
			
			System.GC.Collect();

			return true;
		}

		/// <summary>
		/// Removes LOD group from an object, but not children.
		/// </summary>
		/// <param name="gameObject">Game object.</param>
		public static void RemoveLODGroup (GameObject gameObject)
		{
			LODGroup lodGroup = gameObject.GetComponent<LODGroup>();
			if(lodGroup != null)
			{
#if UNITY_EDITOR
				Undo.DestroyObjectImmediate(lodGroup);
#else
				GameObject.Destroy(lodGroup);
#endif
			}
		}

		/// <summary>
		/// Destroys any short or mishandled rooms without walls in a building
		/// </summary>
		/// <param name="buildingBp">Building bp.</param>
		public static void CleanNullAndShortPerimeterWalls (BuildingBlueprint buildingBp)
		{
			int breaker = 0;
			
			while(breaker < 100)
			{
				breaker++;
				bool breakThisRound = false;
				
				for(int i = 0; i < buildingBp.Floors.Count; i++)
				{
					for(int j = 0; j < buildingBp.Floors[i].RoomBlueprints.Count; j++)
					{
						if(buildingBp.Floors[i].RoomBlueprints[j].PerimeterWalls == null
						   || buildingBp.Floors[i].RoomBlueprints[j].PerimeterWalls.Count < 3)
						{
							buildingBp.Floors[i].RoomBlueprints.RemoveAt(j);
							breakThisRound = true;
							break;
						}
					}
					if(breakThisRound) break;
				}
				
				if(breakThisRound == false)
					break;
			}
		}

		/// <summary>
		/// Returns the default type of Building Style found in the database. Does not work in runtime
		/// </summary>
		public static BuildingStyle GetDefaultBuildingStyle()
		{
			BuildingStyle buildingStyle = null;

	#if UNITY_EDITOR
			string[] paths = AssetDatabase.FindAssets("t:BuildingStyle");
			

			
			if(paths.Length == 0)
				return null;
			
			for (int i = 0; i < paths.Length; i++) 
			{
				string path = AssetDatabase.GUIDToAssetPath (paths[i]);
				
				if(path.Contains("Generic") || path.Contains("General"))
				{
					buildingStyle = AssetDatabase.LoadAssetAtPath<BuildingStyle>(path);
				}
			}
			
			if(buildingStyle == null)
			{
				buildingStyle = AssetDatabase.LoadAssetAtPath<BuildingStyle>(paths[0]);
			}
	#endif
			return buildingStyle;
		}

		/// <summary>
		/// Destroies all meshes with the string "Precdural" in them within the building bp. This is to prevent memory leaks.
		/// </summary>
		/// <param name="buildingBp">Building bp.</param>
		public static void DestroyAllBuildingMeshes(BuildingBlueprint buildingBp)
		{
			DestroyAllProceduralMeshes(buildingBp.gameObject, false);
		}

		public static void DestroyAllProceduralMeshes(GameObject gameObject, bool destroyParent = false)
		{
//			allowUndo = false;

#if UNITY_EDITOR
//			if(allowUndo)
//				DestroyWithUndo(gameObject, destroyParent);
//			else
//			{
				DestroyWithoutUndo(gameObject, destroyParent);
				Undo.ClearUndo(gameObject);
//			}
//			return;
#else
			DestroyWithoutUndo(gameObject, destroyParent);
#endif
		}

		private static void DestroyWithUndo(GameObject gameObject, bool destroyParent = false)
		{
#if UNITY_EDITOR
			ProceduralGameObject[] proceduralObjects = gameObject.GetComponentsInChildren<ProceduralGameObject>(true); 
			
			if(proceduralObjects.Length > 0)
			{
				for(int i = 0; i < proceduralObjects.Length; i++)
				{
					MeshFilter meshFilter = proceduralObjects[i].GetComponent<MeshFilter>();
					// Find all procedual objects in the mesh
					
					// Check if in an asset that already exists in the assets folder. If not, destroy it
					if(meshFilter != null && meshFilter.sharedMesh != null)
						Undo.DestroyObjectImmediate(meshFilter.sharedMesh);
				}
			}
			
			while(gameObject.transform.childCount != 0)
				Undo.DestroyObjectImmediate(gameObject.transform.GetChild(0).gameObject);

			if(destroyParent)
				Undo.DestroyObjectImmediate(gameObject);
#endif
		}

		private static void DestroyWithoutUndo(GameObject gameObject, bool destroyParent = false)
		{
			if(gameObject == null)
				return;

			ProceduralGameObject[] proceduralObjects = gameObject.GetComponentsInChildren<ProceduralGameObject>(true); 

			if(proceduralObjects.Length > 0)
			{
				for(int i = 0; i < proceduralObjects.Length; i++)
				{
					MeshFilter meshFilter = proceduralObjects[i].GetComponent<MeshFilter>();
					// Find all procedual objects in the mesh
					
					// Check if in an asset that already exists in the assets folder. If not, destroy it
					if(meshFilter != null && meshFilter.sharedMesh != null)
						GameObject.DestroyImmediate(meshFilter.sharedMesh, true);
				}
			}
			
			while(gameObject.transform.childCount != 0)
				GameObject.DestroyImmediate(gameObject.transform.GetChild(0).gameObject);

			if(destroyParent)
				GameObject.DestroyImmediate(gameObject);
		}
	}
}