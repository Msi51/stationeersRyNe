using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Util;
using Effects;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class DiodeSlide : Diode
{
	[SerializeField]
	private Transform bulb1Transform;

	[SerializeField]
	private MaterialChanger bulb1;

	[SerializeField]
	private MaterialChanger bulb2;

	private double _setting;

	public double Setting
	{
		get
		{
			return _setting;
		}
		set
		{
			if (!RocketMath.Approximately(_setting, value))
			{
				_setting = value;
				if (NetworkManager.IsServer && NetworkServer.HasClients())
				{
					base.NetworkUpdateFlags |= 256;
				}
				if (ThreadedManager.IsThread)
				{
					UnityMainThreadDispatcher.Instance().Enqueue(UpdateBulbTransform);
				}
				else
				{
					UpdateBulbTransform();
				}
			}
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

	private void UpdateBulbTransform()
	{
		float num = Mathf.Clamp01((float)Setting);
		if (!float.IsNaN(num))
		{
			bulb1Transform.localScale = new Vector3(num, 1f, 1f);
		}
	}

	protected override void ToggleLightsOn(bool on)
	{
		bulb1.ChangeState(on ? Defines.Animator.On : Defines.Animator.Off);
		bulb2.ChangeState(on ? Defines.Animator.On : Defines.Animator.Off);
		SetCustomColor(on);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new DiodeSlideSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is DiodeSlideSaveData diodeSlideSaveData)
		{
			Setting = diodeSlideSaveData.Setting;
		}
		SetCustomColor(OnOff);
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is DiodeSlideSaveData diodeSlideSaveData)
		{
			diodeSlideSaveData.Setting = Setting;
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
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
		if (logicType == LogicType.Setting)
		{
			return Setting;
		}
		return base.GetLogicValue(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		if (logicType == LogicType.Setting)
		{
			Setting = value;
		}
	}
}
