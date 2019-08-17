using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BuildingCrafter
{
	/// <summary>
	/// This is used when attached to a wall information
	/// </summary>
	public struct Opening
	{
		public Opening(Opening copyWall)
		{
			this.StartDistance = copyWall.StartDistance;
			this.EndDistance = copyWall.EndDistance;
			this.Top = copyWall.Top;
			this.Bottom = copyWall.Bottom;
			this.IsValid = copyWall.IsValid;
			this.NoFrame = copyWall.NoFrame;
		}

		public Opening(Vector3 wallStartPoint, Vector3 wallEndPoint, WindowInfo windowInfo)
		{
			wallStartPoint.y = wallEndPoint.y = 0;
			windowInfo.Start.y = windowInfo.End.y = 0;
			Vector3 wallDirection = (wallEndPoint - wallStartPoint).normalized;
			float wallDistance = (wallStartPoint - wallEndPoint).magnitude;
			Vector3 openingDirection = (windowInfo.End - windowInfo.Start).normalized;

			// If the window is going the opposite way from the wall, then flip it around
			float dot = Vector3.Dot(wallDirection, openingDirection);
			if(dot < 1)
			{
				Vector3 temp = windowInfo.Start;
				windowInfo.Start = windowInfo.End;
				windowInfo.End = temp;
				wallDirection = (wallEndPoint - wallStartPoint).normalized;
				openingDirection = (windowInfo.End - windowInfo.Start).normalized;
			}

			this.StartDistance = 0;
			this.EndDistance = wallDistance;

			Vector3 startPointDirection = (windowInfo.Start - wallStartPoint).normalized;
			float startDot = Vector3.Dot(wallDirection, startPointDirection);
			if(startDot < 0 || (windowInfo.Start - wallStartPoint).sqrMagnitude < 0.000001f)
				this.StartDistance = 0;
			else
			{
				this.StartDistance = (windowInfo.Start - wallStartPoint).magnitude;
			}

			Vector3	endPointDirection = (windowInfo.End - wallEndPoint).normalized;
			float endDot = Vector3.Dot(wallDirection, endPointDirection);

			if(endDot > 0 || (windowInfo.End - wallEndPoint).sqrMagnitude < 0.000001f)
				this.EndDistance = wallDistance;
			else
			{
				this.EndDistance =  (windowInfo.End - wallStartPoint).magnitude;
			}

			// HACK - need to tighten up the windows by an inset
			this.StartDistance += 0.1f;
			this.EndDistance -= 0.1f;

			this.Top = windowInfo.TopHeight;
			this.Bottom = windowInfo.BottomHeight;
			this.IsValid = true;
			this.NoFrame = false;
		}

		public Opening(Vector3 wallStartPoint, Vector3 wallEndPoint, DoorInfo doorInfo)
		{
			wallStartPoint.y = wallEndPoint.y = 0;
			doorInfo.Start.y = doorInfo.End.y = 0;
			Vector3 wallDirection = (wallEndPoint - wallStartPoint).normalized;
			float wallDistance = (wallStartPoint - wallEndPoint).magnitude;
			Vector3 openingDirection = (doorInfo.End - doorInfo.Start).normalized;

			// If the window is going the opposite way from the wall, then flip it around
			float dot = Vector3.Dot(wallDirection, openingDirection);
			if(dot < 1)
			{
				Vector3 temp = doorInfo.Start;
				doorInfo.Start = doorInfo.End;
				doorInfo.End = temp;
				wallDirection = (wallEndPoint - wallStartPoint).normalized;
				openingDirection = (doorInfo.End - doorInfo.Start).normalized;
			}

			this.StartDistance = 0;
			this.EndDistance = wallDistance;

			Vector3 startPointDirection = (doorInfo.Start - wallStartPoint).normalized;
			float startDot = Vector3.Dot(wallDirection, startPointDirection);
			if(startDot < 0 || (doorInfo.Start - wallStartPoint).sqrMagnitude < 0.000001f)
				this.StartDistance = 0;
			else
			{
				this.StartDistance = (doorInfo.Start - wallStartPoint).magnitude;
			}

			Vector3	endPointDirection = (doorInfo.End - wallEndPoint).normalized;
			float endDot = Vector3.Dot(wallDirection, endPointDirection);

			if(endDot > 0 || (doorInfo.End - wallEndPoint).sqrMagnitude < 0.000001f)
				this.EndDistance = wallDistance;
			else
			{
				this.EndDistance =  (doorInfo.End - wallStartPoint).magnitude;
			}

			if(doorInfo.DoorType == DoorTypeEnum.SkinnyOpen
				|| doorInfo.DoorType == DoorTypeEnum.Closet
				|| doorInfo.DoorType == DoorTypeEnum.TallOpen)
			{
				this.StartDistance += 0.1f;
				this.EndDistance -= 0.1f;
			}

			this.Top = doorInfo.DoorHeight;
			this.Bottom = 0;
			this.IsValid = true;
			this.NoFrame = false;
		}

		public bool IsValid;

		/// <summary> The distance from the start of the NON outset point to the opening start</summary>
		public float StartDistance;
		/// <summary> The distance from the start of the NON outset point to the opening end</summary>
		public float EndDistance;

		public float Top;
		public float Bottom;

		public bool NoFrame;

		public bool HasTop(float wallMax)
		{
			return Top < wallMax;
		}
		public bool HasBottom()
		{
			return Bottom > 0;
		}

		public Vector3 GetStartPosition(WallInformation wallInfo, float windowBorder = 0.1f)
		{
			return wallInfo.Start + (wallInfo.End - wallInfo.Start).normalized * (this.StartDistance + windowBorder);
		}

		public Vector3 GetEndPosition(WallInformation wallInfo, float windowBorder = 0.1f)
		{
			return wallInfo.Start + (wallInfo.End - wallInfo.Start).normalized * (this.EndDistance - windowBorder);
		}

		public Vector3 GetStartPositionOutset(WallInformation wallInfo, float windowBorder = 0.1f)
		{
			return wallInfo.Start + (wallInfo.End - wallInfo.Start).normalized * (this.StartDistance + windowBorder) + wallInfo.OutsetDirection * wallInfo.Outset;
		}

		public Vector3 GetEndPositionOutset(WallInformation wallInfo, float windowBorder = 0.1f)
		{
			return wallInfo.Start + (wallInfo.End - wallInfo.Start).normalized  * (this.EndDistance - windowBorder) + wallInfo.OutsetDirection * wallInfo.Outset;;
		}

		public static bool operator ==(Opening a, Opening b)
		{
			if(a.Top != b.Top
				|| a.Bottom != b.Bottom
				|| a.StartDistance != b.StartDistance
				|| a.EndDistance != b.EndDistance
				|| a.IsValid != b.IsValid)
				return false;

			return true;
		}

		public static bool operator !=(Opening a, Opening b)
		{
			return !(a == b);
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return (Opening)obj == this;
		}

		public override string ToString ()
		{
//			this.StartDistance = copyWall.StartDistance;
//			this.EndDistance = copyWall.EndDistance;
//			this.Top = copyWall.Top;
//			this.Bottom = copyWall.Bottom;
//			this.IsValid = copyWall.IsValid;
//			this.NoFrame = copyWall.NoFrame;

			return "Opening | Start: " + this.StartDistance + ", End: " + this.EndDistance +", IsValid: " + this.IsValid;
		}
	}
}