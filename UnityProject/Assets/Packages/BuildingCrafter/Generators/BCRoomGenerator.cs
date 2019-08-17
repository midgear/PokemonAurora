using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibTessDotNet;
using UnityMesh = UnityEngine.Mesh;

namespace BuildingCrafter
{

	public static partial class BCGenerator
	{
		#region GenerateRoom

		public static GameObject GenerateRoomGameObject(RoomBlueprint roomBp, BuildingBlueprint buildingBp, int floor, int roomIndex, GameObject floorGameObject)
		{
			Bounds bounds = new Bounds(roomBp.PerimeterWalls[0], Vector3.zero);
			for(int i = 0; i < roomBp.PerimeterWalls.Count; i++)
				bounds.Encapsulate(roomBp.PerimeterWalls[i]);

			// Generates the visual mesh for the player
			GameObject newRoom = GenerateRoomWalls(roomBp, buildingBp, floor, roomIndex);

			if(newRoom == null)
				return null;

			// Generates the new colliders for the system
			FloorBlueprint floorBp = BCUtils.GetFloorFromBlueprint(buildingBp, floor);
			FloorBlueprint floorBelow = BCUtils.GetFloorFromBlueprint(buildingBp, floor - 1);

			BCColliderBuilders.GenerateUprightWallColliders(roomBp, floorBp, newRoom);
			BCColliderBuilders.GenerateFloorColliders(roomBp, newRoom, floorBelow);

			// Offsets the just generate colliders correctly
			BoxCollider[] boxColliders = newRoom.GetComponentsInChildren<BoxCollider>();
			for(int i = 0; i < boxColliders.Length; i++)
				boxColliders[i].center -= bounds.center - bounds.extents;

			newRoom.transform.SetParent(floorGameObject.transform);
			newRoom.transform.localPosition = new Vector3(newRoom.transform.localPosition.x, 0, newRoom.transform.localPosition.z);
			newRoom.name = "Room (" + roomIndex + ")";
			newRoom.transform.position += bounds.center - bounds.extents;

			if(buildingBp.GenerateLOD == true)
				AddLODGroupToRoom(roomBp, buildingBp, floor, newRoom);
			return newRoom;
		}

