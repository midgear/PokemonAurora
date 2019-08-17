using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace BuildingCrafter
{
	[System.Serializable]
	public class BuildingBpSerialized
	{
		public BuildingBpSerialized(BuildingBlueprint buildingBp)
		{
			this.CopyBp(buildingBp);
		}

		public string BuildingStyle;

		public bool FancyFront;
		public bool FancyBack;
		public bool FancyRightSide;
		public bool FancyLeftSide;

		public Quaternion BuildingRotation;
		public Vector3 LastGeneratedPosition;

		public List<FloorBlueprint> Floors = new List<FloorBlueprint>();
		public List<RoofInfo> RoofInfos = new List<RoofInfo>();

		public bool GenerateCappers = false;

		public bool WindowsGenerateAsStatic = false;
		public bool GenerateBrokenGlass = false;

		public bool GenerateLOD = true;

		public bool GenerateFBXAssetsAndPrefab;
		public string ExportedMeshPath;
		public bool ExportMaterials = true;
		public bool ExportTextures = true;
		public bool ExportLODFillers = false;

		public void CopyBp(BuildingBlueprint bp)
		{
			this.BuildingStyle = bp.BuildingStyle.name;

			this.FancyFront = bp.FancyFront;
			this.FancyBack = bp.FancyBack;
			this.FancyRightSide = bp.FancyRightSide;
			this.FancyLeftSide = bp.FancyLeftSide;

			this.BuildingRotation = bp.BuildingRotation;
			this.LastGeneratedPosition = bp.LastGeneratedPosition;

			this.Floors = bp.Floors.ToList<FloorBlueprint>();
			this.RoofInfos = bp.RoofInfos.ToList<RoofInfo>();

			this.GenerateCappers = bp.GenerateCappers;

			this.WindowsGenerateAsStatic = bp.WindowsGenerateAsStatic;
			this.GenerateBrokenGlass = bp.GenerateBrokenGlass;

			this.GenerateLOD = bp.GenerateLOD;

			this.GenerateFBXAssetsAndPrefab = bp.GenerateFBXAssetsAndPrefab;
			this.ExportedMeshPath = bp.ExportedMeshPath;
			this.ExportMaterials = bp.ExportMaterials;
			this.ExportTextures = bp.ExportTextures;
			this.ExportLODFillers = bp.ExportLODFillers;
		}

		public void WriteToBp(ref BuildingBlueprint bp)
		{
			// Load a building style
			#if UNITY_EDITOR

			// This grabbing of assets is a bit HACKY
			string[] guids = UnityEditor.AssetDatabase.FindAssets(this.BuildingStyle);
			for(int i = 0; i < guids.Length; i++)
			{	
				BuildingStyle style = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingStyle>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]));
				if(style != null)
				{
					bp.BuildingStyle = style;
					Debug.Log(bp.BuildingStyle);
					break;
				}
					
			}
			#endif

			bp.FancyFront = this.FancyFront;
			bp.FancyBack = this.FancyBack;
			bp.FancyRightSide = this.FancyRightSide;
			bp.FancyLeftSide = this.FancyLeftSide;

			bp.BuildingRotation = this.BuildingRotation;
			bp.LastGeneratedPosition = this.LastGeneratedPosition;

			bp.Floors = this.Floors.ToList<FloorBlueprint>();
			bp.RoofInfos = this.RoofInfos.ToList<RoofInfo>();

			bp.GenerateCappers = this.GenerateCappers;

			bp.WindowsGenerateAsStatic = this.WindowsGenerateAsStatic;
			bp.GenerateBrokenGlass = this.GenerateBrokenGlass;

			bp.GenerateLOD = this.GenerateLOD;

			bp.GenerateFBXAssetsAndPrefab = this.GenerateFBXAssetsAndPrefab;
			bp.ExportedMeshPath = this.ExportedMeshPath;
			bp.ExportMaterials = this.ExportMaterials;
			bp.ExportTextures = this.ExportTextures;
			bp.ExportLODFillers = this.ExportLODFillers;
		}
	}
}