using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Networking;

public class SetLogicFromClient : ProcessedMessage<SetLogicFromClient>
{
	public long LogicId;

	public LogicType LogicType;

	public double Value;

	public override void Process(long hostId)
	{
		if (GameManager.RunSimulation)
		{
			if (!Referencable.Exists<ISetable>(LogicId, out var thing))
			{
				ConsoleWindow.PrintError($"SetLogicFromClient: ISetable #{LogicId} not found");
			}
			else if (thing.CanLogicWrite(LogicType))
			{
				thing.SetLogicValue(LogicType, Value);
			}
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out LogicId);
		LogicType = (LogicType)reader.ReadUInt16();
		Value = reader.ReadDouble();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, LogicId);
		writer.WriteUInt16((ushort)LogicType);
		writer.WriteDouble(Value);
	}
}
