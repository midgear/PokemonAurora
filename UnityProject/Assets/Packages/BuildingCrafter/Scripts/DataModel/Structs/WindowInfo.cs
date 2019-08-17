using UnityEngine;
using System.Collections;

namespace BuildingCrafter
{

	[System.Serializable]
	public struct WindowInfo
	{
		public WindowTypeEnum WindowType;
		public Vector3 Start;
		public Vector3 End;
		public bool IsWindowEmpty;
		public BCWindow OverriddenWindowType;

		// Used for override heights
		public float OverriddenBottom;
		public float OverriddenTop;

		public WindowInfo (WindowTypeEnum windowType, Vector3 start, Vector3 end)
		{
			WindowType = windowType;
			Start = start;
			End = end;
			IsWindowEmpty = false;
			OverriddenWindowType = null;
			OverriddenBottom = 1;
			OverriddenTop = 2.5f;
		}

		public WindowInfo (BCWindow overridenWindow, WindowTypeEnum windowType, Vector3 start, Vector3 end) : this(windowType, start, end)
		{
			OverriddenWindowType = overridenWindow;
		}

		/// <summary>
		/// Returns the height from the bottom of the window sill to the top
		/// </summary>
		/// <value>The height of the window.</value>
		public float WindowHeight { get { return this.TopHeight - this.BottomHeight; } }

		public float BottomHeight
		{
			get
			{
				float bottomHeight = 1f;
				switch(this.WindowType)
				{
				case(WindowTypeEnum.Standard):
				case(WindowTypeEnum.Short):
					bottomHeight = 1f;
					break;
				case(WindowTypeEnum.Medium):
					bottomHeight = 0.5f;
					break;
				case(WindowTypeEnum.Tall2p5):
				case(WindowTypeEnum.Tall2p8):
					bottomHeight = 0.1f;
					break;
				case(WindowTypeEnum.HighSmall):
					bottomHeight = 2f;
					break;
				case(WindowTypeEnum.Override):
					bottomHeight = this.OverriddenBottom;
					break;
				}
				return bottomHeight;
			}
		}

		public float TopHeight
		{
			get
			{
				float topHeight = 2.5f;
				switch(this.WindowType)
				{
				case(WindowTypeEnum.Standard):
					topHeight = 2.5f;
					break;
				case(WindowTypeEnum.Short):
				case(WindowTypeEnum.Medium):
					topHeight = 2f;
					break;
				case(WindowTypeEnum.HighSmall):
					topHeight = 2.4f;
					break;
				case(WindowTypeEnum.Tall2p5):
					topHeight = 2.5f;
					break;
				case(WindowTypeEnum.Tall2p8):
					topHeight = 2.8f;
					break;
				case(WindowTypeEnum.Override):
					topHeight = this.OverriddenTop;
					break;
				}
				return topHeight;
			}
		}

		public static WindowTypeEnum GetWindowType(float bottom, float top)
		{
			if(bottom == 1 && top == 2.5f)
				return WindowTypeEnum.Standard;
			
			if(bottom == 1 && top == 2)
				return WindowTypeEnum.Short;
			
			if(bottom == 0.5f && top == 2f)
				return WindowTypeEnum.Medium;
			
			if(bottom == 0.1f && top == 2.8f)
				return WindowTypeEnum.Tall2p8;
			
			if(bottom == 0.1f && top == 2.5f)
				return WindowTypeEnum.Tall2p8;
			
			if(bottom == 2 && top == 2.4f)
				return WindowTypeEnum.HighSmall;
			
			return WindowTypeEnum.Override;
		}

		public static bool operator ==(WindowInfo a, WindowInfo b)
		{
			// If both are null, or both are same instance, return true.
			if (System.Object.ReferenceEquals(a, b))
				return true;
			
			// If one is null, but not both, return false.
			if (((object)a == null) || ((object)b == null))
				return false;

			if(a.WindowType != b.WindowType
			   || a.IsWindowEmpty != b.IsWindowEmpty
			   || a.OverriddenWindowType != b.OverriddenWindowType
			   || a.OverriddenBottom != b.OverriddenBottom
			   || a.OverriddenTop != b.OverriddenTop)
				return false;

			if(Vector3.Equals(a.Start, b.Start) == false || Vector3.Equals(a.End, b.End) == false)
				return false;
			
			return true;
		}
		
		public static bool operator !=(WindowInfo a, WindowInfo b)
		{
			return !(a == b);
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if(obj.GetType() != typeof(WindowInfo))
				return false;
			
			return (WindowInfo)obj == this;
		}
	}
}
