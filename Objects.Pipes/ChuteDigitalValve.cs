using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Pipes;

public class ChuteDigitalValve : ChuteDevice
{
	[SerializeField]
	private Knob _knob;

	[SerializeField]
	private GameObject _pin;

	[SerializeField]
	private GameObject _shutter;

	private ChuteOpenEnd _outputOpenEnd;

	private int _setting;

	private int _quantity;

	private static readonly int _maxSetting = 300;

	private static readonly int _minSetting = 0;

	private static readonly Vector3 _openPosition = new Vector3(0f, 0f, 0f);

	private static readonly Vector3 _closedPosition = new Vector3(0f, -0.12f, 0f);

	private static readonly Vector3 _openShutterScale = new Vector3(1f, 0.015f, 1f);

	private static readonly Vector3 _closedShutterScale = new Vector3(1f, 1f, 1f);

	private static readonly float _openCloseTime = 0.2f;

	public int Quantity
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

	public int Setting
	{
		get
		{
			return _setting;
		}
		set
		{
			value = Mathf.Clamp(value, _minSetting, _maxSetting);
			_setting = value;
			_knob.SetKnob(Setting, _maxSetting).Forget();
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	private void IncrementPassedThroughCount()
	{
		if (Setting != 0)
		{
			Quantity++;
			if (Quantity >= Setting)
			{
				Quantity = 0;
				OnServer.Interact(base.InteractOpen, 0);
			}
		}
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (!base.TransportSlot.Occupant || NextTickMove == OcclusionManager.LastOnServerTick || !OnOff || !Powered || !IsOpen)
		{
			return;
		}
		SmallGrid chuteOrDevice = _outputOpenEnd.Connection.GetChuteOrDevice();
		if ((bool)chuteOrDevice)
		{
			if (chuteOrDevice is IChute chute)
			{
				if ((bool)chute.TransportSlot.Occupant || !Chute.IsValidInputConnection(chute.SmallGridOpenEnds, this))
				{
					return;
				}
				chute.SetNeighbor(this);
				OnServer.MoveToSlot(base.TransportSlot.Occupant, chute.TransportSlot);
			}
		}
		else
		{
			OnServer.MoveToWorld(base.TransportSlot.Occupant, _outputOpenEnd.DropPosition, ThingTransform.rotation, _outputOpenEnd.DropVelocity, Random.insideUnitSphere);
		}
		IncrementPassedThroughCount();
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
			delayedActionInstance.AppendStateMessage(GameStrings.CloseThresholdToolTip, StringManager.Get(Setting));
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
			delayedActionInstance.AppendStateMessage(GameStrings.CloseThresholdToolTip, StringManager.Get(Setting));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			_knob.PlayKnobSound(Setting, increase: true, _maxSetting);
			Setting = Mathf.Min(Setting + ((!interaction.AltKey) ? 1 : 10), _maxSetting);
			_knob.SetKnob(Setting, _maxSetting).Forget();
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
			return GameStrings.CloseThreshold.DisplayString;
		}
		return base.GetContextualName(interactable);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Open)
		{
			LeanTween.cancel(_pin);
			LeanTween.cancel(_shutter);
			LeanTween.moveLocal(_pin, IsOpen ? _openPosition : _closedPosition, _openCloseTime);
			LeanTween.scale(_shutter, IsOpen ? _openShutterScale : _closedShutterScale, _openCloseTime);
		}
	}

	public override void Awake()
	{
		base.Awake();
		_knob.Initialize(this);
		_outputOpenEnd = InitOpenEnd(ConnectionRole.Output);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Setting => true, 
			LogicType.Quantity => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Setting => true, 
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
		writer.WriteUInt16((ushort)Quantity);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Setting = reader.ReadUInt16();
		Quantity = reader.ReadUInt16();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new ChuteDigitalValveSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData thingSaveData)
	{
		base.DeserializeSave(thingSaveData);
		if (thingSaveData is ChuteDigitalValveSaveData chuteDigitalValveSaveData)
		{
			Setting = chuteDigitalValveSaveData.Setting;
			Quantity = chuteDigitalValveSaveData.Quantity;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData thingSaveData)
	{
		base.InitialiseSaveData(ref thingSaveData);
		if (thingSaveData is ChuteDigitalValveSaveData chuteDigitalValveSaveData)
		{
			chuteDigitalValveSaveData.Setting = Setting;
			chuteDigitalValveSaveData.Quantity = Quantity;
		}
	}
}
