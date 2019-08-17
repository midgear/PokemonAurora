using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BuildingCrafter
{
	public class BCBlueprintUtils : MonoBehaviour 
	{
		public static SimpleBuildingBlueprint SimplifyBuildingBp(BuildingBlueprint oldBuildingBp)
		{
			SimpleBuildingBlueprint simpleBp = new SimpleBuildingBlueprint();

			for(int i = 0; i < oldBuildingBp.Floors.Count; i++)
				simpleBp.Floors.Add(BCUtils.DeepCopyFloor(oldBuildingBp.Floors[i]));

			simpleBp.PartyWalls.AddRange(oldBuildingBp.PartyWalls.ToList<PartyWall>());

			simpleBp.RoofInfos.AddRange(oldBuildingBp.RoofInfos.ToArray<RoofInfo>());

			simpleBp.FancyFront = oldBuildingBp.FancyFront;
			simpleBp.FancyBack = oldBuildingBp.FancyBack;
			simpleBp.FancyLeftSide = oldBuildingBp.FancyLeftSide;
			simpleBp.FancyRightSide = oldBuildingBp.FancyRightSide;

			return simpleBp;
		}

		public static BuildingDiff FindBuildingDiferences(BuildingBlueprint newBuildingBp, SimpleBuildingBlueprint oldBuildingBp)
		{
			BuildingDiff diff = new BuildingDiff();

			// Figure out if the floors are different right now
			if(newBuildingBp.Floors.Count < oldBuildingBp.Floors.Count)
			{
				int floorIndex = -1;

				for(int i = 0; i < oldBuildingBp.Floors.Count; i++)
				{
					if(i >= newBuildingBp.Floors.Count)
					{
						floorIndex = i;
						break;
					}

					if(oldBuildingBp.Floors[i] != newBuildingBp.Floors[i])
					{
						floorIndex = i;
						break;
					}
				}

				if(floorIndex > -1)
				{
					diff.HasFloorRemoved = true;
					diff.FloorRemovedIndex = floorIndex;
				}
			}
			else if(newBuildingBp.Floors.Count > oldBuildingBp.Floors.Count)
			{
				int floorIndex = -1;
				
				for(int i = 0; i < newBuildingBp.Floors.Count; i++)
				{
					if(i >= oldBuildingBp.Floors.Count)
					{
						floorIndex = i;
						break;
					}
					
					if(oldBuildingBp.Floors[i] != newBuildingBp.Floors[i])
					{
						floorIndex = i;
						break;
					}
				}
				
				if(floorIndex > -1)
				{
					diff.HasFloorAdded = true;
					diff.FloorAddedIndex = floorIndex;
				}
			}

			// ========== TEST FOR WINDOW AND DOORS CHANGE FIRST ===========
			
			if(newBuildingBp.Floors.Count == oldBuildingBp.Floors.Count)
			{
				for(int floorIndex = 0; floorIndex < newBuildingBp.Floors.Count; floorIndex++)
				{
					FloorBlueprint floorBp = newBuildingBp.Floors[floorIndex];
					FloorBlueprint oldFloorBp = oldBuildingBp.Floors[floorIndex];
					
					if(floorBp == oldFloorBp)
						continue;

					diff.FloorChangedIndex = floorIndex;

					// =======================================
					//         Test for Window Change
					// =======================================
					
					if(floorBp.Windows.Count == oldFloorBp.Windows.Count)
					{
						for(int i = 0; i < floorBp.Windows.Count; i++)
						{
							if(floorBp.Windows[i] != oldFloorBp.Windows[i])
							{
								diff.WindowsHaveChanged = true;
								diff.WindowChangedIndex = i;
							}
						}
					}
					else if(floorBp.Windows.Count > oldFloorBp.Windows.Count)
					{
						diff.WindowsHaveChanged = true;
						diff.WindowChangedIndex = floorBp.Windows.Count - 1;
						diff.NewWindowAdded = true;
					}
					else if(floorBp.Windows.Count < oldFloorBp.Windows.Count)
					{
						diff.WindowsHaveChanged = true;
						diff.WindowChangedIndex = -1;
						for(int i = 0; i < oldFloorBp.Windows.Count; i++)
						{
							if(i >= floorBp.Windows.Count)
							{
								diff.WindowChangedIndex = i;
								break;
							}

							if(floorBp.Windows[i] != oldFloorBp.Windows[i])
							{
								diff.WindowChangedIndex = i;
								break;
							}
						}
						diff.WindowDestroyed = true;
					}


					// ====================================
					//         Test for Door Change
					// ====================================
					if(floorBp.Doors.Count == oldFloorBp.Doors.Count)
					{
						for(int i = 0; i < floorBp.Doors.Count; i++)
						{
							if(floorBp.Doors[i] != oldFloorBp.Doors[i])
							{
								diff.DoorsHaveChanged = true;
								diff.DoorChangedIndex = i;
								return diff;
							}
						}
					}
					else if(floorBp.Doors.Count > oldFloorBp.Doors.Count)
					{
						diff.DoorsHaveChanged = true;
						diff.DoorChangedIndex = floorBp.Doors.Count - 1;
						diff.NewDoorAdded = true;
						return diff;
					}
					else if(floorBp.Doors.Count < oldFloorBp.Doors.Count)
					{
						diff.DoorsHaveChanged = true;
						diff.DoorChangedIndex = -1;
						for(int i = 0; i < oldFloorBp.Doors.Count; i++)
						{
							if(i >= floorBp.Doors.Count)
							{
								diff.DoorChangedIndex = i;
								break;
							}
							
							if(floorBp.Doors[i] != oldFloorBp.Doors[i])
							{
								diff.DoorChangedIndex = i;
								break;
							}
						}
						diff.DoorDestroyed = true;
					}

					// ====================================
					//         Test for Stair Change
					// ====================================
					if(floorBp.Stairs.Count > oldFloorBp.Stairs.Count)
					{
						diff.StairsHaveChanged = true;
						diff.StairChangedIndex = floorBp.Stairs.Count - 1;
						diff.NewStairAdded = true;
					}
					else if(floorBp.Stairs.Count < oldFloorBp.Stairs.Count)
					{
						diff.StairsHaveChanged = true;
						diff.StairChangedIndex = -1;
						for(int i = 0; i < oldFloorBp.Stairs.Count; i++)
						{
							if(i >= floorBp.Stairs.Count)
							{
								diff.StairChangedIndex = i;
								break;
							}
							
							if(floorBp.Stairs[i] != oldFloorBp.Stairs[i])
							{
								diff.StairChangedIndex = i;
								break;
							}
						}
						diff.StairDestroyed = true;
					}
				}
			}

			// ===== CHANGE IN ROOMS =====
			if(newBuildingBp.Floors.Count == oldBuildingBp.Floors.Count)
			{
				for(int floorIndex = 0; floorIndex < newBuildingBp.Floors.Count; floorIndex++)
				{
					FloorBlueprint floorBp = newBuildingBp.Floors[floorIndex];
					FloorBlueprint oldFloorBp = oldBuildingBp.Floors[floorIndex];

					if(floorBp == oldFloorBp)
						continue;

					diff.FloorChangedIndex = floorIndex;

					if(floorBp.RoomBlueprints.Count == oldFloorBp.RoomBlueprints.Count)
					{
						for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
						{
							if(floorBp.RoomBlueprints[i] != oldFloorBp.RoomBlueprints[i])
							{
								diff.RoomsHaveChanged = true;
								diff.RoomChangedIndex = i;
								return diff;
							}
						}
					}
					else if(floorBp.RoomBlueprints.Count < oldFloorBp.RoomBlueprints.Count) // deleted a room
					{
						int index = 0;

						for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
						{
							if(floorBp.RoomBlueprints[i] != oldFloorBp.RoomBlueprints[i])
								break;

							index++;
						}

						diff.RoomsHaveChanged = true;
						diff.RoomDestroyed = true;
						diff.RoomChangedIndex = index;
						return diff;
					}
					else if(floorBp.RoomBlueprints.Count > oldFloorBp.RoomBlueprints.Count)
					{
						diff.RoomsHaveChanged = true;
						diff.RoomAdded = true;
						diff.RoomChangedIndex = floorBp.RoomBlueprints.Count - 1;
					}
				}
			}

			// ===== CHANGE IN PARTY WALLS =====
			if(newBuildingBp.Floors.Count == oldBuildingBp.Floors.Count)
			{
				for(int floorIndex = 0; floorIndex < newBuildingBp.Floors.Count; floorIndex++)
				{
					FloorBlueprint floorBp = newBuildingBp.Floors[floorIndex];
					FloorBlueprint oldFloorBp = oldBuildingBp.Floors[floorIndex];

					if(floorBp == oldFloorBp)
						continue;

					diff.FloorChangedIndex = floorIndex;

					if(floorBp.RoomBlueprints.Count == oldFloorBp.RoomBlueprints.Count)
					{
						for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
						{
							if(floorBp.RoomBlueprints[i] != oldFloorBp.RoomBlueprints[i])
							{
								diff.RoomsHaveChanged = true;
								diff.RoomChangedIndex = i;
								return diff;
							}
						}
					}
					else if(floorBp.RoomBlueprints.Count < oldFloorBp.RoomBlueprints.Count) // deleted a room
					{
						int index = 0;

						for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
						{
							if(floorBp.RoomBlueprints[i] != oldFloorBp.RoomBlueprints[i])
								break;

							index++;
						}

						diff.RoomsHaveChanged = true;
						diff.RoomDestroyed = true;
						diff.RoomChangedIndex = index;
						return diff;
					}
					else if(floorBp.RoomBlueprints.Count > oldFloorBp.RoomBlueprints.Count)
					{
						diff.RoomsHaveChanged = true;
						diff.RoomAdded = true;
						diff.RoomChangedIndex = floorBp.RoomBlueprints.Count - 1;
					}
				}
			}

			if(newBuildingBp.PartyWalls.Count != oldBuildingBp.PartyWalls.Count)
				diff.PartyWallsHaveChanged = true;
			else
			{
				for(int newIndex = 0; newIndex < newBuildingBp.PartyWalls.Count; newIndex++)
				{
					bool didNotFindMatchingWall = true;

					for(int oldIndex = 0; oldIndex < oldBuildingBp.PartyWalls.Count; oldIndex++)
					{
						if(oldBuildingBp.PartyWalls[oldIndex] == newBuildingBp.PartyWalls[newIndex])
						{
//							Debug.Log("Matching " + oldIndex + " & " + newIndex);
							didNotFindMatchingWall = false;
							break;
						}
					}

					if(didNotFindMatchingWall == true)
					{
//						Debug.Log(oldBuildingBp.PartyWalls.Count + " and NEW " + newBuildingBp.PartyWalls.Count);
//						Debug.Log("Did not find matching party wall");
						diff.PartyWallsHaveChanged = true;
					}
				}
			}

			// ============ Fancy Siding Change Test ============
			if(newBuildingBp.FancyBack != oldBuildingBp.FancyBack
				|| newBuildingBp.FancyFront != oldBuildingBp.FancyFront
				|| newBuildingBp.FancyLeftSide != oldBuildingBp.FancyLeftSide
				|| newBuildingBp.FancyRightSide != oldBuildingBp.FancyRightSide)
			{
				diff.FancySidesHaveChanged = true;
			}

			return diff;
		}

		/// <summary>
		/// Finds the floor height y axis in LOCAL space
		/// </summary>
		public static float FindFloorHeightRelativeToGround(BuildingBlueprint buildingBp, int floorIndex)
		{
			float height = 0;

			for(int i = 0; i < floorIndex; i++)
				height += buildingBp.Floors[i].Height;

			return height;
		}

		/// <summary>
		/// Finds the top floor in a building, or returns -1 if the whole building is empty
		/// </summary>
		/// <returns>The top floor index.</returns>
		/// <param name="buildingBp">Building bp.</param>
		public static int FindTopFloorIndex(BuildingBlueprint buildingBp)
		{
			for(int i = buildingBp.Floors.Count - 1; i >= 0; i--)
			{
				if(buildingBp.Floors[i].RoomBlueprints == null || buildingBp.Floors[i].RoomBlueprints.Count < 1)
				{
					// This is not the top floor
				}
				else
				{
					return i;
				}
			}

			return -1;
		}

		public static GameObject FindOrCreateFloorGameObject(BuildingBlueprint buildingBp, int floorIndex)
		{
			FloorHolder[] floorHolders = buildingBp.GetComponentsInChildren<FloorHolder>();

			GameObject floorGameObject = null;

			for(int i = 0; i < floorHolders.Length; i++)
			{
				if(floorHolders[i].FloorIndex == floorIndex)
				{
					floorGameObject = floorHolders[i].gameObject;
					break;
				}
			}

			if(floorGameObject == null)
			{
				floorGameObject = BCMesh.GenerateEmptyGameObject("Create Floor GameObject");
				floorGameObject.name = "Floor " + (floorIndex + 1);
				FloorHolder floorHolder = floorGameObject.AddComponent<FloorHolder>();
				floorHolder.FloorIndex = floorIndex;
				
				floorGameObject.transform.position = new Vector3(buildingBp.transform.position.x, 0, buildingBp.transform.position.z);
				floorGameObject.transform.SetParent(buildingBp.transform);
				Vector3 localPos = floorGameObject.transform.localPosition;
				localPos.y += BCBlueprintUtils.FindFloorHeightRelativeToGround(buildingBp, floorIndex);
				floorGameObject.transform.localPosition = localPos;
			}

			return floorGameObject;
		}

		public static FloorHolder FindRealFloor (BuildingBlueprint buildingBp, int floorIndex)
		{
			FloorHolder[] floorHolders = buildingBp.GetComponentsInChildren<FloorHolder>();

			for(int i = 0; i < floorHolders.Length; i++)
			{
				if(floorHolders[i].FloorIndex == floorIndex)
				{
					return floorHolders[i];
				}
			}

			return null;
		}

		public static RoomHolder FindRealRoom (FloorHolder floorHolder, int roomIndex)
		{
			if(floorHolder == null)
				return null;

			RoomHolder[] roomHolder = floorHolder.GetComponentsInChildren<RoomHolder>();
			
			for(int i = 0; i < roomHolder.Length; i++)
			{
				if(roomHolder[i].RoomIndex == roomIndex)
				{
					return roomHolder[i];
				}
			}
			
			return null;
		}

		public static WindowHolder FindRealWindow(FloorHolder floorHolder, int windowIndex)
		{
			if(floorHolder == null)
				return null;

			WindowHolder[] roomHolder = floorHolder.GetComponentsInChildren<WindowHolder>();
			
			for(int i = 0; i < roomHolder.Length; i++)
			{
				if(roomHolder[i].Index == windowIndex)
				{
					return roomHolder[i];
				}
			}
			
			return null;
		}

		public static DoorHolder FindRealDoor(FloorHolder floorHolder, int doorIndex)
		{
			DoorHolder[] roomHolder = floorHolder.GetComponentsInChildren<DoorHolder>();
			
			for(int i = 0; i < roomHolder.Length; i++)
			{
				if(roomHolder[i].Index == doorIndex)
				{
					return roomHolder[i];
				}
			}
			
			return null;
		}

		public static void RegenerateWallsAroundOpening(Vector3 start, Vector3 end, BuildingBlueprint buildingBp, int floorIndex, bool addHeight = true)
		{
			int[] rooms = BCUtils.FindRoomIndexesAroundLine(start, end, buildingBp.Floors[floorIndex]);

			if(rooms != null)
			{
				// Destroy the rooms in question to be generated again
				for(int i = 0; i < rooms.Length; i++)
				{
					BCGenerator.DestroyRoom(buildingBp, floorIndex, rooms[i]);
					GameObject newRoom = BCGenerator.GenerateSpecificRoom(buildingBp, floorIndex, rooms[i]);

					if(addHeight)
						newRoom.transform.localPosition = new Vector3(newRoom.transform.localPosition.x, 0, newRoom.transform.localPosition.z);
				}
			}

			BCWallRoofGenerator.GenerateWallsForFloor(buildingBp, floorIndex);
		}

		public static void ShowAllMeshesInBuilding(BuildingBlueprint buildingBp)
		{
			MeshRenderer[] allMeshRenderers = buildingBp.GetComponentsInChildren<MeshRenderer>();
			for(int i = 0; i < allMeshRenderers.Length; i++)
				allMeshRenderers[i].enabled = true;
		}
	}

	/// <summary>
	/// A simple version of a blueprint used for serialization
	/// </summary>
	public class SimpleBuildingBlueprint
	{
		public List<FloorBlueprint> Floors = new List<FloorBlueprint>();
		public List<RoofInfo> RoofInfos = new List<RoofInfo>();

		public List<PartyWall> PartyWalls = new List<PartyWall>();

		public BuildingStyle BuildingStyle;
		
		public bool FancyFront = false;
		public bool FancyBack = false;
		public bool FancyRightSide = false;
		public bool FancyLeftSide = false;
	}
	
	public struct BuildingDiff
	{
		// Floor Add
		public bool HasFloorAdded;
		public int FloorAddedIndex;

		// Floor Remove
		public bool HasFloorRemoved;
		public int FloorRemovedIndex;

		// Related to Any Room Changes
		public int FloorChangedIndex;

		// Room Remove
		public bool RoomsHaveChanged;
		public bool RoomDestroyed;
		public bool RoomAdded;
		public int RoomChangedIndex;

		// Door Changes
		public bool DoorsHaveChanged;
		public bool DoorDestroyed;
		public bool NewDoorAdded;
		public int DoorChangedIndex;

		// Window Changes
		public bool WindowsHaveChanged;
		public bool NewWindowAdded;
		public bool WindowDestroyed;
		public int WindowChangedIndex;

		// Stairs have changed
		public bool StairsHaveChanged;
		public bool StairDestroyed;
		public bool NewStairAdded;
		public int StairChangedIndex;

		// Sides
		public bool FancySidesHaveChanged;

		// Party walls have changed
		public bool PartyWallsHaveChanged;

		public bool HasDiff
		{
			get
			{
				if(HasFloorAdded
				    || HasFloorRemoved
				    || RoomsHaveChanged
				    || DoorsHaveChanged
				    || WindowsHaveChanged
				    || StairsHaveChanged
					|| FancySidesHaveChanged
					|| PartyWallsHaveChanged)
					return true;
				
				return false;
			}
		}
	}
}