		public static GameObject GenerateRoomWalls(RoomBlueprint roomBp, BuildingBlueprint buildingBp, int floor, int roomIndex)
		{
			FloorBlueprint floorBp = BCUtils.GetFloorFromBlueprint(buildingBp, floor);
			FloorBlueprint floorBelow = BCUtils.GetFloorFromBlueprint(buildingBp, floor - 1);

			// Create a bounds of the entire wall of the system
			Bounds bounds = new Bounds(roomBp.PerimeterWalls[0], Vector3.zero);
			for(int i = 0; i < roomBp.PerimeterWalls.Count; i++)
				bounds.Encapsulate(roomBp.PerimeterWalls[i]);

//			MeshInfo floorMeshInfo = new MeshInfo();
			MeshInfo floorMeshInfo = GenerateRoomFloorMeshes(roomBp, floorBelow);

			// HACK
			// Creates the floor and sets the submesh to the proper index
//			if(floor == 1 && roomIndex == 0)
//				floorMeshInfo = GenerateRoomFloorMeshes(roomBp, floorBelow);

			// Generates the room sides
			MeshInfo wallMeshInfo = GenerateWallMeshes(roomBp, floorBp);

			MeshInfo ceilingMeshInfo = GenerateCeilingMesh(roomBp, floorBp);
			// HACK 
			// Creates a list of roof verticies and floor triangles at z, zero height plus ceiling height that is up relative to the floor
//			MeshInfo ceilingMeshInfo = new MeshInfo();

			// Generates the stair drop walls, making sure that the stairs have little walls that desend so the player can't see through the floors
			MeshInfo stairDropWalls = GenerateStairDropWalls(roomBp, floorBp);
			if(stairDropWalls.IsValid)
				ceilingMeshInfo = BCMesh.CombineMeshInfos(ceilingMeshInfo, stairDropWalls);

			if(floorMeshInfo.IsValid == false || ceilingMeshInfo.IsValid == false || wallMeshInfo.IsValid == false)
			{
				// TODO - reimplement this
//				Debug.LogError("Room " + roomIndex + " on floor " + (floor + 1) + " is unable to generate. This is probably because of some weird layout that wasn't covered by the regular generator."
//					+ " Try modifying the layout of this room to fix it. You can also send this layout to buildingcrafter at 8bitgoose dot com to see if I can fix it");
				return null;
			}

			// Offset all verticies so that the center of this object is at the bottom corner
			for(int i = 0; i < floorMeshInfo.Vertices.Length; i++)
				floorMeshInfo.Vertices[i] -= bounds.center - bounds.extents;

			for(int i = 0; i < ceilingMeshInfo.Vertices.Length; i++)
				ceilingMeshInfo.Vertices[i] -= bounds.center - bounds.extents;

			for(int i = 0; i < wallMeshInfo.Vertices.Length; i++)
				wallMeshInfo.Vertices[i] -= bounds.center - bounds.extents;

			// Generate a parent for the new meshes
			GameObject newRoomParent = BCMesh.GenerateEmptyGameObject("Create Room", true);
			RoomHolder roomHolder = newRoomParent.AddComponent<RoomHolder>();
			roomHolder.RoomIndex = roomIndex;

			// PICK THE ROOM STYLE
			RoomStyle roomStyle = PickRoomStyle(roomBp, buildingBp.BuildingStyle);
			Material[] materials = LoadMaterialByRoomType(roomStyle); // 0 is floor, 1 is walls 2, is ceiling

			// Generate the ceiling mesh
			{
				GameObject newCeiling = BCMesh.GenerateEmptyGameObject("Create Ceiling", true);
				MeshFilter meshFilter = newCeiling.AddComponent<MeshFilter>();
				MeshRenderer meshRenderer = newCeiling.AddComponent<MeshRenderer>();

				meshRenderer.material = materials[2]; // loads the ceiling material
				// PICK THE ROOM STYLE

				UnityMesh m = BCMesh.GetMeshFromMeshInfo(ceilingMeshInfo);
				m.name = "Procedural_Ceiling_Mesh";
				meshFilter.mesh = m;

				newCeiling.transform.SetParent(newRoomParent.transform);
				newCeiling.transform.rotation = Quaternion.identity;
				newCeiling.name = "Room (" +  roomIndex + ") Ceiling ";
			}

			// Generate the wall mesh
			{
				GameObject newWall = BCMesh.GenerateEmptyGameObject("Create Walls", true);
				MeshFilter meshFilter = newWall.AddComponent<MeshFilter>();
				MeshRenderer meshRenderer = newWall.AddComponent<MeshRenderer>();

				meshRenderer.material = materials[1]; // loads the ceiling material
				// PICK THE ROOM STYLE

				UnityMesh m = BCMesh.GetMeshFromMeshInfo(wallMeshInfo);
				m.name = "Procedural_Wall_Mesh";
				meshFilter.mesh = m;

				newWall.transform.SetParent(newRoomParent.transform);
				newWall.transform.rotation = Quaternion.identity;
				newWall.name = "Room (" +  roomIndex + ") Walls";
			}

			// Generate the ceiling mesh
			{
				GameObject newFloor = BCMesh.GenerateEmptyGameObject("Create Floors", true);
				MeshFilter meshFilter = newFloor.AddComponent<MeshFilter>();
				MeshRenderer meshRenderer = newFloor.AddComponent<MeshRenderer>();

				meshRenderer.material = materials[0]; // loads the ceiling material
				// PICK THE ROOM STYLE

				UnityMesh m = BCMesh.GetMeshFromMeshInfo(floorMeshInfo);
				m.name = "Procedural_Floor_Mesh";
				meshFilter.mesh = m;

				newFloor.transform.SetParent(newRoomParent.transform);
				newFloor.transform.localRotation = Quaternion.identity;
				newFloor.name = "Room (" +  roomIndex + ") Bottom";
			}

			return newRoomParent;
		}



