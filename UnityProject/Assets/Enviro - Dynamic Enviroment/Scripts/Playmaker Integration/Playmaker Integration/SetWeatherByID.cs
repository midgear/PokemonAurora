using UnityEngine;
using System.Collections.Generic;

namespace HutongGames.PlayMaker.Actions
{
	[ActionCategory("ENVIRO")]
	[Tooltip("Changes the current weather by ID.")]
	public class SetWeatherByID : FsmStateAction
	{
		[RequiredField]
		public FsmInt WeatherID;

		[Tooltip("Repeat every frame.")]
		public bool everyFrame;

		public override void OnEnter()
		{
			if (EnviroSky.instance.Weather.WeatherPrefabs.Count >= WeatherID.Value)
				EnviroSky.instance.ChangeWeather (WeatherID.Value);

			if (!everyFrame)
			{
				Finish();
			}
		}

		public override void OnUpdate()
		{
			if (EnviroSky.instance.Weather.WeatherPrefabs.Count >= WeatherID.Value)
				EnviroSky.instance.SetWeatherOverwrite (WeatherID.Value);
		}
		
	}


}