using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BuildingCrafter
{
	public class BCWindow : MonoBehaviour 
	{
		[HideInInspector]
		public Bounds bounds = new Bounds();
		
		[Header("Frame Inset Cuts")]
		public float LeftCut = 0.2f;
		public float RightCut = 0.8f;
		public float TopCut = 0.2f;
		public float BottomCut = 0.8f;
		
		[Header("Center and Frame Related")]
		/// <summary> Determines how far inset a window is </summary>
		[Range(-1, 1)]
		public float CenterOfWindow = 0f;

		[Range(-1, 1)]
		public float FrameChangePosition = 0f;
		public bool GenerateFrame = true;
		
		[Header("Max Width of Window (meters)")]
		[Range(0.5f, 20f)]
		public float MaxWidth = 50;
	}
}
