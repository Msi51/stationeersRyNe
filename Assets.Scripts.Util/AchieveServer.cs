using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;

namespace Assets.Scripts.Util;

public struct AchieveServer : ISyncListable
{
	public Achievements.Kind Kind;

	public Human ParentHuman;

	public void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteByte((byte)Kind);
		Network.WritePackedId(writer, ParentHuman);
	}

	public static void DeserializeNew(RocketBinaryReader reader)
	{
		Create(reader).Execute();
	}

	public static AchieveServer Create(RocketBinaryReader reader)
	{
		Achievements.Kind kind = (Achievements.Kind)reader.ReadByte();
		Network.ReadPackedId(reader, out var referenceId);
		Human parentHuman = Referencable.Find<Human>(referenceId);
		return new AchieveServer
		{
			Kind = kind,
			ParentHuman = parentHuman
		};
	}

	public void Execute()
	{
		if (ParentHuman == null || InventoryManager.ParentHuman == ParentHuman)
		{
			Achievements.Achieve(Kind);
		}
	}
}
