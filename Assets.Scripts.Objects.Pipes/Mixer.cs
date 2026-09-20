using System;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets;

namespace Assets.Scripts.Objects.Pipes;

public class Mixer : SettableAtmosDevice, IRocketInternals, IRocketComponent
{
	private const float FLOW_RATE_LIMITER = 5f;

	public Event OnGasMixChanged;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	public float Ratio1 => base.OutputSetting;

	public float Ratio2 => 100f - base.OutputSetting;

	protected override bool IsOperable
	{
		get
		{
			bool flag = base.IsInputValid && base.IsInput2Valid && base.IsOutputValid;
			if (GameManager.RunSimulation && HasErrorState && Error == 0 && !flag)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (GameManager.RunSimulation && HasErrorState && Error == 1 && flag)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			return flag;
		}
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	protected override void CheckConnections()
	{
		INetworkedPipe iNetworkedPipe = InputConnection.GetINetworkedPipe();
		INetworkedPipe iNetworkedPipe2 = InputConnection2.GetINetworkedPipe();
		INetworkedPipe iNetworkedPipe3 = OutputConnection.GetINetworkedPipe();
		InputNetwork = iNetworkedPipe?.PipeNetwork;
		InputNetwork2 = iNetworkedPipe2?.PipeNetwork;
		OutputNetwork = iNetworkedPipe3?.PipeNetwork;
		AssessError();
	}

	public override void AssessError()
	{
		bool flag = base.IsInputValid && base.IsInput2Valid && base.IsOutputValid;
		if (GameManager.RunSimulation && HasErrorState && Error == 0 && !flag)
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if (GameManager.RunSimulation && HasErrorState && Error == 1 && flag)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		GasMixture gasMixture = GasMixtureHelper.Create();
		OnGasMixChanged?.Invoke();
		if (!OnOff || !Powered || Error == 1 || !IsOperable)
		{
			return;
		}
		MoleQuantity moleQuantity = IdealGas.Quantity(base.PressurePerTick * ((double)Ratio1 / 100.0), Chemistry.PipeVolume, InputNetwork.Atmosphere.Temperature);
		MoleQuantity moleQuantity2 = IdealGas.Quantity(base.PressurePerTick * ((double)Ratio2 / 100.0), Chemistry.PipeVolume, InputNetwork2.Atmosphere.Temperature);
		PressurekPa pressurekPa = RocketMath.WeightedAverage(InputNetwork.Atmosphere.PressureGasses, InputNetwork2.Atmosphere.PressureGasses, Ratio1 / 100f) - OutputNetwork.Atmosphere.PressureGasses;
		if (pressurekPa > PressurekPa.Zero)
		{
			MoleQuantity moleQuantity3 = MaxMolesPerTick(InputNetwork.Atmosphere, pressurekPa);
			MoleQuantity moleQuantity4 = MaxMolesPerTick(InputNetwork2.Atmosphere, pressurekPa);
			float num = (moleQuantity3 / moleQuantity).ToFloat();
			float num2 = (moleQuantity4 / moleQuantity2).ToFloat();
			double num3 = 1.0;
			num3 = (RocketMath.Approximately(Ratio1, 100.0) ? ((double)num) : ((!RocketMath.Approximately(Ratio2, 100.0)) ? ((double)Math.Min(num, num2)) : ((double)num2)));
			num3 = Math.Max(1.0, num3);
			moleQuantity *= num3;
			moleQuantity2 *= num3;
		}
		MoleQuantity getTotalMolesGassesAndLiquids = InputNetwork.Atmosphere.GasMixture.GetTotalMolesGassesAndLiquids;
		MoleQuantity getTotalMolesGassesAndLiquids2 = InputNetwork2.Atmosphere.GasMixture.GetTotalMolesGassesAndLiquids;
		if (getTotalMolesGassesAndLiquids < moleQuantity || getTotalMolesGassesAndLiquids2 < moleQuantity2)
		{
			MoleQuantity moleQuantity5 = MoleQuantity.Zero;
			if (moleQuantity > MoleQuantity.Zero && moleQuantity2 > MoleQuantity.Zero)
			{
				moleQuantity5 = RocketMath.Min(getTotalMolesGassesAndLiquids / moleQuantity, getTotalMolesGassesAndLiquids2 / moleQuantity2);
			}
			else if (moleQuantity2 < Chemistry.MINIMUM_VALID_TOTAL_MOLES && moleQuantity > MoleQuantity.Zero)
			{
				moleQuantity5 = getTotalMolesGassesAndLiquids / moleQuantity;
			}
			else if (moleQuantity < Chemistry.MINIMUM_VALID_TOTAL_MOLES && moleQuantity2 > MoleQuantity.Zero)
			{
				moleQuantity5 = getTotalMolesGassesAndLiquids2 / moleQuantity2;
			}
			moleQuantity *= moleQuantity5;
			moleQuantity2 *= moleQuantity5;
		}
		if (moleQuantity > MoleQuantity.Zero)
		{
			gasMixture.Add(InputNetwork.Atmosphere.Remove(moleQuantity, AtmosphereHelper.MatterState.All));
		}
		if (moleQuantity2 > MoleQuantity.Zero)
		{
			gasMixture.Add(InputNetwork2.Atmosphere.Remove(moleQuantity2, AtmosphereHelper.MatterState.All));
		}
		OutputNetwork.Atmosphere.Add(gasMixture);
	}

	private MoleQuantity MaxMolesPerTick(Atmosphere inputAtmosphere, PressurekPa pressureDifferential)
	{
		if (inputAtmosphere.TotalMoles <= MoleQuantity.Zero)
		{
			return MoleQuantity.Zero;
		}
		PressurekPa pressurekPa = IdealGas.PressurePerMole(inputAtmosphere.Temperature, inputAtmosphere.Volume);
		PressurekPa pressurekPa2 = IdealGas.PressurePerMole(inputAtmosphere.Temperature, OutputNetwork.Atmosphere.Volume);
		PressurekPa pressurekPa3 = pressurekPa + pressurekPa2;
		return new MoleQuantity(pressureDifferential.ToDouble() / pressurekPa3.ToDouble() / 5.0);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = HandleButtonSetting(interactable, interaction, doAction);
		if (delayedActionInstance != null)
		{
			return delayedActionInstance;
		}
		DelayedActionInstance delayedActionInstance2 = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		delayedActionInstance2.AppendStateMessage(GameStrings.Input1Ratio, StringManager.Get(Ratio1));
		delayedActionInstance2.AppendStateMessage(GameStrings.Input2Ratio, StringManager.Get(Ratio2));
		delayedActionInstance2.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
		delayedActionInstance2.AppendStateMessage(GameStrings.UseLabelerToSet);
		switch (interactable.Action)
		{
		case InteractableType.Button1:
			if (!doAction)
			{
				return delayedActionInstance2.Succeed();
			}
			SettingWheel.PlayWheelSound(this, SettingWheel.Wheel, increaseSetting: true, interaction.AltKey, base.OutputSetting, MinSetting, MaxSetting, WheelSettingIncrement, WheelAltSettingIncrement);
			if (GameManager.RunSimulation)
			{
				base.OutputSetting += (interaction.AltKey ? WheelAltSettingIncrement : WheelSettingIncrement);
			}
			OnGasMixChanged?.Invoke();
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button2:
			if (!doAction)
			{
				return delayedActionInstance2.Succeed();
			}
			SettingWheel.PlayWheelSound(this, SettingWheel.Wheel, increaseSetting: false, interaction.AltKey, base.OutputSetting, MinSetting, MaxSetting, WheelSettingIncrement, WheelAltSettingIncrement);
			if (GameManager.RunSimulation)
			{
				base.OutputSetting -= (interaction.AltKey ? WheelAltSettingIncrement : WheelSettingIncrement);
			}
			OnGasMixChanged?.Invoke();
			return DelayedActionInstance.Success(interactable.ContextualName);
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}
}
