using UnityEngine;
using System.Collections;

namespace BuildingCrafter
{
	public class Vector2ArrayDisplayStyle
	{
		public bool DisplayCrossGrid = true;
		public bool xLeftIsPositive = true;
		public bool yUpIsPositive = true;

		public bool YZeroInFrame = true;
		public bool XZeroInFrame = true;
		
		public float XMin = -2;
		public float XMax = 2;
		public float YMin = -2;
		public float YMax = 2;
		
		public float DisplayWidth = 200;
		public float DisplayHeight = 100;
		
		/// <summary>Scale that the whole number will be multiplied by</summary>
		public float Scale { get { return scale; } }
		
		public Vector2 CenterOffsetFromTopLeft;
		
		public Color LineColor = Color.white;
		public Color BackgroundColor = Color.black;
		public Color GridColor = Color.gray;
		public Color SelectColor = Color.yellow;
		public Color HoverColor = Color.cyan;

		/// <summary>How far the mouse has to be from a point to consider it hovering</summary>
		public float HoverRadius = 5f;

		// Styles for fonts and such
		public GUIStyle ScaleWhiteFont = new GUIStyle();


		// Quads to Display
		public Color ExtraQuadColor = new Color(.3f, .3f, .3f);
		public Vector2[] ExtraQuadPoint = new Vector2[0];
		public string ExtraQuadLabel = "";
		public GUIStyle ExtraQuadFont = new GUIStyle();
		// TODO - Display an additional message on the quad

		// Private stuff
		private float scale;

		public Vector2ArrayDisplayStyle()
		{
			IntializeDisplay();
		}

		public Vector2ArrayDisplayStyle(float xMin, float xMax, float yMin, float yMax, float displayWidth)
		{
			this.XMax = xMax;
			this.XMin = xMin;
			this.YMax = yMax;
			this.YMin = yMin;

			this.DisplayWidth = displayWidth;

			this.IntializeDisplay();
		}

		private void IntializeDisplay()
		{
			// Find the maximium display width
			float xLength = XMax - XMin;
			scale = DisplayWidth / xLength;
			
			float yLength = YMax - YMin;
			
			DisplayHeight = (int)(scale * yLength);
			
			// Find the 0,0 point
			float leftOffset = (xLength - XMax) * scale;
			float topOffset = (yLength - YMax) * scale;
			
			if(leftOffset < 0 || leftOffset > DisplayWidth)
				XZeroInFrame = false;
			
			if(topOffset < 0 || topOffset > DisplayHeight)
				YZeroInFrame = false;
			
			CenterOffsetFromTopLeft = new Vector2(leftOffset, topOffset);
			
			// Sets the styles for the white fonts
			ScaleWhiteFont.fontStyle = FontStyle.Bold;
			ScaleWhiteFont.normal.textColor = Color.white;
			ScaleWhiteFont.fontSize = 8;
			ScaleWhiteFont.wordWrap = true;
			ScaleWhiteFont.alignment = TextAnchor.UpperCenter;
			
			ExtraQuadFont.normal.textColor = Color.white;
			ExtraQuadFont.fontSize = 9;
			ExtraQuadFont.alignment = TextAnchor.LowerRight;
			ExtraQuadFont.wordWrap = true;
			ExtraQuadFont.fontStyle = FontStyle.Bold;
		}
	}
}