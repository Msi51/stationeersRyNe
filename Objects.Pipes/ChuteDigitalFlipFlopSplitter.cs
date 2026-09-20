using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Effects;
using UnityEngine;

namespace Objects.Pipes;

public class ChuteDigitalFlipFlopSplitter : ChuteDevice
{
	[SerializeField]
	private Knob _knob;

	[SerializeField]
	private MaterialChanger _arrowVertical;

	[SerializeField]
	private MaterialChanger _arrowHorizontal;

	private ChuteOpenEnd _output1OpenEnd;

	private ChuteOpenEnd _output2OpenEnd;

	private int _setting = 1;

	private int _setting2 = 1;

	private int _quantity;

	private static readonly int _blockedMaterial = Animator.StringToHash("Blocked");

	private static readonly int _flowingMaterial = Animator.StringToHash("Flowing");

	private static readonly int _maxSetting = 300;

	private static readonly int _minSetting = 0;

	private int Quantity
	{
		get
		{
			return _quantity;
		}
		set
		{
			_quantity = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
		}
	}

	private int Setting
	{
		get
		{
			return _setting;
		}
		set
		{
			value = Mathf.Clamp(value, _minSetting, _maxSetting);
			if (value != _setting)
			{
				if (NetworkManager.IsServer && NetworkServer.HasClients())
				{
					base.NetworkUpdateFlags |= 256;
				}
				_knob.SetKnob(Setting, _maxSetting).Forget();
			}
			_setting = value;
		}
	}

	private int Setting2
	{
		get
		{
			return _setting2;
		}
		set
		{
			value = Mathf.Clamp(value, _minSetting, _maxSetting);
			if (NetworkManager.IsServer && NetworkServer.HasClients() && value != _setting2)
			{
				base.NetworkUpdateFlags |= 256;
			}
			_setting2 = value;
		}
	}

	private void IncrementPassedThroughCount()
	{
		Quantity++;
		int num = ((Mode == 0) ? Setting : _setting2);
		if (num != 0 && Quantity >= num)
		{
			Quantity = 0;
			OnServer.Interact(base.InteractMode, 1 - Mode);
		}
	}

	private void ResetPassedThroughCount()
	{
		Quantity = 0;
		OnServer.Interact(base.InteractMode, 1 - Mode);
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if ((bool)base.TransportSlot.Occupant && NextTickMove != OcclusionManager.LastOnServerTick && OnOff && Powered && MoveItem((Mode == 0) ? _output1OpenEnd : _output2OpenEnd))
		{
			IncrementPassedThroughCount();
		}
	}

