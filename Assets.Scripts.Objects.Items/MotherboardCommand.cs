using Assets.Scripts.Networking;

namespace Assets.Scripts.Objects.Items;

public struct MotherboardCommand(int commandId, long motherboardId, long referenceId, int referenceInt, string text)
{
	private int _commandId = commandId;

	private long _motherboardId = motherboardId;

	private long _referenceId = referenceId;

	private int _referenceInt = referenceInt;

	private string _text = text;

	public void Execute()
	{
		if (Thing.TryFind(_motherboardId, out Motherboard thing))
		{
			Thing reference = Thing.Find(_referenceId);
			thing.MotherboardCommand(_commandId, reference, _referenceInt, _text);
		}
	}

	public void Deserialize(RocketBinaryReader reader)
	{
		_commandId = reader.ReadInt32();
		_motherboardId = reader.ReadInt64();
		_referenceId = reader.ReadInt64();
		_referenceInt = reader.ReadInt32();
		if (reader.ReadBoolean())
		{
			_text = reader.ReadString();
		}
	}

	public void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt32(_commandId);
		writer.WriteInt64(_motherboardId);
		writer.WriteInt64(_referenceId);
		writer.WriteInt32(_referenceInt);
		bool flag = !string.IsNullOrEmpty(_text);
		writer.WriteBoolean(flag);
		if (flag)
		{
			writer.WriteString(_text);
		}
	}
}
