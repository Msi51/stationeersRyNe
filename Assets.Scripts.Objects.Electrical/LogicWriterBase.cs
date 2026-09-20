using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Electrical;

public abstract class LogicWriterBase : LogicUnitBase
{
	private LogicUnitBase _input1;

	protected bool _IsInputDirty;

	private LogicType _logicType;

	private Device _currentDevice;

	public virtual LogicUnitBase Input1
	{
		get
		{
			return _input1;
		}
		set
		{
			_input1 = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 8192;
			}
			if (this.OnInputChanged != null)
			{
				this.OnInputChanged();
			}
			if (this.OnSettingsChangeEvent != null)
			{
				this.OnSettingsChangeEvent();
			}
		}
	}

	public LogicType LogicType
	{
		get
		{
			return _logicType;
		}
		set
		{
			if (_logicType != value)
			{
				_logicType = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 2048;
				}
				LogicChanged();
			}
		}
	}

	public Device CurrentOutput
	{
		get
		{
			return _currentDevice;
		}
		set
		{
			if (!(_currentDevice == value))
			{
				_currentDevice = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 1024;
				}
				LogicChanged();
			}
		}
	}

	public event Event OnInputChanged;

	public event Event OnSettingsChangeEvent;

	public override void OnSettingChanged()
	{
		base.OnSettingChanged();
		if (base.ShouldPlayLogicSound)
		{
			PlayPooledAudioSound(Defines.Sounds.LogicWrite, LogicUnitBase.SoundOffset);
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteUInt16((ushort)LogicType);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		LogicType = (LogicType)reader.ReadUInt16();
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			Network.ReadPackedId(reader, out var referenceId);
			CurrentOutput = Thing.Find<Device>(referenceId);
		}
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			LogicType = (LogicType)reader.ReadUInt16();
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			Network.ReadPackedId(reader, out var referenceId2);
			Input1 = Thing.Find<LogicUnitBase>(referenceId2);
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			Network.WritePackedId(writer, CurrentOutput);
		}
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			writer.WriteUInt16((ushort)LogicType);
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			Network.WritePackedId(writer, Input1);
		}
	}

	protected virtual void _WriteValue()
	{
		if (this.OnSettingsChangeEvent != null)
		{
			this.OnSettingsChangeEvent();
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.LogicWriterCategory);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.ForceWrite)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (logicType == LogicType.ForceWrite)
		{
			if (OnOff && Powered && IsOperable && value > 0.0)
			{
				_IsInputDirty = true;
			}
		}
		else
		{
			base.SetLogicValue(logicType, value);
		}
	}
}
