using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BuildingCrafter
{
	public struct WallInformation
	{
		public WallInformation(Vector3 start, Vector3 end, float outset)
		{
			this.Outset = outset;
			this.Start = start;
			this.End = end;

			// Everything else is not ready to do stuff
			this.Openings = null;
			this.StartOffset = this.Start;
			this.EndOffset = this.End;
			this.OutsetDirection = Vector3.zero;
			this.ReadyForGeneration = false;
			this.SideIndex = 0;

			this.NoNonOutsetVersion = false;
			this.IsPartyWall = false;

			this.IsStart = false;
			this.IsEnd = false;
		}

		public WallInformation(Vector3 outsetStart, Vector3 outsetEnd)
		{
			this.Outset = 0;
			this.Start = outsetStart;
			this.End = outsetEnd;

			// Everything else is not ready to do stuff
			this.Openings = null;
			this.StartOffset = this.Start;
			this.EndOffset = this.End;
			this.OutsetDirection = Vector3.zero;
			this.ReadyForGeneration = true;
			this.SideIndex = 0;

			this.IsPartyWall = false;
			this.NoNonOutsetVersion = true;

			this.IsStart = false;
			this.IsEnd = false;
		}

		public WallInformation(WallInformation copyWall)
		{
			this.Outset = copyWall.Outset;
			this.Start = copyWall.Start;
			this.End = copyWall.End;

			if(copyWall.Openings != null)
			{
				this.Openings = new Opening[copyWall.Openings.Length];
				for(int i = 0; i < this.Openings.Length; i++)
					this.Openings[i] = new Opening(copyWall.Openings[i]);
			}
			else
				this.Openings = null;

			this.StartOffset = copyWall.StartOffset;
			this.EndOffset = copyWall.EndOffset;
			this.OutsetDirection = copyWall.OutsetDirection;

			this.ReadyForGeneration = copyWall.ReadyForGeneration;
			this.SideIndex = copyWall.SideIndex;

			this.IsPartyWall = copyWall.IsPartyWall;
			this.NoNonOutsetVersion = copyWall.NoNonOutsetVersion;

			this.IsStart = copyWall.IsStart;
			this.IsEnd = copyWall.IsEnd;
		}

		// The base info
		/// <summary> NON outset start and end </summary>
		public Vector3 Start;
		/// <summary> NON outset start and end </summary>
		public Vector3 End;
		/// <summary> Wall Outset (positive for bigger than the wall (outsides) and negative fo smaller than the wall (insides)</summary>
		public float Outset;

		/// <summary> For not none looped sections, this indicates this is the start of a section </summary>
		public bool IsStart;

		/// <summary> For not none looped sections, this indicates this is the start of a section </summary>
		public bool IsEnd;

		// Info about meta wall information
		public Opening[] Openings;
		// Used to figure out which side it is generating
		public int SideIndex;

		// This info gets filled out upon generation
		public bool NoNonOutsetVersion;
		public bool ReadyForGeneration;
		public Vector3 StartOffset;
		public Vector3 EndOffset;
		public Vector3 OutsetDirection;
		public bool IsPartyWall;

		// The wall information should hold ALL the info for a wall. So it will hold all the openings, where they are how high they are.
		// Functions

		public float GetStartOutsetSpacing()
		{
			if(ReadyForGeneration == false)
				return 0;

			return (StartOffset - (Start + OutsetDirection * Outset)).magnitude;
		}

		public float GetEndOutsetSpacing()
		{
			if(ReadyForGeneration == false)
				return 0;

			return ((End + OutsetDirection * Outset) - EndOffset).magnitude;
		}
	}
}