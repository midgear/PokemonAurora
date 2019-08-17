using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{

	public partial class BuildingCrafterPanel : Editor
	{

		private List<Vector3[]> currentFloorFillInset = new List<Vector3[]>();
		private List<Vector3[]> currentFloorOutline = new List<Vector3[]>();
		
		private List<Vector3[]> currentYardFillInset = new List<Vector3[]>();
		private List<Vector3[]> currentYardOutline = new List<Vector3[]>();

	//	private List<Vector3[]> roofPointOverlay;
		
		private List<Vector3[]> roofBackbones;
		private List<Vector3[]> actualRoofBackbone;

		private Vector3[] highlightedOpening;

		public Vector3[] FloorOutline;

		/// <summary>
		/// Draws the floor from floor number. Note, Floor 0 is the ground floor
		/// </summary>
		/// <param name="floorNumber">Floor number.</param>
		private void DrawFloor(int floorNumber)
		{
			Vector3 floorHeight = (floorNumber * 3f) * Vector3.up + Script.BuildingBlueprint.BlueprintGroundHeight;
			
			DrawFloorOutlines(floorHeight);
			DrawYardOutlines(Script.BuildingBlueprint.BlueprintGroundHeight);
			DrawNewDoorOutlines(floorHeight);
			DrawNewWindowOutlines(floorHeight);
			DrawNewStairsOutlines(floorHeight);
			DrawNewRoomPlansBelow(floorHeight);
		}

		public void ClearFloorOutlines()
		{
			currentFloorOutline.Clear();
			currentFloorFillInset.Clear();
			Script.DoorDisplays.Clear();
			Script.WindowDisplays.Clear();
			Script.StairsDisplay.Clear();
			Script.RoomsBelowCurrentFloor.Clear();
		}

		void DrawFloorOutlines (Vector3 currentFloorHeight)
		{
			currentFloorOutline.Clear();
			currentFloorFillInset.Clear();
			
			if(Script.CurrentFloorBlueprint == null || Script.CurrentFloorBlueprint.RoomBlueprints == null || Script.CurrentFloorBlueprint.RoomBlueprints.Count < 1)
				return;
			
			for(int j = 0; j < Script.CurrentFloorBlueprint.RoomBlueprints.Count; j++)
			{
				
				// Creating the outline for each floor
				RoomBlueprint roomBp = Script.CurrentFloorBlueprint.RoomBlueprints[j];
				if(roomBp.PerimeterWalls == null)
					continue;
				
				Vector3[] wall = roomBp.PerimeterWalls.ToArray<Vector3>();
				
				for(int i = 0; i < wall.Length; i++)
					wall[i] += currentFloorHeight;
				
				currentFloorOutline.Add(wall);
				
				// Creating the fill in for each room
				MeshInfo meshInfo = BCMesh.GenerateGenericMeshInfo(wall);
				
				for(int i = 0; i < meshInfo.Triangles.Length - 1; i += 3)
				{
					int tri1 = meshInfo.Triangles[i];
					int tri2 = meshInfo.Triangles[i + 1];
					int tri3 = meshInfo.Triangles[i + 2];
					
					Vector3 p1 = meshInfo.Vertices[tri1];
					Vector3 p2 = meshInfo.Vertices[tri2];
					Vector3 p3 = meshInfo.Vertices[tri3];
					
					currentFloorFillInset.Add(new Vector3[3] { p1, p2, p3 });
				}
			}
		}
		
		void DrawYardOutlines (Vector3 currentFloorHeight)
		{
			currentYardOutline.Clear();
			currentYardFillInset.Clear();
			
			if(Script.CurrentFloorBlueprint == null || Script.CurrentFloorBlueprint.YardLayouts == null || Script.CurrentFloorBlueprint.YardLayouts.Count < 1)
				return;
			
			for(int j = 0; j < Script.CurrentFloorBlueprint.YardLayouts.Count; j++)
			{
				
				// Creating the outline for each floor
				YardLayout yardLayout = Script.CurrentFloorBlueprint.YardLayouts[j];
				if(yardLayout.PerimeterWalls == null)
					continue;
				
				Vector3[] wall = yardLayout.PerimeterWalls.ToArray<Vector3>();
				
				for(int i = 0; i < wall.Length; i++)
					wall[i] += currentFloorHeight;
				
				currentYardOutline.Add(wall);
				
				// Creating the fill in for each room
				MeshInfo meshInfo = BCMesh.GenerateGenericMeshInfo(wall);
				
				for(int i = 0; i < meshInfo.Triangles.Length - 1; i += 3)
				{
					int tri1 = meshInfo.Triangles[i];
					int tri2 = meshInfo.Triangles[i + 1];
					int tri3 = meshInfo.Triangles[i + 2];
					
					Vector3 p1 = meshInfo.Vertices[tri1];
					Vector3 p2 = meshInfo.Vertices[tri2];
					Vector3 p3 = meshInfo.Vertices[tri3];
					
					currentYardFillInset.Add(new Vector3[3] { p1, p2, p3 });
				}
			}
		}

		
		public void DrawNewDoorOutlines(Vector3 floorHeight)
		{
			List<Vector3[]> newDoorOutlines = new List<Vector3[]>();

			if(Script.CurrentFloorBlueprint == null)
				return;
			
			// Goes through each door and figures out which wall it collides with
			for (int doorIndex = 0; doorIndex < Script.CurrentFloorBlueprint.Doors.Count; doorIndex++) 
			{
				var door = Script.CurrentFloorBlueprint.Doors [doorIndex];
				bool doorInsideWall = false;
				for (int j = 0; j < Script.CurrentFloorBlueprint.RoomBlueprints.Count; j++) 
				{
					RoomBlueprint roomBp = Script.CurrentFloorBlueprint.RoomBlueprints [j];
					if (roomBp.PerimeterWalls == null)
						continue;
					// If it doesn't collide with a wall, then it goes on and tests the next step
					for (int n = 0; n < roomBp.PerimeterWalls.Count - 1; n++) 
					{
						int nextPoint = n + 1;
						if (nextPoint >= roomBp.PerimeterWalls.Count)
							nextPoint = n;
						Vector3 v1 = roomBp.PerimeterWalls [n];
						Vector3 v2 = roomBp.PerimeterWalls [nextPoint];
						if (BCUtils.TestBetweenTwoPoints ((door.Start + door.End) / 2, v1, v2) == true) 
						{
							Vector3[] outline = BCMesh.DoorOutline (door, BCUtils.FindInsetDirection (v1, v2, roomBp.PerimeterWalls.ToArray ()) * 0.1f);
							for (int i = 0; i < outline.Length; i++)
								outline [i] += floorHeight;
							newDoorOutlines.Add (outline);
							doorInsideWall = true;
							break;
						}
					}
				}
				if (doorInsideWall == false) {
					Vector3[] outline = BCMesh.DoorOutline (door, Vector3.zero);
					for (int i = 0; i < outline.Length; i++)
						outline [i] += floorHeight;
					newDoorOutlines.Add (outline);
				}
			}
			
			Script.DoorDisplays.Clear();
			Script.DoorDisplays = newDoorOutlines.ToList<Vector3[]>();
		}

		public void DrawNewWindowOutlines(Vector3 floorHeight)
		{
			List<Vector3[]> newWindowOutlines = new List<Vector3[]>();

			if(Script.CurrentFloorBlueprint == null)
				return;

			// Goes through each door and figures out which wall it collides with
			for (int windowIndex = 0; windowIndex < Script.CurrentFloorBlueprint.Windows.Count; windowIndex++) 
			{
				var window = Script.CurrentFloorBlueprint.Windows[windowIndex];

				bool windowInWall = false;

				for (int j = 0; j < Script.CurrentFloorBlueprint.RoomBlueprints.Count; j++) 
				{
					RoomBlueprint roomBp = Script.CurrentFloorBlueprint.RoomBlueprints[j];

					Vector3 windowMiddlePoint = (window.Start + window.End) / 2;

					if(roomBp.PerimeterWalls == null)
						continue;

					if(BCUtils.IsPointAlongAWall(windowMiddlePoint, roomBp.PerimeterWalls) == false)
						continue;

					// If it doesn't collide with a wall, then it goes on and tests the next step
					for (int n = 0; n < roomBp.PerimeterWalls.Count - 1; n++) 
					{
						int nextPoint = n + 1;
						if (nextPoint >= roomBp.PerimeterWalls.Count)
							nextPoint = n;
						Vector3 v1 = roomBp.PerimeterWalls [n];
						Vector3 v2 = roomBp.PerimeterWalls [nextPoint];

						if (BCUtils.TestBetweenTwoPoints ((window.Start + window.End) / 2, v1, v2) == true) 
						{
							Vector3[] outline = BCMesh.WindowOutline (window, BCUtils.FindInsetDirection (v1, v2, roomBp.PerimeterWalls.ToArray ()) * 0.1f);
							for (int i = 0; i < outline.Length; i++)
								outline [i] += floorHeight;
							newWindowOutlines.Add (outline);
							windowInWall = true;
							break;
						}
					}
				}
				if (windowInWall == false) 
				{
					Vector3[] outline = BCMesh.WindowOutline (window, Vector3.zero);
					for (int i = 0; i < outline.Length; i++)
						outline [i] += floorHeight;
					newWindowOutlines.Add (outline);
				}
			}

			Script.WindowDisplays.Clear();
			Script.WindowDisplays = newWindowOutlines;
		}
		
		public void DrawNewStairsOutlines(Vector3 floorHeight)
		{
			List<Vector3[]> newOutlines = new List<Vector3[]>();
			
			if(Script.CurrentFloorBlueprint == null)
				return;
			
			// Goes through each door and figures out which wall it collides with
			foreach(var stair in Script.CurrentFloorBlueprint.Stairs)
			{
				
				// Rounds to 4 meters 
				Vector3 stairsDirection = (stair.Start - stair.End).normalized;
				
				Vector3 rectOffset = Vector3.zero;
				
				if(stairsDirection.x < 0)
					rectOffset = Vector3.forward;
				if(stairsDirection.x > 0)
					rectOffset = Vector3.back;
				if(stairsDirection.z < 0)
					rectOffset = Vector3.left;
				if(stairsDirection.z > 0)
					rectOffset = Vector3.right;

				Vector3[] stairsOutline = BCUtils.Get3DStairsOutline(stair, rectOffset);

				for(int i = 0; i < stairsOutline.Length; i++)
					stairsOutline[i] += floorHeight;
				
				newOutlines.Add(stairsOutline);
			}
			
			if(Script.CurrentFloor > 0)
			{
				foreach(var stair in Script.BuildingBlueprint.Floors[Script.CurrentFloor - 1].Stairs)
				{
					Vector3[] stairsOutline = Get3DStairOutline(stair, (Script.CurrentFloor - 1) * 3f + Script.BuildingBlueprint.BlueprintGroundHeight.y);
					
					newOutlines.Add(stairsOutline);
				}
			}
			
			Script.StairsDisplay.Clear();
			Script.StairsDisplay = newOutlines.ToList<Vector3[]>();
		}

		
		Vector3[] Get3DStairOutline(StairInfo stair, float floorHeight)
		{
			Vector3 stairsDirection = (stair.Start - stair.End).normalized;
			
			Vector3 rectOffset = Vector3.zero;
			
			if(stairsDirection.x < 0)
				rectOffset = Vector3.forward;
			if(stairsDirection.x > 0)
				rectOffset = Vector3.back;
			if(stairsDirection.z < 0)
				rectOffset = Vector3.left;
			if(stairsDirection.z > 0)
				rectOffset = Vector3.right;
			
			Vector3[] stairsOutline = new Vector3[10]
			{
				stair.Start + rectOffset, stair.End + rectOffset + Vector3.up * 3, stair.End - rectOffset + Vector3.up * 3, stair.Start - rectOffset,
				stair.Start + rectOffset, stair.End + rectOffset, stair.End + rectOffset + Vector3.up * 3,  stair.End - rectOffset + Vector3.up * 3,
				stair.End - rectOffset, stair.Start - rectOffset,
			};
			
			for(int i = 0; i < stairsOutline.Length; i++)
				stairsOutline[i] += floorHeight * Vector3.up;
			
			return stairsOutline;
		}
		
		void DisplayRoofOutline()
		{
			Handles.color = Color.black;
			
			for(int i = 0; i < roofBacks.Count; i++)
				Handles.DrawAAPolyLine(4, roofBacks[i]);
			
			for(int i = 0; i < roofLefts.Count; i++)
				Handles.DrawAAPolyLine(4, roofLefts[i]);
			
			for(int i = 0; i < roofRights.Count; i++)
				Handles.DrawAAPolyLine(4, roofRights[i]);
			
			for(int i = 0; i < roofBacks.Count; i++)
				Handles.DrawAAPolyLine(4, roofBacks[i]);
			
			for(int i = 0; i < roofBases.Count; i++)
				Handles.DrawAAPolyLine(4, roofBases[i]);
		}

		
		
		/// <summary>
		/// Draws the floor below the current floor
		/// </summary>
		/// <param name="floorHeight">Floor height.</param>
		public void DrawNewRoomPlansBelow(Vector3 floorHeight)
		{
			if(Script.CurrentFloor == 0)
			{
				Script.RoomsBelowCurrentFloor.Clear();
				return;
			}
			
			List<Vector3[]> newOutlines = new List<Vector3[]>();
			
			// Change start floor to 0 to show entire floor
			//		int startFloor = 0;
			int startFloor = Script.CurrentFloor - 1;
			
			for(int i = startFloor; i < Script.CurrentFloor; i++)
			{
				Vector3 currentFloorHeight = floorHeight - Vector3.up * 3 * (Script.CurrentFloor - i);
				
				if(i >= Script.BuildingBlueprint.Floors.Count || i < 0)
					continue;
				
				for(int j = 0; j < Script.BuildingBlueprint.Floors[i].RoomBlueprints.Count; j++)
				{
					RoomBlueprint roomBp = Script.BuildingBlueprint.Floors[i].RoomBlueprints[j];
					
					List<Vector3> floorWallsOutline = new List<Vector3>();
					
					for(int n = 0; n < roomBp.PerimeterWalls.Count; n++)
					{
						floorWallsOutline.Add(roomBp.PerimeterWalls[n] + currentFloorHeight + Vector3.up * 2.99f);
						floorWallsOutline.Add(roomBp.PerimeterWalls[n] + currentFloorHeight);
						floorWallsOutline.Add(roomBp.PerimeterWalls[n] + currentFloorHeight + Vector3.up * 2.99f);
					}
					
					List<Vector3> floorOutline = new List<Vector3>();
					
					newOutlines.Add(floorWallsOutline.ToArray());
					newOutlines.Add(floorOutline.ToArray());
				}
			}
			
			Script.RoomsBelowCurrentFloor.Clear();
			Script.RoomsBelowCurrentFloor = newOutlines;
		}

		/// <summary>
		/// Draws gray outlines of every floor if there is no building generated
		/// </summary>
		public void DrawAllGreyFloorOutlines()
		{
			ClearFloorOutlines();
			if(Script.BuildingBlueprint.Transform.childCount < 1)
			{
				for(int i = 0; i < Script.BuildingBlueprint.Floors.Count; i++)
				{
					DrawGreyFloorOutline(i, false);
				}
			}
		}

		/// <summary>
		/// Draws a gray outline for the floor in question. Clear floors will wipe clean the current gray floors
		/// </summary>
		/// <param name="floor">Floor.</param>
		/// <param name="clearFloors">If set to <c>true</c> clear floors.</param>
		public void DrawGreyFloorOutline(int floor, bool clearFloors = true)
		{
			if(floor >= Script.BuildingBlueprint.Floors.Count)
				return;

			List<Vector3[]> newOutlines = new List<Vector3[]>();

			for(int j = 0; j < Script.BuildingBlueprint.Floors[floor].RoomBlueprints.Count; j++)
			{
				RoomBlueprint roomBp = Script.BuildingBlueprint.Floors[floor].RoomBlueprints[j];
				
				List<Vector3> floorWallsOutline = new List<Vector3>();

				Vector3 baseFloorHeight = Script.BuildingBlueprint.BlueprintGroundHeight + (Vector3.up * 3) * floor;

				for(int n = 0; n < roomBp.PerimeterWalls.Count; n++)
				{
					floorWallsOutline.Add(roomBp.PerimeterWalls[n] + baseFloorHeight + Vector3.up * 2.99f);
					floorWallsOutline.Add(roomBp.PerimeterWalls[n] + baseFloorHeight);
					floorWallsOutline.Add(roomBp.PerimeterWalls[n] + baseFloorHeight + Vector3.up * 2.99f);
				}
				
				List<Vector3> floorOutline = new List<Vector3>();
				
				newOutlines.Add(floorWallsOutline.ToArray());
				newOutlines.Add(floorOutline.ToArray());
			}

			if(clearFloors == true)
				Script.RoomsBelowCurrentFloor.Clear();
				
			Script.RoomsBelowCurrentFloor.AddRange(newOutlines);
		}
	}
}