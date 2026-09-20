using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Networks;
using Objects.Rockets;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class Transformer : ElectricalInputOutput, ISetable, ILogicable, IReferencable, IEvaluable, IRocketInternals, IRocketComponent
{
	[Tooltip("The needle (required)")]
	public GameObject Needle;

	[Tooltip("Minimum degrees rotation on local Y")]
	public float NeedleMinimum = -160f;

	[Tooltip("Maximum degrees rotation on local Y")]
	public float NeedleMaximum = 160f;

	public float OutputMaximum = 10000f;

	public float StepSmall = 100f;

	public float StepNormal = 1000f;

	private float _outputSetting;

	private Transform _needleTransform;

	private Quaternion _needleBaseRotation;

	private float _powerProvided;

	private float _needleRotation;

	[SerializeField]
	private RocketInternalCellType _rocketInternalCellType;

	[SerializeField]
	private bool _strictlyInternal;

	[ByteArraySync]
	public double Setting
	{
		get
		{
			return _outputSetting;
		}
		set
		{
			_outputSetting = Mathf.Clamp((float)value, 0f, OutputMaximum);
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
			SetKnob();
		}
	}

	protected override float EnergyToHeatRatio => 0.05f;

	public RocketInternalCellType InternalCellType => _rocketInternalCellType;

	public bool StrictlyInternal => _strictlyInternal;

	public RocketNetwork RocketNetwork { get; set; }

	public override bool DoSubmergableTick => true;

	protected override bool CanShortOut
	{
		get
		{
			if (OnOff)
			{
				return Powered;
			}
			return false;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteDouble(Setting);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			Setting = reader.ReadDouble();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteDouble(Setting);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Setting = reader.ReadDouble();
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.CableCategory);
	}

	public DelayedActionInstance HandleButtonSetting(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action != InteractableType.Button1 && interactable.Action != InteractableType.Button2)
		{
			return base.InteractWith(interactable, interaction, doAction);
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		Labeller labeller = interaction.SourceSlot.Occupant as Labeller;
		if ((bool)labeller)
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
		return null;
	}

	public override void Awake()
	{
		base.Awake();
		_needleTransform = Needle.transform;
		_needleBaseRotation = _needleTransform.localRotation;
		SetKnob();
	}

	public override bool AllowSetPower(CableNetwork cableNetwork)
	{
		if (InputNetwork == cableNetwork)
		{
			return true;
		}
		return false;
	}

	public override void UsePower(CableNetwork cableNetwork, float powerUsed)
	{
		if (Error != 1 && OnOff && cableNetwork == OutputNetwork)
		{
			_powerProvided += powerUsed;
		}
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		if ((InputNetwork == null || cableNetwork == InputNetwork) && OnOff && InputNetwork != null)
		{
			_powerProvided -= powerAdded;
		}
	}

	public override float GetUsedPower([NotNull] CableNetwork cableNetwork)
	{
		if (InputNetwork == null || OutputNetwork == null || cableNetwork != InputNetwork)
		{
			return 0f;
		}
		if (Error == 1)
		{
			if (!OnOff)
			{
				return 0f;
			}
			return UsedPower;
		}
		if (!OnOff)
		{
			return 0f;
		}
		return Mathf.Min((float)Setting + UsedPower, _powerProvided);
	}

	public override float GetGeneratedPower(CableNetwork cableNetwork)
	{
		if (OutputNetwork == null || Error == 1 || cableNetwork != OutputNetwork)
		{
			return 0f;
		}
		if (!OnOff || InputNetwork == null)
		{
			return 0f;
		}
		return Mathf.Min((float)Setting, InputNetwork.PotentialLoad);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Setting || logicType - 23 <= LogicType.Power)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Setting => Setting, 
			LogicType.Maximum => OutputMaximum, 
			LogicType.Ratio => Setting / (double)OutputMaximum, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		if (logicType == LogicType.Setting)
		{
			Setting = (float)value.Clamp(0.0, OutputMaximum);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new TransformerSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is TransformerSaveData transformerSaveData)
		{
			transformerSaveData.OutputSetting = (float)Setting;
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is TransformerSaveData transformerSaveData)
		{
			Setting = transformerSaveData.OutputSetting;
		}
		SetKnob();
	}

	private async UniTaskVoid SetKnobFromThread()
	{
		await UniTask.SwitchToMainThread();
		SetKnob();
	}

	public void SetKnob()
	{
		if (ThreadedManager.IsThread)
		{
			SetKnobFromThread().Forget();
		}
		else if (!(_needleTransform == null))
		{
			_needleRotation = Mathf.Lerp(NeedleMinimum, NeedleMaximum, (float)Setting / OutputMaximum);
			_needleTransform.localRotation = _needleBaseRotation;
			_needleTransform.Rotate(0f, _needleRotation, 0f, Space.Self);
		}
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
		delayedActionInstance2.AppendStateMessage(GameStrings.OutputWatts, StringManager.Get((int)Setting));
		delayedActionInstance2.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
		delayedActionInstance2.AppendStateMessage(GameStrings.UseLabelerToSet);
		double num = Setting;
		switch (interactable.Action)
		{
		case InteractableType.Button2:
			delayedActionInstance2.ActionMessage = GameStrings.GlobalIncrease.AsString();
			if (doAction && num < (double)OutputMaximum)
			{
				PlayPooledAudioSound(Defines.Sounds.DialTurn, _needleTransform.localPosition);
			}
			if (GameManager.RunSimulation && doAction)
			{
				if (num < (double)OutputMaximum)
				{
					num += (double)(interaction.AltKey ? StepSmall : StepNormal);
				}
				Setting = Mathf.Min((float)num, OutputMaximum);
			}
			return delayedActionInstance2.Succeed();
		case InteractableType.Button1:
			delayedActionInstance2.ActionMessage = GameStrings.GlobalDecrease.AsString();
			if (doAction && num > 0.0)
			{
				PlayPooledAudioSound(Defines.Sounds.DialTurn, _needleTransform.localPosition);
			}
			if (GameManager.RunSimulation && doAction)
			{
				if (num > 0.0)
				{
					num -= (double)(interaction.AltKey ? StepSmall : StepNormal);
				}
				Setting = Mathf.Max((float)num, 0f);
			}
			return delayedActionInstance2.Succeed();
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	private void CheckError()
	{
		if (GameManager.RunSimulation)
		{
			if (!IsOperable && Error == 0)
			{
				OnServer.Interact(base.InteractError, 1, skipAnimation: true);
			}
			else if (IsOperable && Error == 1)
			{
				OnServer.Interact(base.InteractError, 0, skipAnimation: true);
			}
		}
	}

	public override void OnAddCableNetwork(CableNetwork newNetwork)
	{
		base.OnAddCableNetwork(newNetwork);
		CheckError();
	}

	public override void OnRemoveCableNetwork(CableNetwork oldNetwork)
	{
		base.OnRemoveCableNetwork(oldNetwork);
		CheckError();
	}

	private async UniTaskVoid CheckStateNextFrame()
	{
		await UniTask.NextFrame();
		CheckError();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.RunSimulation && interactable.Action == InteractableType.OnOff && interactable.State == 1)
		{
			CheckStateNextFrame().Forget();
		}
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}
}
