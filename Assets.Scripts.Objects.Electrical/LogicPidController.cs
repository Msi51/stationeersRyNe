using System;
using System.Text;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using JetBrains.Annotations;

namespace Assets.Scripts.Objects.Electrical;

public class LogicPidController : LogicReader
{
	[CanBeNull]
	private PidController _pidController;

	private double _setpoint;

	private double _processValue;

	private float _proportionalGain = 1f;

	private float _derivativeGain = 0.01f;

	private float _integralGain = 0.1f;

	private float _outputMaximum = float.MinValue;

	private float _outputMinimum = float.MaxValue;

	private static readonly TimeSpan HalfSecond = TimeSpan.FromSeconds(0.5);

	[ByteArraySync]
	public float ProportionalGain
	{
		get
		{
			return _proportionalGain;
		}
		private set
		{
			float proportionalGain = _proportionalGain;
			_proportionalGain = value;
			if (!RocketMath.Approximately(proportionalGain, _proportionalGain) && NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 8192;
			}
			if (_pidController != null)
			{
				_pidController.Kp = _proportionalGain;
			}
		}
	}

	[ByteArraySync]
	public float DerivativeGain
	{
		get
		{
			return _derivativeGain;
		}
		private set
		{
			float derivativeGain = _derivativeGain;
			_derivativeGain = value;
			if (!RocketMath.Approximately(derivativeGain, _derivativeGain) && NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 32768;
			}
			if (_pidController != null)
			{
				_pidController.Kd = _derivativeGain;
			}
		}
	}

