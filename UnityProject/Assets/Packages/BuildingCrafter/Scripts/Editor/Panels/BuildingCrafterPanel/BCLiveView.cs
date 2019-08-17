using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace BuildingCrafter
{
	public partial class BuildingCrafterPanel : Editor 
	{
		SimpleBuildingBlueprint lastBuildingBp = null;
		int lastViewedFloorIndex = -1;
		int lastFloorCount = -1;

		/// <summary>
		/// Checks to see if the building needs to be generated when the user starts looking at it
		/// </summary>
		public void GenerateOnEnable()
		{
			bool needsGeneration = false;

			// Checks to see if 
			if(Script.BuildingBlueprint.LiveViewEnabled == true
				&& Script.BuildingBlueprint.GetComponentsInChildren<FloorHolder>().Length == 0
			   	&& Script.BuildingBlueprint.Floors.Count > 0)
				needsGeneration = true;

			if(needsGeneration)
				BCGenerator.GenerateFullBuilding(this.Script.BuildingBlueprint);
		}

		public void OnDisableLiveView()
		{
			if(Script.BuildingBlueprint != null && Script.BuildingBlueprint.LiveViewEnabled)
				BCBlueprintUtils.ShowAllMeshesInBuilding(Script.BuildingBlueprint);
		}

		public void ShowLiveView()
		{
			if(Script.BuildingBlueprint.LiveViewEnabled == false)
				return;

			if(lastBuildingBp == null)
				lastBuildingBp = BCBlueprintUtils.SimplifyBuildingBp(Script.BuildingBlueprint);

			// get a reference to the current floor
			int currentlySelectedFloor = Script.BuildingBlueprint.CurrentFloorIndex;

			if(this.lastViewedFloorIndex != currentlySelectedFloor)
			{
				HideFloorsNotInUse(Script.BuildingBlueprint);
				this.lastViewedFloorIndex = currentlySelectedFloor;
			}

			if(this.lastFloorCount != Script.BuildingBlueprint.Floors.Count)
			{
				HideFloorsNotInUse(Script.BuildingBlueprint);
				this.lastFloorCount = Script.BuildingBlueprint.Floors.Count;
			}

			BuildingDiff diff = BCBlueprintUtils.FindBuildingDiferences(Script.BuildingBlueprint, lastBuildingBp);

			if(diff.HasDiff && Event.current.type == EventType.Repaint)
			{
				LiveUpdateBuilding(Script.BuildingBlueprint, diff);
				lastBuildingBp = BCBlueprintUtils.SimplifyBuildingBp(Script.BuildingBlueprint);
			}
		}

		public static void HideFloorsNotInUse(BuildingBlueprint buildingBp)
		{
			// First unhide all floors
			BCBlueprintUtils.ShowAllMeshesInBuilding(buildingBp);

			// Disables all floors above this height
			for(int floorIndex = buildingBp.Floors.Count - 1; floorIndex > buildingBp.CurrentFloorIndex; floorIndex--)
			{
				FloorHolder floorHolder = BCBlueprintUtils.FindRealFloor(buildingBp, floorIndex);
				if(floorHolder == null)
					continue;

				MeshRenderer[] meshRenderers = floorHolder.GetComponentsInChildren<MeshRenderer>(true);

				for(int i = 0; i < meshRenderers.Length; i++)
				{
					meshRenderers[i].enabled = false;
				}	
			}

			FloorHolder currentFloor = BCBlueprintUtils.FindRealFloor(buildingBp, buildingBp.CurrentFloorIndex);

			if(currentFloor == null)
				return;

			// Disables all the non room walls
			Transform currentFloorTrans = currentFloor.transform;
			for(int i = 0; i < currentFloorTrans.childCount; i++)
			{
				Transform testingChild = currentFloorTrans.GetChild(i);

				if(testingChild.name.Contains("Wall") || testingChild.name == "Roof")
				{
					MeshRenderer meshRend = testingChild.GetComponentInChildren<MeshRenderer>();
					meshRend.enabled = false;
				}
			}
		}

		/// <summary>
		/// Shows off what the laying room will look like
		/// </summary>
		void DrawPreviewOfNewRoom (Vector3 currentGridPoint)
		{
			if(Script.BuildingBlueprint.LiveViewEnabled == false || Script.EditingState == EditingState.LayYard)
				return;

			// Destroys any extra preview objects
			PreviewProceduralGameObject[] allPreviewObjects = Script.BuildingBlueprint.GetComponentsInChildren<PreviewProceduralGameObject>();
			if(allPreviewObjects.Length > 1)
			{
				for(int i = 0; i < allPreviewObjects.Length; i++)
				{
					GameObject.DestroyImmediate(allPreviewObjects[i].gameObject);
				}
			}

			if(Script.CurrentWallPath.Count < 1)
			{
				if(Script.TempWallDisplay != null)
					BCGenerator.DestroyAllProceduralMeshes(Script.TempWallDisplay.gameObject, true);
				return;
			}

			if(Script.CurrentWallPath.Count != Script.LastWallPathCount || currentGridPoint != lastGridPoint)
			{
				BCGenerator.DestroyAllProceduralMeshes(Script.TempWallDisplay, true);

				Script.LastWallPathCount = Script.CurrentWallPath.Count;

				MeshInfo allMeshes = new MeshInfo();

				RoomStyle roomStyle = Script.BuildingBlueprint.BuildingStyle.GeneralRoomStyle.FirstOrDefault();
				RoomMaterials roomMat = null;
				Material wallMat = null;
				if(roomStyle != null)
					roomMat = roomStyle.RoomMaterials.FirstOrDefault();
				if(roomMat != null)
					wallMat = roomMat.WallMaterial;

				for(int i = 0; i < Script.CurrentWallPath.Count - 1; i++)
				{
					int thisPoint = i;
					int nextPoint = i + 1;
					
					Vector3 p1 = Script.CurrentWallPath[thisPoint];
					Vector3 p2 = Script.CurrentWallPath[nextPoint];

					float currentFloorHeight = BCBlueprintUtils.FindFloorHeightRelativeToGround(Script.BuildingBlueprint, Script.CurrentFloor);

					MeshInfo meshes = BCMesh.GeneratePreviewWall(p1, p2, Vector3.zero, Vector3.zero, 
	                                                          currentFloorHeight,
	                                                          Script.CurrentFloorBlueprint.Height);
					MeshInfo otherWay = BCMesh.GeneratePreviewWall(p1, p2, Vector3.zero, Vector3.zero, 
					                                            currentFloorHeight,
					                                            Script.CurrentFloorBlueprint.Height, 0.1f, 0, true);

					allMeshes = BCMesh.CombineMeshInfos(allMeshes, otherWay);
					allMeshes = BCMesh.CombineMeshInfos(allMeshes, meshes);
				}

				{
					Vector3 p1 = Script.CurrentWallPath[Script.CurrentWallPath.Count - 1];
					Vector3 p2 = currentGridPoint;
					
					float currentFloorHeight = BCBlueprintUtils.FindFloorHeightRelativeToGround(Script.BuildingBlueprint, Script.CurrentFloor);
					
					MeshInfo meshes = BCMesh.GeneratePreviewWall(p1, p2, Vector3.zero, Vector3.zero, 
					                                          currentFloorHeight,
					                                          Script.CurrentFloorBlueprint.Height);
					MeshInfo otherWay = BCMesh.GeneratePreviewWall(p1, p2, Vector3.zero, Vector3.zero, 
					                                            currentFloorHeight,
					                                            Script.CurrentFloorBlueprint.Height, 0.1f, 0, true);
					
					allMeshes = BCMesh.CombineMeshInfos(allMeshes, otherWay);
					allMeshes = BCMesh.CombineMeshInfos(allMeshes, meshes);
				}

				Script.TempWallDisplay = BCMesh.GenerateGameObjectFromMesh(allMeshes, Script.BuildingBlueprint.BlueprintXZCenter, "Preview Wall", "Preview Procedural Mesh", wallMat);
				Script.TempWallDisplay.AddComponent<PreviewProceduralGameObject>();
				Script.TempWallDisplay.transform.SetParent(Script.BuildingBlueprint.Transform);
				Script.TempWallDisplay.transform.localPosition = new Vector3(Script.TempWallDisplay.transform.localPosition.x, 0, Script.TempWallDisplay.transform.localPosition.z);
				Script.TempWallDisplay.transform.localPosition += Vector3.up * BCBlueprintUtils.FindFloorHeightRelativeToGround(Script.BuildingBlueprint, Script.CurrentFloor);
			}
		}

		public static void LiveUpdateBuilding(BuildingBlueprint buildingBp, BuildingDiff diff)
		{
			if(diff.HasDiff == false)
				return;

			if(buildingBp == null)
				return;

			CheckForIncorrectlyGeneratedFloors(buildingBp, diff);

			if(buildingBp.Transform.childCount < 1)
				BCGenerator.GenerateFullBuilding(buildingBp);

			if(diff.PartyWallsHaveChanged)
			{
				BCGenerator.DestroyOutsideWalls(buildingBp);
				BCGenerator.GenerateBuilding(buildingBp, false, false, true, false, false);
//				Debug.Log("party walls have changed");
			}

			// FLOOR ADD
			if(diff.HasFloorAdded)
			{
				if(buildingBp.Floors[diff.FloorAddedIndex].RoomBlueprints.Count != 0)
				{
					FloorHolder[] floorHolders = buildingBp.GetComponentsInChildren<FloorHolder>();
					int floorAddedIndex = diff.FloorAddedIndex;

					// Moves the current floors up
					for(int i = 0; i < floorHolders.Length; i++)
					{
						if(floorHolders[i].FloorIndex >= floorAddedIndex)
						{
							floorHolders[i].FloorIndex++;							
							Vector3 currentPos = floorHolders[i].transform.localPosition;
							float newHeight = BCBlueprintUtils.FindFloorHeightRelativeToGround(buildingBp, floorHolders[i].FloorIndex);
							floorHolders[i].transform.localPosition = new Vector3(currentPos.x, newHeight, currentPos.z);
							floorHolders[i].name = "Floor " + (floorHolders[i].FloorIndex + 1);
						}
					}

					// Generate the new floor
					BCWallRoofGenerator.GenerateWallsForFloor(buildingBp, floorAddedIndex);
					BCGenerator.GenerateFloor(buildingBp, floorAddedIndex);
				}
			}
			// FLOOR REMOVE
			else if(diff.HasFloorRemoved)
			{
				int floorIndex = diff.FloorRemovedIndex;

				BCGenerator.DestroyFloor(buildingBp, floorIndex);

				FloorHolder[] floorHolders = buildingBp.GetComponentsInChildren<FloorHolder>();
				for(int i = 0; i < floorHolders.Length; i++)
				{
					if(floorHolders[i].FloorIndex >= floorIndex)
					{
						floorHolders[i].FloorIndex--;

						Vector3 currentPos = floorHolders[i].transform.localPosition;
						float newHeight = BCBlueprintUtils.FindFloorHeightRelativeToGround(buildingBp, floorHolders[i].FloorIndex);
						floorHolders[i].transform.localPosition = new Vector3(currentPos.x, newHeight, currentPos.z);
						floorHolders[i].name = "Floor " + (floorHolders[i].FloorIndex + 1);
					}
				}

				BCWallRoofGenerator.GenerateWallsForFloor(buildingBp, floorIndex);
			}
			// ONE OF THE ROOMS HAS CHANGED
			else if(diff.RoomsHaveChanged)
			{
				int floorIndex = diff.FloorChangedIndex;
				int roomIndex = diff.RoomChangedIndex;

				// We've added a new room, lets build it and do the outside walls
				if(diff.RoomAdded)
				{
					BCGenerator.CalculatePartyWalls(buildingBp);
					roomIndex = diff.RoomChangedIndex;
					BCWallRoofGenerator.GenerateWallsForFloor(buildingBp, floorIndex);
					BCGenerator.GenerateSpecificRoom(buildingBp, floorIndex, roomIndex); // ~ 3ms
				}
				else if(diff.RoomDestroyed)
				{
					BCGenerator.CalculatePartyWalls(buildingBp);
					BCWallRoofGenerator.GenerateWallsForFloor(buildingBp, floorIndex);

					FloorHolder floorHolder = BCBlueprintUtils.FindRealFloor(buildingBp, floorIndex);
					RoomHolder roomHolder = BCBlueprintUtils.FindRealRoom(floorHolder, roomIndex);

					// Reassign the index in each holder so it is right
					if(floorHolder != null)
					{
						RoomHolder[] roomHolders = floorHolder.GetComponentsInChildren<RoomHolder>();
						for(int i = 0; i < roomHolders.Length; i++)
						{
							if(roomHolders[i].RoomIndex >= roomIndex)
								roomHolders[i].RoomIndex--;
						}

						if(roomHolder != null)
						{
							BCGenerator.DestroyAllProceduralMeshes(roomHolder.gameObject, true);
							BCWallRoofGenerator.GenerateWallsForFloor(buildingBp, floorIndex);
						}
//						else
//							Debug.Log("Room " + diff.RoomChangedIndex + " has not be destroyed");

//						BCWallRoofGenerator.GenerateWallsForFloor(buildingBp, floorIndex);
					}
				}
				else if(roomIndex > -1)
				{
					BCGenerator.CalculatePartyWalls(buildingBp);
					BCWallRoofGenerator.GenerateWallsForFloor(buildingBp, floorIndex);

					FloorHolder floorHolder = BCBlueprintUtils.FindRealFloor(buildingBp, floorIndex);
					RoomHolder roomHolder = BCBlueprintUtils.FindRealRoom(floorHolder, roomIndex);

					// If the room is found regenerate everything
					if(roomHolder != null)
					{
						BCGenerator.DestroyAllProceduralMeshes(roomHolder.gameObject, true);
						BCWallRoofGenerator.GenerateWallsForFloor(buildingBp, floorIndex);
					}

					if(floorIndex > 0)
					{	
						BCGenerator.DestroyOutsideWallForFloor(buildingBp, floorIndex - 1);
						BCWallRoofGenerator.GenerateWallsForFloor(buildingBp, floorIndex - 1);
					}
					BCGenerator.GenerateSpecificRoom(buildingBp, floorIndex, roomIndex); // ~ 3ms
				}
			}
			else if(diff.WindowsHaveChanged)
			{
				BCGenerator.CalculatePartyWalls(buildingBp);

				FloorBlueprint floorBp = buildingBp.Floors[diff.FloorChangedIndex];
				FloorHolder floorHolder = BCBlueprintUtils.FindRealFloor(buildingBp, diff.FloorChangedIndex);

				if(floorHolder != null)
				{	
					int floorIndex = diff.FloorChangedIndex;
					
					// If a window was destroyed, unfortunately we need to destroy everything
					if(diff.WindowDestroyed == true)
					{
						// Destroy the one in question
						WindowHolder windowHolder = BCGenerator.FindSpecificWindow(buildingBp, floorIndex, diff.WindowChangedIndex);

						if(windowHolder != null)
						{
							Vector3 fakeEnd = windowHolder.transform.position + windowHolder.transform.right * 0.25f;
							Vector3 fakeWindowStart = new Vector3(windowHolder.transform.position.x, 0, windowHolder.transform.position.z);
							Vector3 fakeWindowEnd = new Vector3(fakeEnd.x, 0, fakeEnd.z);

							BCGenerator.DestroySpecificWindow(buildingBp, floorIndex, diff.WindowChangedIndex);
							BCBlueprintUtils.RegenerateWallsAroundOpening(fakeWindowStart, fakeWindowEnd, buildingBp, diff.FloorChangedIndex);

							// Go through all the indexes and subtract them by one
							WindowHolder[] windowHolders = floorHolder.GetComponentsInChildren<WindowHolder>();
							if(windowHolders != null)
							{
								for(int i = 0; i < windowHolders.Length; i++)
								{
									if(windowHolders[i].Index >= diff.WindowChangedIndex)
									{
										windowHolders[i].Index--;
									}
								}
							}
						}
					}
					else
					{
						// The info for the changed window
						WindowInfo newWindow = floorBp.Windows[diff.WindowChangedIndex];

						// Destroy the window and then rebuild it
						if(diff.WindowChangedIndex > -1)
						{
							BCGenerator.DestroySpecificWindow(buildingBp, floorIndex, diff.WindowChangedIndex);
							BCGenerator.GenerateSpecificWindow(diff.WindowChangedIndex, floorIndex, buildingBp, floorHolder.transform);
						}

						// ===== Regenerate the Interior rooms around the walls
						BCBlueprintUtils.RegenerateWallsAroundOpening(newWindow.Start, newWindow.End, buildingBp, diff.FloorChangedIndex);
					}
				}
			}
			else if(diff.DoorsHaveChanged)
			{
				BCGenerator.CalculatePartyWalls(buildingBp);

				FloorBlueprint floorBp = buildingBp.Floors[diff.FloorChangedIndex];
				FloorHolder floorHolder = BCBlueprintUtils.FindRealFloor(buildingBp, diff.FloorChangedIndex);

				if(floorHolder != null)
				{
					int floorIndex = diff.FloorChangedIndex;

					// Destroy all the frames and regenerate them.
					// HACK - Remove once the door frames are integrated into the rooms
					DoorHolder[] doorFrames = floorHolder.GetComponentsInChildren<DoorHolder>();
					for(int i = 0; i < doorFrames.Length; i++)
					{
						if(doorFrames[i].Index == -2 && doorFrames[i] != null)
							BCGenerator.DestroyAllProceduralMeshes(doorFrames[i].gameObject, true);
					}

					if(diff.DoorDestroyed == false)
					{
						// The info for the changed door
						DoorInfo newDoor = floorBp.Doors[diff.DoorChangedIndex];
						
						// ===== Regenerate the Interior rooms around the walls
						BCBlueprintUtils.RegenerateWallsAroundOpening(newDoor.Start, newDoor.End, buildingBp, diff.FloorChangedIndex);

						if(diff.DoorChangedIndex > -1)
						{
							if(diff.NewDoorAdded == false)
							{
								BCGenerator.DestroySpecificDoor(buildingBp, floorIndex, diff.DoorChangedIndex);
								BCGenerator.GenerateSpecificDoor(diff.DoorChangedIndex, floorIndex, buildingBp, floorHolder.transform);
							}
							else
							{
								BCGenerator.GenerateSpecificDoor(diff.DoorChangedIndex, floorIndex, buildingBp, floorHolder.transform);
							}
						}
					}
					else
					{
						// Destroy the one in question
						DoorHolder doorHolder = BCGenerator.FindSpecificDoor(buildingBp, floorIndex, diff.DoorChangedIndex);

						if(doorHolder != null)
						{
							Vector3 fake = doorHolder.transform.position + doorHolder.transform.right * 0.25f;
							Vector3 fakeStart = new Vector3(doorHolder.transform.position.x, 0, doorHolder.transform.position.z);
							Vector3 fakeEnd = new Vector3(fake.x, 0, fake.z);
							
							BCGenerator.DestroySpecificDoor(buildingBp, floorIndex, diff.DoorChangedIndex);
							BCBlueprintUtils.RegenerateWallsAroundOpening(fakeStart, fakeEnd, buildingBp, diff.FloorChangedIndex);
							
							// Go through all the indexes and subtract them by one
							DoorHolder[] doorHolders = floorHolder.GetComponentsInChildren<DoorHolder>();
							if(doorHolders != null)
							{
								for(int i = 0; i < doorHolders.Length; i++)
								{
									if(doorHolders[i].Index >= diff.DoorChangedIndex)
									{
										doorHolders[i].Index--;
									}
								}
							}
						}
					}
				}
			}
			else if(diff.StairsHaveChanged)
			{
				// TODO: Make this more efficient in stairs update (1.0.0) so it doesn't regenerate two floors when added
				if(diff.NewStairAdded || diff.StairDestroyed)
				{
					int floorIndex = diff.FloorChangedIndex;
					int floorAbove = diff.FloorChangedIndex + 1;

					// Destroy all the rooms on the floor
					BCGenerator.DestroyAllRoomsOnFloor(buildingBp, floorIndex);
					if(floorAbove < buildingBp.Floors.Count)
					{
						BCGenerator.DestroyAllRoomsOnFloor(buildingBp, floorAbove);

						for(int i = 0; i < buildingBp.Floors[floorAbove].RoomBlueprints.Count; i++)
							BCGenerator.GenerateSpecificRoom(buildingBp, floorAbove, i);
					}

					for(int i = 0; i < buildingBp.Floors[floorIndex].RoomBlueprints.Count; i++)
						BCGenerator.GenerateSpecificRoom(buildingBp, floorIndex, i);

					FloorHolder floorHolder = BCBlueprintUtils.FindRealFloor(buildingBp, floorIndex);

					BCGenerator.DestroyAllStairs(buildingBp, floorIndex);
					BCGenerator.GenerateStairs(floorIndex, buildingBp, floorHolder.transform);
				}
			}

			if(diff.FancySidesHaveChanged)
			{
				BCGenerator.DestroyOutsideWalls(buildingBp);
				BCGenerator.GenerateBuilding(buildingBp, false, false, true, false, false);
			}

			HideFloorsNotInUse(buildingBp);
		}

		/// <summary>
		/// UNDO action can sometimes leave issues with destroyed rooms. This checks to see if there are problems with anything in the building and then regenerates it if there are
		/// </summary>
		public static void CheckForIncorrectlyGeneratedFloors (BuildingBlueprint buildingBp, BuildingDiff diff)
		{
			if(diff.HasFloorRemoved)
				return;

			// Check to see if the floor holders are all at zero height
			CheckForFlattendBuilding(buildingBp);


			if(ShouldRegenBuilding(buildingBp))
			{
				BCGenerator.GenerateFullBuilding(buildingBp);
				HideFloorsNotInUse(buildingBp);
			}
		}

		public static void CheckForFlattendBuilding(BuildingBlueprint buildingBp)
		{
			bool refreshFloorHeights = false;
			FloorHolder[] floorHolders = buildingBp.GetComponentsInChildren<FloorHolder>();
			for(int i = 1; i < floorHolders.Length; i++)
			{
				if(floorHolders[i].transform.localPosition.y == 0)
				{
					refreshFloorHeights = true;
					break;
				}
			}

			if(refreshFloorHeights == false)
				return;

			for(int i = 0; i < floorHolders.Length; i++)
			{
				Vector3 localPos = floorHolders[i].transform.localPosition;
				localPos.y = BCBlueprintUtils.FindFloorHeightRelativeToGround(buildingBp, i);
				floorHolders[i].transform.localPosition = localPos;
			}
		}

		static List<int> roomIndexRecorded = new List<int>();

		private static bool ShouldRegenBuilding(BuildingBlueprint buildingBp)
		{
			if(buildingBp.transform.childCount == 0)
				return false;

			// Find all the floor holders
			FloorHolder[] floorHolders = buildingBp.GetComponentsInChildren<FloorHolder>();
			for(int i = 0; i < floorHolders.Length; i++)
			{
				if(floorHolders[i].FloorIndex < 0 || floorHolders[i].FloorIndex >= buildingBp.Floors.Count)
					return true;
			}

			for(int floorIndex = 0; floorIndex < buildingBp.Floors.Count; floorIndex++)
			{
				FloorHolder floorHolder = BCBlueprintUtils.FindRealFloor(buildingBp, floorIndex);

				if(floorHolder == null)
					continue;

				RoomHolder[] roomHolders = floorHolder.GetComponentsInChildren<RoomHolder>();

				if(roomHolders.Length == 0)
					continue;

				if(roomHolders.Length > (buildingBp.Floors[floorIndex].RoomBlueprints.Count + 2))
					return true;

				roomIndexRecorded.Clear();

				for(int i = 0; i < roomHolders.Length; i++)
				{
					if(roomHolders[i].RoomIndex < 0 || roomHolders[i].RoomIndex >= buildingBp.Floors[floorIndex].RoomBlueprints.Count)
						return true;

					// Keeps track of if there are two of the same room and should it delete another one
					if(roomIndexRecorded.Contains(roomHolders[i].RoomIndex))
						return true;
					else
						roomIndexRecorded.Add(roomHolders[i].RoomIndex);
				}
			}

			return false;
		}
	}
}