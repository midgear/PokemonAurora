using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
	[ActionCategory("ENVIRO")]
	[Tooltip("Changes the current time (Date).")]
	public class SetTimeDate : FsmStateAction
	{	
		[Tooltip("Set new second?")]
		public bool SetSeconds = true;
		[HasFloatSlider(0, 59)]
		public FsmInt Second;
		[Tooltip("Set new minute?")]
		public bool SetMinute = true;
		[HasFloatSlider(0, 59)]
		public FsmInt Minute;
		[Tooltip("Set new hour?")]
		public bool SetHour = true;
		[HasFloatSlider(0, 24)]
		public FsmInt Hour;
		[Tooltip("Set new day?")]
		public bool SetDay = true;
		[HasFloatSlider(0, 365)]
		public FsmInt Day;
		[Tooltip("Set new year?")]
		public bool SetYear = true;
		public FsmInt Year;

		[Tooltip("Repeat every frame.")]
		public bool everyFrame;

		
		public override void OnEnter()
		{
			if (Second.Value != null && SetSeconds) 
				EnviroSky.instance.GameTime.Seconds = Second.Value;	
			if (Minute.Value != null && SetMinute) 
				EnviroSky.instance.GameTime.Minutes = Minute.Value;	
			if (Hour.Value != null && SetHour) 
				EnviroSky.instance.GameTime.Hours = Hour.Value;	
			if (Day.Value != null && SetDay)
				EnviroSky.instance.GameTime.Days = Day.Value;
			if (Year.Value != null && SetYear)
				EnviroSky.instance.GameTime.Years = Year.Value;

			if (!everyFrame)
			{
				Finish();
			}
		}

		public override void OnUpdate()
		{
			if (Second.Value != null && SetSeconds) 
				EnviroSky.instance.GameTime.Seconds = Second.Value;	
			if (Minute.Value != null && SetMinute) 
				EnviroSky.instance.GameTime.Minutes = Minute.Value;	
			if (Hour.Value != null && SetHour) 
				EnviroSky.instance.GameTime.Hours = Hour.Value;	
			if (Day.Value != null && SetDay)
				EnviroSky.instance.GameTime.Days = Day.Value;
			if (Year.Value != null && SetYear)
				EnviroSky.instance.GameTime.Years = Year.Value;
		}
	}
}