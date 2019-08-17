using UnityEngine;

namespace BuildingCrafter
{
	/// <summary>
	/// Used to control a display. Need one of these for each display
	/// </summary>
	public class Vector2ArrayDisplayControls
	{
		// Point movement
		public int SelectedPoint = -1;

		// Line movement
		public int SelectedLine = -1;
		public Vector2 ThisPointPanelOffset = new Vector2();
		public Vector2 NextPointPanelOffset = new Vector2();

		// Foldouts
		public bool Foldout = false;
	}
}