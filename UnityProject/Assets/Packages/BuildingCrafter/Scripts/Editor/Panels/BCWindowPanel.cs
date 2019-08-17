using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

namespace BuildingCrafter
{
	[CustomEditor(typeof(BCWindow))]
	public class BCWindowPanel : Editor 
	{
		private BCWindow bcWindow;
		private bool noValidMesh;

		private static Material lineMaterial;

		public bool HasTransformChanged
		{
			get
			{
				if(bcWindow == null)
					return false;

				if(cachedTranform == null)
					cachedTranform = bcWindow.transform;

				if(lastPosition != cachedTranform.position)
				{
					lastPosition = cachedTranform.position;
					return true;
				}
				if(lastRotation != cachedTranform.localRotation)
				{
					lastRotation = cachedTranform.localRotation;
					return true;
				}
				if(lastScale != cachedTranform.localScale)
				{
					lastScale = cachedTranform.localScale;
					return true;
				}

				return false;
			}
		}
		private Vector3 lastPosition;
		private Vector3 lastScale;
		private Quaternion lastRotation;
		private Transform cachedTranform;

		// Displaying the 3D grids
		private static Mesh displayMesh = null;
		private Bounds displayBounds = new Bounds();
		private static Mesh displayMeshTopDown = null;
		private Bounds displayBoundsTopDown = new Bounds();

		// For selection the GUI in the meshes
		private bool selectedTopCut = false;
		private bool selectedBottomCut = false;
		private bool selectedRightCut = false;
		private bool selectedLeftCut = false;

		private bool selectedWallOffset = false;
		private bool selectedFrameCentre = false;

		// Default settings for showing a window off in real time
		private BuildingStyle testBuildingStyle;
		private bool alwaysGenerateWindow = false;
		private WindowTypeEnum testWindowType = WindowTypeEnum.Standard;
		private float testWidth = 1.0f;

		// Tests to see if anything has changed
		WindowTypeEnum lastWindowType = WindowTypeEnum.Standard;
		float lastTestWidth = 0.5f;
		float lastWidowCenter = 0;
		float lastFrameOffset = 0;
		float lastLeftCut = 0;
		float lastRightCut = 0;
		float lastTopCut = 0;
		float lastBottomCut = 0;
		float lastMaxFrameWidth = 0;
		bool lastGenerateFrame = true;

		// Window content informatino
		GUIContent wallLabel 				= new GUIContent("Wall Offset", "Where the window should be offset compared to the wall from a topdown view");
		GUIContent frameOffsetLabel			= new GUIContent("Frame Center Offset", "Where each side of the frame will end");
		GUIContent liveTestWall 			= new GUIContent("Show Live Test", "Shows a live version of the window so you can modify it.");

		private static void CreateLineMaterial()
		{
			if (!lineMaterial)
				lineMaterial = Resources.Load("FlatLineMat") as Material;
		}

		void OnEnable()
		{
			bcWindow = (BCWindow)target; 

			if(bcWindow == null)
				return;

			BCTestWindow.DestroyAllTestWindows();

			// Create the meshes for the viewing area
			CreateRenderableMesh(bcWindow, Vector3.forward, out displayMesh, out displayBounds);
			CreateRenderableMesh(bcWindow, Vector3.down, out displayMeshTopDown, out displayBoundsTopDown);

			if(displayMesh == null || displayMeshTopDown == null)
				noValidMesh = true;

			bcWindow.bounds = displayBounds;

			// Loads materials to make the wall tester prettier 
			string[] genericStyle = AssetDatabase.FindAssets("WindowTestingStyle");
			if(genericStyle.Length > 0)
			{
				testBuildingStyle = AssetDatabase.LoadAssetAtPath<BuildingStyle>(AssetDatabase.GUIDToAssetPath(genericStyle[0]));
			}
			else
				Debug.LogError("A style titled WindowTestingStyle was not found, please create one so that you can test the windows in the editor");

		}

		void OnDisable()
		{
			// Makes sure that the mesh created here is destroyed on the world
			DestroyDisplayMeshes();

			Resources.UnloadUnusedAssets();

			if(this.alwaysGenerateWindow)
				BCTest.DestroyAllTextBoxesAndMeshes();
		}

		/// <summary>
		/// Called to ensure that the scene does not leak meshes
		/// </summary>
		public static void DestroyDisplayMeshes()
		{
			// NOTE: These meshes should never be undo destroyed. Will cause a crash.
			GameObject.DestroyImmediate(displayMesh);
			GameObject.DestroyImmediate(displayMeshTopDown);
		}

