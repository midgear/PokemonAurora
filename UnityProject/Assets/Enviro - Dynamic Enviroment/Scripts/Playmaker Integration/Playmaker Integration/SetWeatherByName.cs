using UnityEngine;
using System.Collections.Generic;

namespace HutongGames.PlayMaker.Actions
{
	[ActionCategory("ENVIRO")]
	[Tooltip("Changes the current weather by name.")]
	public class SetWeatherByName : FsmStateAction
	{
		[RequiredField]
		public FsmString WeatherName;

		[Tooltip("Repeat every frame.")]
		public bool everyFrame;

		public override void OnEnter()
		{
			EnviroSky.instance.ChangeWeather(WeatherName.Value);

			if (!everyFrame)
			{
				Finish();
			}
		}

		public override void OnUpdate()
		{
			EnviroSky.instance.ChangeWeather(WeatherName.Value);
		}
		
	}


}