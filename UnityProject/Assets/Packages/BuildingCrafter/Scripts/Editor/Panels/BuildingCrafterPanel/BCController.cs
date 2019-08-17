using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{
	public partial class BuildingCrafterPanel : Editor
	{
		private void EditPivot (Event currentEvent)
		{
			Vector3 gridPoint;
			bool point = GetGridPoint(out gridPoint, false, currentEvent);

			if(point == false)
			{
				Script.EditingState = EditingState.None;
				return;
			}
				
			gridCursorColor = greenGridCursor;
			gridCursor = gridPoint + Script.BuildingBlueprint.BlueprintGroundHeight;

			if(TestMouseClick(currentEvent))
			{
				Undo.RegisterFullObjectHierarchyUndo(Script.BuildingBlueprint, "Update Pivot Point");

				Script.BuildingBlueprint.transform.position = new Vector3(gridPoint.x, Script.BuildingBlueprint.BlueprintGroundHeight.y, gridPoint.z);
				Script.BuildingBlueprint.LastGeneratedPosition = gridPoint;
				BCGenerator.DestroyGeneratedBuilding(Script.BuildingBlueprint);
			}

			ResetClickUp(currentEvent);
		}
	}
}