		/// <summary>
		/// Generates the mesh for a room
		/// </summary>
		private static MeshInfo GenerateRoomFloorMeshes (RoomBlueprint roomBp, FloorBlueprint floorBelow = null)
		{
			MeshInfo meshInfo = new MeshInfo();

			List<Vector3[]> roomOutline = BCPaths.GetRoomOutline(roomBp, floorBelow);
			List<Vector3[]> roomFloorCutouts = roomOutline.ToList<Vector3[]>();
			roomFloorCutouts.RemoveAt(0);

			List<Vector3[]> roomTiles = BCTilingFloors.GetSquareTiles(roomOutline[0], roomFloorCutouts);

			// Find a square tile and use that as the starting point

			Vector3 startUVPoint = BCTilingFloors.FindFirstSquareTileStart(roomTiles);

			for(int i = 0; i < roomTiles.Count; i++)
			{
//				roomTiles[i] = roomTiles[i].Reverse().ToArray<Vector3>();
				meshInfo = BCMesh.CombineMeshInfos(meshInfo, BCTilingFloors.GenerateSquareTiles(roomTiles[i], startUVPoint));
			}

			return meshInfo;
		}


		/// <summary>
		/// Generates the ceiling mesh for a room
		/// </summary>
		private static MeshInfo GenerateCeilingMesh(RoomBlueprint roomBp, FloorBlueprint floorBp)
		{
			MeshInfo meshInfo = new MeshInfo();

			List<Vector3[]> roomOutline = BCPaths.GetRoomOutline(roomBp, floorBp);
			List<Vector3[]> roomFloorCutouts = roomOutline.ToList<Vector3[]>();
			roomFloorCutouts.RemoveAt(0);

			List<Vector3[]> roomTiles = BCTilingFloors.GetSquareTiles(roomOutline[0], roomFloorCutouts);
			Vector3 startUVPoint = BCTilingFloors.FindFirstSquareTileStart(roomTiles);

			for(int i = 0; i < roomTiles.Count; i++)
			{
				for(int newIndex = 0; newIndex < roomTiles[i].Length; newIndex++)
					roomTiles[i][newIndex] += Vector3.up * roomBp.CeilingHeight;
				meshInfo = BCMesh.CombineMeshInfos(meshInfo,  BCTilingFloors.GenerateSquareTiles(roomTiles[i], startUVPoint, true));
			}

			return meshInfo;
		}

		private static MeshInfo GenerateStairDropWalls(RoomBlueprint roomBp, FloorBlueprint floorBp)
		{
			MeshInfo meshInfo = new MeshInfo();

			Vector3[] roomOutline = roomBp.PerimeterWalls.ToArray<Vector3>();
			// TODO - combine all the stair outlines in each room to do a proper drop (for example, stairs are beside each other).
			// May just leave this off until the stairs are redone in a future version of BC.

			for(int stairIndex = 0; stairIndex < floorBp.Stairs.Count; stairIndex++)
			{
				// have to check to see if the stairs are within the room
				Vector3[] stairOutline = BCUtils.GetStairsOutline(floorBp.Stairs[stairIndex]);
				bool stairsWithin = false;
				for(int i = 0; i < stairOutline.Length; i++)
				{
					if(BCPaths.PointInPolygonXZ(stairOutline[i], roomOutline))
						stairsWithin = true;
				}

				if(stairsWithin == false)
					continue;

			    WallInformation[] wallInfos = CreateWallInfos(stairOutline, 0);

				float ceilingToNextFloorHeight = floorBp.Height - roomBp.CeilingHeight;

				for(int wallIndex = 0; wallIndex < wallInfos.Length; wallIndex++)
				{
					MeshInfo tempMeshInfo = BCTiledWall.CreateSingleWall(wallInfos[wallIndex], ceilingToNextFloorHeight, 1, false);
					for(int i = 0; i < tempMeshInfo.Vertices.Length; i++)
						tempMeshInfo.Vertices[i] += roomBp.CeilingHeight * Vector3.up;

					meshInfo = BCMesh.CombineMeshInfos(tempMeshInfo, meshInfo);
				}
			}

			return meshInfo;
		}

