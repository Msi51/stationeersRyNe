using Assets.Scripts.Networking;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class ElectricDeepMiner : DeepMiner
{
	private float _motorRpm;

	private const float SPIN_UP = 1.5f;

	private const float SPIN_DOWN = 4f;

	protected override Slot ProgrammableChipSlot => null;

	private float MotorRpm
	{
		get
		{
			return _motorRpm;
		}
		set
		{
			_motorRpm = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 16384;
			}
		}
	}

	public override float Rpm => MotorRpm;

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (base.IsStructureCompleted && OnOff && Powered && Error == 0)
		{
			if (MotorRpm < 200f)
			{
				MotorRpm = Mathf.Min(200f, MotorRpm + 1.5f);
			}
		}
		else if (MotorRpm > 0f)
		{
			MotorRpm = Mathf.Max(0f, MotorRpm - 4f);
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte((byte)MotorRpm);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		MotorRpm = (int)reader.ReadByte();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteByte((byte)MotorRpm);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			MotorRpm = (int)reader.ReadByte();
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is ElectricDeepMinerSaveData electricDeepMinerSaveData)
		{
			electricDeepMinerSaveData.Rpm = MotorRpm;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new ElectricDeepMinerSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is ElectricDeepMinerSaveData electricDeepMinerSaveData)
		{
			MotorRpm = electricDeepMinerSaveData.Rpm;
		}
	}
}
