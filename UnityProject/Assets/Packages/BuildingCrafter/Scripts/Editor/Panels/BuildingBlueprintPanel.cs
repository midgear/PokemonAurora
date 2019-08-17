using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace BuildingCrafter
{
	[CustomEditor(typeof(BuildingBlueprint))]
	public class BuildingBlueprintPanel : Editor 
	{

		public override void OnInspectorGUI ()
		{
			if(GUILayout.Button("Edit this building"))
			{
				BuildingCrafterGenerator buildingCrafter = GameObject.FindObjectOfType<BuildingCrafterGenerator>();
				if(buildingCrafter == null)
				{
					GameObject gameObj = BCMesh.GenerateEmptyGameObject("Create Building Crafter Generator");
					gameObj.name = "Building Crafter";
					buildingCrafter = gameObj.AddComponent<BuildingCrafterGenerator>();
				}

				buildingCrafter.BuildingBlueprint = (BuildingBlueprint)target;

				// Offset the entire building according to the new position
				BCUtils.ShiftBlueprintCenter(buildingCrafter.BuildingBlueprint, 
				                             buildingCrafter.BuildingBlueprint.LastGeneratedPosition, 
				                             buildingCrafter.BuildingBlueprint.Transform.position);
				buildingCrafter.BuildingBlueprint.LastGeneratedPosition = buildingCrafter.BuildingBlueprint.Transform.position;
	//
				buildingCrafter.gameObject.transform.position = Vector3.zero;

				Selection.activeGameObject = buildingCrafter.gameObject;
			}

			if(GUILayout.Button("Generate This Building Anew"))
			{
				BCGenerator.GenerateFullBuilding((BuildingBlueprint)target);

			}
			if(GUILayout.Button("Generate Only Interiors"))
			{
				BCGenerator.GenerateOnlyInteriors((BuildingBlueprint)target);
			}

			base.OnInspectorGUI();
		}
	}
}