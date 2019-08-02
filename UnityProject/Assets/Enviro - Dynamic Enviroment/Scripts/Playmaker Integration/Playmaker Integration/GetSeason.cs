using UnityEngine;
using System.Collections.Generic;

namespace HutongGames.PlayMaker.Actions
{
	[ActionCategory("ENVIRO")]
	[Tooltip("Gets the current season.")]
	public class GetSeason : FsmStateAction
	{
		public EnviroSeasons.Seasons currentSeason;

		[Tooltip("Repeat every frame.")]
		public bool everyFrame;

		public override void OnEnter()
		{
			currentSeason = EnviroSky.instance.Seasons.currentSeasons;

			if (!everyFrame)
			{
				Finish();
			}
		}

		public override void OnUpdate()
		{
			currentSeason = EnviroSky.instance.Seasons.currentSeasons;
		}
		
	}


}