		public override void OnInspectorGUI ()
		{
			if(displayMesh == null || displayMeshTopDown == null)
			{
				OnDisable();
				OnEnable();
			}

			if(noValidMesh)
			{
				EditorGUILayout.LabelField("The window you've added the BCWindow Component to has no meshes inside it. " +
					"Please ensure you have valid meshes in the window. You can not have any null meshes in the MeshFilter component.", EditorStyles.helpBox);
				base.OnInspectorGUI();
				return;
			}

			if(HasTransformChanged)
			{
				OnDisable();
				OnEnable();
			}

			EditorGUILayout.Separator();

			EditorGUILayout.LabelField("The object must be rotated so the front faces the z-forward axis (blue arrow must point away from front of the window). " +
				"You can find this by clicking on 2D in the scene view. All windows must be rotated this way to work.",
			                           EditorStyles.helpBox);

			DisplayBCWindowEditor(displayMesh, bcWindow, displayBounds, bcWindow.gameObject);

			EditorGUILayout.Separator();

			// Draw the center of the window offset
			{
				float margin = 5f;
				float topDownScale = 200f;
				float maxHeight = 200f;
				Rect topdownRectWall;
				SetupRectFor3DObjectWireFrame(wallLabel, displayBoundsTopDown, maxHeight, margin, out topdownRectWall, out topDownScale);
				
				// Draw the background
				DisplayWireFrameBackground(topdownRectWall, Color.black);
				
				// Draw the dragable area
				GUIContent wallThicknessLabel = new GUIContent("Wall Position", "The wall dimentions as seen from above.");
				// Figure out the size of the quad
				
				bcWindow.CenterOfWindow = DrawDragableQuad(bcWindow.CenterOfWindow, 0.2f, topdownRectWall, displayBoundsTopDown, topDownScale, false, ref selectedWallOffset, wallThicknessLabel);
				
				// Draw the mesh on top of it
				Display3DObjectWireFrame(displayMeshTopDown, displayBoundsTopDown, topdownRectWall, topDownScale, margin, false);
			}

			// Draw the center of the frame offset which only shows if you can generate a frame
			if(bcWindow.GenerateFrame == true)
			{
				float margin = 5f;
				float topDownScale = 200f;
				float maxHeight = 200f;
				Rect topdownRectWall;
				SetupRectFor3DObjectWireFrame(frameOffsetLabel, displayBoundsTopDown, maxHeight, margin, out topdownRectWall, out topDownScale);

				DisplayWireFrameBackground(topdownRectWall, Color.black);
				DrawNonDragableQuad(bcWindow.CenterOfWindow, new Color(0.3f, .3f, .3f), 0.2f, topdownRectWall, margin, topDownScale);

				// Draw the mesh on top of it
				Display3DObjectWireFrame(displayMeshTopDown, displayBoundsTopDown, topdownRectWall, topDownScale, margin, false);

				GUIContent wallThicknessLabel = new GUIContent("Frame Offset", "Where the frame changes materials from inside to out");
				bcWindow.FrameChangePosition = DrawDragableLine(bcWindow.FrameChangePosition, topdownRectWall, margin, displayBoundsTopDown, topDownScale, false, ref selectedFrameCentre, wallThicknessLabel);
			}

			EditorGUILayout.Separator();
			EditorGUILayout.Separator();

			if(GUILayout.Button("Generate Test Stretch Windows"))
			{
				this.alwaysGenerateWindow = false;
				BCTestWindow.DestroyAllTestWindows();
				GenerateAllWallHeights(WindowTestWallType.AllWindowTypes);
			}

			bool testToGen = EditorGUILayout.Toggle(liveTestWall, alwaysGenerateWindow);
			bool updateAnyways = false;

			if(testToGen != alwaysGenerateWindow)
			{
				Undo.RegisterCompleteObjectUndo(this, "Change Always Generate Test Window");
				alwaysGenerateWindow = testToGen;
				updateAnyways = true;
			}

			if(alwaysGenerateWindow)
			{
				this.testWindowType = (WindowTypeEnum)EditorGUILayout.EnumPopup("Type of Window", this.testWindowType);
				this.testWidth = EditorGUILayout.Slider(new GUIContent("Width of Test Building", "Set the width of how wide the test building is."), this.testWidth, 0.5f, 50);
				this.testWidth = Mathf.Round(this.testWidth * 2) / 2f;

				if(updateAnyways || HasWindowChanged || this.lastWindowType != this.testWindowType || this.testWidth != this.lastTestWidth)
				{
					BCTestWindow.DestroyAllTestWindows();
					GenerateAllWallHeights(WindowTestWallType.SingleWindow, testWindowType, testWidth);
				}

				this.lastWindowType = testWindowType;
				this.lastTestWidth = testWidth;
			}

			EditorGUILayout.LabelField("BC Window Values", EditorStyles.helpBox);

			base.OnInspectorGUI ();

			if(Event.current.type == EventType.Repaint)
				EditorUtility.SetDirty(bcWindow);
		}

		private void OnSceneGUI()
		{
			// TODO - Move Grid Display code to here
		}

