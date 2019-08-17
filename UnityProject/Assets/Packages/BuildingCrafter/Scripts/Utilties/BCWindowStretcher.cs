using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

namespace BuildingCrafter
{
	public static class BCWindowStretcher 
	{
		public static GameObject GenerateStretchedWindows(BCWindow bcWindow, Vector3 startOfWindowGroup, Vector3 endOfWindowGroup, float height, bool addDestroyer = false)
		{
			// Make sure that the window section is on the same plane
			if(startOfWindowGroup.y != endOfWindowGroup.y)
				endOfWindowGroup = new Vector3(endOfWindowGroup.x, startOfWindowGroup.y, endOfWindowGroup.z);

			Vector3 directionOfWindowGroup = (endOfWindowGroup - startOfWindowGroup).normalized;
			float totalWidth = (endOfWindowGroup - startOfWindowGroup).magnitude;

			// Test if the window can't fit in here
			if(TestForWindowHeightFit(bcWindow, height) == false || TestForWindowWidthFit(bcWindow, totalWidth) == false)
				return null;

			// Will always generate the window group along the X+ axis and then rotate it at the end
			Vector3 start = startOfWindowGroup;
			Vector3 end = startOfWindowGroup + totalWidth * Vector3.right;

			if(bcWindow.MaxWidth >= totalWidth)
				return RotateWindowGroupAndAddCollider(GenerateStretchedWindow(bcWindow, start, totalWidth, height, addDestroyer), directionOfWindowGroup);

			Vector3 middlePoint = (end + start) / 2;
			Vector3 direction = (end - start).normalized;

			// Test if a max window is generated will the remaining space be able to take a window
			// Do ALL the calculations first before trying to generate the strips. Will increase the speed of the windows
			// 1. Test to see if middle window can have windows beside it
			Vector3 middleLeftStartPosition = middlePoint - direction * (bcWindow.MaxWidth / 2);
			float firstSectionSize = (start - middleLeftStartPosition).magnitude;

			// If the window is ALMOST the same size as the max window width, 
			// then over stretch it a bit to make it fit.
			float smallestMiddleSpace = 0.1f; // The size that the interior of a window can have before it is crushed too much

			if(firstSectionSize < smallestMiddleSpace)
				return RotateWindowGroupAndAddCollider(GenerateStretchedWindow(bcWindow, start, totalWidth, height, addDestroyer), directionOfWindowGroup);

			if(GetSmallestWindowWidth(bcWindow, 0.1f) > bcWindow.MaxWidth) // TestForWindowWidthFit(bcWindow, totalWidth / 3) == false ||
			{
				Debug.LogError("WARNING: " + bcWindow.name + " has a smaller max width than the smallest generation it can produce. Fix this in the BCWindow panel or it won't render ever");
				return null;
			}

			GameObject multipleWindowHolder = BCMesh.GenerateEmptyGameObject("Window Holder");
			multipleWindowHolder.name = "Compound Window Holder";
			if(addDestroyer)
				multipleWindowHolder.AddComponent<DestroyDis>();

			multipleWindowHolder.transform.position = start;

			// If the remaining space is too small to fit a window, add a window of the smallest size possible plu 0.1. Find that width
			// If 3 won't fit in the space, instead throw two stretched ones
			if(TestForWindowWidthFit(bcWindow, firstSectionSize) == false)
			{
				float smallestWindowWidth = GetSmallestWindowWidth(bcWindow, smallestMiddleSpace);
				float middleWidth = totalWidth - smallestWindowWidth * 2;

				GameObject smallLeftWindow = null;
				GameObject middleWindow = null;
				GameObject smallRightWindow = null;

				if(middleWidth < smallestWindowWidth)
				{
					smallLeftWindow = GenerateStretchedWindow(bcWindow, start, totalWidth / 2, height, addDestroyer);
					smallRightWindow = GenerateStretchedWindow(bcWindow, start + direction * totalWidth / 2, totalWidth / 2, height, addDestroyer);
				}
				else
				{
					smallLeftWindow = GenerateStretchedWindow(bcWindow, start, smallestWindowWidth, height, addDestroyer);
					middleWindow = GenerateStretchedWindow(bcWindow, start + smallestWindowWidth * direction, totalWidth - smallestWindowWidth * 2, height, addDestroyer);
					smallRightWindow = GenerateStretchedWindow(bcWindow, end - smallestWindowWidth * direction, smallestWindowWidth, height, addDestroyer);
				}

				if(smallLeftWindow == null || smallRightWindow == null)
				{
#if UNITY_EDITOR
					UnityEditor.Undo.DestroyObjectImmediate(multipleWindowHolder);
#else
					GameObject.Destroy(multipleWindowHolder);
#endif
					return null;
				}

				smallLeftWindow.transform.SetParent(multipleWindowHolder.transform);

				if(middleWindow != null)
					middleWindow.transform.SetParent(multipleWindowHolder.transform);

				smallRightWindow.transform.SetParent(multipleWindowHolder.transform);

				return RotateWindowGroupAndAddCollider(multipleWindowHolder, directionOfWindowGroup);
			}

			Vector3 middlePosition = middlePoint - direction * (bcWindow.MaxWidth / 2);
			GameObject centerWindow = BCWindowStretcher.GenerateStretchedWindow(bcWindow, middlePosition, bcWindow.MaxWidth, height, addDestroyer);
			if(centerWindow == null)
			{
				Debug.LogError("WARNING: " + bcWindow.name + " has a smaller max width than the smallest generation it can produce. Fix this in the BCWindow panel or it won't render ever");
				return null;
			}

			centerWindow.transform.SetParent(multipleWindowHolder.transform);
		
			Vector3 endStartPoint = middlePosition;
			float distanceOffsetFromMiddle = bcWindow.MaxWidth;

			int breaker = 0;
			while(breaker < 100)
			{
				float remainingDistance = (endStartPoint - start).magnitude;
				float remainingNextDistance = remainingDistance - bcWindow.MaxWidth;

				if(remainingDistance >= bcWindow.MaxWidth && TestForWindowWidthFit(bcWindow, remainingNextDistance) == true)
				{
					// Add the window to the left of this current one
					GameObject leftWindow = GenerateStretchedWindow(bcWindow, middleLeftStartPosition - distanceOffsetFromMiddle * direction, bcWindow.MaxWidth, height, addDestroyer);
					leftWindow.transform.SetParent(multipleWindowHolder.transform);
	
					// Add the window to the right of the current one
					GameObject rightWindow = GenerateStretchedWindow(bcWindow, middleLeftStartPosition + distanceOffsetFromMiddle * direction, bcWindow.MaxWidth, height, addDestroyer);
					rightWindow.transform.SetParent(multipleWindowHolder.transform);

					endStartPoint -= direction * bcWindow.MaxWidth;
					distanceOffsetFromMiddle += bcWindow.MaxWidth;
				}
				else
				{
					GameObject leftWindow = GenerateStretchedWindow(bcWindow, start, remainingDistance, height, addDestroyer);
					leftWindow.transform.SetParent(multipleWindowHolder.transform);
					GameObject rightWindow = GenerateStretchedWindow(bcWindow, middleLeftStartPosition + distanceOffsetFromMiddle * direction, remainingDistance, height, addDestroyer);
					rightWindow.transform.SetParent(multipleWindowHolder.transform);

					break;
				}

				if(breaker == 100)
					Debug.LogError("A breaker was hit on the window stretcher, check it out, this shouldn't ever show");

				breaker++;
			}

			// Rotate the window holder to the direction of the generation
			return RotateWindowGroupAndAddCollider(multipleWindowHolder, directionOfWindowGroup);
		}

