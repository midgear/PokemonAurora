using UnityEngine;
using System.Collections;
using UnityEditor;

namespace BuildingCrafter
{
	public static class BCEditUtils {

		public static BuildingCrafterGenerator GetBuildingCrafter()
		{
			BuildingCrafterGenerator buildingCrafter = GameObject.FindObjectOfType<BuildingCrafterGenerator>();
			if(buildingCrafter == null)
			{
				GameObject gameObj = BCMesh.GenerateEmptyGameObject("Create Building Crafter");
				gameObj.name = "Building Crafter";
				gameObj.transform.position = new Vector3();
				buildingCrafter = gameObj.AddComponent<BuildingCrafterGenerator>();
			}

			return buildingCrafter;
		}

		public static void DestroyAllProceduralMeshes(BuildingBlueprint buildingBp)
		{
			MeshFilter[] meshFilters = buildingBp.gameObject.GetComponentsInChildren<MeshFilter>();
			
			if(meshFilters.Length > 0)
			{
				// Destroys all this objects meshes to prevent huge memory leaks
				for(int i = 0; i < meshFilters.Length; i++)
				{
					// Check if in an asset that already exists in the assets folder. If not, destroy it
					if(meshFilters[i].sharedMesh != null && meshFilters[i].sharedMesh.name.Contains("Procedural"))
					{
						Undo.DestroyObjectImmediate(meshFilters[i].sharedMesh);
					}
				}
			}
		}
	}
}
