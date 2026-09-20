using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using CharacterCustomisation;

namespace Assets.Scripts.Networking;

public class BlendShapeEmoteMessage : ProcessedMessage<BlendShapeEmoteMessage>
{
	public long HumanNetId;

	public BlendShapeType BlendShapeType;

	public float Intensity;

	public int Duration;

	public override void Process(long hostId)
	{
		Human human = Thing.Find<Human>(HumanNetId);
		if ((bool)human)
		{
			if ((bool)InventoryManager.Parent && InventoryManager.Parent.ReferenceId != HumanNetId)
			{
				human.CosmeticsBehaviour.SetExpression(BlendShapeType, tween: true, Duration, Intensity);
			}
			if (NetworkManager.IsServer)
			{
				NetworkServer.SendToClients(this, NetworkChannel.GeneralTraffic, -1L);
			}
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		HumanNetId = reader.ReadInt64();
		BlendShapeType = (BlendShapeType)reader.ReadByte();
		Intensity = reader.ReadSingle();
		Duration = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(HumanNetId);
		writer.WriteByte((byte)BlendShapeType);
		writer.WriteSingle(Intensity);
		writer.WriteInt32(Duration);
	}
}
