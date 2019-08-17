using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{
	public class BuildingStyle : ScriptableObject
	{
		// SUPER IMPORTANT NOTE
		// If you add anything here, in the Building Style Panel you MUST MUST add it to the duplicate copy below
		// NOTE: If you add or remove ANY of these fields, you MUST update the BCPackage exporter as it is somewhat hand built - KBH Aug 2015

		public Material[] FancySidings;
		public Material PlainSiding;
		public Material Window;
		public Material DoorWindowFrames;
		public Material Rooftop;
		public Material Grass; // Not Added to BCPackageExporter
		public Material Concrete; // Not Added to BCPackageExporter
		public Material DirtPath; // Not Added to BCPackageExporter

		public Vector2[] FancyCrown;
		public Vector2[] PlainCrown;

		// Prefabs used when generating items
		public GameObject OutsideFancyDoor;
		public GameObject OutsidePlainDoor;
		public GameObject StandardDoor;
		public GameObject HeavyDoor;
		public GameObject TwoByFourStairs;
		public GameObject StairsToRoof;
		public List<GameObject> FancyWindows = new List<GameObject>();
		public List<WindowTypeEnum> FancyWindowTypes = new List<WindowTypeEnum>();
		public GameObject BaseWindow;
		
		// These fields ARE NOT grabbed by the BC Package Exporter
		public List<RoomStyle> GeneralRoomStyle = new List<RoomStyle>();
		public List<RoomStyle> LivingRoomStyle = new List<RoomStyle>();
		public List<RoomStyle> BedroomStyle = new List<RoomStyle>();
		public List<RoomStyle> ClosetStyle = new List<RoomStyle>();
		public List<RoomStyle> HallwaysStyle = new List<RoomStyle>();
		public List<RoomStyle> KitchenStyle = new List<RoomStyle>();
		public List<RoomStyle> DiningStyle = new List<RoomStyle>();
		public List<RoomStyle> BathroomStyle = new List<RoomStyle>();
		public List<RoomStyle> KidsStyle = new List<RoomStyle>();
		public List<RoomStyle> UtilityStyle = new List<RoomStyle>();
		public List<RoomStyle> PatioStyle = new List<RoomStyle>();
		public List<RoomStyle> GarageStyle = new List<RoomStyle>();
		public List<RoomStyle> OfficeStyle = new List<RoomStyle>();
		public List<RoomStyle> StoreStyle = new List<RoomStyle>();
		public List<RoomStyle> StoreBackroomStyle = new List<RoomStyle>();

		// Atlasing
		public BCAtlas.Size AtlasSize = BCAtlas.Size.Medium4096;
		public bool IsAtlased 
		{
			get 
			{
				if(this.AtlasMaterials == null)
					return false;

				if(this.AtlasMaterials.Length == 0)
					return false;

				for(int i = 0; i < this.AtlasMaterials.Length; i++)
				{
					if(this.AtlasMaterials[i].Material == null)
						return false;
				}

				return true;
			} 
		}
		public BCAtlas[] AtlasMaterials = new BCAtlas[0];

		/// <summary>
		/// Ensures there are no problems with the building style
		/// </summary>
		public void ValidateThisBuildingStyle()
		{

		}

		public Material[] GetAllMaterials()
		{
			List<Material> materialList = new List<Material>();

			materialList.AddRange(this.FancySidings);
			materialList.Add(this.PlainSiding);

			materialList.Add(this.Rooftop);

			materialList.AddRange(GetMaterialsFromRoomStyle(GeneralRoomStyle));
			materialList.AddRange(GetMaterialsFromRoomStyle(LivingRoomStyle));
			materialList.AddRange(GetMaterialsFromRoomStyle(BedroomStyle));
			materialList.AddRange(GetMaterialsFromRoomStyle(ClosetStyle));
			materialList.AddRange(GetMaterialsFromRoomStyle(HallwaysStyle));
			materialList.AddRange(GetMaterialsFromRoomStyle(KitchenStyle));
			materialList.AddRange(GetMaterialsFromRoomStyle(DiningStyle));
			materialList.AddRange(GetMaterialsFromRoomStyle(BathroomStyle));
			materialList.AddRange(GetMaterialsFromRoomStyle(KidsStyle));
			materialList.AddRange(GetMaterialsFromRoomStyle(UtilityStyle));
			materialList.AddRange(GetMaterialsFromRoomStyle(PatioStyle));
			materialList.AddRange(GetMaterialsFromRoomStyle(GarageStyle));
			materialList.AddRange(GetMaterialsFromRoomStyle(OfficeStyle));
			materialList.AddRange(GetMaterialsFromRoomStyle(StoreStyle));
			materialList.AddRange(GetMaterialsFromRoomStyle(StoreBackroomStyle));

			// TODO: the yard stuff like concrete, grass and path

			List<Material> uniqueMaterials = new List<Material>();
			for(int i = 0; i < materialList.Count; i++)
			{
				if(uniqueMaterials.Contains(materialList[i]) == false)
					uniqueMaterials.Add(materialList[i]);
			}

			return uniqueMaterials.ToArray<Material>();
		}

		/// <summary>
		/// Provides a reference copy of the building Style (since Unity Nativly doesn't do it)
		/// </summary>
		/// <returns>The this style.</returns>
		/// <param name="script">Script.</param>
		public static BuildingStyle DuplicateThisStyle (BuildingStyle oldAsset)
		{
			BuildingStyle newAsset = BuildingStyle.CreateInstance<BuildingStyle>();

			newAsset.FancySidings = DuplicateMaterialArray(oldAsset.FancySidings);
			newAsset.PlainSiding = oldAsset.PlainSiding;
			newAsset.Window = oldAsset.Window;
			newAsset.DoorWindowFrames= oldAsset.DoorWindowFrames;
			newAsset.Rooftop= oldAsset.Rooftop;
			newAsset.Grass= oldAsset.Grass;
			newAsset.Concrete= oldAsset.Concrete;
			newAsset.DirtPath= oldAsset.DirtPath;

			newAsset.FancyCrown = oldAsset.FancyCrown.ToArray<Vector2>();
			newAsset.PlainCrown = oldAsset.PlainCrown.ToArray<Vector2>();

			newAsset.OutsideFancyDoor = oldAsset.OutsideFancyDoor;
			newAsset.OutsidePlainDoor = oldAsset.OutsidePlainDoor;
			newAsset.StandardDoor = oldAsset.StandardDoor;
			newAsset.HeavyDoor = oldAsset.HeavyDoor;
			newAsset.TwoByFourStairs = oldAsset.TwoByFourStairs;
			newAsset.StairsToRoof = oldAsset.StairsToRoof;
			newAsset.BaseWindow = oldAsset.BaseWindow;
			newAsset.FancyWindows = oldAsset.FancyWindows;
			newAsset.FancyWindowTypes = oldAsset.FancyWindowTypes;

			newAsset.GeneralRoomStyle = DuplicateList(oldAsset.GeneralRoomStyle);
			newAsset.LivingRoomStyle = DuplicateList(oldAsset.LivingRoomStyle);
			newAsset.BedroomStyle = DuplicateList(oldAsset.BedroomStyle);
			newAsset.ClosetStyle = DuplicateList(oldAsset.ClosetStyle);
			newAsset.HallwaysStyle = DuplicateList(oldAsset.HallwaysStyle);
			newAsset.KitchenStyle = DuplicateList(oldAsset.KitchenStyle);
			newAsset.DiningStyle = DuplicateList(oldAsset.DiningStyle);
			newAsset.BathroomStyle = DuplicateList(oldAsset.BathroomStyle);
			newAsset.KidsStyle = DuplicateList(oldAsset.KidsStyle);
			newAsset.UtilityStyle = DuplicateList(oldAsset.UtilityStyle);
			newAsset.PatioStyle = DuplicateList(oldAsset.PatioStyle);
			newAsset.GarageStyle = DuplicateList(oldAsset.GarageStyle);
			newAsset.OfficeStyle = DuplicateList(oldAsset.OfficeStyle);
			newAsset.StoreStyle = DuplicateList(oldAsset.StoreStyle);
			newAsset.StoreBackroomStyle = DuplicateList(oldAsset.StoreBackroomStyle);

			newAsset.AtlasSize = oldAsset.AtlasSize;

			return newAsset;
		}

		private static List<RoomStyle> DuplicateList(List<RoomStyle> roomStyles)
		{
			List<RoomStyle> newList = new List<RoomStyle>();
			for(int i = 0; i < roomStyles.Count; i++)
			{
				newList.Add(roomStyles[i]);
			}

			return newList;
		}

		private static Material[] DuplicateMaterialArray(Material[] materials)
		{
			Material[] returnMaterial = new Material[materials.Length];

			for(int i = 0; i < returnMaterial.Length; i++)
				returnMaterial[i] = materials[i];

			return returnMaterial;
		}

		private static GameObject[] DuplicateGameOBjectArray(GameObject[] gameObjects)
		{
			GameObject[] returnGameObject = new GameObject[gameObjects.Length];
			
			for(int i = 0; i < returnGameObject.Length; i++)
				returnGameObject[i] = gameObjects[i];
			
			return returnGameObject;
		}

		private List<Material> GetMaterialsFromRoomStyle(List<RoomStyle> roomStyles)
		{
			List<Material> roomMaterials = new List<Material>();

			for(int i = 0; i < roomStyles.Count; i++)
				roomMaterials.AddRange(GetMaterialsFromRoomStyle(roomStyles[i]));

			return roomMaterials;
		}

		private List<Material> GetMaterialsFromRoomStyle(RoomStyle roomStyle)
		{
			if(roomStyle.RoomMaterials == null || roomStyle.RoomMaterials.Count < 1)
				return new List<Material>();
			
			List<Material> roomMaterials = new List<Material>();
			for(int i = 0; i < roomStyle.RoomMaterials.Count; i++)
			{
				roomMaterials.Add(roomStyle.RoomMaterials[i].FloorMaterial);
				roomMaterials.Add(roomStyle.RoomMaterials[i].WallMaterial);
				roomMaterials.Add(roomStyle.RoomMaterials[i].CeilingMaterial);
			}

			return roomMaterials;
		}
	}
}