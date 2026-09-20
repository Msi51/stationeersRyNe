using System;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Electrical;

public class LogicStopWatch : LogicUnitBase
{
	private double _offSet;

	private float _startTime;

	private double _time;

	private double _pauseStartTime;

	private bool _pauseStartTimeStored;

	[ByteArraySync]
	private double OffSet
	{
		get
		{
			return _offSet;
		}
		set
		{
			if (NetworkManager.IsServer && !RocketMath.Approximately(_offSet, value, 9.999999747378752E-06))
			{
				base.NetworkUpdateFlags |= 256;
			}
			_offSet = value;
		}
	}

	[ByteArraySync]
	private float StartTime
	{
		get
		{
			return _startTime;
		}
		set
		{
			if (NetworkManager.IsServer && !RocketMath.Approximately(_startTime, value))
			{
				base.NetworkUpdateFlags |= 256;
			}
			_startTime = value;
		}
	}

	private double _pauseOffset => _pauseStartTime - (double)NetworkTime.time;

	public override double Setting
	{
		get
		{
			return _time;
		}
		set
		{
			base.Setting = value;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteDouble(OffSet);
			writer.WriteSingle(StartTime);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			OffSet = reader.ReadDouble();
			StartTime = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteDouble(OffSet);
		writer.WriteSingle(StartTime);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		OffSet = reader.ReadDouble();
		StartTime = reader.ReadSingle();
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (!doAction)
		{
			return delayedActionInstance.Succeed();
		}
		if (GameManager.RunSimulation && interactable.Action == InteractableType.Activate)
		{
			OnServer.Interact(base.InteractActivate, (Activate == 0) ? 1 : 0);
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (!GameManager.RunSimulation)
		{
			return;
		}
		if (interactable == base.InteractOnOff)
		{
			ResetStartTime();
			if (!OnOff)
			{
				_pauseStartTimeStored = false;
			}
			if (OnOff && Activate == 0)
			{
				_pauseStartTime = NetworkTime.time;
				_pauseStartTimeStored = true;
			}
		}
		if (interactable == base.InteractPowered)
		{
			ResetStartTime();
			if (!Powered)
			{
				_pauseStartTimeStored = false;
			}
		}
		if (interactable == base.InteractActivate && OnOff)
		{
			if (interactable.State == 0)
			{
				_pauseStartTime = NetworkTime.time;
				_pauseStartTimeStored = true;
			}
			else if (interactable.State == 1 && _pauseStartTimeStored)
			{
				OffSet += _pauseOffset;
			}
		}
	}

	public override void OnThreadUpdate()
	{
		base.OnThreadUpdate();
		if (OnOff && Powered)
		{
			if (Activate != 0)
			{
				_time = Math.Max((double)(NetworkTime.time - StartTime) + OffSet, 0.0);
			}
			return;
		}
		if (GameManager.RunSimulation && OffSet != 0.0)
		{
			OffSet = 0.0;
		}
		_time = 0.0;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		result.Title = DisplayName;
		foreach (Connection openEnd in OpenEnds)
		{
			if (hitCollider == openEnd.Collider && hitCollider != null)
			{
				return result.Populate(openEnd);
			}
		}
		Localization.Variable1 = _time.ToStringExact();
		result.Extended = InterfaceStrings.LogicState;
		return result;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (!(savedData is LogicStopWatchSaveData logicStopWatchSaveData))
		{
			return;
		}
		OffSet = logicStopWatchSaveData.Offset;
		_time = OffSet;
		if (Powered)
		{
			ResetStartTime();
			if (Activate == 0)
			{
				_pauseStartTime = NetworkTime.time;
				_pauseStartTimeStored = true;
			}
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LogicStopWatchSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LogicStopWatchSaveData logicStopWatchSaveData)
		{
			logicStopWatchSaveData.Offset = _time;
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Time)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Time:
			if (OnOff && Powered)
			{
				return _time;
			}
			return 0.0;
		case LogicType.Setting:
			if (OnOff && Powered)
			{
				return _time;
			}
			return 0.0;
		default:
			return base.GetLogicValue(logicType);
		}
	}

	private void ResetStartTime()
	{
		StartTime = NetworkTime.time;
	}
}
