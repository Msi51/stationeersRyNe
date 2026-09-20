using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;

namespace Assets.Scripts.Networking;

public class SetRecipeMessage : ProcessedMessage<SetRecipeMessage>
{
	public long TargetId;

	public int RecipeIndex;

	public override void Process(long hostId)
	{
		SimpleFabricatorBase simpleFabricatorBase = Thing.Find<SimpleFabricatorBase>(TargetId);
		if (simpleFabricatorBase == null)
		{
			ConsoleWindow.PrintError($"SetRecipeMessage: fabricator #{TargetId} not found");
		}
		else
		{
			simpleFabricatorBase.CurrentIndex = RecipeIndex;
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		TargetId = reader.ReadInt64();
		RecipeIndex = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(TargetId);
		writer.WriteInt32(RecipeIndex);
	}
}