		private bool HasWindowChanged
		{
			get
			{
				if(lastWidowCenter != bcWindow.CenterOfWindow
				   || lastFrameOffset != bcWindow.FrameChangePosition
				   || lastLeftCut != bcWindow.LeftCut
				   || lastRightCut != bcWindow.RightCut
				   || lastTopCut != bcWindow.TopCut
				   || lastBottomCut != bcWindow.BottomCut
				   || lastMaxFrameWidth != bcWindow.MaxWidth
				   || lastGenerateFrame != bcWindow.GenerateFrame)
				{
					lastWidowCenter = bcWindow.CenterOfWindow;
					lastFrameOffset = bcWindow.FrameChangePosition;
					lastLeftCut = bcWindow.LeftCut;
					lastRightCut = bcWindow.RightCut;
					lastTopCut = bcWindow.TopCut;
					lastBottomCut = bcWindow.BottomCut;
					lastMaxFrameWidth = bcWindow.MaxWidth;
					lastGenerateFrame = bcWindow.GenerateFrame;
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Creates the mesh that needs to be rendered. Should be used on enabled
		/// </summary>
		/// <returns>The renderable mesh.</returns>
		/// <param name="gameObject">Game object.</param>
		/// <param name="viewAngle">View angle.</param>
		private void CreateRenderableMesh(BCWindow bcWindow, Vector3 directionOfViewing, out Mesh finalMesh, out Bounds bounds)
		{
			// First create a 2D version from the front of the shared mesh
			MeshFilter[] meshFilters = bcWindow.GetComponentsInChildren<MeshFilter>(true);

			for(int i = 0; i < meshFilters.Length; i++)
			{
				if(meshFilters[i].sharedMesh == null)
				{
					finalMesh = null;
					bounds = new Bounds();
					return;
				}
			}
			
			List<CombineInstance> combine = new List<CombineInstance>();
			List<Mesh> simpleMeshesToDestroy = new List<Mesh>();

			for (int i = 0; i < meshFilters.Length; i++) 
			{
				Vector3[] verts = meshFilters[i].sharedMesh.vertices;
				List<int> allTriangles = new List<int>();

				for(int j = 0; j < meshFilters[i].sharedMesh.subMeshCount; j++)
					allTriangles.AddRange(meshFilters[i].sharedMesh.GetTriangles(j));

				Mesh simpleMesh = new Mesh();
				simpleMesh.name = "simple_mesh_combine";
				simpleMesh.vertices = verts;
				simpleMesh.triangles = allTriangles.ToArray<int>();

				CombineInstance instance = new CombineInstance();
				
				instance.mesh = simpleMesh;
				instance.transform = meshFilters[i].transform.localToWorldMatrix;
				combine.Add(instance);

				// Ensure no memory leak
				simpleMeshesToDestroy.Add(simpleMesh);
			}

			Mesh meshCombined = new Mesh();
			meshCombined.name = bcWindow + "_display_mesh";
			meshCombined.CombineMeshes(combine.ToArray<CombineInstance>(), true, true);

			Bounds totalBounds = meshCombined.bounds;
			Vector3[] vertices = meshCombined.vertices.ToArray<Vector3>();

			// Now we need to rotate the entire mesh to the angle we want to look at. This may be hard
			Vector3 center = bcWindow.gameObject.transform.position;//any V3 you want as the pivot point.
			Quaternion newRotation = Quaternion.identity;

			// Figures out what direction this should be viewing from
			if(directionOfViewing == Vector3.forward)
				newRotation = Quaternion.identity;
			else if(directionOfViewing == Vector3.back)
				newRotation = Quaternion.Euler(0, 180, 0);
			else if(directionOfViewing == Vector3.right)
				newRotation = Quaternion.Euler(0, 90, 0);
			else if(directionOfViewing == Vector3.left)
				newRotation = Quaternion.Euler(0, 270, 0);
			else if(directionOfViewing == Vector3.down)
				newRotation = Quaternion.Euler(270, 0, 0);
			else if(directionOfViewing == Vector3.up)
				newRotation = Quaternion.Euler(90, 0, 0);
			else
				Debug.LogError("The Vector " + directionOfViewing + " is not on one of the 6 axises");

			// Rotates the verticies around the center point to get the right view angle
			for(int i = 0; i < vertices.Length; i++) 
				vertices[i] = newRotation * (vertices[i] - center) + center;

			meshCombined.vertices = vertices;
			meshCombined.RecalculateBounds();
			totalBounds = meshCombined.bounds;
			vertices = meshCombined.vertices;

			// Offset the entire mesh to be centered in the positive and offset correctly
			for(int i = 0; i < vertices.Length; i++)
			{
				vertices[i] -= totalBounds.center;
				vertices[i] += totalBounds.extents;
			}

			meshCombined.vertices = vertices;
			meshCombined.RecalculateBounds();

			// Sets the display mesh 
			finalMesh = meshCombined;
			bounds = meshCombined.bounds;

			// Destroy all the meshes generated while crafting this thing
			for(int i = 0; i < simpleMeshesToDestroy.Count; i++)
				DestroyImmediate(simpleMeshesToDestroy[i]);
		}

		private Rect DisplayBCWindowEditor(Mesh mesh, BCWindow bcWindow, Bounds bounds, GameObject gameObject)
		{
			float margin = 5;
			float maxHeight = 200f;

			float scale = maxHeight / bounds.size.y;

			if(scale > maxHeight)
				scale = maxHeight / bounds.size.x;

			// Sets up the top left corner for the array
			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.PrefixLabel(" ");

			Rect vert = EditorGUILayout.BeginVertical(GUILayout.Height(20));
			GUI.Label(new Rect(14f, vert.y, 100f, 24f), "Forward View");

			float verticalOffset = vert.y + margin * 2;
			float horizontalOffset = vert.x + margin * 3;

			Color bgColor = Color.black;

			float width = bounds.size.x * scale;
			float height = bounds.size.y * scale;

			if(vert.size.x < width + margin * 3 && vert.size.x > 0)
			{
				float shrinkBy = vert.size.x / (width + margin * 3);
				
				scale *= shrinkBy * 0.95f;
				
				width = bounds.size.x * scale;
				height = bounds.size.y * scale;
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginVertical();
			GUILayout.Space(height);
			EditorGUILayout.EndVertical();

			// Creates a Unity style box to display the offsets
			Rect rect = new Rect(horizontalOffset, verticalOffset, width, height);

			if(mesh == null)
				return rect; 
					
			// ======== GRAPHICS START ==============
			CreateLineMaterial();
			GL.PushMatrix();
			lineMaterial.SetPass(0); // Sets what point this is drawn
			GL.LoadPixelMatrix();

			GL.Begin(GL.QUADS);
			GL.Color(bgColor);
			
			GL.Vertex3(rect.xMin - margin, rect.yMin - margin, 0); // Top left point
			GL.Vertex3(rect.xMax + margin, rect.yMin - margin, 0); // Top right point
			GL.Vertex3(rect.xMax + margin, rect.yMax + margin, 0); // Bottom Right Point
			GL.Vertex3(rect.xMin - margin, rect.yMax + margin, 0); // Bottom Left Point
			GL.End();

			GL.Begin(GL.LINES);
			GL.Color(Color.gray);

			Vector3 panelOffset = Vector3.zero;

			float xOffset = 0;
			float yOffset = 0 + bounds.size.y * scale;
			xOffset += panelOffset.x * scale;
			yOffset -= panelOffset.z * scale; // Offsets by a meter so it renders right
			//
			// HUGE GC problem if you don't pull the vertices at the start
			Vector3[] vertices = mesh.vertices.ToArray<Vector3>();
			int[] triangles = mesh.triangles.ToArray<int>();

			for(int i = 0; i < triangles.Length; i += 3)
			{
				Vector3 p1 = vertices[triangles[i]];
				Vector3 p2 = vertices[triangles[i + 1]];
				Vector3 p3 = vertices[triangles[i + 2]];
				
				Vector2 l1 = new Vector2(p1.x * 1, p1.y * -1) * scale + new Vector2(rect.xMin + xOffset, rect.yMin + yOffset);
				Vector2 l2 = new Vector2(p2.x * 1, p2.y * -1) * scale + new Vector2(rect.xMin + xOffset, rect.yMin + yOffset);
				Vector2 l3 = new Vector2(p3.x * 1, p3.y * -1) * scale + new Vector2(rect.xMin + xOffset, rect.yMin + yOffset);
				
				GL.Vertex3(l1.x, l1.y, 0);
				GL.Vertex3(l2.x, l2.y, 0);
				GL.Vertex3(l2.x, l2.y, 0);
				GL.Vertex3(l3.x, l3.y, 0);
				GL.Vertex3(l3.x, l3.y, 0);
				GL.Vertex3(l1.x, l1.y, 0);
			}
			GL.End();

			// Now draw the control points for the drag points

			bool leftHovering = DrawHighlightableLine(new Vector2(bcWindow.LeftCut * scale + rect.xMin, rect.yMin), new Vector2(bcWindow.LeftCut * scale + rect.xMin, rect.yMax), Color.yellow);
			bool rightHovering = DrawHighlightableLine(new Vector2(bcWindow.RightCut * scale + rect.xMin, rect.yMin), new Vector2(bcWindow.RightCut * scale + rect.xMin, rect.yMax), Color.yellow);
			bool topHovering = DrawHighlightableLine(new Vector2(rect.xMin, rect.yMax - bcWindow.TopCut * scale), new Vector2(rect.xMax, rect.yMax - bcWindow.TopCut * scale), Color.yellow);
			bool bottomHovering = DrawHighlightableLine(new Vector2(rect.xMin, rect.yMax - bcWindow.BottomCut * scale), new Vector2(rect.xMax, rect.yMax - bcWindow.BottomCut * scale), Color.yellow);

			if(Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
			{
				if(leftHovering)
					selectedLeftCut = true;
				else if(rightHovering)
					selectedRightCut = true;
				if(topHovering)
					selectedTopCut = true;
				else if(bottomHovering)
					selectedBottomCut = true;

				if(selectedLeftCut || selectedRightCut || selectedTopCut || selectedBottomCut)
				{
					Undo.RegisterCompleteObjectUndo(bcWindow, "Undo Move Cuts");
				}

				Event.current.Use();
			}

			if(Event.current.type == EventType.MouseDrag && Event.current.button == 0)
			{
				if(selectedLeftCut)
					bcWindow.LeftCut = (Event.current.mousePosition.x - rect.xMin) / scale;
				else if(selectedRightCut)
					bcWindow.RightCut = (Event.current.mousePosition.x - rect.xMin) / scale;
				if(selectedTopCut)
					bcWindow.TopCut = (rect.yMax - Event.current.mousePosition.y) / scale;
				else if(selectedBottomCut)
					bcWindow.BottomCut = (rect.yMax - Event.current.mousePosition.y) / scale;
			}

			if(Event.current.button == 0 && Event.current.type == EventType.MouseUp)
			{
				selectedLeftCut = false;
				selectedRightCut = false;
				selectedTopCut = false;
				selectedBottomCut = false;
			}

			bcWindow.LeftCut = Mathf.Clamp(bcWindow.LeftCut, 0, bounds.size.x);
			bcWindow.LeftCut = (float)System.Math.Round(bcWindow.LeftCut, 3);

			bcWindow.RightCut = Mathf.Clamp(bcWindow.RightCut, 0, bounds.size.x);
			bcWindow.RightCut = (float)System.Math.Round(bcWindow.RightCut, 3);

			bcWindow.TopCut = Mathf.Clamp(bcWindow.TopCut,0, bounds.size.y);
			bcWindow.TopCut = (float)System.Math.Round(bcWindow.TopCut, 3);

			bcWindow.BottomCut = Mathf.Clamp(bcWindow.BottomCut, 0, bounds.size.y);
			bcWindow.BottomCut = (float)System.Math.Round(bcWindow.BottomCut, 3);

			if(bcWindow.LeftCut > bcWindow.RightCut)
			{
				float oldLeft = bcWindow.LeftCut;
				float oldRight = bcWindow.RightCut;

				bcWindow.LeftCut = oldRight;
				bcWindow.RightCut = oldLeft;
			}

			if(bcWindow.BottomCut > bcWindow.TopCut)
			{
				float oldTop = bcWindow.TopCut;
				float oldBottom = bcWindow.BottomCut;
				
				bcWindow.TopCut = oldBottom;
				bcWindow.BottomCut = oldTop;
			}

			GL.End();
			GL.PopMatrix();

			return rect;
		}

		void SetupRectFor3DObjectWireFrame(GUIContent label, Bounds bounds, float maxHeight, float margin, out Rect rect, out float scale)
		{
			scale = maxHeight / bounds.size.y;

			if(scale > maxHeight)
			{
				scale = maxHeight / bounds.size.x;
				maxHeight = scale * bounds.size.y;
			}

			if(maxHeight < scale * 0.2f)
				maxHeight = scale * 0.2f;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(label);
			Rect vert = EditorGUILayout.BeginVertical(GUILayout.Height(maxHeight + margin * 3));
			float verticalOffset = vert.y + margin * 2;
			float horizontalOffset = vert.x + margin * 2;
			GUILayout.Space(1);
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();

			// First figure out the scale of the window
			float width = bounds.size.x * scale + margin * 2;
			float height = bounds.size.y * scale + margin * 2;

			if(vert.size.x < width + 20 && vert.size.x > 0)
			{
				float shrinkBy = vert.size.x / (width + 20f);

				scale *= shrinkBy;

				width = bounds.size.x * scale + margin * 2;
				height = bounds.size.y * scale + margin * 2;
			}

			// Ensures the space is always big enough to display the wall section
			if(height < maxHeight)
				height = maxHeight + margin * 2;

			// Creates a Unity style box to display the offsets
			rect = new Rect(horizontalOffset, verticalOffset, width, height);
		}

		void Display3DObjectWireFrame(Mesh mesh, Bounds bounds, Rect rect, float scale, float margin, bool displayBackground = true)
		{
			if(mesh == null)
				return;

			Color bgColor = Color.black;

			// ======== GRAPHICS START ==============
			CreateLineMaterial();
			GL.PushMatrix();
			lineMaterial.SetPass(0); // Sets what point this is drawn
			GL.LoadPixelMatrix();

			if(displayBackground)
			{
				GL.Begin(GL.QUADS);
				GL.Color(bgColor);
				
				GL.Vertex3(rect.xMin, rect.yMin, 0); // Top left point
				GL.Vertex3(rect.xMax, rect.yMin, 0); // Top right point
				GL.Vertex3(rect.xMax, rect.yMax, 0); // Bottom Right Point
				GL.Vertex3(rect.xMin, rect.yMax, 0); // Bottom Left Point
				GL.End();
			}

			GL.Begin(GL.LINES);
			GL.Color(Color.gray);
			
			Vector3 panelOffset = Vector3.zero;
			
			float xOffset = 0;
			float yOffset = 0 + rect.size.y / 2 + bounds.size.y * scale / 2; // Offsets the centre enough 
			xOffset += panelOffset.x * scale + margin;
			yOffset -= panelOffset.z * scale; // Offsets by a meter so it renders right

			// HUGE GC problem if you don't pull the vertices at the start
			Vector3[] vertices = mesh.vertices.ToArray<Vector3>();
			int[] triangles = mesh.triangles.ToArray<int>();
			
			for(int i = 0; i < triangles.Length; i += 3)
			{
				Vector3 p1 = vertices[triangles[i]];
				Vector3 p2 = vertices[triangles[i + 1]];
				Vector3 p3 = vertices[triangles[i + 2]];
				
				Vector2 l1 = new Vector2(p1.x * 1, p1.y * -1) * scale + new Vector2(rect.xMin + xOffset, rect.yMin + yOffset);
				Vector2 l2 = new Vector2(p2.x * 1, p2.y * -1) * scale + new Vector2(rect.xMin + xOffset, rect.yMin + yOffset);
				Vector2 l3 = new Vector2(p3.x * 1, p3.y * -1) * scale + new Vector2(rect.xMin + xOffset, rect.yMin + yOffset);

				GL.Vertex3(l1.x, l1.y, 0);
				GL.Vertex3(l2.x, l2.y, 0);
				GL.Vertex3(l2.x, l2.y, 0);
				GL.Vertex3(l3.x, l3.y, 0);
				GL.Vertex3(l3.x, l3.y, 0);
				GL.Vertex3(l1.x, l1.y, 0);
			}
			GL.End();
			GL.PopMatrix();
			// Now draw the control points for the drag points
		}

		void DisplayWireFrameBackground(Rect rect, Color bgColor)
		{
			CreateLineMaterial();
			GL.PushMatrix();
			lineMaterial.SetPass(0); // Sets what point this is drawn
			GL.LoadPixelMatrix();

			GL.Begin(GL.QUADS);
			GL.Color(bgColor);
			
			GL.Vertex3(rect.xMin, rect.yMin, 0); // Top left point
			GL.Vertex3(rect.xMax, rect.yMin, 0); // Top right point
			GL.Vertex3(rect.xMax, rect.yMax, 0); // Bottom Right Point
			GL.Vertex3(rect.xMin, rect.yMax, 0); // Bottom Left Point
			GL.End();
			GL.PopMatrix();
		}

		#region Drawing Lines
		
		public float DrawDragableLine(float currentValue, Rect rect, float margin, Bounds bounds, float scale, bool isYDirection, ref bool selected, GUIContent xLineLabel = null)
		{
			bool isHovering = false;
			
			if(isYDirection == false)// && currentValue > 0 && currentValue  < bounds.size.x) // Ensures that if the line is beyond the bounds it is not shown
			{
				float yPos = (rect.yMin + rect.yMax) / 2 - currentValue * scale;
				isHovering = DrawHighlightableLine(new Vector2(rect.xMin, yPos), new Vector2(rect.xMax, yPos), Color.yellow);
			}
			else
			{
				float xPos = (rect.xMin + rect.xMax) / 2 + currentValue * scale;
				isHovering = DrawHighlightableLine(new Vector2(xPos, rect.yMin), new Vector2(xPos, rect.yMax), Color.yellow);
			}

			// Displays an explanation of the x axis line
			if(isYDirection == false && xLineLabel != null)
			{
				float labelHeight = 14;
				Rect explanationPosition = new Rect(rect.xMax + margin, (rect.yMin + rect.yMax) / 2 - currentValue * scale - labelHeight / 2, 100f, labelHeight);
				explanationPosition.y = Mathf.Clamp(explanationPosition.y, rect.yMin, rect.yMax - labelHeight);
				EditorGUI.LabelField(explanationPosition, xLineLabel);
			}
			
			if(Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
			{
				if(isHovering)
					selected = true;
				
				if(selected)
				{
					Undo.RegisterCompleteObjectUndo(bcWindow, "Undo Move Line");
				}
				
				Event.current.Use();
			}
			
			if(Event.current.type == EventType.MouseDrag && Event.current.button == 0)
			{
				if(selected)
				{
					if(isYDirection == false)
						currentValue = ((rect.yMin + rect.yMax) / 2 - Event.current.mousePosition.y) / scale;
					else
						currentValue = (Event.current.mousePosition.x - rect.xMin) / scale;
				}
			}
			
			if(Event.current.button == 0 && Event.current.type == EventType.MouseUp)
				selected = false;
			
			return currentValue;
		}
		
		
		
		public bool DrawHighlightableLine(Vector2 startPointInPanelSpace, Vector2 endPointInPanelSpace, Color lineColor)
		{
			bool isHovering = false;
			
			CreateLineMaterial();
			GL.PushMatrix();
			lineMaterial.SetPass(0); // Sets what point this is drawn
			GL.LoadPixelMatrix();
			GL.Begin(GL.LINES);
			GL.Color(Color.cyan);
			
			if(BCUtils.IsPointCloseToLine(Event.current.mousePosition, startPointInPanelSpace, endPointInPanelSpace, 10f))
			{
				GL.Color(Color.green);
				isHovering = true;
			}
			
			GL.Vertex3(startPointInPanelSpace.x, startPointInPanelSpace.y, 0);
			GL.Vertex3(endPointInPanelSpace.x, endPointInPanelSpace.y, 0);
			
			GL.End();
			GL.PopMatrix();
			
			return isHovering;
		}
		
		#endregion

		#region Drawing Dragable Quads

		public void DrawNonDragableQuad(float currentValue, Color color, float quadThickness, Rect rect, float margin, float scale)
		{
			Rect drawableQuad = new Rect(new Vector2(rect.xMin, (rect.yMin + rect.yMax) / 2 - currentValue * scale - quadThickness * scale / 2), new Vector2(rect.size.x, quadThickness * scale));
			DrawHighlightableQuad(drawableQuad, color, rect, false);
		}

		public float DrawDragableQuad(float currentValue, float quadThickness, Rect rect, Bounds bounds, float scale, bool isYDirection, ref bool quadSelected, GUIContent xLineLabel = null)
		{
			bool isHovering = false;

			if(isYDirection == false)// && currentValue > 0 && currentValue  < bounds.size.x) // Ensures that if the line is beyond the bounds it is not shown
			{
				Rect drawableQuad = new Rect(new Vector2(rect.xMin, (rect.yMin + rect.yMax) / 2 - currentValue * scale - quadThickness * scale / 2), new Vector2(rect.size.x, quadThickness * scale));
				// TODO - Wall should not be shown outside of its own rect
				isHovering = DrawHighlightableQuad(drawableQuad, Color.cyan, rect);
			}
			else
				Debug.Log("Need to implement the X quads here");

			// Displays an explanation of the x axis line
			if(isYDirection == false && xLineLabel != null)
			{
				float labelHeight = 14;
				Rect explanationPosition = new Rect(rect.xMax + 5f, (rect.yMin + rect.yMax) / 2 - currentValue * scale - quadThickness * scale / 2 + labelHeight, 100f, labelHeight);
				explanationPosition.y = Mathf.Clamp(explanationPosition.y, rect.yMin, rect.yMax - labelHeight);
				EditorGUI.LabelField(explanationPosition, xLineLabel);
			}

			if(Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
			{
				if(isHovering)
					quadSelected = true;

				if(quadSelected)
				{
					Undo.RegisterCompleteObjectUndo(bcWindow, "Undo Move Line");
				}

				Event.current.Use();
			}

			if(Event.current.type == EventType.MouseDrag && Event.current.button == 0)
			{
				if(quadSelected)
				{
					if(isYDirection == false)
						currentValue = ((rect.yMin + rect.yMax) / 2 - Event.current.mousePosition.y) / scale;
					else
						Debug.LogError("Need to set up X direction");
				}
			}
			
			if(Event.current.button == 0 && Event.current.type == EventType.MouseUp)
				quadSelected = false;

			return currentValue;
		}

		public bool DrawHighlightableQuad(Rect quad, Color quadColor, Rect containingQuad, bool canHighlight = true)
		{
			bool isHovering = false;

			if(canHighlight && quad.Contains(Event.current.mousePosition))
			{
				quadColor = Color.green;
				isHovering = true;
			}

			quad.yMin = Mathf.Clamp(quad.yMin, containingQuad.yMin, containingQuad.yMax);
			quad.yMax = Mathf.Clamp(quad.yMax, containingQuad.yMin, containingQuad.yMax);
			quad.xMin = Mathf.Clamp(quad.xMin, containingQuad.xMin, containingQuad.xMax);
			quad.xMax = Mathf.Clamp(quad.xMax, containingQuad.xMin, containingQuad.xMax);

			CreateLineMaterial();
			GL.PushMatrix();
			lineMaterial.SetPass(0); // Sets what point this is drawn
			GL.LoadPixelMatrix();
			GL.Begin(GL.QUADS);
			GL.Color(quadColor);

			GL.Vertex3(quad.xMin, quad.yMin, 0);
			GL.Vertex3(quad.xMax, quad.yMin, 0);
			GL.Vertex3(quad.xMax, quad.yMax, 0);
			GL.Vertex3(quad.xMin, quad.yMax, 0);

			GL.End();
			GL.PopMatrix();

			return isHovering;
		}

		#endregion

		#region Creating test walls
		public enum WindowTestWallType
		{
			AllWindowTypes = 0,
			SingleLongWall = 1,
			SingleWindow = 2,
		}

		public void GenerateAllWallHeights(WindowTestWallType windowTestWall, WindowTypeEnum testType = WindowTypeEnum.Standard, float size = 1f)
		{
			GameObject newBuilding = BCMesh.GenerateEmptyGameObject("Create Test Building");
			newBuilding.name = bcWindow.name + "_testbuilding";
			BuildingBlueprint buildingBp = newBuilding.AddComponent<BuildingBlueprint>();
			buildingBp.GenerateLOD = false;

			Vector3 windowPoint = bcWindow.transform.position;
			windowPoint = new Vector3(Mathf.Round(windowPoint.x), 0, Mathf.Round(windowPoint.z));
			buildingBp.transform.position = windowPoint;
			float fakeWallLength = 19f;
			if(windowTestWall == WindowTestWallType.SingleWindow)
				fakeWallLength = size + 1f;

			fakeWallLength += 0.25f;
			fakeWallLength = Mathf.Round(fakeWallLength);

			// Create a floor with all the shit that you need
			FloorBlueprint floorStamp = new FloorBlueprint();
			RoomBlueprint roomStamp = new RoomBlueprint();
			roomStamp.PerimeterWalls = new List<Vector3>() 
			{
				windowPoint,
				windowPoint + Vector3.right * fakeWallLength,
				windowPoint + Vector3.right * fakeWallLength + Vector3.forward * 10,
				windowPoint + Vector3.forward * 10,
				windowPoint
			};

			floorStamp.RoomBlueprints.Add(roomStamp);
			buildingBp.Floors.Add(floorStamp);

			if(windowTestWall == WindowTestWallType.SingleWindow)
				buildingBp.Floors[0].Windows.Add(new WindowInfo(bcWindow, testType, windowPoint + Vector3.right * .5f, windowPoint + Vector3.right * (size + 0.5f)));
			else
				buildingBp.Floors[0].Windows.AddRange(CreateTestWindows(bcWindow, WindowTypeEnum.Standard, windowPoint, fakeWallLength, 0.5f));

			if(windowTestWall == WindowTestWallType.AllWindowTypes)
			{
				buildingBp.Floors.Add(CopyAndUpdateFloorWindowsToType(buildingBp.Floors[0], WindowTypeEnum.Short));
				buildingBp.Floors.Add(CopyAndUpdateFloorWindowsToType(buildingBp.Floors[0], WindowTypeEnum.Medium));
				buildingBp.Floors.Add(CopyAndUpdateFloorWindowsToType(buildingBp.Floors[0], WindowTypeEnum.HighSmall));
				buildingBp.Floors.Add(CopyAndUpdateFloorWindowsToType(buildingBp.Floors[0], WindowTypeEnum.Tall2p5));
				buildingBp.Floors.Add(CopyAndUpdateFloorWindowsToType(buildingBp.Floors[0], WindowTypeEnum.Tall2p8));
			}
			else
			{
				for(int i = 0; i < buildingBp.Floors[0].Windows.Count; i++)
				{
					WindowInfo changedWindow = buildingBp.Floors[0].Windows[i];
					changedWindow.WindowType = testType;
					buildingBp.Floors[0].Windows[i] = changedWindow;
				}
			}

			buildingBp.BuildingStyle = this.testBuildingStyle;
			BCGenerator.GenerateBuilding(buildingBp, false, true, true, true, false);
			buildingBp.gameObject.transform.position += Vector3.right * 2f;
			newBuilding.AddComponent<BCTestWindow>();
		}

		private static WindowInfo[] CreateTestWindows(BCWindow windowToTest, WindowTypeEnum windowType, Vector3 startPos, float length, float windowIncrementSize)
		{
			List<WindowInfo> windowInfos = new List<WindowInfo>();

			float currentPosition = windowIncrementSize;
			float windowSize = windowIncrementSize;
			int breaker = 0;

			while(currentPosition + windowSize < length && breaker < 100)
			{
				WindowInfo newWindow = new WindowInfo(windowType, startPos + Vector3.right * currentPosition, startPos + Vector3.right * (currentPosition + windowSize));
				newWindow.OverriddenWindowType = windowToTest;

				currentPosition += windowSize;
				windowSize += windowIncrementSize;
				breaker++;

				windowInfos.Add(newWindow);
			}

			return windowInfos.ToArray<WindowInfo>();
		}

		private static FloorBlueprint CopyAndUpdateFloorWindowsToType(FloorBlueprint floorBp, WindowTypeEnum newWindowType)
		{
			// Short windows
			FloorBlueprint newFloor = BCUtils.DeepCopyFloor(floorBp);
			for(int i = 0; i < newFloor.Windows.Count; i++)
			{
				WindowInfo newWindow = newFloor.Windows[i];
				newWindow.WindowType = newWindowType;

				newFloor.Windows[i] = newWindow;
			}
			return newFloor;
		}
		#endregion
	}

	public class DisposeOfMeshesBeforeSave : UnityEditor.AssetModificationProcessor 
	{
		static string[] OnWillSaveAssets (string[] paths) 
		{
			BCWindowPanel.DestroyDisplayMeshes();

			return paths;
		}
	}
}

