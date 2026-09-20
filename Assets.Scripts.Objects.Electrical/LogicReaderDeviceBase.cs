using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Objects.Electrical;

public class LogicReaderDeviceBase : LogicReaderBase
{
	private Device _currentDevice;

	protected long _savedId;

	public Device CurrentDevice
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
				this.ReaderOnSettingsChangeEvent?.Invoke();
				LogicChanged();
			}
		}
	}

	public event Event ReaderOnSettingsChangeEvent;

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt64(CurrentDevice?.ReferenceId ?? 0);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_savedId = reader.ReadInt64();
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			CurrentDevice = Thing.Find<Device>(reader.ReadInt64());
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteInt64(CurrentDevice?.ReferenceId ?? 0);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		CurrentDevice = Thing.Find<Device>(_savedId);
	}
}
