using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
	[ActionCategory("ENVIRO")]
	[Tooltip("Gets the current weather with name.")]
	public class GetWeather : FsmStateAction
	{
		public FsmInt WeatherID;
		public FsmString WeatherName;

		[Tooltip("Repeat every frame.")]
		public bool everyFrame;

		public override void OnEnter()
		{

			for (int i = 0; i < EnviroSky.instance.Weather.WeatherPrefabs.Count; i++)
			{
				if (EnviroSky.instance.Weather.WeatherPrefabs[i] == EnviroSky.instance.Weather.currentActiveWeatherPrefab)
					WeatherID.Value = i;
			}

			if (EnviroSky.instance.Weather.currentActiveWeatherPreset != null)
				WeatherName.Value = EnviroSky.instance.Weather.currentActiveWeatherPreset.Name;


			if (!everyFrame)
			{
				Finish();
			}
		}

		public override void OnUpdate()
		{
			for (int i = 0; i < EnviroSky.instance.Weather.WeatherPrefabs.Count; i++)
			{
				if (EnviroSky.instance.Weather.WeatherPrefabs[i] == EnviroSky.instance.Weather.currentActiveWeatherPrefab)
					WeatherID.Value = i;
			}
			
			WeatherName.Value = EnviroSky.instance.Weather.currentActiveWeatherPreset.Name;
	
		}

	}
}