		private static GameObject RotateWindowGroupAndAddCollider(GameObject windowObject, Vector3 direction)
		{
			if(windowObject == null)
				return null;

			// Add a box collider

			BoxCollider boxCollider = windowObject.AddComponent<BoxCollider>();
			MeshRenderer[] meshRenderer = windowObject.GetComponentsInChildren<MeshRenderer>();
			if(meshRenderer.Length > 0)
			{
				Bounds newBounds = meshRenderer[0].bounds;
				for(int index = 1; index < meshRenderer.Length; index++)
				{
					newBounds.Encapsulate(meshRenderer[index].bounds);
				}
				
				boxCollider.size = newBounds.size;
				boxCollider.center = newBounds.center - windowObject.transform.position;
			}

			direction = new Vector3(direction.x, 0, direction.z);

			windowObject.transform.LookAt(direction + windowObject.transform.position);
			windowObject.transform.Rotate(Vector3.up, -90f);

			return windowObject;
		}

		private static GameObject GenerateStretchedWindow(BCWindow bcWindow, Vector3 position, float newWidth, float newHeight, bool addDestroyer = false)
		{
			return GenerateStretchedWindow(bcWindow, position, newWidth, newHeight, Vector3.right, addDestroyer);
		}

		private static Vector3 defaultWindowDirection = new Vector3(1, 0, 0);

