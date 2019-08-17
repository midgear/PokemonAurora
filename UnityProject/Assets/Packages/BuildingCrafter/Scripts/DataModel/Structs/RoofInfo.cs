using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BuildingCrafter
{

	[System.Serializable]
	public struct RoofInfo 
	{
		public Vector3 BackLeftCorner;
		public Vector3 FrontRightCorner;

		public Vector3 BackRightCorner { get { return new Vector3 (FrontRightCorner.x, BackLeftCorner.y, BackLeftCorner.z); } }
		public Vector3 FrontLeftCorner { get { return new Vector3 (BackLeftCorner.x, FrontRightCorner.y, FrontRightCorner.z); } } 

		public bool IsRoofDirectionZ;
		public bool IsRoofStartSlanted;
		public bool IsRoofEndSlanted;

		public void UpdateBaseOutline()
		{
			roofBaseOutline = new Vector3[5]
			{
				BackLeftCorner, BackRightCorner, FrontRightCorner, FrontLeftCorner, BackLeftCorner
			};
		}

		public Vector3[] RoofSpine
		{ 
			get
			{
				if(IsRoofDirectionZ == false)
				{
					return new Vector3[2] { (BackLeftCorner + BackRightCorner) / 2 + Vector3.up * width / 2, (FrontRightCorner + FrontLeftCorner) / 2 + Vector3.up * width / 2 };
				}

				return new Vector3[2] { (BackLeftCorner + FrontLeftCorner) / 2  + Vector3.up * length / 2, (FrontRightCorner + BackRightCorner) / 2 + Vector3.up * length / 2};

			}
		}

		public Vector3[] LeftRoof
		{
			get 
			{ 
				Vector3 offsetBackLeft = (Vector3.left - Vector3.back) * 0.2f;
				Vector3 offsetFrontLeft = (Vector3.left + Vector3.back) * 0.2f;

				if(IsRoofDirectionZ == true)
				{
					Vector3 cornerTip = (BackLeftCorner + FrontLeftCorner) / 2 + Vector3.up * length / 2;

					if(IsRoofStartSlanted == true 
					   && (BackLeftCorner - Vector3.left * length / 2).x < ((BackLeftCorner + BackRightCorner) / 2).x) // TODO: Add in a check to ensure we can't go past half way
						cornerTip -= Vector3.left * length / 2;


					return new Vector3[3] { 
						BackLeftCorner + offsetBackLeft,
						cornerTip,
						FrontLeftCorner + offsetFrontLeft} ;
				}
				else
				{
					return new Vector3[4] { 
						BackLeftCorner + offsetBackLeft,
						BackRoof[1],
						FrontRoof[1],
						FrontLeftCorner + offsetFrontLeft} ;
				}
			}
		}

		public Vector3[] RightRoof
		{
			get 
			{ 
				Vector3 offset1 = (Vector3.back + Vector3.right) * 0.2f;
				Vector3 offset2 = (Vector3.forward - Vector3.left) * 0.2f;
				
				if(IsRoofDirectionZ == true)
				{
					Vector3 cornerTip = (BackRightCorner + FrontRightCorner) / 2 + Vector3.up * length / 2;
					
					if(IsRoofEndSlanted == true
					   && (BackRightCorner - Vector3.right * length / 2).x > ((FrontLeftCorner + FrontRightCorner) / 2).x) // TODO: Add in a check to ensure we can't go past half way
						cornerTip -= Vector3.right * length / 2;
					
					
					return new Vector3[3] { 
						FrontRightCorner + offset1,
						cornerTip,
						BackRightCorner + offset2} ;
				}
				else
				{
					return new Vector3[4] { 
						FrontRightCorner + offset1,
						FrontRoof[1],
						BackRoof[1],
						BackRightCorner + offset2} ;
				}
			}
		}

		public Vector3[] FrontRoof
		{
			get 
			{ 
				Vector3 offset1 = (Vector3.back + Vector3.left) * 0.2f;
				Vector3 offset2 = (Vector3.back - Vector3.left) * 0.2f;
				
				if(IsRoofDirectionZ == false)
				{
					Vector3 cornerTip = (FrontRightCorner + FrontLeftCorner) / 2 + Vector3.up * width / 2;
					
					if(IsRoofStartSlanted == true
					   && (FrontRightCorner - Vector3.back * width / 2).z < ((BackRightCorner + FrontRightCorner) / 2).z)
						cornerTip -= Vector3.back * width / 2;
					
					
					return new Vector3[3] { 
						FrontLeftCorner + offset1,
						cornerTip,
						FrontRightCorner + offset2,} ;
				}
				else
				{
					return new Vector3[4] { 
						FrontLeftCorner + offset1,
						LeftRoof[1],
						RightRoof[1],
						FrontRightCorner + offset2,} ;
				}
			}
		}

		public Vector3[] BackRoof
		{
			get 
			{ 
				Vector3 offset1 = (Vector3.forward + Vector3.right) * 0.2f;
				Vector3 offset2 = (Vector3.forward - Vector3.right) * 0.2f;
				
				if(IsRoofDirectionZ == false)
				{
					Vector3 cornerTip = (BackRightCorner + BackLeftCorner) / 2 + Vector3.up * width / 2;
					
					if(IsRoofEndSlanted == true
					   && (BackRightCorner - Vector3.forward * width / 2).z > ((BackLeftCorner + FrontLeftCorner) / 2).z)
						cornerTip -= Vector3.forward * width / 2;

					return new Vector3[3] { 
						BackRightCorner + offset1,
						cornerTip,
						BackLeftCorner + offset2} ;
				}
				else
				{
					return new Vector3[4] { 
						BackRightCorner + offset1,
						RightRoof[1],
						LeftRoof[1],
						BackLeftCorner + offset2} ;
				}
			}
		}

		private float width { get { return Mathf.Abs(BackLeftCorner.x - FrontRightCorner.x); } }
		private float length { get { return Mathf.Abs(BackLeftCorner.z - FrontRightCorner.z); } }

		public Vector3[] RoofBaseOutline 
		{ 
			get 
			{ 
				if(roofBaseOutline == null)
					this.UpdateBaseOutline();

				return roofBaseOutline; 
			} 
		}
		private Vector3[] roofBaseOutline;

		public RoofInfo(Vector3 startCorner, Vector3 endCorner, bool roofDirectionIsZ, bool isStartSlanted, bool isEndSlanted)
		{
			// Ensures all roof tiles are always position in the same way
			float LeftX = 0;
			float RightX = 0;

			if(startCorner.x < endCorner.x)
			{
				LeftX = startCorner.x;
				RightX = endCorner.x;
			}
			else
			{
				LeftX = endCorner.x;
				RightX = startCorner.x;
			}
				
			float BackZ = 0;
			float FrontZ = 0;

			if(startCorner.z > endCorner.z)
			{
				BackZ = startCorner.z;
				FrontZ = endCorner.z;
			}
			else
			{
				BackZ = endCorner.z;
				FrontZ = startCorner.z;
			}

			BackLeftCorner = new Vector3(LeftX, 0, BackZ);
			FrontRightCorner = new Vector3(RightX, 0, FrontZ);

			IsRoofDirectionZ = roofDirectionIsZ;
			IsRoofStartSlanted = isStartSlanted;
			IsRoofEndSlanted = isEndSlanted;

			IsRoofStartSlanted = true;
			IsRoofEndSlanted = true;

			roofBaseOutline = new Vector3[0];

			UpdateBaseOutline();
		}
	}
}