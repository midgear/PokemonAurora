using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Linq;

namespace BuildingCrafter
{
	public class Vector2ArrayEditor : Editor 
	{

		private static Material lineMaterial;
		
		private static void CreateLineMaterial()
		{
			if (!lineMaterial)
			{
				lineMaterial = Resources.Load("FlatLineMat") as Material;
			}
		}

		public static Vector2[] DisplayVectors2Array(Vector2[] array, 
		                                             Vector2ArrayDisplayControls control,
		                                             Object undoingObject, 
		                                             string name, 
		                                             Vector2ArrayDisplayStyle displayStyle)
		{
			// Sets up the style
			
			float width = displayStyle.DisplayWidth;
			float height = displayStyle.DisplayHeight;
			Color lineColor = displayStyle.LineColor;
			Color bgColor = displayStyle.BackgroundColor;
			Color gridColor = displayStyle.GridColor;
			Color selectColor = displayStyle.SelectColor;
			Color hoverColor = displayStyle.HoverColor;
			float scale = displayStyle.Scale;
			Vector2 centerOffset = displayStyle.CenterOffsetFromTopLeft;
			float hoverRadius = displayStyle.HoverRadius;

			// Other styling things
			float graphicMargin = 0;

			if(array == null)
			{
				array = new Vector2[0];
			}

			// Sets up the top left corner for the array
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(name);
			Rect vert = EditorGUILayout.BeginVertical(GUILayout.Height(height + graphicMargin));
			float verticalOffset = vert.y;
			float horizontalOffset = vert.x + 1;
			GUILayout.Space(1);
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();

			// Before starting, restrict all the points to within the bounds of the box
			for(int i = 0; i < array.Length; i++)
			{
				float x = Mathf.Clamp(array[i].x, displayStyle.XMin, displayStyle.XMax);
				float y = Mathf.Clamp(array[i].y, displayStyle.YMin, displayStyle.YMax);

				array[i] = new Vector2(x, y);
			}

			// Creates a Unity style box to display the offsets
			Rect rect = new Rect(horizontalOffset, verticalOffset, width, height);

			bool mouseInsideFrame = false;
			if(rect.Contains(Event.current.mousePosition))
			{
				mouseInsideFrame = true;
			}

			// ======== GRAPHICS START ==============
			CreateLineMaterial();
			GL.PushMatrix();
			lineMaterial.SetPass(0); // Sets what point this is drawn
			GL.LoadPixelMatrix();

			// Draws the background
			GL.Begin(GL.QUADS);
			GL.Color(bgColor);
			
			GL.Vertex3(rect.xMin, rect.yMin, 0); // Top left point
			GL.Vertex3(rect.xMax, rect.yMin, 0); // Top right point
			GL.Vertex3(rect.xMax, rect.yMax, 0); // Bottom Right Point
			GL.Vertex3(rect.xMin, rect.yMax, 0); // Bottom Left Point
			GL.End();

			// Calculate where the anchor will be for calculating where points will end up
			Vector2 anchor = new Vector2(rect.xMin, rect.yMin);
			int xDirection = 1;
			int yDirection = 1;

			if(displayStyle.xLeftIsPositive)
			{
				anchor = new Vector2(rect.xMax, anchor.y);
				xDirection = -1;
			}
				
			if(displayStyle.yUpIsPositive)
			{
				anchor = new Vector2(anchor.x, rect.yMax);
				yDirection = -1;
			}

			Vector2 zeroZeroPoint = new Vector2(anchor.x + (xDirection * centerOffset.x), anchor.y + (yDirection * centerOffset.y));
			Vector2[] extraQuadPoints = new Vector2[0];
			// =============== DISPLAY EXTRA QUAD FROM DISPLAY ===============
			if(displayStyle.ExtraQuadPoint.Length > 0)
			{
				GL.Begin(GL.QUADS);
				GL.Color(displayStyle.ExtraQuadColor);
				
				extraQuadPoints = new Vector2[displayStyle.ExtraQuadPoint.Length];
				for(int i = 0; i < extraQuadPoints.Length; i++)
				{
					extraQuadPoints[i] = new Vector2(zeroZeroPoint.x + xDirection * Mathf.Round(displayStyle.ExtraQuadPoint[i].x * scale), 
					                                 zeroZeroPoint.y + yDirection * Mathf.Round(displayStyle.ExtraQuadPoint[i].y * scale));
				}
				
				
				for(int i = 0; i < extraQuadPoints.Length; i++)
				{			
					GL.Vertex3(extraQuadPoints[i].x, extraQuadPoints[i].y, 0);
				}
				GL.End();
			}
			// ================ END EXTRA QUAD DISPLAY ===================

			// Display vertical grid line
			GL.Begin(GL.LINES);
			GL.Color(gridColor);
			if(displayStyle.XZeroInFrame)
			{
				GL.Vertex3(rect.xMin, zeroZeroPoint.y, 0); // Left Start Point
				GL.Vertex3(rect.xMax, zeroZeroPoint.y, 0); // Right End Point
			}

			if(displayStyle.YZeroInFrame)
			{
				GL.Vertex3(zeroZeroPoint.x, rect.yMin, 0); // Left Start Point
				GL.Vertex3(zeroZeroPoint.x, rect.yMax, 0); // Right End Point
			}
			GL.End();

			Vector2[] points = new Vector2[array.Length];
			for(int i = 0; i < array.Length; i++)
			{
				points[i] = new Vector2(zeroZeroPoint.x + xDirection * Mathf.Round(array[i].x * scale), zeroZeroPoint.y + yDirection * Mathf.Round(array[i].y * scale));
			}
			
			// ============= DISPLAY THE LINES ==============
			for(int i = 0; i < points.Length; i++)
			{
				// Draws a small circle around each point
				Vector2 pointToCircle = points[i];

				if(mouseInsideFrame)
				{
					GL.Begin(GL.QUADS);
					GL.Color(lineColor);

					float radius = 2f;
					if(i == 0)
						GL.Color(Color.green);
					else if(i == points.Length - 1)
						GL.Color(Color.red);
					
					GL.Vertex3(pointToCircle.x - radius, pointToCircle.y - radius + 1, 0);
					GL.Vertex3(pointToCircle.x + radius - 1, pointToCircle.y - radius + 1, 0);
					GL.Vertex3(pointToCircle.x + radius - 1, pointToCircle.y + radius, 0);
					GL.Vertex3(pointToCircle.x - radius, pointToCircle.y + radius, 0);
					GL.End();
				}

				if(i == points.Length - 1)
					continue;

				GL.Begin(GL.LINES);
				GL.Color(lineColor);
				
				GL.Vertex3(points[i].x, points[i].y, 0);
				GL.Vertex3(points[i + 1].x, points[i + 1].y, 0);
				
				GL.End();
			}

			// ================= LINES FOR ANY CURRENTLY SELECTED ITEMS ===============

			// Points that are selected
			if(control.SelectedPoint > -1)
			{
				if(control.SelectedPoint >= points.Length)
				{
					control.SelectedPoint = -1;
				}
				else
				{
					Vector2 pointToCircle = points[control.SelectedPoint];
					
					float radius = 3f;
					
					GL.Begin(GL.QUADS);
					GL.Color(selectColor);
					
					GL.Vertex3(pointToCircle.x - radius, pointToCircle.y - radius + 1, 0);
					GL.Vertex3(pointToCircle.x + radius - 1, pointToCircle.y - radius + 1, 0);
					GL.Vertex3(pointToCircle.x + radius - 1, pointToCircle.y + radius, 0);
					GL.Vertex3(pointToCircle.x - radius, pointToCircle.y + radius, 0);

					GL.End();
				}
			}

			// Lines that are selected
			if(control.SelectedLine > -1)
			{
				if(control.SelectedLine >= points.Length - 1)
				{
					control.SelectedLine = -1;
				}
				else
				{
					Vector2 thisPoint = points[control.SelectedLine];
					Vector2 nextPoint = points[control.SelectedLine + 1];

					GL.Begin(GL.LINES);
					GL.Color(selectColor);
					
					GL.Vertex3(thisPoint.x, thisPoint.y, 0);
					GL.Vertex3(nextPoint.x, nextPoint.y, 0);
					
					GL.End();

					float radius = 2f;

					GL.Begin(GL.QUADS);
					GL.Color(selectColor);
					
					GL.Vertex3(thisPoint.x - radius, thisPoint.y - radius + 1, 0);
					GL.Vertex3(thisPoint.x + radius - 1, thisPoint.y - radius + 1, 0);
					GL.Vertex3(thisPoint.x + radius - 1, thisPoint.y + radius, 0);
					GL.Vertex3(thisPoint.x - radius, thisPoint.y + radius, 0);

					GL.Vertex3(nextPoint.x - radius, nextPoint.y - radius + 1, 0);
					GL.Vertex3(nextPoint.x + radius - 1, nextPoint.y - radius + 1, 0);
					GL.Vertex3(nextPoint.x + radius - 1, nextPoint.y + radius, 0);
					GL.Vertex3(nextPoint.x - radius, nextPoint.y + radius, 0);
					
					GL.End();
				}
			}

			// Variables for the hover point index
			int hoverPointIndex = -1;
			int hoverLineIndex = -1;

			// Allow a player to click on an item

			// Find where the player is hovering
			hoverPointIndex = TestForHover(Event.current.mousePosition, points, rect, hoverRadius);
			hoverLineIndex = TestForLineHover(Event.current.mousePosition, points, rect, hoverRadius);

			// Display a hover over a single point
			if(hoverPointIndex > -1)
			{
				Vector2 pointToCircle = points[hoverPointIndex];
				float radius = 2f;
				
				GL.Begin(GL.QUADS);
				GL.Color(hoverColor);
				
				GL.Vertex3(pointToCircle.x - radius, pointToCircle.y - radius + 1, 0);
				GL.Vertex3(pointToCircle.x + radius - 1, pointToCircle.y - radius + 1, 0);
				GL.Vertex3(pointToCircle.x + radius - 1, pointToCircle.y + radius, 0);
				GL.Vertex3(pointToCircle.x - radius, pointToCircle.y + radius, 0);
				
				GL.End();

				// Sets the hover line to not register
				hoverLineIndex = -1;
			}

			if(hoverLineIndex > -1)
			{
				GL.Begin(GL.LINES);
				GL.Color(hoverColor);
				
				GL.Vertex3(points[hoverLineIndex].x, points[hoverLineIndex].y, 0);
				GL.Vertex3(points[hoverLineIndex + 1].x, points[hoverLineIndex + 1].y, 0);
				
				GL.End();
			}

			// Drag anything around
			if(Event.current.button == 0 && Event.current.type == EventType.MouseDrag)
			{
				// Drag a point around
				if(control.SelectedPoint > -1)
				{
					Vector2 mousePos = Event.current.mousePosition;
					array[control.SelectedPoint] = ConvertPanelSpaceToVector2(mousePos, zeroZeroPoint, rect, xDirection, yDirection, scale);
				}
				if(control.SelectedLine > -1)
				{
					Vector2 mousePos = Event.current.mousePosition;

					// difference in space between the grab point

					Vector2 newPoint = ConvertPanelSpaceToVector2(mousePos - control.ThisPointPanelOffset, zeroZeroPoint, rect, xDirection, yDirection, scale);
					Vector2 nextNewPoint = ConvertPanelSpaceToVector2(mousePos - control.NextPointPanelOffset, zeroZeroPoint, rect, xDirection, yDirection, scale);

					array[control.SelectedLine] = newPoint;
					array[control.SelectedLine + 1] = nextNewPoint;
				}

				SceneView.RepaintAll();
			}


			GL.PopMatrix();

			// Displays the floating box in the extra quad
			if(extraQuadPoints.Length > 3)
			{
				float margin = 3;
				float left = 3000;
				float top = 3000;
				float right = 0;
				float bottom = 0;
				
				for(int i = 0; i < 4; i++)
				{
					left = Mathf.Min(extraQuadPoints[i].x, left);
					top = Mathf.Min(extraQuadPoints[i].y, top);
					right = Mathf.Max(extraQuadPoints[i].x, right);
					bottom = Mathf.Max(extraQuadPoints[i].y, bottom);
				}

				// Creates the label and where it goes
				Rect quadLabel = new Rect(left + margin, top + margin, right - left - margin * 2, bottom - top - margin * 2);
				
				EditorGUI.LabelField(quadLabel, displayStyle.ExtraQuadLabel, displayStyle.ExtraQuadFont);
			}


			// Select a point by clicking on it
			if(Event.current.button == 0 && Event.current.type == EventType.MouseDown)
			{

				if(rect.Contains(Event.current.mousePosition))
				{
					// For selecting a point
					if(hoverPointIndex > -1)
					{
						Undo.RegisterCompleteObjectUndo(undoingObject, "Move Point");
						control.SelectedPoint = hoverPointIndex;
					}
					else
						control.SelectedPoint = -1;

					// For selecting a line
					if(hoverLineIndex > -1)
					{
						Undo.RegisterCompleteObjectUndo(undoingObject, "Move Line");
						control.SelectedLine = hoverLineIndex;
						control.ThisPointPanelOffset = Event.current.mousePosition - points[control.SelectedLine];
						control.NextPointPanelOffset = Event.current.mousePosition - points[control.SelectedLine + 1];
					}
					else
						control.SelectedLine = -1;



					Event.current.Use();
				}
				else // If we click outside of the area, then reset things
				{
					control.SelectedPoint = -1;
					control.SelectedLine = -1;
				}
			}

			// Delete a point
			int removePoint = -1;
			bool removeNextPoint = false;

			// Allows a player to delete a point or section
			if(Event.current.isKey && (Event.current.keyCode == KeyCode.Backspace || Event.current.keyCode == KeyCode.Delete))
			{
				if(control.SelectedPoint > -1)
				{
					removePoint = control.SelectedPoint;
					Event.current.Use();
					control.SelectedPoint = -1;
				}
				else if(control.SelectedLine > -1)
				{
					removePoint = control.SelectedLine;
					removeNextPoint = true;
					Event.current.Use();
					control.SelectedLine = -1;
				}
			}

			EditorGUILayout.BeginHorizontal();

			control.Foldout = EditorGUILayout.Foldout(control.Foldout, "Show All Points");
			if(control.Foldout == false)
			{
				if(control.SelectedPoint > -1)
				{
					GUI.enabled = false;
					GUILayout.Label("Point " + control.SelectedPoint, GUILayout.Width(60));
					EditorGUILayout.FloatField(array[control.SelectedPoint].x, GUILayout.Width(40));
					EditorGUILayout.FloatField(array[control.SelectedPoint].y, GUILayout.Width(40));
					GUI.enabled = true;
				}
			}

			EditorGUILayout.EndHorizontal();
			if(control.Foldout)
			{
				for(int i = 0; i < array.Length - 0; i++)
				{
					EditorGUILayout.BeginHorizontal();
					Vector2 position = EditorGUILayout.Vector2Field("Point " + i, array[i]);
					if(GUILayout.Button("X", GUILayout.Width(20)))
					{
						removePoint = i;
					}
					EditorGUILayout.EndHorizontal();
					
					float x = position.x;
					float y = position.y;
					
					array[i] = new Vector2(x, y);
				}
			}



			// Actually delete the point
			if(removePoint > -1)
			{
				Undo.RegisterCompleteObjectUndo(undoingObject, "Delete Point");

				int newArrayLength = array.Length - 1;
				if(removeNextPoint)
					newArrayLength--;

				Vector2[] newArray = new Vector2[newArrayLength];
				int index = 0;
				for(int i = 0; i < array.Length; i++)
				{
					if(i == removePoint)
					{
						if(removeNextPoint)
							i++;
						continue;
					}
					
					newArray[index] = array[i];
					
					index++;
				}
				
				array = newArray;
			}

			EditorGUILayout.BeginHorizontal();
			
			EditorGUILayout.PrefixLabel("Options");
			
			if(GUILayout.Button(" + "))
			{
				Undo.RegisterCompleteObjectUndo(undoingObject, "Add Crown Point");
				
				Vector2[] newCrown = new Vector2[array.Length + 1];
				for(int i = 0; i < array.Length; i++)
				{
					newCrown[i] = array[i];
				}
				array = newCrown;
				
				EditorUtility.SetDirty(undoingObject);
			}
			if(GUILayout.Button("Clear"))
			{
				Undo.RegisterCompleteObjectUndo(undoingObject, "Reset Profile");
				array = new Vector2[0];
				EditorUtility.SetDirty(undoingObject);
			}
			
			if(GUILayout.Button("Reverse"))
			{
				Undo.RegisterCompleteObjectUndo(undoingObject, "Reset Profile");
				array = array.Reverse().ToArray<Vector2>();
				EditorUtility.SetDirty(undoingObject);
			}
			
			EditorGUILayout.EndHorizontal();

			// Displays the XMin
			if(displayStyle.xLeftIsPositive)
				displayStyle.ScaleWhiteFont.alignment = TextAnchor.MiddleRight;
			else
				displayStyle.ScaleWhiteFont.alignment = TextAnchor.MiddleLeft;
			EditorGUI.LabelField(rect, displayStyle.XMin.ToString() + "m", displayStyle.ScaleWhiteFont);

			// Displays the XMax
			if(displayStyle.xLeftIsPositive == false)
				displayStyle.ScaleWhiteFont.alignment = TextAnchor.MiddleRight;
			else
				displayStyle.ScaleWhiteFont.alignment = TextAnchor.MiddleLeft;
			EditorGUI.LabelField(rect, displayStyle.XMax.ToString() + "m", displayStyle.ScaleWhiteFont);

			// Displays the YMin
			if(displayStyle.yUpIsPositive)
				displayStyle.ScaleWhiteFont.alignment = TextAnchor.LowerCenter;
			else
				displayStyle.ScaleWhiteFont.alignment = TextAnchor.UpperCenter;
			EditorGUI.LabelField(rect, displayStyle.YMin.ToString() + "m", displayStyle.ScaleWhiteFont);

			// Displays the YMax
			if(displayStyle.yUpIsPositive == false)
				displayStyle.ScaleWhiteFont.alignment = TextAnchor.LowerCenter;
			else
				displayStyle.ScaleWhiteFont.alignment = TextAnchor.UpperCenter;
			EditorGUI.LabelField(rect, displayStyle.YMax.ToString() + "m", displayStyle.ScaleWhiteFont);

			EditorGUILayout.Separator();

			return array;
		}