		private static GameObject GenerateStretchedWindow(BCWindow bcWindow, Vector3 position, float newWidth, float newHeight, Vector3 direction, bool addDestroyer = false)
		{
			MeshFilter[] meshFilters = bcWindow.GetComponentsInChildren<MeshFilter>(bcWindow.gameObject);

			GameObject windowParent = BCMesh.GenerateEmptyGameObject("Create Stretched Window", true);
			windowParent.name = "New Window Parent";
			if(addDestroyer)
				windowParent.AddComponent<DestroyDis>();

			List<Mesh> boundedMeshes = new List<Mesh>();

			for(int i = 0; i < meshFilters.Length; i++)
			{
				// Find original lower bounds
				Transform localTrans = meshFilters[i].transform;
				Quaternion forwardRotation = bcWindow.transform.rotation;
				Vector3 offsetFromParent = Vector3.zero;

				if(bcWindow.gameObject != meshFilters[i].gameObject)
				{
					forwardRotation = meshFilters[i].transform.rotation;
				}

				// Create this mesh in the correct forward facing space
				Mesh rotatedCorrectlyMesh = DuplicateMesh(meshFilters[i].sharedMesh);

				if(rotatedCorrectlyMesh == null)
					continue;

				Vector3[] vertices = rotatedCorrectlyMesh.vertices;

				if(localTrans.lossyScale.x < 0 || localTrans.lossyScale.y < 0 || localTrans.lossyScale.z < 0)
					Debug.LogError("On mesh " + meshFilters[i].name + " the scale is negative, this will cause major issues. If you think this should be a feature, email me at buildingcrafter@8bitgoose.com");

				offsetFromParent = meshFilters[i].transform.position;

				Matrix4x4 matrix = Matrix4x4.TRS(offsetFromParent, forwardRotation, meshFilters[i].transform.lossyScale);
				int index = 0;
				while (index < vertices.Length) 
				{
					Vector3 point = matrix.MultiplyPoint3x4(vertices[index]);
					vertices[index] = point;
					index++;
				}

				rotatedCorrectlyMesh.vertices = vertices;
				rotatedCorrectlyMesh.RecalculateBounds();
				rotatedCorrectlyMesh.name = bcWindow.name + "_boundedMesh";

				boundedMeshes.Add(rotatedCorrectlyMesh);
			}

			MoveBoundedMeshesToPositive(ref boundedMeshes);

			// If there is no complex component, just add the window to the parent
			if(boundedMeshes.Count == 1)
			{
				Mesh stretchedMesh = StretchMesh(boundedMeshes[0], bcWindow, newWidth, newHeight);
				
				if(stretchedMesh != null)
				{
					stretchedMesh.RecalculateNormals();
					
					MeshRenderer meshRenderer = windowParent.AddComponent<MeshRenderer>();
					meshRenderer.materials = meshFilters[0].gameObject.GetComponent<MeshRenderer>().sharedMaterials;
					
					MeshFilter meshFilter = windowParent.AddComponent<MeshFilter>();
					meshFilter.sharedMesh = stretchedMesh;
					
					windowParent.name = meshFilters[0].name + "_stretched";
					windowParent.AddComponent<ProceduralGameObject>();
				}
			}
			else
			{
				for(int i = 0; i < boundedMeshes.Count; i++)
				{
					Mesh stretchedMesh = StretchMesh(boundedMeshes[i], bcWindow, newWidth, newHeight);

					if(stretchedMesh == null)
						continue;

					stretchedMesh.RecalculateNormals();

					GameObject newGameObject = BCMesh.GenerateEmptyGameObject("Create Stretch Mesh Window", true);

					MeshRenderer meshRenderer = newGameObject.AddComponent<MeshRenderer>();
					meshRenderer.materials = meshFilters[i].gameObject.GetComponent<MeshRenderer>().sharedMaterials;

					MeshFilter meshFilter = newGameObject.AddComponent<MeshFilter>();
					meshFilter.sharedMesh = stretchedMesh;

					newGameObject.name = meshFilters[i].name + "_stretched";
					newGameObject.transform.SetParent(windowParent.transform);
				}
			}

			// TODO: possibly combine all the meshes except for the breakable glass

			// If this item can't generate, destroy it
			if(windowParent.transform.childCount == 0 && windowParent.GetComponent<MeshFilter>() == null)
			{
#if UNITY_EDITOR
				UnityEditor.Undo.DestroyObjectImmediate(windowParent);
#else
				GameObject.Destroy(windowParent);
#endif
				return null;
			}

			if(boundedMeshes.Count > 1)
				windowParent.name = "Complex_" + bcWindow.name + "_stretched";

			windowParent.transform.position = position;

			if(direction != defaultWindowDirection)
			{
				windowParent.transform.LookAt(direction + windowParent.transform.position);
				windowParent.transform.Rotate(Vector3.up, -90f);
			}

			// Destroy all the bounded meshes - TODO reintroduce this
			for(int i = 0; i < boundedMeshes.Count; i++)
			{
#if UNITY_EDITOR
				UnityEditor.Undo.DestroyObjectImmediate(boundedMeshes[i]);
#else
				GameObject.Destroy(boundedMeshes[i]);
#endif
			}

			return windowParent;
		}

