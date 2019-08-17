using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{
	public static partial class BCGenerator
	{
		public static WallInformation[] CreateWallInfos(Vector3[] loopedWallPoints, float outset, int sideIndex, BuildingBlueprint buildingBp, FloorBlueprint floorBp)
		{
			WallInformation[] wallInfos = new WallInformation[loopedWallPoints.Length - 1];
			for(int i = 0; i < loopedWallPoints.Length - 1; i++)
			{
				WallInformation newWall = new WallInformation();
				newWall.Outset = outset;
				newWall.Start = RoundVector3(loopedWallPoints[i]);
				newWall.End =  RoundVector3(loopedWallPoints[i + 1]);

				newWall.IsPartyWall = IsPartyWall(newWall, buildingBp);

				if(floorBp != null)
					newWall = BCGenerator.AddOpeningsToWallInfo(newWall, floorBp);
				
				newWall.SideIndex = sideIndex;

				if(newWall.Outset > 0)
					newWall.Outset = BCGenerator.GetPartyWallOutset(newWall, buildingBp);

				wallInfos[i] = newWall;
			}
			return wallInfos;
		}

		/// <summary>
		/// Creates walls without party walls or openings and preps it for generation
		/// </summary>
		public static WallInformation[] CreateWallInfos(Vector3[] loopedWallPoints, float outset)
		{
			WallInformation[] wallInfos = new WallInformation[loopedWallPoints.Length - 1];
			for(int i = 0; i < loopedWallPoints.Length - 1; i++)
			{
				WallInformation newWall = new WallInformation();
				newWall.Outset = outset;
				newWall.Start = RoundVector3(loopedWallPoints[i]);
				newWall.End =  RoundVector3(loopedWallPoints[i + 1]);

				newWall.IsPartyWall = false;

				wallInfos[i] = newWall;
			}

			OutsetWallInfos(ref wallInfos);

			return wallInfos;
		}

		public static bool IsPartyWall(WallInformation newWall, BuildingBlueprint bp)
		{
			for(int i = 0; i < bp.PartyWalls.Count; i++)
			{
				if(bp.PartyWalls[i].IsOnPartyWall(newWall.Start, newWall.End))
					return true;
			}

			return false;
		}

		private static Vector3 RoundVector3(Vector3 vectorToRound, int decimals = 4)
		{
			return new Vector3(
				(float)System.Math.Round(vectorToRound.x, decimals),
				(float)System.Math.Round(vectorToRound.y, decimals),
				(float)System.Math.Round(vectorToRound.z, decimals));
		}

		public static void OutsetWallInfos(ref WallInformation[] wallInfos, bool outsetStarts = false)
		{
			for(int i = 0; i < wallInfos.Length; i++)
			{
				WallInformation currentWallInfo = wallInfos[i];
				WallInformation newWallInfo = new WallInformation(currentWallInfo);

				Vector3 wallStartPos = currentWallInfo.Start;
				Vector3 wallEndPos = currentWallInfo.End;

				Vector3 thisVector, thisDirection, thisCross;
				float thisDistance;
				BCTiledWall.GetVecDistanceDirectionAndCross(wallStartPos, wallEndPos, out thisVector, out thisDirection, out thisCross, out thisDistance);

				Vector3 intersectionStart, intersectionEnd;
				BCTiledWall.GetStartAndEndOutsetPoints(i, wallInfos, currentWallInfo.Outset, out intersectionStart, out intersectionEnd);

				// Now we have all the wall infos for the surrounding building. let's add those to new wall infos
				newWallInfo.StartOffset =  RoundVector3(intersectionStart);
				newWallInfo.EndOffset =  RoundVector3(intersectionEnd);

				newWallInfo.OutsetDirection = thisCross;
				newWallInfo.ReadyForGeneration = true;

				wallInfos[i] = newWallInfo;
			}
		}

		public static WallInformation AddOpeningsToWallInfo(WallInformation newWall, FloorBlueprint floorBp)
		{
			// Add windows to the opening
			for(int windowIndex = 0; windowIndex < floorBp.Windows.Count; windowIndex++)
			{
				Vector3 startOfOpening = floorBp.Windows[windowIndex].Start;
				Vector3 endOfOpening = floorBp.Windows[windowIndex].End;

				// Find all the indexes of the new 
				if(BCUtils.IsPointAlongLineXZ(startOfOpening, newWall.Start, newWall.End))
				{
					if(BCUtils.IsPointAlongLineXZ(endOfOpening, newWall.Start, newWall.End))
					{
						Opening newOpening = new Opening(newWall.Start, newWall.End, floorBp.Windows[windowIndex]);
						Opening[] currentOpenings = newWall.Openings;
						Opening[] newOpenings = new Opening[1] { newOpening };
						if(currentOpenings != null)
						{
							newOpenings = new Opening[currentOpenings.Length + 1];
							for(int openingIndex = 0; openingIndex < currentOpenings.Length; openingIndex++)
								newOpenings[openingIndex] = new Opening(currentOpenings[openingIndex]);
							newOpenings[currentOpenings.Length] = newOpening;
						}

						newWall.Openings = newOpenings;
					}
				}
			}

			// Add Doors to the opening
			for(int doorIndex = 0; doorIndex < floorBp.Doors.Count; doorIndex++)
			{
				Vector3 startOfOpening = floorBp.Doors[doorIndex].Start;
				Vector3 endOfOpening = floorBp.Doors[doorIndex].End;

				if(BCUtils.IsPointAlongLineXZ(startOfOpening, newWall.Start, newWall.End))
				{
					if(BCUtils.IsPointAlongLineXZ(endOfOpening, newWall.Start, newWall.End))
					{
						Opening newOpening = new Opening(newWall.Start, newWall.End, floorBp.Doors[doorIndex]);
						Opening[] currentOpenings = newWall.Openings;
						Opening[] newOpenings = new Opening[1] { newOpening };
						if(currentOpenings != null)
						{
							newOpenings = new Opening[currentOpenings.Length + 1];
							for(int openingIndex = 0; openingIndex < currentOpenings.Length; openingIndex++)
								newOpenings[openingIndex] = new Opening(currentOpenings[openingIndex]);
							newOpenings[currentOpenings.Length] = newOpening;
						}

						newWall.Openings = newOpenings;
					}
				}
			}

			return newWall;
		}

		public static float GetPartyWallOutset(WallInformation newWall, BuildingBlueprint bp)
		{
			for(int i = 0; i < bp.PartyWalls.Count; i++)
			{
				if(bp.PartyWalls[i].IsOnPartyWall(newWall.Start, newWall.End))
					return 0f;
			}

			return newWall.Outset;
		}

		public static List<WallInformation> StackVectorsToOutsetWallInfos(List<Vector3[]> outlines, BuildingBlueprint buildingBp, int sideIndex = 0)
		{
			List<WallInformation> wallInfos = new List<WallInformation>();

			for(int vIndex = 0; vIndex < outlines.Count; vIndex++)
			{
				for(int i = 0; i < outlines[vIndex].Length - 1; i++)
				{
					WallInformation newWall = new WallInformation(outlines[vIndex][i], outlines[vIndex][i + 1]);

					newWall.IsPartyWall = IsPartyWall(newWall, buildingBp);
					newWall.SideIndex = sideIndex;

					if(i == 0)
						newWall.IsStart = true;
					if(i == outlines[vIndex].Length - 2)
						newWall.IsEnd = true;

					wallInfos.Add(newWall);
				}
			}

			return wallInfos;
		}
	}
}