		public static int TestForHover(Vector2 testPoint, Vector2[] pointsInPanelSpace, Rect boxPosition, float radiusOffset)
		{
			if(pointsInPanelSpace == null || pointsInPanelSpace.Length <= 0)
				return -1;
			
			float shortestDistance = (testPoint - pointsInPanelSpace[0]).sqrMagnitude;
			int index = 0;
			
			for(int i = 1; i < pointsInPanelSpace.Length; i++)
			{
				float newShortestDistance = (testPoint - pointsInPanelSpace[i]).sqrMagnitude;
				if(newShortestDistance < shortestDistance)
				{
					index = i;
					shortestDistance = newShortestDistance;
				}
			}
			
			if(boxPosition.Contains(testPoint) == false)
				return -1;
			
			if(shortestDistance > radiusOffset * radiusOffset)
				return -1;
			
			return index;
		}

		public static int TestForLineHover(Vector2 testPoint, Vector2[] pointsInPanelSpace, Rect boxPosition, float radiusOffset)
		{
			for(int i = 0; i < pointsInPanelSpace.Length - 1; i++)
			{
				if(BCUtils.IsPointCloseToLine(Event.current.mousePosition, pointsInPanelSpace[i], pointsInPanelSpace[i + 1], 10f))
				{
					return i;
				}
			}

			return -1;
		}