		private static Vector3 MoveMeshToPositive(Mesh mesh)
		{
			Vector3 firstVector = mesh.vertices.First();
			
			float xMin = firstVector.x;
			float yMin = firstVector.y;
			float zMin = firstVector.z;

			Vector3[] vertices = mesh.vertices;
				
			for(int i = 0; i < vertices.Length; i++)
			{
				xMin = Mathf.Min(xMin, vertices[i].x);
				yMin = Mathf.Min(yMin, vertices[i].y);
				zMin = Mathf.Min(zMin, vertices[i].z);
			}

			Vector3 offsetFromZero = new Vector3(xMin, yMin, zMin);

			for(int i = 0; i < vertices.Length; i++)
			{
				vertices[i] -= offsetFromZero;
			}
			
			mesh.vertices = vertices;
			mesh.RecalculateBounds();

			return offsetFromZero;
		}

		static void MoveBoundedMeshByAmount (Mesh mesh, Vector3 offsetFromParent)
		{
			Vector3[] vertices = mesh.vertices;

			for(int i = 0; i < vertices.Length; i++)
			{
				vertices[i] += offsetFromParent;
			}

			mesh.vertices = vertices;
			mesh.RecalculateBounds();
		}

		private static void MoveBoundedMeshesToPositive(ref List<Mesh> boundedMeshes)
		{
			if(boundedMeshes.Count < 1)
				return;
			
			// Find the mininimum point for ALL the meshes
			Vector3 firstVector = boundedMeshes.First().vertices.First();

			float xMin = firstVector.x;
			float yMin = firstVector.y;
			float zMin = firstVector.z;

			for(int index = 0; index < boundedMeshes.Count; index++)
			{
				Vector3[] vertices = boundedMeshes[index].vertices;

				for(int i = 0; i < vertices.Length; i++)
				{
					xMin = Mathf.Min(xMin, vertices[i].x);
					yMin = Mathf.Min(yMin, vertices[i].y);
					zMin = Mathf.Min(zMin, vertices[i].z);
				}
			}

			Vector3 offsetFromZero = new Vector3(xMin, yMin, zMin);

			for(int index = 0; index < boundedMeshes.Count; index++)
			{
				Vector3[] vertices = boundedMeshes[index].vertices;
				
				for(int i = 0; i < vertices.Length; i++)
				{
					vertices[i] -= offsetFromZero;
				}

				boundedMeshes[index].vertices = vertices;
				boundedMeshes[index].RecalculateBounds();
			}
		}


		private static Bounds GetFullMeshBounds(List<Mesh> meshes)
		{
			CombineInstance[] combine = new CombineInstance[meshes.Count];
			for (int i = 0; i < meshes.Count; i++) 
			{
				combine[i].mesh = meshes[i];
			}
			
			Mesh meshCombined = new Mesh();
			meshCombined.CombineMeshes(combine);
			meshCombined.RecalculateBounds();
			
			Bounds bounds = meshCombined.bounds;
			
			return bounds;
		}