		/// <summary>
		/// Generates the wall mesh for a room floor
		/// </summary>
		/// <returns>The wall meshes.</returns>
		/// <param name="roomBp">Room bp.</param>
		/// <param name="floorBp">Floor bp.</param>
		private static MeshInfo GenerateWallMeshes(RoomBlueprint roomBp, FloorBlueprint floorBp)
		{
			WallInformation[] wallInfos = GenerateWallInfosForMeshGeneration(roomBp.PerimeterWalls.ToArray<Vector3>(), null, floorBp, -0.1f);

			MeshInfo wallMeshInfo = new MeshInfo();

			float startDistance = 0;
			for(int wallIndex = 0; wallIndex < wallInfos.Length; wallIndex++)
			{	
				startDistance = 0;
				wallMeshInfo = BCMesh.CombineMeshInfos(wallMeshInfo, BCTiledWall.CreateSingleWall(wallInfos[wallIndex], floorBp.Height, 3, startDistance, out startDistance, false));

				if(wallInfos[wallIndex].Openings != null)
				{
					// Now add in the openings with the frame generator
					for(int i = 0; i < wallInfos[wallIndex].Openings.Length; i++)
					{
						Opening openings = wallInfos[wallIndex].Openings[i];

						Vector3 openingStart = openings.GetStartPositionOutset(wallInfos[wallIndex], 0) + Vector3.up * openings.Bottom;
						Vector3 openingEnd = openings.GetEndPositionOutset(wallInfos[wallIndex], 0) + Vector3.up * openings.Bottom;
						float height = openings.Top - openings.Bottom;

						Vector3 wallDirection = (openingEnd - openingStart).normalized;
						Vector3 cross = Vector3.Cross(wallDirection.normalized, Vector3.up) * -1;

						bool generateBottomLip = true;
						if(openings.Bottom == 0)
							generateBottomLip = false;

						wallMeshInfo = BCMesh.CombineMeshInfos(wallMeshInfo, 
							BCFrameGenerator.CreateFrame(openingStart, openingEnd, cross, 0.1f, height, wallInfos[wallIndex].StartOffset, false, 3, generateBottomLip));

						// TODO - Delete thie stuff
//						wallMeshInfo = BCMesh.CombineMeshInfos(wallMeshInfo, 
//							BCFrameGenerator.GetFrameFromOutline(wallInfos[wallIndex].StartOffset, wallInfos[wallIndex].EndOffset, 							
//								openings.GetStartPositionOutset(wallInfos[wallIndex], 0), 
//								openings.GetEndPositionOutset(wallInfos[wallIndex], 0), 
//								openings.Bottom, openings.Top, 0.1f));
					}
				}
			}

			// Returns a new object that has all this information in it
			return wallMeshInfo;
		}