		public static Vector2 ConvertPanelSpaceToVector2(Vector2 pointInPanelSpace, Vector2 panelCenter, Rect boxPosition, int xDirection, int yDirection, float scale)
		{
			float x = (pointInPanelSpace.x - panelCenter.x) / scale * xDirection;
			float y = (pointInPanelSpace.y - panelCenter.y) / scale * yDirection;
			
			return new Vector2(x, y);
		}

		public static Vector2[] ConvertOffsetsToSpace(Vector2 grabPoint, Vector2 p1InPanelSpace, Vector2 p2InPanelSpace, Vector2 panelCenter, 
		                                              Rect boxPosition, int xDirection, int yDirection, float scale)
		{
			// First find the offset for the grab point
			Vector2 p1Offset = grabPoint - p1InPanelSpace;
			Vector2 p2Offset = grabPoint - p2InPanelSpace;


			Vector2 p1Local = ConvertPanelSpaceToVector2(p1Offset, panelCenter, boxPosition, xDirection, yDirection, scale);
			Vector2 p2Local = ConvertPanelSpaceToVector2(p2Offset, panelCenter, boxPosition, xDirection, yDirection, scale);

			return new Vector2[2] { p1Local, p2Local };
		}

		/// <summary>
		/// Must have a GL going to use this
		/// </summary>
		public static void DrawSquare(Vector2 point, float radius, Color color)
		{
			GL.Begin(GL.QUADS);
			GL.Color(color);
			
			GL.Vertex3(point.x - radius, point.y - radius, 0);
			GL.Vertex3(point.x + radius, point.y - radius, 0);
			GL.Vertex3(point.x + radius, point.y + radius, 0);
			GL.Vertex3(point.x - radius, point.y + radius, 0);
			
			GL.End();
		}
	}
}