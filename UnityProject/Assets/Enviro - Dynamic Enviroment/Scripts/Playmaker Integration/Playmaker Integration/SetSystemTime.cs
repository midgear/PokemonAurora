using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
	[ActionCategory("ENVIRO")]
	[Tooltip("Changes the current time to your system time.")]
	public class SetSystemTime : FsmStateAction
	{	
		[Tooltip("Repeat every frame.")]
		public bool everyFrame;
		
		public override void OnEnter()
		{
			EnviroSky.instance.SetTime (System.DateTime.Now);

			if (!everyFrame) {
				Finish ();
			} else {
				EnviroSky.instance.GameTime.ProgressTime = EnviroTime.TimeProgressMode.None;
			}
		}

		public override void OnUpdate()
		{
			EnviroSky.instance.SetTime (System.DateTime.Now);
		}
	}
}