	private bool MoveItem(ChuteOpenEnd openEnd)
	{
		SmallGrid chuteOrDevice = openEnd.Connection.GetChuteOrDevice();
		if ((bool)chuteOrDevice)
		{
			if (chuteOrDevice is IChute chute)
			{
				if ((bool)chute.TransportSlot.Occupant)
				{
					return false;
				}
				if (!Chute.IsValidInputConnection(chute.SmallGridOpenEnds, this))
				{
					return false;
				}
				chute.SetNeighbor(this);
				OnServer.MoveToSlot(base.TransportSlot.Occupant, chute.TransportSlot);
				return true;
			}
			return false;
		}
		OnServer.MoveToWorld(base.TransportSlot.Occupant, openEnd.DropPosition, ThingTransform.rotation, openEnd.DropVelocity, Random.insideUnitSphere);
		return true;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		switch (interactable.Action)
		{
		case InteractableType.Button1:
			delayedActionInstance.ActionMessage = GameStrings.GlobalDecrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.RatioTooltip, StringManager.Get(Setting), StringManager.Get(_setting2));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			_knob.PlayKnobSound(Setting, increase: false, _maxSetting);
			Setting = Mathf.Max(Setting - ((!interaction.AltKey) ? 1 : 10), 0);
			_knob.SetKnob(Setting, _maxSetting).Forget();
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button2:
			delayedActionInstance.ActionMessage = GameStrings.GlobalIncrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.RatioTooltip, StringManager.Get(Setting), StringManager.Get(_setting2));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			_knob.PlayKnobSound(Setting, increase: true, _maxSetting);
			Setting = Mathf.Min(Setting + ((!interaction.AltKey) ? 1 : 10), _maxSetting);
			_knob.SetKnob(Setting, _maxSetting).Forget();
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button3:
			delayedActionInstance.ActionMessage = GameStrings.ToggleMode.DisplayString;
			delayedActionInstance.AppendStateMessage(GameStrings.QuantityAndRatioTooltip, StringManager.Get(Quantity), StringManager.Get(Setting), StringManager.Get(Setting2));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			ResetPassedThroughCount();
			return DelayedActionInstance.Success(interactable.ContextualName);
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		InteractableType action = interactable.Action;
		if (action == InteractableType.Button1 || action == InteractableType.Button2)
		{
			return GameStrings.Ratio.DisplayString;
		}
		return base.GetContextualName(interactable);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Mode)
		{
			ChangeMaterials();
		}
	}

	private void ChangeMaterials()
	{
		bool flag = Mode == 0;
		_arrowHorizontal.ChangeState(flag ? _flowingMaterial : _blockedMaterial);
		_arrowVertical.ChangeState(flag ? _blockedMaterial : _flowingMaterial);
	}

	public override void Awake()
	{
		base.Awake();
		_knob.Initialize(this);
		_output1OpenEnd = InitOpenEnd(ConnectionRole.Output);
		_output2OpenEnd = InitOpenEnd(ConnectionRole.Output2);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Setting => true, 
			LogicType.SettingOutput => true, 
			LogicType.Quantity => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Setting => true, 
			LogicType.SettingOutput => true, 
			LogicType.Quantity => true, 
			_ => base.CanLogicWrite(logicType), 
		};
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		switch (logicType)
		{
		case LogicType.Setting:
			Setting = (int)value;
			break;
		case LogicType.SettingOutput:
			Setting2 = (int)value;
			break;
		case LogicType.Quantity:
			Quantity = (int)value;
			break;
		}
		base.SetLogicValue(logicType, value);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Setting => Setting, 
			LogicType.SettingOutput => Setting2, 
			LogicType.Quantity => Quantity, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteUInt16((ushort)Setting);
			writer.WriteUInt16((ushort)Setting2);
		}
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteUInt16((ushort)Quantity);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			Setting = reader.ReadUInt16();
			Setting2 = reader.ReadUInt16();
		}
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			Quantity = reader.ReadUInt16();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteUInt16((ushort)Setting);
		writer.WriteUInt16((ushort)Setting2);
		writer.WriteUInt16((ushort)Quantity);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Setting = reader.ReadUInt16();
		Setting2 = reader.ReadUInt16();
		Quantity = reader.ReadUInt16();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new ChuteDigitalFlipFlopSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData thingSaveData)
	{
		base.DeserializeSave(thingSaveData);
		if (thingSaveData is ChuteDigitalFlipFlopSaveData chuteDigitalFlipFlopSaveData)
		{
			Setting = chuteDigitalFlipFlopSaveData.Setting;
			Setting2 = chuteDigitalFlipFlopSaveData.Setting2;
			Quantity = chuteDigitalFlipFlopSaveData.Quantity;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData thingSaveData)
	{
		base.InitialiseSaveData(ref thingSaveData);
		if (thingSaveData is ChuteDigitalFlipFlopSaveData chuteDigitalFlipFlopSaveData)
		{
			chuteDigitalFlipFlopSaveData.Setting = Setting;
			chuteDigitalFlipFlopSaveData.Setting2 = Setting2;
			chuteDigitalFlipFlopSaveData.Quantity = Quantity;
		}
	}
}
