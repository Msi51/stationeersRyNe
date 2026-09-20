using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class CombustorMachine : DeviceInputOutputCircuit, ISetable, ILogicable, IReferencable, IEvaluable
{
	[SerializeField]
	private float volume = 1000f;

	public VolumeLitres Volume => new VolumeLitres(volume);

	public override bool HasReadableAtmosphere => true;

	public double Setting
	{
		get
		{
			return base.OutputSetting;
		}
		set
		{
			base.OutputSetting = (float)value;
		}
	}

	public float Ratio1 => base.OutputSetting;

	public float Ratio2 => 100f - base.OutputSetting;

	protected override bool IsOperable
	{
		get
		{
			if (!base.IsStructureCompleted)
			{
				return false;
			}
			bool flag = (object)base.ProgrammableChip != null && (CodeErrorState != 0 || base.ProgrammableChip.CompilationError);
			bool flag2 = (base.IsInputValid || base.IsInput2Valid) && base.IsOutputValid && !flag;
			if (Error == 1)
			{
				if (!flag2)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (flag2)
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

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, Volume, 0L);
		}
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

	public override void OnAtmosphericTick()
	{
		if (!OnOff || !Powered || !IsOperable)
		{
			if (Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			base.ProcessedMoles = MoleQuantity.Zero;
			return;
		}
		AtmosphereHelper.MoveToEqualize(base.InternalAtmosphere, OutputNetwork.Atmosphere, PressurekPa.MaxValue, AtmosphereHelper.MatterState.All);
		if (Mode == 0)
		{
			if (Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			base.ProcessedMoles = MoleQuantity.Zero;
			return;
		}
		if (!_hasInput())
		{
			if (Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			base.ProcessedMoles = MoleQuantity.Zero;
			return;
		}
		Atmosphere atmosphere = InputNetwork?.Atmosphere;
		Atmosphere atmosphere2 = InputNetwork2?.Atmosphere;
		TemperatureKelvin temperature = atmosphere?.Temperature ?? atmosphere2?.Temperature ?? base.InternalAtmosphere.Temperature;
		MoleQuantity moleQuantity = IdealGas.Quantity(base.PressurePerTick, Chemistry.PipeVolume, temperature);
		MoleQuantity val = moleQuantity * ((double)Ratio1 / 100.0);
		MoleQuantity val2 = moleQuantity * ((double)Ratio2 / 100.0);
		MoleQuantity moleQuantity2 = atmosphere?.GasMixture.GetTotalMolesGassesAndLiquids ?? MoleQuantity.Zero;
		MoleQuantity moleQuantity3 = atmosphere2?.GasMixture.GetTotalMolesGassesAndLiquids ?? MoleQuantity.Zero;
		MoleQuantity moleQuantity4 = RocketMath.Min(val, moleQuantity2);
		MoleQuantity moleQuantity5 = RocketMath.Min(val2, moleQuantity3);
		MoleQuantity moleQuantity6 = moleQuantity - moleQuantity4 - moleQuantity5;
		if (moleQuantity6 > MoleQuantity.Zero)
		{
			MoleQuantity moleQuantity7 = RocketMath.Min(moleQuantity6, moleQuantity2 - moleQuantity4);
			moleQuantity4 += moleQuantity7;
			moleQuantity6 -= moleQuantity7;
			MoleQuantity moleQuantity8 = RocketMath.Min(moleQuantity6, moleQuantity3 - moleQuantity5);
			moleQuantity5 += moleQuantity8;
		}
		bool flag = false;
		if (atmosphere != null && moleQuantity4 > MoleQuantity.Zero)
		{
			base.InternalAtmosphere.Add(atmosphere.Remove(moleQuantity4, AtmosphereHelper.MatterState.All));
			flag = true;
		}
		if (atmosphere2 != null && moleQuantity5 > MoleQuantity.Zero)
		{
			base.InternalAtmosphere.Add(atmosphere2.Remove(moleQuantity5, AtmosphereHelper.MatterState.All));
			flag = true;
		}
		if (flag)
		{
			if (Activate == 0)
			{
				OnServer.Interact(base.InteractActivate, 1);
			}
		}
		else if (Activate == 1)
		{
			OnServer.Interact(base.InteractActivate, 0);
		}
		base.InternalAtmosphere.TryCombust(0.99, force: true);
		base.InternalAtmosphere.StateChange();
		base.ProcessedMoles = moleQuantity4 + moleQuantity5;
	}

	public override CanConstructInfo CanConstruct()
	{
		if (HasFrameBelow())
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
	}

	public override void AssessError()
	{
		bool flag = !base.IsInputValid && !base.IsInput2Valid;
		bool flag2 = !base.IsOutputValid;
		bool flag3 = (object)base.ProgrammableChip != null && (CodeErrorState != 0 || base.ProgrammableChip.CompilationError);
		if (GameManager.RunSimulation && HasErrorState && Error == 0 && (flag || flag2 || flag3))
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if ((GameManager.RunSimulation && HasErrorState && Error == 1 && !flag && !flag2 && !flag3) || !OnOff || !Powered)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	private bool _hasInput()
	{
		if (InputNetwork?.Atmosphere == null || !(InputNetwork.Atmosphere.TotalMoles >= AtmosphereHelper.MinimumMolesForProcessing))
		{
			if (InputNetwork2?.Atmosphere != null)
			{
				return InputNetwork2.Atmosphere.TotalMoles >= AtmosphereHelper.MinimumMolesForProcessing;
			}
			return false;
		}
		return true;
	}

	private DelayedActionInstance HandleButtonSetting(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action != InteractableType.Button3 && interactable.Action != InteractableType.Button4)
		{
			return null;
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		Labeller labeller = interaction.SourceSlot.Occupant as Labeller;
		if (!labeller)
		{
			return null;
		}
		delayedActionInstance.ActionMessage = ActionStrings.Set;
		delayedActionInstance.AppendStateMessage(GameStrings.DeviceManualInputWindow);
		if (!labeller.OnOff)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
		}
		if (!labeller.IsOperable)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
		}
		if (!doAction)
		{
			return delayedActionInstance.Succeed();
		}
		labeller.Set(this);
		return delayedActionInstance.Succeed();
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action != InteractableType.Button3 && interactable.Action != InteractableType.Button4)
		{
			return base.InteractWith(interactable, interaction, doAction);
		}
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
		case InteractableType.Button3:
			if (!doAction)
			{
				return delayedActionInstance2.Succeed();
			}
			SettingWheel.PlayWheelSound(this, SettingWheel.Wheel, increaseSetting: true, interaction.AltKey, base.OutputSetting, MinSetting, MaxSetting, WheelSettingIncrement, WheelAltSettingIncrement);
			if (GameManager.RunSimulation)
			{
				base.OutputSetting += (interaction.AltKey ? WheelAltSettingIncrement : WheelSettingIncrement);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button4:
			if (!doAction)
			{
				return delayedActionInstance2.Succeed();
			}
			SettingWheel.PlayWheelSound(this, SettingWheel.Wheel, increaseSetting: false, interaction.AltKey, base.OutputSetting, MinSetting, MaxSetting, WheelSettingIncrement, WheelAltSettingIncrement);
			if (GameManager.RunSimulation)
			{
				base.OutputSetting -= (interaction.AltKey ? WheelAltSettingIncrement : WheelSettingIncrement);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		return interactable.Action switch
		{
			InteractableType.Button3 => GameStrings.GlobalIncrease.DisplayString, 
			InteractableType.Button4 => GameStrings.GlobalDecrease.DisplayString, 
			_ => base.GetContextualName(interactable), 
		};
	}

	protected override StringBuilder GetInfoPanelOperationText()
	{
		StringBuilder infoPanelOperationText = base.GetInfoPanelOperationText();
		infoPanelOperationText.AppendLine(GameStrings.Input1Ratio.AsString(StringManager.Get(Ratio1)));
		infoPanelOperationText.AppendLine(GameStrings.Input2Ratio.AsString(StringManager.Get(Ratio2)));
		if (base.InternalAtmosphere != null)
		{
			infoPanelOperationText.AppendLine(GameStrings.Pressure.DisplayString + " " + base.InternalAtmosphere.PressureGassesAndLiquids.ToFloat().ToStringPrefix("kPa", "yellow"));
			infoPanelOperationText.AppendLine(GameStrings.Temperature.DisplayString + " " + base.InternalAtmosphere.Temperature.ToFloat().ToStringPrefix("K", "yellow"));
		}
		return infoPanelOperationText;
	}
}
