using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{
	public class BCColliderBuilders
	{
		/// <summary>
		/// Finds if the room has been hit
		/// </summary>
		private static bool RaycastForRoomFloor(Vector3 testPoint, GameObject newRoom)
		{
			RaycastHit[] rayHits = Physics.RaycastAll(testPoint + Vector3.up, Vector3.down, 2);
			for(int i = 0; i < rayHits.Length; i++)
			{
				if(rayHits[i].collider.gameObject == newRoom)
				{
					return true;
				}
			}
			return false;
		}


		public static void GenerateFloorColliders (RoomBlueprint roomBp, GameObject newRoom, FloorBlueprint floorBelow = null)
		{
			// TODO: Generate using Plane Cast instead of Raycast against collider
			GenerateFloorColliders(roomBp.PerimeterWalls.ToArray<Vector3>(), newRoom, 0.1f, -1, floorBelow);
		}

		/// <summary>
		/// Generates the floor colliders at ZERO height
		/// </summary>
		/// <param name="outline">Outline.</param>
		/// <param name="colliderGameObject">Collider game object.</param>
		/// <param name="floorThickness">Floor thickness.</param>
		/// <param name="thicknessDirection">Either -1 or 1 to send the tickness in a certain direction</param>
		/// <param name="floorBelow">Floor below.</param>
		public static void GenerateFloorColliders (Vector3[] outline, 
		                                            GameObject colliderGameObject,
		                                            float floorThickness, 
		                                            int thicknessDirection = -1, 
		                                            FloorBlueprint floorBelow = null, 
		                                            Vector3[] interiorCutout = null)
		{
			if(colliderGameObject == null)
				return;
			
			if(floorThickness > 2)
				return;
			
			// Start off by creating a bounds at the zero point
			
			// We could just take the bounds and go across until we it an edge
			// Then we could go south until with hit an edge
			// Then Go to the next one, find if it is inside and already created bounds / collider. If not, start all over again
			// If so, skip
			
			// Take the first point, go left, test. If inside, expand bounds, if not do not
			if(outline == null || outline.Length < 1)
				return;
			
			Bounds roomFloorBounds = new Bounds(outline[0], Vector3.zero);
			for(int i = 0; i < outline.Length; i++)
			{
				roomFloorBounds.Encapsulate(outline[i]);
			}
			
			// Now roomFloorBounds has found a bounding box worth of the walls in question. Next we are going to test across all the bounds
			Vector3 startPoint = roomFloorBounds.center + roomFloorBounds.extents;
			
			// Find the width of the bounds
			int totalWidth = (int)(roomFloorBounds.extents.x * 2);
			int totalLength = (int)(roomFloorBounds.extents.z * 2);
			
			int breaker = 0;
			
			while(breaker < 50)
			{
				breaker ++;
				if(breaker == 50)
					Debug.Log("Breaker hit");
				// Take the start point off the bounds, and go across it to find all the points
				
				bool pointTestBreaker = false;
				Vector3 colliderStart = Vector3.zero;
				
				for(int z = 0; z < totalLength; z++)
				{
					for(int x = 0; x < totalWidth; x++)
					{
						Vector3 testPosition = startPoint - new Vector3(x + 0.5f, 0, z + 0.5f);
						
						// Test if this point is inside the square;
						if(BCUtils.PointInPolygonXZ(testPosition, outline) == true
						   && BCUtils.PointWithinFloorStairs(testPosition, floorBelow) == false)
						{
							// If the point is within the interior cutout, then this isn't the start position you are looking for
							if(interiorCutout != null && BCUtils.PointInPolygonXZ(testPosition, interiorCutout) == true)
							{
								continue;
							}
							
							
							// Do a raycast down. If a raycast is found, break out and do calculations for expanding points
							bool hitRoom = RaycastForRoomFloor(testPosition, colliderGameObject);
							
							if(hitRoom == false)
							{
								pointTestBreaker = true;
								colliderStart = startPoint - new Vector3(x, 0, z);
								break;
							}
						}
					}
					if(pointTestBreaker == true)
						break;
				}
				
				// If there are no empty holes to fill, break out of this while loop
				if(pointTestBreaker == false)
					break;
				
				// Now that we have a collider start, we need to scan the squares down to get the length of this collider
				
				int endLength = 0;
				int endWidth = 0;
				
				// Find the long length
				while(true)
				{
					Vector3 testPosition = colliderStart - new Vector3(0 + 0.5f, 0, endLength + 0.5f);
					if(BCUtils.PointInPolygonXZ(testPosition, outline) == false 
					   || BCUtils.PointWithinFloorStairs(testPosition, floorBelow) == true 
					   || RaycastForRoomFloor(testPosition, colliderGameObject)
					   || (interiorCutout != null && BCUtils.PointInPolygonXZ(testPosition, interiorCutout)))
						break;
					
					endLength++;
				}
				
				bool breakOut = false;
				
				// Now try to find the point of greatest rectangle
				while(true)
				{
					for(int z = 0; z < endLength; z++)
					{
						Vector3 testPosition = colliderStart - new Vector3(endWidth + 0.5f, 0, z + 0.5f);
						if(BCUtils.PointInPolygonXZ(testPosition, outline) == false 
						   || BCUtils.PointWithinFloorStairs(testPosition, floorBelow) == true 
						   || RaycastForRoomFloor(testPosition, colliderGameObject)
						   || (interiorCutout != null && BCUtils.PointInPolygonXZ(testPosition, interiorCutout)) == true)
						{
							breakOut = true;
							break;
						}
					}
					
					if(breakOut)
						break;
					
					endWidth++;
				}
				
				Bounds bound = new Bounds(colliderStart, Vector3.zero);
				bound.Encapsulate(colliderStart - new Vector3(endWidth, 0, endLength));
				
				BoxCollider collider = colliderGameObject.AddComponent<BoxCollider>();
				
				collider.size = new Vector3(bound.extents.x * 2, floorThickness, bound.extents.z * 2f);
				collider.center = new Vector3(bound.center.x, bound.center.y + (floorThickness / 2 * thicknessDirection), bound.center.z);
			}
		}
		
		/// <summary>
		/// Generates a Plane Collider without using Raycasts.
		/// </summary>
		/// <param name="outline">Outline.</param>
		/// <param name="colliderGameObject">Collider game object.</param>
		/// <param name="floorThickness">Floor thickness.</param>
		/// <param name="thicknessDirection">Thickness direction.</param>
		/// <param name="interiorCutout">Interior cutout.</param>
		public static void GeneratePlaneColliders(Vector3[] outline, List<Vector3[]> cutouts, GameObject colliderGameObject, Vector3 offset, float floorThickness, int thicknessDirection = 1)
		{
			if(floorThickness > 2)
				return;
			
			// Start off by creating a bounds at the zero point
			
			// We could just take the bounds and go across until we it an edge
			// Then we could go south until with hit an edge
			// Then Go to the next one, find if it is inside and already created bounds / collider. If not, start all over again
			// If so, skip
			
			// Take the first point, go left, test. If inside, expand bounds, if not do not
			if(outline == null || outline.Length < 1)
				return;
			
			Bounds roomFloorBounds = new Bounds(outline[0], Vector3.zero);
			for(int i = 0; i < outline.Length; i++)
			{
				roomFloorBounds.Encapsulate(outline[i]);
			}
			
			// Now roomFloorBounds has found a bounding box worth of the walls in question. Next we are going to test across all the bounds
			Vector3 startPoint = roomFloorBounds.center + roomFloorBounds.extents;
			
			// Find the width of the bounds
			int totalWidth = (int)(roomFloorBounds.extents.x * 2);
			int totalLength = (int)(roomFloorBounds.extents.z * 2);
			
			int[,] floorLayout = new int[totalWidth,totalLength];
			
			// 0 = empty
			// 1 = Area within the outline
			// 2 = Area taken by any cutout
			// 3 = Area taken up by a collider
			
			for(int z = 0; z < floorLayout.GetLength(1); z++)
			{
				for(int x = 0; x < floorLayout.GetLength(0); x++)
				{
					Vector3 testPosition = startPoint - new Vector3(x + 0.5f, 0, z + 0.5f);
					
					if(BCUtils.PointInPolygonXZ(testPosition, outline) == true)
						floorLayout[x, z] = 1;
					
					if(cutouts != null)
					{
						for(int i = 0; i < cutouts.Count; i++)
						{
							if(cutouts[i] == null)
								continue;

							if(BCUtils.PointInPolygonXZ(testPosition, cutouts[i]))
							{
								floorLayout[x, z] = 2;
								break;
							}
						}
					}
				}
			}
			
			int breaker = 0;
			
			while(breaker < 100)
			{
				breaker ++;
				if(breaker == 100)
					Debug.Log("Breaker hit");
				// Take the start point off the bounds, and go across it to find all the points
				
				bool pointTestBreaker = false;
				Vector3 colliderStart = Vector3.zero;
				int xStart = 0;
				int zStart = 0;
				
				
				for(int z = 0; z < totalLength; z++)
				{
					for(int x = 0; x < totalWidth; x++)
					{
						// Test if this point is inside the square;
						if(floorLayout[x, z] == 1)
						{
							pointTestBreaker = true;
							xStart = x;
							zStart = z;
							colliderStart = startPoint - new Vector3(xStart, 0, zStart);
							break;
						}
					}
					if(pointTestBreaker == true)
						break;
				}
				
				// If there are no empty holes to fill, break out of this while loop
				if(pointTestBreaker == false)
					break;
				
				// Now that we have a collider start, we need to scan the squares down to get the length of this collider
				
				int endLength = 0;
				int endWidth = 0;
				
				// Find the long length
				while(true)
				{
					// Break out if would deliver a bad point
					if(zStart + endLength > floorLayout.GetUpperBound(1))
						break;
					
					if(floorLayout[xStart, zStart + endLength] != 1)
						break;
					
					endLength++;
				}
				
				bool breakOut = false;
				
				// Now try to find the point of greatest rectangle
				while(true)
				{
					for(int z = 0; z < endLength; z++)
					{
						if(endWidth + xStart > floorLayout.GetUpperBound(0) || z + zStart > floorLayout.GetUpperBound(1))
						{
							breakOut = true;
							break;
						}
						
						if(floorLayout[xStart + endWidth, z + zStart] != 1)
						{
							breakOut = true;
							break;
						}
					}
					
					if(breakOut)
						break;
					
					endWidth++;
				}
				
				for(int l = 0; l < endLength; l++)
				{
					for(int w = 0; w < endWidth; w++)
					{
						floorLayout[w + xStart, l + zStart] = 3;
					}
				}
				
				Bounds bound = new Bounds(colliderStart, Vector3.zero);
				bound.Encapsulate(colliderStart - new Vector3(endWidth, 0, endLength));
				
				BoxCollider collider = colliderGameObject.AddComponent<BoxCollider>();
				
				collider.size = new Vector3(bound.extents.x * 2, floorThickness, bound.extents.z * 2f);
				collider.center = new Vector3(bound.center.x, bound.center.y + (floorThickness / 2 * thicknessDirection), bound.center.z) + offset;
			}
		}
		
		public static void GenerateUprightWallColliders(RoomBlueprint roomBp, FloorBlueprint floorBp, GameObject newRoom)
		{
			// NOTE: All floors are generated at ground level and then moved up upon generation
			
			Vector3[] walls = roomBp.PerimeterWalls.ToArray<Vector3>();
			
			List<DoorInfo> scratchDoors = new List<DoorInfo>();
			List<WindowInfo> scratchWindows = new List<WindowInfo>();
			
			// Go along the permeter walls, checking for doors and window and then generate colliders
			for(int i = 0; i < walls.Length - 1; i++)
			{
				scratchDoors.Clear();
				scratchWindows.Clear();
				
				int nextPoint = i + 1;
				int nextNextPoint = i + 2;
				if(nextPoint >= walls.Length)
					nextPoint = 0;
				if(nextNextPoint >= walls.Length)
					nextNextPoint = 1;
				
				Vector3 offset = BCUtils.FindInsetDirection(walls[i], walls[nextPoint], walls);
				
				// Find all the door locations and add them to the area
				// Since we are trying for clockwise, first do the roof and then come back and do the doors in descending order
				// Find all the doors in this wall place
				
				for(int n = 0; n < floorBp.Doors.Count; n++)
				{
					DoorInfo door = floorBp.Doors[n];
					
					if(BCUtils.TestBetweenTwoPoints(door.Start, walls[i], walls[nextPoint]) && BCUtils.TestBetweenTwoPoints(door.End, walls[i], walls[nextPoint]))
					{
						scratchDoors.Add(door);
					}
				}		
				
				for(int n = 0; n < floorBp.Windows.Count; n++)
				{
					WindowInfo window = floorBp.Windows[n];
					
					if(BCUtils.TestBetweenTwoPoints(window.Start, walls[i], walls[nextPoint]) && BCUtils.TestBetweenTwoPoints(window.End, walls[i], walls[nextPoint]))
					{
						scratchWindows.Add(window);
					}
				}
				
				// Now we have all the doors along this side, step through the length of the wall
				Vector3 wallDirection = (walls[nextPoint] - walls[i]).normalized;
				float wallLength = (walls[nextPoint] - walls[i]).magnitude;
				
				foreach(var door in scratchDoors)
				{
					Vector3[] doorOutline = BCMesh.DoorOutline(door, Vector3.zero);
					
					AddDoorBlocker(doorOutline, offset, newRoom);
				}
				foreach(var window in scratchWindows)
				{
					Vector3[] windowOutline = BCMesh.WindowOutline(window, Vector3.zero);
					
					// From the window outline, we can grab all the info we need
					AddWindowBlocker(windowOutline, offset, newRoom);
				}
				
				// Started 
				Vector3 startOfWall = walls[i];
				Vector3 currentStartPoint = startOfWall;
				
				bool lastSpaceOpen = false;
				bool changedHappened = false; // This is to prevent full wall length doors from having a collider placed in front of this wall
				
				for(float f = 0; f <= wallLength; f += 0.05f)
				{
					f = (float)System.Math.Round(f, 2); // To prevent issues with floating points
					
					RaycastHit[] rayHit = Physics.RaycastAll(startOfWall + wallDirection * f + Vector3.up * 4, Vector3.down, 5);
					bool spaceOpen = true;
					
					for(int n = 0; n < rayHit.Length; n++)
					{
						if(rayHit[n].collider.gameObject == newRoom)
						{
							spaceOpen = false;
						}
					}
					
					if(spaceOpen != lastSpaceOpen)
					{
						changedHappened = true;
						
						if(spaceOpen == false)
						{
							AddUprightCollider(currentStartPoint, startOfWall + wallDirection * f, offset, newRoom);
							lastSpaceOpen = false;
						}
						else if(spaceOpen == true)
						{
							currentStartPoint = startOfWall + wallDirection * (f - 0.05f);
							lastSpaceOpen = true;
						}
					}
					
					// This deals with the last collider along the path
					if(changedHappened && (Mathf.Approximately(f, wallLength)) || f >= wallLength) // Using Approx here because of floating point issues
					{
						
						if(i < walls.Length - 2) // The very last section of a wall would cause a double section at the end. Removed it
							AddUprightCollider(currentStartPoint, startOfWall + wallDirection * f, offset, newRoom);
						
					}
				}
			}
		}
		
		public static void AddUprightCollider(Vector3 startPoint, Vector3 endPoint, Vector3 offset, GameObject newRoom)
		{
			if(newRoom == null)
				return;

			Bounds bound = new Bounds(startPoint, Vector3.zero);
			bound.Encapsulate(startPoint + Vector3.up * 3);
			bound.Encapsulate(endPoint);
			
			if(startPoint == endPoint) // Any zero length colliders are not created
				return;
			
			BoxCollider collider = newRoom.AddComponent<BoxCollider>();
			collider.size = new Vector3(bound.extents.x * 2, 3f, bound.extents.z * 2f) + offset * 0.2f;
			collider.center = new Vector3(bound.center.x, bound.center.y, bound.center.z);
		}

		private static void AddDoorBlocker(Vector3[] doorOutline, Vector3 offset, GameObject newRoom)
		{
			if(newRoom == null)
				return;

			{ // Generates the upper collider
				BoxCollider collider = newRoom.AddComponent<BoxCollider>();
				Bounds bound = new Bounds(doorOutline[1], Vector3.zero);
				bound.Encapsulate(new Vector3(doorOutline[2].x, newRoom.transform.position.y + 3, doorOutline[2].z));
				
				collider.size = new Vector3(bound.extents.x * 2, bound.extents.y * 2f, bound.extents.z * 2f) + offset * 0.2f;
				collider.center = new Vector3(bound.center.x, bound.center.y, bound.center.z);
			}
		}
		
		private static void AddWindowBlocker(Vector3[] outline, Vector3 offset, GameObject newRoom)
		{
			if(newRoom == null)
				return;

			{ // Generates the lower collider
				BoxCollider collider = newRoom.AddComponent<BoxCollider>();
				Bounds bound = new Bounds(new Vector3(outline[0].x, newRoom.transform.position.y, outline[0].z), Vector3.zero);
				bound.Encapsulate(outline[1]);
				
				collider.size = new Vector3(bound.extents.x * 2, bound.extents.y * 2f, bound.extents.z * 2f) + offset * 0.2f;
				collider.center = new Vector3(bound.center.x, bound.center.y, bound.center.z);
			}
			{ // Generates the upper collider
				BoxCollider collider = newRoom.AddComponent<BoxCollider>();
				Bounds bound = new Bounds(outline[3], Vector3.zero);
				bound.Encapsulate(new Vector3(outline[2].x, newRoom.transform.position.y + 3, outline[2].z));
				
				collider.size = new Vector3(bound.extents.x * 2, bound.extents.y * 2f, bound.extents.z * 2f) + offset * 0.2f;
				collider.center = new Vector3(bound.center.x, bound.center.y, bound.center.z);
			}
		}

		public static void GenerateRoofColliders(GameObject roofGameObj, BuildingBlueprint buildingBp, bool generateTopRoof)
		{
			for(int floor = 0; floor < buildingBp.Floors.Count; floor++)
			{
				if(generateTopRoof == false && floor == buildingBp.Floors.Count - 1)
					continue;

				Vector3[] roof = BCMesh.GenerateOutlineFloor(buildingBp.Floors[floor]);
				Vector3[] roofAbove = null;

				if(floor < buildingBp.Floors.Count - 1)
					roofAbove = BCMesh.GenerateOutlineFloor(buildingBp.Floors[floor + 1]);

				List<Vector3[]> difference =  BCMesh.GenerateDifferentVectors(roof, roofAbove);

				if(difference == null)
					return;

				for(int i = 0; i < difference.Count; i++)
				{
					GeneratePlaneColliders(difference[i], new List<Vector3[]>() { roofAbove }, roofGameObj, -buildingBp.BlueprintXZCenter + Vector3.up * 3 * (floor + 1), 0.5f, 1);
				}
			}
		}
	}
}