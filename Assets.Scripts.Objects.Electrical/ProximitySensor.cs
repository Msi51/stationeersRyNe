using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class ProximitySensor : Sensor, IDoorControl
{
	private int _setting = 2;

	private static int MaxSetting = 250;

	[SerializeField]
	private Knob knob;

	public override bool IsTriggered => Activate > 0;

	public int Setting
	{
		get
		{
			return _setting;
		}
		set
		{
			value = Mathf.Clamp(value, 0, MaxSetting);
			_setting = value;
			knob.SetKnob(Setting, MaxSetting).Forget();
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	public override void Awake()
	{
		base.Awake();
		knob.Initialize(this);
	}

	public override void SetMotherboards(bool isTriggered)
	{
		foreach (Motherboard linkedMotherboard in LinkedMotherboards)
		{
			if (linkedMotherboard is Circuitboard circuitboard && circuitboard.ParentComputer.AsDevice().Powered)
			{
				circuitboard.RemoteToggle(isTriggered);
			}
		}
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
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalRadius, StringManager.Get(Setting));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			knob.PlayKnobSound(Setting, increase: false, MaxSetting);
			Setting = Mathf.Max(Setting - ((!interaction.AltKey) ? 1 : 10), 0);
			knob.SetKnob(Setting, MaxSetting).Forget();
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button2:
			delayedActionInstance.ActionMessage = GameStrings.GlobalIncrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalRadius, StringManager.Get(Setting));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			knob.PlayKnobSound(Setting, increase: true, MaxSetting);
			Setting = Mathf.Min(Setting + ((!interaction.AltKey) ? 1 : 10), MaxSetting);
			knob.SetKnob(Setting, MaxSetting).Forget();
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
			return InterfaceStrings.Radius;
		}
		return base.GetContextualName(interactable);
	}

	public override void OnThreadUpdate()
	{
		base.OnThreadUpdate();
		if (!GameManager.RunSimulation)
		{
			return;
		}
		int num = 0;
		foreach (Human allHuman in Human.AllHumans)
		{
			if (Vector3.SqrMagnitude(allHuman.Position - base.Position) < (float)(Setting * Setting) && IsAuthorized(allHuman))
			{
				num++;
			}
		}
		if (Activate != num)
		{
			OnServer.Interact(base.InteractActivate, num);
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteByte((byte)Setting);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			Setting = reader.ReadByte();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte((byte)Setting);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Setting = reader.ReadByte();
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
			LogicType.Activate => false, 
			_ => base.CanLogicWrite(logicType), 
		};
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (logicType == LogicType.Setting)
		{
			Setting = (int)value;
		}
		base.SetLogicValue(logicType, value);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Setting => Setting, 
			LogicType.Quantity => Activate, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new ProximitySensorSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is ProximitySensorSaveData proximitySensorSaveData)
		{
			proximitySensorSaveData.Setting = Setting;
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is ProximitySensorSaveData proximitySensorSaveData)
		{
			Setting = proximitySensorSaveData.Setting;
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		if (!BaseAnimator && !(MaterialChanger == null))
		{
			MaterialChanger.ChangeState((Activate > 0) ? Defines.Animator.On : Defines.Animator.Off);
		}
	}
}
