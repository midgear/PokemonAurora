using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BuildingCrafter
{
	[System.Serializable]
	public class BuildingBlueprint : MonoBehaviour 
	{
		// NOTE:
		// If you change anything in here, you'll want to look at BCBlueprintUtils and make sure the checks already work well there
		// NOTE on SERIALIZATION
		// If you add anything in here, you'll need to check BuildingBpSerialized to ensure that all the information is serialized
		public BuildingStyle BuildingStyle;

		public bool FancyFront = true;
		public bool FancyBack = false;
		public bool FancyRightSide = false;
		public bool FancyLeftSide = false;

		public bool BuildingIsAllFancy
		{ 
			get { return this.FancyBack && this.FancyFront && this.FancyLeftSide && this.FancyRightSide; }
		}

		public Quaternion BuildingRotation = new Quaternion();
		public Vector3 LastGeneratedPosition = Vector3.zero;

		public List<FloorBlueprint> Floors = new List<FloorBlueprint>();
		public List<RoofInfo> RoofInfos = new List<RoofInfo>();

		[Header("Generate Options")]
		public bool GenerateCappers = false;
		public bool UseAtlases = false;

		[Header("Window Generate Options")]
		public bool WindowsGenerateAsStatic = false;
		public bool GenerateBrokenGlass = false;

		[Header("LOD Settings")]
		public bool GenerateLOD = true;

		[Header("Export Settings")]
		public bool GenerateFBXAssetsAndPrefab = false;
		public string ExportedMeshPath = "/Assets/Packages/BuildingCrafterAssets/Resources/ExportedOBJs/";
		public bool ExportMaterials = false;
		public bool ExportTextures = false;
		public bool ExportLODFillers = false;

//		[Header("Atlas Settings")]
//		public string AtlasParentFolder = "/Assets/Packages/BuildingCrafterAssets/Atlases/";

		// ================== LIVE VIEW STUFF ===================
		[Header("Live View Settings")]
		[Tooltip("Generates the building on the fly while you edit, note can have some perforamnce issues.")]
		public bool LiveViewEnabled = true;

		// ======================================================
		// ================ NON SERIALIZED STUFF ================
		// ======================================================

		// ON THE WAY OUT
		public List<float> XPartyWalls = new List<float>();
		public List<float> ZPartyWalls = new List<float>();

		public List<PartyWall> PartyWalls = new List<PartyWall>();

		public int LastFancySideIndex = 0;
		[HideInInspector]
		/// <summary>What is the fancy wall index of this entire building that was generated last. Used for the live view</summary>
		public int LastWallIndex = 0;

		[HideInInspector]
		public bool ShowGenerationOptions = false;
		[HideInInspector]
		public bool ShowAtlasOptions = false;
		[HideInInspector]
		public int SelectedFloor = 0;
		[HideInInspector]
		public int LastSelectedFloor = 0;
		[HideInInspector]
		public int PreviousFloor = 0;

		// Private local varibles
		private Transform cachedTrans;
		// Debug images
		public List<Vector3[]> DebugOutline = new List<Vector3[]>();

		// Ensures we don't leak meshes when destroying this object by delete 
		void OnDestroy()
		{
			BCGenerator.DestroyAllBuildingMeshes(this);
		}

		/// <summary>
		/// Gets the transform from a cached value
		/// </summary>
		/// <value>The transform.</value>
		public Transform Transform
		{
			get
			{
				if(cachedTrans == null)
					cachedTrans = this.transform;
				return cachedTrans;
			}
		}

		[HideInInspector]
		public int CurrentFloorIndex
		{
			get { return this.Floors.Count - this.SelectedFloor - 1; }
		}

		public Vector3 BlueprintXZCenter
		{
			get { return new Vector3(Transform.position.x, 0, Transform.position.z); }
		}

		public Vector3 BlueprintGroundHeight
		{
			get { return new Vector3(0, Transform.position.y, 0); }
		}

		void Awake()
		{
			// Ensures that the building is always fully visible even if live view was used and 
			// the building crafter generator wasn't deselected
			BCBlueprintUtils.ShowAllMeshesInBuilding(this);
		}

		public void GenerateSelf()
		{
			BCGenerator.GenerateFullBuilding(this);
		}
	}
}