	[ByteArraySync]
	public float IntegralGain
	{
		get
		{
			return _integralGain;
		}
		private set
		{
			float integralGain = _integralGain;
			_integralGain = value;
			if (!RocketMath.Approximately(integralGain, _integralGain) && NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 16384;
			}
			if (_pidController != null)
			{
				_pidController.Ki = _integralGain;
			}
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		switch (interactable.Action)
		{
		case InteractableType.Button3:
		case InteractableType.Button5:
		case InteractableType.Button7:
			return GameStrings.GlobalIncrease;
		case InteractableType.Button4:
		case InteractableType.Button6:
		case InteractableType.Button8:
			return GameStrings.GlobalDecrease;
		default:
			return base.GetContextualName(interactable);
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		InteractableType action = interactable.Action;
		if (action != InteractableType.Button3 && action != InteractableType.Button4 && action != InteractableType.Button5 && action != InteractableType.Button6 && action != InteractableType.Button7 && action != InteractableType.Button8)
		{
			return base.InteractWith(interactable, interaction, doAction);
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (interaction.SourceSlot.Occupant is Labeller labeller)
		{
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
		if (!(interaction.SourceSlot.Occupant is Screwdriver))
		{
			return delayedActionInstance.Fail(GameStrings.RequiresScrewdriver);
		}
		if (Handle(doAction, delayedActionInstance, interaction, interactable.Action, InteractableType.Button3, InteractableType.Button4, ref _proportionalGain))
		{
			base.NetworkUpdateFlags |= 8192;
			return delayedActionInstance;
		}
		if (Handle(doAction, delayedActionInstance, interaction, interactable.Action, InteractableType.Button5, InteractableType.Button6, ref _integralGain))
		{
			base.NetworkUpdateFlags |= 16384;
			return delayedActionInstance;
		}
		if (Handle(doAction, delayedActionInstance, interaction, interactable.Action, InteractableType.Button7, InteractableType.Button8, ref _derivativeGain))
		{
			base.NetworkUpdateFlags |= 32768;
			return delayedActionInstance;
		}
		return delayedActionInstance.Fail();
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.LogicProcessorsCategory);
	}

	private bool Handle(bool doAction, DelayedActionInstance result, Interaction interaction, InteractableType action, InteractableType increase, InteractableType decrease, ref float value, float step = 1f)
	{
		if (action != increase && action != decrease)
		{
			return false;
		}
		if (interaction.AltKey)
		{
			step *= 0.1f;
		}
		float num = ((action == increase) ? step : (0f - step));
		float num2 = value + num;
		result.AppendStateMessage(GameStrings.GlobalValue, value.ToStringExact());
		result.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
		result.AppendStateMessage(GameStrings.UseLabelerToSet);
		if (!doAction)
		{
			return true;
		}
		if (!GameManager.RunSimulation)
		{
			return true;
		}
		if (RocketMath.Approximately(num2, Setting))
		{
			return true;
		}
		value = num2;
		PlayNetworkSound(Defines.Sounds.ScrewdriverSound);
		return true;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			writer.WriteFloatHalf(_proportionalGain);
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteFloatHalf(_integralGain);
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			writer.WriteFloatHalf(_derivativeGain);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			ProportionalGain = reader.ReadFloatHalf();
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			IntegralGain = reader.ReadFloatHalf();
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			DerivativeGain = reader.ReadFloatHalf();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteFloatHalf(ProportionalGain);
		writer.WriteFloatHalf(IntegralGain);
		writer.WriteFloatHalf(DerivativeGain);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		ProportionalGain = reader.ReadFloatHalf();
		IntegralGain = reader.ReadFloatHalf();
		DerivativeGain = reader.ReadFloatHalf();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LogicPidControllerSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LogicPidControllerSaveData logicPidControllerSaveData)
		{
			Setting = logicPidControllerSaveData.Setting;
			_setpoint = logicPidControllerSaveData.SetPoint;
			_proportionalGain = logicPidControllerSaveData.ProportionalGain;
			_integralGain = logicPidControllerSaveData.IntegralGain;
			_derivativeGain = logicPidControllerSaveData.DerivativeGain;
			_outputMaximum = logicPidControllerSaveData.OutputMaximum;
			_outputMinimum = logicPidControllerSaveData.OutputMinimum;
			_processValue = logicPidControllerSaveData.ProcessValue;
			_pidController = new PidController(_proportionalGain, _integralGain, _derivativeGain, 1.0, _outputMaximum, _outputMinimum);
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LogicPidControllerSaveData logicPidControllerSaveData)
		{
			logicPidControllerSaveData.Setting = Setting;
			logicPidControllerSaveData.SetPoint = _setpoint;
			logicPidControllerSaveData.ProportionalGain = _proportionalGain;
			logicPidControllerSaveData.IntegralGain = _integralGain;
			logicPidControllerSaveData.DerivativeGain = _derivativeGain;
			logicPidControllerSaveData.OutputMaximum = _outputMaximum;
			logicPidControllerSaveData.OutputMinimum = _outputMinimum;
			logicPidControllerSaveData.ProcessValue = _processValue;
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Setting || logicType == LogicType.Maximum || logicType - 274 <= LogicType.Error)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.ProportionalGain => _proportionalGain, 
			LogicType.DerivativeGain => _derivativeGain, 
			LogicType.IntegralGain => _integralGain, 
			LogicType.Setpoint => _setpoint, 
			LogicType.Maximum => _outputMaximum, 
			LogicType.Minimum => _outputMinimum, 
			LogicType.Setting => Setting, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Maximum || logicType - 274 <= LogicType.Pressure)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (ShowStateTooltip)
		{
			extendedText.Append("Proportional Gain ");
			extendedText.AppendLine(ProportionalGain.ToStringRounded().AsColor("yellow"));
			extendedText.Append("Integral Gain ");
			extendedText.AppendLine(IntegralGain.ToStringRounded().AsColor("yellow"));
			extendedText.Append("Derivative Gain ");
			extendedText.AppendLine(DerivativeGain.ToStringRounded().AsColor("yellow"));
		}
		return extendedText;
	}

	public override void Calculate()
	{
		if (_pidController == null)
		{
			_pidController = new PidController(_proportionalGain, _integralGain, _derivativeGain, 1.0, _outputMaximum, _outputMinimum);
		}
		_processValue = base.CurrentDevice.GetLogicValue(base.LogicType);
		double num = _pidController.Iterate(_setpoint, _processValue, HalfSecond);
		if (!RocketMath.Approximately(num, Setting))
		{
			Setting = num;
		}
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		switch (logicType)
		{
		case LogicType.ProportionalGain:
			_proportionalGain = (float)value;
			if (_pidController != null)
			{
				_pidController.Kp = _proportionalGain;
			}
			break;
		case LogicType.DerivativeGain:
			_derivativeGain = (float)value;
			if (_pidController != null)
			{
				_pidController.Kd = _derivativeGain;
			}
			break;
		case LogicType.IntegralGain:
			_integralGain = (float)value;
			if (_pidController != null)
			{
				_pidController.Ki = _integralGain;
			}
			break;
		case LogicType.Setpoint:
			_setpoint = (float)value;
			break;
		case LogicType.Maximum:
			_outputMaximum = (float)value;
			if (_pidController != null)
			{
				_pidController.OutputUpperLimit = _outputMaximum;
			}
			break;
		case LogicType.Minimum:
			_outputMinimum = (float)value;
			if (_pidController != null)
			{
				_pidController.OutputLowerLimit = _outputMinimum;
			}
			break;
		case LogicType.Reset:
			if (value > 0.0)
			{
				_pidController?.ResetController();
			}
			break;
		default:
			base.SetLogicValue(logicType, value);
			break;
		}
	}
}
