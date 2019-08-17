using UnityEngine;
using System.Collections;
using UnityEditor;

namespace BuildingCrafter
{

	public partial class BuildingCrafterPanel : Editor 
	{

		SerializedProperty fancyFront, fancyBack, fancyLeft, fancyRight;

		private void DisplayExteriorEditor()
		{
			// If the system has changed whatever editing state is going on, then update
			if(Script.FloorEditType != Script.LastFloorEditType
				|| fancyFront == null || fancyBack == null || fancyLeft == null || fancyRight == null)
			{
				DrawAllGreyFloorOutlines();
				buildingBlueprintGameObject = serializedObject.FindProperty("BuildingBlueprint");
				buildingBlueprintObject = new SerializedObject(buildingBlueprintGameObject.objectReferenceValue);
				fancyFront = buildingBlueprintObject.FindProperty("FancyFront");
				fancyBack = buildingBlueprintObject.FindProperty("FancyBack");
				fancyLeft = buildingBlueprintObject.FindProperty("FancyLeftSide");
				fancyRight = buildingBlueprintObject.FindProperty("FancyRightSide");

				Script.LastFloorEditType = Script.FloorEditType;
			}

			EditorGUILayout.PropertyField(fancyFront);
			EditorGUILayout.PropertyField(fancyBack);
			EditorGUILayout.PropertyField(fancyLeft);
			EditorGUILayout.PropertyField(fancyRight);
		}
	}
}