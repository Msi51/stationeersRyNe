using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Electrical;

public class LandingPadGas : LandingPadPump
{
	[SerializeField]
	private Transform fan;

	[Tooltip("Is this an input or an output: input if set to true and output if set to false")]
	[SerializeField]
	private bool input;

	[SerializeField]
	private AtmosphereHelper.MatterState pumpType = AtmosphereHelper.MatterState.Gas;

	private static readonly float DegreesRotationPerSecond = 1440f;

	private static readonly float MinOperatingSoundPitch = 1f;

	private static readonly float MaxOperatingSoundPitch = 1.2f;

	protected override bool IsOperable
	{
		get
		{
			bool flag = (input ? base.IsInputValid : (base.IsOutputValid && base.LandingPadCenter != null && base.LandingPadCenter.Error == 0 && base.IsStructureCompleted));
			if (Error == 1)
			{
				if (!flag)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (flag)
			{
				return true;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			return false;
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (OnOff && Powered && IsOperable)
		{
			Atmosphere inputAtmos = (input ? InputNetwork.Atmosphere : base.LandingPadNetwork.Atmosphere);
			Atmosphere outputAtmos = (input ? base.LandingPadNetwork.Atmosphere : OutputNetwork.Atmosphere);
			switch (pumpType)
			{
			case AtmosphereHelper.MatterState.Gas:
				AtmosphereHelper.MoveVolume(inputAtmos, outputAtmos, new VolumeLitres(base.OutputSetting), AtmosphereHelper.MatterState.Gas, new MoleQuantity(base.OutputSetting));
				break;
			case AtmosphereHelper.MatterState.Liquid:
				AtmosphereHelper.MoveLiquidVolume(inputAtmos, outputAtmos, new VolumeLitres(base.OutputSetting));
				break;
			default:
				ConsoleWindow.PrintError(DisplayName + " unsupported matter state");
				break;
			}
		}
	}

	public override void AssessError()
	{
		bool flag = (input ? base.IsInputValid : (base.IsOutputValid && base.LandingPadCenter != null && base.LandingPadCenter.Error == 0 && base.IsStructureCompleted));
		if (GameManager.RunSimulation && HasErrorState && Error == 0 && !flag)
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if ((GameManager.RunSimulation && HasErrorState && Error == 1 && flag) || !OnOff || !Powered)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	public override void UpdateAudio(float deltaTime)
	{
		base.UpdateAudio(deltaTime);
		if (OnOff && Powered && Error != 1 && !IsOccluded && !(base.OutputSetting < 1f))
		{
			GetAudioEvent(Defines.Sounds.VolumePumpRunnningHash)?.SetPitchMultiplier(Mathf.Lerp(MinOperatingSoundPitch, MaxOperatingSoundPitch, base.OutputSetting / MaxSetting));
		}
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (!GameManager.IsBatchMode && OnOff && Powered && Error != 1 && !IsOccluded && !(base.OutputSetting < 1f) && fan != null)
		{
			fan.Rotate(Vector3.right, DegreesRotationPerSecond * GameManager.DeltaTime * Mathf.Clamp(base.OutputSetting / MaxSetting, 0.1f, 1f));
		}
	}
}
