using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using BuildingCrafter;


[CustomEditor(typeof(CenterVaseExtension))]
public class CenterVasePanel : Editor
{
	CenterVaseExtension Script;

	/// <summary>
	/// Use to create a new Scriptable object
	/// </summary>
	/// <returns>The room filler asset.</returns>
	[MenuItem("Assets/BuildingCrafter/Room Extenders/Create Vase Generator")]
	public static CenterVaseExtension CreateRoomFillerAsset()
	{
		BCFiles.CreateBuildingCrafterAssetDirectories();

		string path = EditorUtility.SaveFilePanel("Create New Building Style", "Assets/BuildingCrafter", "new_center_vase_asset", "asset");
		if(path == "")
			return null;
		
		int index = path.IndexOf("Assets");
		path = path.Remove(0, index);
		
		CenterVaseExtension newAsset = CenterVaseExtension.CreateInstance<CenterVaseExtension>();
		
		AssetDatabase.CreateAsset(newAsset, path);
		AssetDatabase.SaveAssets();
		
		if(Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<BuildingCrafterGenerator>() != null)
		{
			// Just stay here;
		}
		else
		{
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = newAsset;
		}
		
		return newAsset;
	}

	/// <summary>
	/// When selecting this, what does it show
	/// </summary>
	public override void OnInspectorGUI ()
	{
		serializedObject.Update();
		if(Script == null)
			Script = (CenterVaseExtension)target;

		Script.ShowPanel(serializedObject);

		// Remove this if you don't want the base inspector
		base.OnInspectorGUI ();

		serializedObject.ApplyModifiedProperties();
	}
}
