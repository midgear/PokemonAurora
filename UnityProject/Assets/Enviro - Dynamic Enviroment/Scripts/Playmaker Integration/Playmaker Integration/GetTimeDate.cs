using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
	[ActionCategory("ENVIRO")]
	[Tooltip("Gets the current time (Date).")]
	public class GetTimeDate : FsmStateAction
	{
		public FsmInt Second;
		public FsmInt Minute;
		public FsmInt Hour;
		public FsmInt Day;
		public FsmInt Year;

		[Tooltip("Repeat every frame.")]
		public bool everyFrame;
		
		public override void OnEnter()
		{
			Second.Value = EnviroSky.instance.GameTime.Seconds;	
			Minute.Value = EnviroSky.instance.GameTime.Minutes;	
			Hour.Value = EnviroSky.instance.GameTime.Hours;		
			Day.Value = EnviroSky.instance.GameTime.Days;
			Year.Value = EnviroSky.instance.GameTime.Years;

			if (!everyFrame)
			{
				Finish();
			}
		}


		public override void OnUpdate()
		{
			Second.Value = EnviroSky.instance.GameTime.Seconds;	
			Minute.Value = EnviroSky.instance.GameTime.Minutes;	
			Hour.Value = EnviroSky.instance.GameTime.Hours;		
			Day.Value = EnviroSky.instance.GameTime.Days;
			Year.Value = EnviroSky.instance.GameTime.Years;
		}
	}
}