		private static Mesh StretchMesh(Mesh mesh, BCWindow bcWindow, float newWidth, float newHeight)
		{
			Mesh stretchedMesh = DuplicateMesh(mesh, "_stretched");

			stretchedMesh.name = mesh.name + "_stretched";

			Vector3[] vertices = stretchedMesh.vertices.ToArray<Vector3>();

			if(TestForWindowWidthFit(bcWindow, newWidth) == false)
				return null;

			if(TestForWindowHeightFit(bcWindow, newHeight) == false)
				return null;

			float interiorWidth = newWidth - bcWindow.LeftCut - (bcWindow.bounds.size.x - bcWindow.RightCut);
			float interiorHeight = newHeight - bcWindow.BottomCut - (bcWindow.bounds.size.y - bcWindow.TopCut);

				for(int i = 0; i < vertices.Length; i++)
			{
				float xPoint = vertices[i].x;
				float yPoint = vertices[i].y;
				float zPoint = vertices[i].z;

				// Deals with a stretch point along the x point
				if(xPoint > bcWindow.LeftCut && xPoint < bcWindow.RightCut)
				{
					float xDistanceBetweenCuts = (xPoint - bcWindow.LeftCut) / (bcWindow.RightCut - bcWindow.LeftCut);
					xPoint = xDistanceBetweenCuts * interiorWidth + bcWindow.LeftCut;
				}
				else if(xPoint >= bcWindow.RightCut)
				{
					float distanceToEnd = bcWindow.bounds.size.x - xPoint;
					xPoint = newWidth - distanceToEnd;
				}

				if(yPoint > bcWindow.BottomCut && yPoint < bcWindow.TopCut)
				{
					float yDistanceBetweenCuts = (yPoint - bcWindow.BottomCut) / (bcWindow.TopCut - bcWindow.BottomCut);
					yPoint = yDistanceBetweenCuts * interiorHeight + bcWindow.BottomCut;
				}
				else if(yPoint >= bcWindow.TopCut)
				{
					float distanceToEnd = bcWindow.bounds.size.y - yPoint;
					yPoint = newHeight - distanceToEnd;
				}

				// Offsets the mesh so the center of the window is at the center of the bounds
				zPoint = vertices[i].z - bcWindow.bounds.extents.z;

				vertices[i] = new Vector3(xPoint, yPoint, zPoint);
			}

			stretchedMesh.vertices = vertices;
			stretchedMesh.RecalculateBounds();

			return stretchedMesh;
		}

		public static bool TestForWindowWidthFit(BCWindow bcWindow, float newWidth)
		{
			if(bcWindow == null)
				return false;

			float rightCutFromBounds = bcWindow.bounds.size.x - bcWindow.RightCut;
			float leftCut = bcWindow.LeftCut;
			
			float scaledRightCut = newWidth - rightCutFromBounds;

			if(scaledRightCut < leftCut)
				return false;

			return true;
		}

		public static bool TestForWindowHeightFit(BCWindow bcWindow, float newHeight)
		{
			if(bcWindow == null)
				return false;

			float topCutFromBounds = bcWindow.bounds.size.y - bcWindow.TopCut; // THIS MAY CAUSE AN ISSUE IF THE BOUNDS ARE SET UP SCREWY IN BC WINDOW
			float bottomCut = bcWindow.BottomCut;
			
			float scaledTopCut = newHeight - topCutFromBounds;
			
			if(scaledTopCut < bottomCut)
				return false;

			return true;
		}

		public static float GetSmallestWindowWidth(BCWindow bcWindow, float middleSpace = 0.1f)
		{
			if(middleSpace < 0)
				middleSpace = 0;

			float rightCutFromBounds = bcWindow.bounds.size.x - bcWindow.RightCut;
			float leftCut = bcWindow.LeftCut;

			return leftCut + rightCutFromBounds + middleSpace;
		}

		public static float GetSmallestWindowHeight(BCWindow bcWindow, float middleSpace = 0.1f)
		{
			if(middleSpace < 0)
				middleSpace = 0;

			float topCutFromBounds = bcWindow.bounds.size.y - bcWindow.TopCut; // THIS MAY CAUSE AN ISSUE IF THE BOUNDS ARE SET UP SCREWY IN BC WINDOW
			float bottomCut = bcWindow.BottomCut;
			
			return topCutFromBounds + bottomCut + middleSpace;
		}

		private static Mesh DuplicateMesh(Mesh mesh, string meshExtensionName = "")
		{
			if(mesh == null)
				return null;

			Mesh newMesh = new Mesh();
			newMesh.vertices = mesh.vertices;
			newMesh.triangles = mesh.triangles;
			newMesh.uv = mesh.uv;
			newMesh.uv2 = mesh.uv2;
			newMesh.uv3 = mesh.uv3;
			newMesh.uv4 = mesh.uv4;

			newMesh.normals = mesh.normals;
			newMesh.colors = mesh.colors;
			newMesh.tangents = mesh.tangents;

			newMesh.name += meshExtensionName;

			newMesh.subMeshCount = mesh.subMeshCount;
			for(int i = 0; i < mesh.subMeshCount; i++)
				newMesh.SetTriangles(mesh.GetTriangles(i), i);
			
			return newMesh;
		}
	}
}




















