namespace Assets.Scripts.Networking;

public class FromServerClientLaunchRocket : ProcessedMessage<FromServerClientLaunchRocket>
{
	public long CommandModuleID;

	public override void Process(long hostId)
	{
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
	}
}