		public static WallInformation[] GenerateWallInfosForMeshGeneration(Vector3[] loopedWallPoints, BuildingBlueprint buildingBp, FloorBlueprint floorBp, float outset = 0.1f)
		{
			// CHECK for clockwise looped wall points
			if(BCUtils.IsClockwisePolygon(loopedWallPoints) == false)
			{
				Debug.Log("Trying to generate a wall mesh info from a non clockwise poly. THIS IS BAD");
			}

			WallInformation[] wallInfos = new WallInformation[loopedWallPoints.Length - 1];

			// So we need to transfer in the wall start and end points
			for(int i = 0; i < loopedWallPoints.Length - 1; i++)
			{
				// There is no way to do separate outsets right now. Some day the outsets will probabbly be set based on the room
				// so let's keep that in mind. For now, outset is just set to 0.1 unless on a party wall

				// Set up the walls
				WallInformation newWall = new WallInformation(loopedWallPoints[i], loopedWallPoints[i + 1], outset);

				if(buildingBp != null)
					newWall.Outset = GetPartyWallOutset(newWall, buildingBp);

				// Then set up the openings along each point
				if(floorBp != null)
					newWall = AddOpeningsToWallInfo(newWall, floorBp);

				wallInfos[i] = newWall;
			}

			// Then generate the offset so it can be created properly
			BCGenerator.OutsetWallInfos(ref wallInfos);

			return wallInfos;
		}

		/// <summary>
		/// Loads the material of a room randomly
		/// </summary>
		private static Material[] LoadMaterialByRoomType(RoomStyle roomStyle)
		{
			if(roomStyle == null)
				return new Material[3] { null, null, null };
			
			int wallTypeIndex = Random.Range(0, roomStyle.RoomMaterials.Count);
			
			if(wallTypeIndex < roomStyle.RoomMaterials.Count)
			{
				return new Material[3] 
				{ 
					roomStyle.RoomMaterials[wallTypeIndex].FloorMaterial,
					roomStyle.RoomMaterials[wallTypeIndex].WallMaterial,
					roomStyle.RoomMaterials[wallTypeIndex].CeilingMaterial
				};
			}
			
			return new Material[3] { null, null, null };
		}

