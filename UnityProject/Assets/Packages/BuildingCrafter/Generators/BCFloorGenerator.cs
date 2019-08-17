using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BuildingCrafter
{
	public static partial class BCGenerator
	{
		public static GameObject GenerateFloor(BuildingBlueprint buildingBp, int floorIndex)
		{
			FloorBlueprint floorBp = BCUtils.GetFloorFromBlueprint(buildingBp, floorIndex);

			if(floorBp.RoomBlueprints.Count == 0)
				Debug.LogError("Floor " + (floorIndex + 1) + " does not have any rooms, this can cause problems building the floor");

			GameObject floorGameObject = BCBlueprintUtils.FindOrCreateFloorGameObject(buildingBp, floorIndex);

			GenerateStairs(floorIndex, buildingBp, floorGameObject.transform);
			GenerateDoors(floorIndex, buildingBp, floorGameObject.transform);

			GameObject[] generatedWindows = GenerateWindows(floorIndex, buildingBp, floorGameObject.transform, buildingBp.GenerateBrokenGlass);

			for(int i = 0; i < floorBp.RoomBlueprints.Count; i++)
			{
				RoomBlueprint roomBp = floorBp.RoomBlueprints[i];

				GameObject newRoom = BCGenerator.GenerateRoomGameObject(roomBp, buildingBp, floorIndex, i, floorGameObject);

				// Runs any extended options on top of the normally execute scripts
				ExecuteExtendedInformation(newRoom, buildingBp, floorIndex, i);

				if(buildingBp.GenerateLOD)
				{
					// Adds frames and windows that were generated to JUST one of the room's LOD controllers
//					AddGameObjectToLOD(ref doorFrameObjs, roomBp, newRoom);
					AddGameObjectToLOD(ref generatedWindows, roomBp, newRoom);
				}
			}

			// TODO: Add an extend per floor in addition to the new room extension

			// Now parents it and then moves it to the correct height in the building
			floorGameObject.transform.SetParent(buildingBp.gameObject.transform);
			floorGameObject.transform.localPosition = new Vector3(floorGameObject.transform.localPosition.x, 0, floorGameObject.transform.localPosition.z);
			floorGameObject.transform.localPosition += BCBlueprintUtils.FindFloorHeightRelativeToGround(buildingBp, floorIndex) * Vector3.up;
			floorGameObject.transform.localRotation = Quaternion.identity;

			return floorGameObject;
		}

		/// <summary>
		/// Executes any extended information for a room style
		/// </summary>
		private static void ExecuteExtendedInformation(GameObject newRoom, BuildingBlueprint buildingBp, int floorIndex, int roomIndex)
		{
			RoomBlueprint roomBp = BCUtils.GetRoomFromBlueprint(buildingBp, floorIndex, roomIndex);

			if(roomBp == null)
				return;

			RoomStyle roomStyle = PickRoomStyle(roomBp, buildingBp.BuildingStyle);

			for(int i = 0; i < roomStyle.RoomExtenders.Count; i++)
			{
				if(roomStyle.RoomExtenders[i] == null)
					continue;

				IRoomExtension roomExtent = roomStyle.RoomExtenders[i] as IRoomExtension;
				if(roomExtent != null)
				{
					roomExtent.ExecuteUponRoomGeneration(newRoom, buildingBp, floorIndex, roomIndex);
				}
			}
		}

		/// <summary>
		/// Generates all the stairs for a floor
		/// </summary>
		public static void GenerateStairs (int floorIndex, BuildingBlueprint buildingBp, Transform parent)
		{
			FloorBlueprint floorBp = buildingBp.Floors[floorIndex];
			BuildingStyle buildingStyle = buildingBp.BuildingStyle;

			for (int i = 0; i < floorBp.Stairs.Count; i++) 
			{
				var stair = floorBp.Stairs [i];
				GameObject newStairs = null;
				
	#if UNITY_EDITOR
				newStairs = PrefabUtility.InstantiatePrefab(buildingStyle.TwoByFourStairs) as GameObject;
	#else
				newStairs = GameObject.Instantiate (buildingStyle.TwoByFourStairs) as GameObject;
	#endif
				if(newStairs == null)
					continue;
				
				newStairs.transform.position = stair.Start;
				newStairs.transform.SetParent(parent);
				newStairs.transform.localPosition = new Vector3(newStairs.transform.localPosition.x, 0, newStairs.transform.localPosition.z);
				Vector3 direction = (stair.Start - stair.End).normalized;
				newStairs.transform.LookAt(direction + newStairs.transform.position);
			}
		}
		
		/// <summary>
		/// Generates a floor's doors
		/// </summary>
		static void GenerateDoors (int floorIndex, BuildingBlueprint buildingBp, Transform parent)
		{
			FloorBlueprint floorBp = buildingBp.Floors[floorIndex];
			
			for (int i = 0; i < floorBp.Doors.Count; i++) 
			{
				GenerateSpecificDoor(i, floorIndex, buildingBp, parent);
			}
		}

		public static void GenerateSpecificDoor(int doorIndex, int floorIndex, BuildingBlueprint buildingBp, Transform parent)
		{
			FloorBlueprint floorBp = buildingBp.Floors[floorIndex];
			DoorInfo door = buildingBp.Floors[floorIndex].Doors[doorIndex];
			Vector3 doorHeight = Vector3.zero; // In case the door is needed to be bumped up for the roof one
			
			// Does not generate doors over 2 meters wide
			if((door.Start - door.End).magnitude > 2.5)
				return;
			
			// Only generates standard, heavy and door to roof types
			if(door.DoorType != DoorTypeEnum.Standard && door.DoorType != DoorTypeEnum.Heavy && door.DoorType != DoorTypeEnum.DoorToRoof)
				return;
			
			bool outsideDoor = false;
			bool fancyDoor = true;
			
			TestForOutsideDoors(door, floorBp, buildingBp, out outsideDoor, out fancyDoor);
			
			GameObject doorPrefab = null;
			
			if(outsideDoor == true)
			{
				if(door.IsForcedPlain == true)
					doorPrefab = buildingBp.BuildingStyle.OutsidePlainDoor;
				else if(fancyDoor == true)
					doorPrefab = buildingBp.BuildingStyle.OutsideFancyDoor;
				else
					doorPrefab = buildingBp.BuildingStyle.OutsidePlainDoor;
			}
			else if(door.DoorType == DoorTypeEnum.Standard)
				doorPrefab = buildingBp.BuildingStyle.StandardDoor;
			else if(door.DoorType == DoorTypeEnum.Heavy)
				doorPrefab = buildingBp.BuildingStyle.HeavyDoor;

			if(door.DoorType == DoorTypeEnum.DoorToRoof)
				doorPrefab = buildingBp.BuildingStyle.HeavyDoor;

			// If the door wasn't loaded properly, just load the standard door
			if(doorPrefab == null)
				doorPrefab = buildingBp.BuildingStyle.StandardDoor;
			
			if(doorPrefab == null)
			{
				Debug.LogError("Building Style " + buildingBp.BuildingStyle + " does not have a Standard Door, can not generate doors");
				return;
			}
			
			
			if(doorPrefab != null)
			{
				// Need to generate the doorin the right direction
				// Then need to offset the hinge so it swings correctly
#if UNITY_EDITOR
				GameObject newDoor = PrefabUtility.InstantiatePrefab(doorPrefab) as GameObject;
#else
				GameObject newDoor = GameObject.Instantiate (doorPrefab, door.Start + doorHeight, Quaternion.identity) as GameObject;
#endif
				DoorMeshInfo doorMeshInfo = newDoor.GetComponentInChildren<DoorMeshInfo>();
				DoorHinge doorHinge = newDoor.GetComponentInChildren<DoorHinge>();
				if(doorMeshInfo == null || doorHinge == null)
				{
					Debug.LogError("Warning: The door " + newDoor.name + " door MUST have DoorMeshInfo OR DoorHing on it to ensure it swings correctly. Please see the 'resources' folder for template.");
#if UNITY_EDITOR
					Undo.DestroyObjectImmediate(newDoor);
#else
					GameObject.Destroy(newDoor);
#endif
					return;
				}
				
				newDoor.transform.position = door.Start + doorHeight;
				
				newDoor.transform.SetParent(parent);
				Vector3 direction = (door.Start - door.End).normalized;
				
				// Sets the hinge offset so the door doesn't clip into the frame when rotating. NOTE: The - 1 reverses the door direction
				doorMeshInfo.transform.localPosition = new Vector3(0, 0, doorMeshInfo.HingeOffset * -1 * door.Direction);
				
				doorMeshInfo.DoorInfo = door;
				
				// Also store the information in this door mesh about the doors 
				newDoor.transform.LookAt (direction + door.Start + doorHeight);
				newDoor.transform.Rotate(Vector3.up, -90f);

				if(door.IsDoubleDoor == false && door.DoorType != DoorTypeEnum.DoorToRoof)
				{
					newDoor.AddComponent<DoorHolder>().Index = doorIndex;
					newDoor.transform.localPosition = new Vector3(newDoor.transform.localPosition.x, 0, newDoor.transform.localPosition.z);
				}
				if(door.DoorType == DoorTypeEnum.DoorToRoof)
				{
					GameObject doorHolder = BCMesh.GenerateEmptyGameObject("Add Door Holder", false);
					doorHolder.AddComponent<DoorHolder>().Index = doorIndex;
					doorHolder.name = newDoor.name + "doorToRoof";
					doorHolder.transform.position = newDoor.transform.position;

					doorHolder.transform.SetParent(parent);
					doorHolder.transform.localPosition = new Vector3(doorHolder.transform.localPosition.x, 0, doorHolder.transform.localPosition.z);

					newDoor.transform.SetParent(doorHolder.transform);
					newDoor.transform.localPosition = new Vector3(newDoor.transform.localPosition.x, 0.5f, newDoor.transform.localPosition.z);

#if UNITY_EDITOR
					GameObject stairsToRoof = PrefabUtility.InstantiatePrefab(buildingBp.BuildingStyle.StairsToRoof) as GameObject;
#else
					GameObject stairsToRoof = GameObject.Instantiate (buildingBp.BuildingStyle.StairsToRoof) as GameObject;
#endif
					
					stairsToRoof.transform.position = door.Start;
					
					Vector3 doorCenter = (door.Start + door.End) / 2f;
					
					Vector3 inset = -1 * BCUtils.GetOutsetFromManyRooms(doorCenter, floorBp);
					
					stairsToRoof.transform.LookAt(stairsToRoof.transform.position + inset * 10f);
					
					Vector3 stairFront = stairsToRoof.transform.position - stairsToRoof.transform.right * 0.5f;
					
					if(BCUtils.TestBetweenTwoPoints(stairFront, door.Start, door.End) == false)
						stairsToRoof.transform.position += stairsToRoof.transform.right * 1f;
					
					// Move the stairs to the inside of the area
					stairsToRoof.transform.position += inset * 1f;
					
					stairsToRoof.transform.SetParent(doorHolder.transform);
					stairsToRoof.transform.localPosition = new Vector3(stairsToRoof.transform.localPosition.x, 0, stairsToRoof.transform.localPosition.z);
				}
				else if(door.IsDoubleDoor) // Create the mirror other door if this doorway is 2 meters wide
				{
#if UNITY_EDITOR
					GameObject otherDoor = PrefabUtility.InstantiatePrefab(doorPrefab) as GameObject;
#else
					GameObject otherDoor = GameObject.Instantiate (doorPrefab, door.End + doorHeight, Quaternion.identity) as GameObject;
#endif
					otherDoor.transform.position = door.End + doorHeight;
					
					otherDoor.transform.SetParent(parent);
					Vector3 otherDirection = (door.End - door.Start).normalized;
					
					DoorMeshInfo otherDoorMeshInfo = otherDoor.GetComponentInChildren<DoorMeshInfo>();
					
					otherDoorMeshInfo.DoorInfo = door;
					otherDoorMeshInfo.DoorInfo.Direction *= -1; // Reverses the door direction so it swings correctly
					
					// Sets the hinge offset so the door doesn't clip into the frame when rotating.
					otherDoorMeshInfo.transform.localPosition = new Vector3(0, 0, otherDoorMeshInfo.HingeOffset * door.Direction);
					
					otherDoor.transform.LookAt (otherDirection + door.Start + doorHeight);
					otherDoor.transform.Rotate(Vector3.up, 90f);

					// Add both doors to a holder
					// Create new holder
					GameObject doorHolder = BCMesh.GenerateEmptyGameObject("Add Door Holder", false);
					doorHolder.AddComponent<DoorHolder>().Index = doorIndex;
					doorHolder.name = newDoor.name + "x2";
					doorHolder.transform.position = newDoor.transform.position;
					
					newDoor.transform.SetParent(doorHolder.transform);
					otherDoor.transform.SetParent(doorHolder.transform);

					doorHolder.transform.SetParent(parent);
					doorHolder.transform.localPosition = new Vector3(doorHolder.transform.localPosition.x, 0, doorHolder.transform.localPosition.z);

					DoorOpener otherOpener = otherDoor.GetComponent<DoorOpener>();
					if(otherOpener != null)
						otherOpener.SetDoorToStartingRotation();
				}

				// Now we set the door to the opening state if it needs it
				DoorOpener opener = newDoor.GetComponent<DoorOpener>();
				if(opener != null)
					opener.SetDoorToStartingRotation();
			}
		}
		
		/// <summary>
		/// Tests to see if a door is on the outside of a building and on the fancy side of the building
		/// </summary>
		public static void TestForOutsideDoors(DoorInfo door, FloorBlueprint floorBp, BuildingBlueprint buildingBp, out bool outsideDoor, out bool fancyDoor)
		{ // Calculate if this door is on the outside
			Vector3 offset = Vector3.zero;
			outsideDoor = false;
			fancyDoor = true;

			// Find which way we should test for the offset
			if(door.Start.x == door.End.x)
				offset = new Vector3(1, 0, 0);
			else
				offset = new Vector3(0, 0, 1);
			
			// Now find the middle of the door;
			Vector3 middleDoor = (door.Start + door.End) / 2;
			
			bool positiveOffsetIsEmpty = false;
			bool negativeOffsetIsEmpty = false;
			
			if(BCUtils.IsPointOnlyInsideAnyRoom(middleDoor + offset, floorBp) == false)
				positiveOffsetIsEmpty = true;
			
			if(BCUtils.IsPointOnlyInsideAnyRoom(middleDoor - offset, floorBp) == false)
				negativeOffsetIsEmpty = true;
			
			if(positiveOffsetIsEmpty || negativeOffsetIsEmpty)
				outsideDoor = true;
			
			// Test the direction of the door
			if(outsideDoor && positiveOffsetIsEmpty == false && negativeOffsetIsEmpty == true && offset.z != 0 && buildingBp.FancyFront == false)
				fancyDoor = false;
			
			if(outsideDoor && positiveOffsetIsEmpty == true && negativeOffsetIsEmpty == false && offset.z != 0 && buildingBp.FancyBack == false)
				fancyDoor = false;
			
			if(outsideDoor && positiveOffsetIsEmpty == false && negativeOffsetIsEmpty == true && offset.x != 0 && buildingBp.FancyLeftSide == false)
				fancyDoor = false;
			
			if(outsideDoor && positiveOffsetIsEmpty == true && negativeOffsetIsEmpty == false && offset.x != 0 && buildingBp.FancyRightSide == false)
				fancyDoor = false;
		}
		
		/// <summary>
		/// Generates a window the size of the window opening
		/// </summary>
		public static GameObject[] GenerateWindows (int floorIndex, BuildingBlueprint buildingBp, Transform parent, bool generateBrokenGlass)
		{
			// Windows are going to be a bit different because we need to generate very different sizes
			
			// First step will be to build up a repo of all the already generated windows
			// If those already generated windows exist as OBJs in the assets, do not do anything
			// If it doesn't exist, then we magically create them and add them to the asset database
			
			// All windows constrain to snapping into 0.5m spaces. They always overlap with windows
			// At the same time as we generate these windows, we will also generate the broken version of the windows as well
			// These will have random smash along the length. Use some stupid math to figure this out.

			FloorBlueprint floorBp = buildingBp.Floors[floorIndex];

			// First step generating a single window panel
			if(floorBp.Windows.Count < 1)
				return new GameObject[0];

			GameObject[] generatedWindows = new GameObject[floorBp.Windows.Count];

			// Generate smashed version of a window - TODO change the style of this window
			for (int i = 0; i < floorBp.Windows.Count; i++) 
			{
				GameObject windowGameObject = GenerateSpecificWindow(i, floorIndex, buildingBp, parent);
				generatedWindows[i] = windowGameObject;
			}

			if(generatedWindows == null)
				return null;
			
			// Adds a window reference so the building can find all windows in this building.
			for(int i = 0; i < generatedWindows.Length; i++)
			{
				if(generatedWindows == null)
					continue;

				if(generatedWindows[i] == null)
					continue;
			}

			return generatedWindows;
		}

		public static GameObject GenerateSpecificWindow(int windowIndex, int floorIndex, BuildingBlueprint buildingBp, Transform parent)
		{
			FloorBlueprint floorBp = buildingBp.Floors[floorIndex];
			BuildingStyle buildingStyle = buildingBp.BuildingStyle;

			WindowInfo window = floorBp.Windows[windowIndex];
			
			if(window.IsWindowEmpty == true)
				return null;
			
			float bottom = window.BottomHeight;
			float top = window.TopHeight;

			float newHeight = top - bottom;
			Vector3 direction = (window.End - window.Start).normalized;
			
			GameObject windowGameObject = null;
			
			// Calculates if the window is facing the wrong way
			Vector3 middleWindow = (window.End + window.Start) / 2;
			Vector3 cross = Vector3.Cross(direction, Vector3.up);
			bool isCrossInsideBuilding = BCUtils.IsPointOnlyInsideAnyRoom(middleWindow + cross * 0.1f, floorBp);
			
			Vector3 startOfWindow = window.Start + Vector3.up * bottom + direction * .1f;
			Vector3 endOfWindow = window.End + Vector3.up * bottom - direction * .1f;
			
			if(isCrossInsideBuilding == false)
			{
				Vector3 oldStart = startOfWindow;
				startOfWindow = endOfWindow;
				endOfWindow = oldStart;
				cross = Vector3.Cross((endOfWindow - startOfWindow).normalized, Vector3.up);
			}
			
			BCWindow bcWindow = BCUtils.GetWindowPrefabTypeFromWindowInfo(window, buildingStyle);
			windowGameObject = BCWindowStretcher.GenerateStretchedWindows(bcWindow, startOfWindow, endOfWindow, newHeight, false);

			if(windowGameObject == null)
				return null;
			
			windowGameObject.AddComponent<WindowHolder>().Index = windowIndex;
			float yHeight = bottom;
			windowGameObject.transform.SetParent(parent);
			windowGameObject.transform.localPosition = new Vector3(windowGameObject.transform.localPosition.x, yHeight, windowGameObject.transform.localPosition.z);
			
			// Offset the window by its centering position
			windowGameObject.transform.position -= cross * bcWindow.CenterOfWindow;

			return windowGameObject;
		}

		/// <summary>
		/// Destructively adds a gameObject to a room's LOD
		/// </summary>
		public static void AddGameObjectToLOD(ref GameObject[] gameObjects, RoomBlueprint roomBp, GameObject newRoom)
		{
			if(newRoom == null)
				return;

			for(int i = 0; i < gameObjects.Length; i++)
			{
				if(gameObjects[i] == null)
					continue;
				
				if(BCUtils.IsPointInARoom(gameObjects[i].transform.position, roomBp))
				{
					// Get the LODs from the roomBp
					LODGroup lodGroup = newRoom.GetComponent<LODGroup>();
					LOD[] lods = lodGroup.GetLODs();
					
					// Get the render to add
					Renderer[] gameObjectRenderers = gameObjects[i].GetComponentsInChildren<Renderer>(true);
					
					// Create a new LOD
					Renderer[] renderers = new Renderer[lods[0].renderers.Length + gameObjectRenderers.Length];

					int lodLength = lods[0].renderers.Length;

					for(int j = 0; j < lodLength; j++)
						renderers[j] = lods[0].renderers[j];

					for(int j = 0; j < gameObjectRenderers.Length; j++)
					{
						renderers[lodLength + j] = gameObjectRenderers[j];
					}

					lods[0] = new LOD(0.3f, renderers);
					
					lodGroup.SetLODs(lods);
					
					gameObjects[i] = null;
				}
			}
		}

		/// <summary>
		/// Will just generate a specific room passed by room index
		/// </summary>
		/// <param name="buildingBp">Building bp.</param>
		/// <param name="floorIndex">Floor index.</param>
		/// <param name="roomIndex">Room index.</param>
		public static GameObject GenerateSpecificRoom(BuildingBlueprint buildingBp, int floorIndex, int roomIndex)
		{
			if(floorIndex >= buildingBp.Floors.Count || roomIndex >= buildingBp.Floors[floorIndex].RoomBlueprints.Count)
				return null;

			RoomBlueprint roomBp = buildingBp.Floors[floorIndex].RoomBlueprints[roomIndex];

			GameObject floorHolder = BCBlueprintUtils.FindOrCreateFloorGameObject(buildingBp, floorIndex);

			GameObject newRoom = BCGenerator.GenerateRoomGameObject(roomBp, buildingBp, floorIndex, roomIndex, floorHolder);
			newRoom.name += " " + roomIndex;
			newRoom.transform.SetParent(floorHolder.transform);
			newRoom.transform.localPosition = new Vector3(newRoom.transform.localPosition.x, 0, newRoom.transform.localPosition.z);

//			floorHolder.transform.localPosition = new Vector3(floorHolder.transform.localPosition.x, 
//			                                                  BCBlueprintUtils.FindFloorHeightRelativeToGround(buildingBp, floorIndex),
//			                                                  floorHolder.transform.localPosition.z);

			return newRoom;
		}
	}
}