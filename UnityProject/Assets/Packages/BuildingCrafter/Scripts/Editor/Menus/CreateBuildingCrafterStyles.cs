//CreateCameraLocationAsset.cs
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace BuildingCrafter
{
	public class CreateBuildStyleAsset
	{
		[MenuItem("Assets/BuildingCrafter/Styles/Create Building Style")]
		public static BuildingStyle CreateBuildingStyle()
		{
			BCFiles.CreateBuildingCrafterAssetDirectories();

			string path = EditorUtility.SaveFilePanel("Create New Building Style", BCFiles.BuildingStyles, "new_building_style", "asset");
			if(path == "")
				return null;

			int index = path.IndexOf("Assets");
			path = path.Remove(0, index);

			BuildingStyle newAsset = BuildingStyle.CreateInstance<BuildingStyle>();

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

		[MenuItem("Assets/Create/Room Style (BuildingCrafter)")]
		public static RoomStyle CreateRoomStyleInAsset()
		{
			return CreateRoomStyle();
		}

		
		[MenuItem("Assets/BuildingCrafter/Styles/Create Room Style")]
		public static RoomStyle CreateRoomStyle()
		{
			BCFiles.CreateBuildingCrafterAssetDirectories();

			string path = EditorUtility.SaveFilePanel("Create New Building Style", BCFiles.RoomStyles, "new_room_style", "asset");
			if(path == "")
				return null;
			
			int index = path.IndexOf("Assets");
			path = path.Remove(0, index);
			
			List<RoomMaterials> roomMats = new List<RoomMaterials>();
			
			RoomStyle newAsset = RoomStyle.CreateInstance<RoomStyle>();
			newAsset.RoomMaterials = roomMats;
			
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
	}
}