		/// <summary>
		/// Returns the style that the room is
		/// </summary>
		public static RoomStyle PickRoomStyle(RoomBlueprint roomBp, BuildingStyle buildingStyle)
		{
			int index = -1;
			
			RoomStyle styleToReturn = null;
			
			if(roomBp.OverrideRoomStyle != null)
			{
				styleToReturn = roomBp.OverrideRoomStyle;
				return styleToReturn;
			}
			
			switch(roomBp.RoomType)
			{
			case(RoomType.Generic):
				if(buildingStyle.GeneralRoomStyle.Count == 0)
					break;
				
				index = Random.Range(0, buildingStyle.GeneralRoomStyle.Count);
				styleToReturn = buildingStyle.GeneralRoomStyle[index];
				break;
				
			case(RoomType.LivingRoom):
				if(buildingStyle.LivingRoomStyle.Count == 0)
					break;
				
				index = Random.Range(0, buildingStyle.LivingRoomStyle.Count);
				styleToReturn =  buildingStyle.LivingRoomStyle[index];
				break;
				
			case(RoomType.Bedroom):
				if(buildingStyle.BedroomStyle.Count == 0)
					break;
				
				index = Random.Range(0, buildingStyle.BedroomStyle.Count);
				styleToReturn =  buildingStyle.BedroomStyle[index];
				break;
				
			case(RoomType.Closet):
				if(buildingStyle.ClosetStyle.Count == 0)
					break;
				
				index = Random.Range(0, buildingStyle.ClosetStyle.Count);
				styleToReturn =  buildingStyle.ClosetStyle[index];
				break;
				
			case(RoomType.Hallways):
				if(buildingStyle.HallwaysStyle.Count == 0)
					break;
				
				index = Random.Range(0, buildingStyle.HallwaysStyle.Count);
				styleToReturn =  buildingStyle.HallwaysStyle[index];
				break;
				
			case(RoomType.Kitchen):
				if(buildingStyle.KitchenStyle.Count == 0)
					break;
				
				index = Random.Range(0, buildingStyle.KitchenStyle.Count);
				styleToReturn =  buildingStyle.KitchenStyle[index];
				break;
				
			case(RoomType.Dining):
				if(buildingStyle.DiningStyle.Count == 0)
					break;
				
				index = Random.Range(0, buildingStyle.DiningStyle.Count);
				styleToReturn =  buildingStyle.DiningStyle[index];
				break;
				
			case(RoomType.Bathroom):
				if(buildingStyle.BathroomStyle.Count == 0)
					break;
				
				index = Random.Range(0, buildingStyle.BathroomStyle.Count);
				styleToReturn =  buildingStyle.BathroomStyle[index];
				break;
				
			case(RoomType.Kids):
				if(buildingStyle.KidsStyle.Count == 0)
					break;
				
				index = Random.Range(0, buildingStyle.KidsStyle.Count);
				styleToReturn =  buildingStyle.KidsStyle[index];
				break;
				
			case(RoomType.Utility):
				if(buildingStyle.UtilityStyle.Count == 0)
					break;
				
				index = Random.Range(0, buildingStyle.UtilityStyle.Count);
				styleToReturn =  buildingStyle.UtilityStyle[index];
				break;
				
			case(RoomType.Patio):
				if(buildingStyle.PatioStyle.Count == 0)
					break;
				
				index = Random.Range(0, buildingStyle.PatioStyle.Count);
				styleToReturn =  buildingStyle.PatioStyle[index];
				break;
				
			case(RoomType.Garage):
				if(buildingStyle.GarageStyle.Count == 0)
					break;
				
				index = Random.Range(0, buildingStyle.GarageStyle.Count);
				styleToReturn =  buildingStyle.GarageStyle[index];
				break;
				
			case(RoomType.Office):
				if(buildingStyle.OfficeStyle.Count == 0)
					break;
				
				index = Random.Range(0, buildingStyle.OfficeStyle.Count);
				styleToReturn =  buildingStyle.OfficeStyle[index];
				break;
				
			case(RoomType.Store):
				if(buildingStyle.StoreStyle.Count == 0)
					break;
				
				index = Random.Range(0, buildingStyle.StoreStyle.Count);
				styleToReturn =  buildingStyle.StoreStyle[index];
				break;
				
			case(RoomType.StoreBackroom):
				if(buildingStyle.StoreBackroomStyle.Count == 0)
					break;
				
				index = Random.Range(0, buildingStyle.StoreBackroomStyle.Count);
				styleToReturn =  buildingStyle.StoreBackroomStyle[index];
				break;
				
			}
			
			if(styleToReturn == null && buildingStyle.GeneralRoomStyle.Count > 0)
				styleToReturn = buildingStyle.GeneralRoomStyle[0];
			else if(styleToReturn == null)
			{
				Debug.LogError("This building style (" + buildingStyle.name + ") does not have a generic style, please add one");
				return null;
			}
			
			return styleToReturn;
		}

		public static void AddLODGroupToRoom(RoomBlueprint roomBp, BuildingBlueprint buildingBp, int floorIndex, GameObject newRoom)
		{
			FloorBlueprint floorBp = BCUtils.GetFloorFromBlueprint(buildingBp, floorIndex);
			LODGroup lodGroup = newRoom.AddComponent<LODGroup>();
			List<Renderer> renderers = new List<Renderer>();
			renderers.AddRange(newRoom.GetComponentsInChildren<Renderer>()); // Adds the room's renderer

			LOD[] lods = new LOD[2];
			lods[0] = new LOD(0.30f, renderers.ToArray<Renderer>());

			GameObject roomLODFill = BCMesh.GenerateRoomFiller(roomBp, floorBp, floorIndex, newRoom, buildingBp);
			if(roomLODFill != null)
			{
				Renderer[] fillerRenderer = roomLODFill.GetComponents<Renderer>();
				lods[1] = new LOD(0.0f, fillerRenderer);
			}
			else
				lods[1] = new LOD(0, new Renderer[0]);
				
			lodGroup.SetLODs(lods);
			lodGroup.RecalculateBounds();
			if(lodGroup.size < 7.5f)
				lodGroup.size = 7.5f;
			
			// TODO: Need to create a local LOD room filler to ensure that you can't see through a building
			// for smaller rooms that have a lower bounding box.
		}
		#endregion
	}
}