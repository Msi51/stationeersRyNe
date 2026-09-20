using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Networking;

public class SyncCustomGrowSpeed : ProcessedMessage<SyncCustomGrowSpeed>
{
	public bool CustomGrowSpeed;

	public override void Process(long hostId)
	{
		Plant.CustomPlantGrowthSpeed = CustomGrowSpeed;
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		CustomGrowSpeed = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteBoolean(CustomGrowSpeed);
	}
}
