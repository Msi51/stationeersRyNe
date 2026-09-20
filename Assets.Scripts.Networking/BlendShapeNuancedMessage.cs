using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using CharacterCustomisation;

namespace Assets.Scripts.Networking;

public class BlendShapeNuancedMessage : ProcessedMessage<BlendShapeNuancedMessage>
{
	public long HumanNetId;

	public float Happy;

	public float Angry;

	public float Open;

	public float Surprised;

	public float None;

	public float Dead;

	public int Duration;

	public override void Process(long hostId)
	{
		Human human = Thing.Find<Human>(HumanNetId);
		if ((bool)human)
		{
			if ((bool)InventoryManager.Parent && InventoryManager.Parent.ReferenceId != HumanNetId)
			{
				human.CosmeticsBehaviour.SetExpression(BlendShapeType.Happy, tween: false, Duration, Happy);
				human.CosmeticsBehaviour.SetExpression(BlendShapeType.Angry, tween: false, Duration, Angry);
				human.CosmeticsBehaviour.SetExpression(BlendShapeType.Open, tween: false, Duration, Open);
				human.CosmeticsBehaviour.SetExpression(BlendShapeType.Surprised, tween: false, Duration, Surprised);
				human.CosmeticsBehaviour.SetExpression(BlendShapeType.None, tween: false, Duration, None);
				human.CosmeticsBehaviour.SetExpression(BlendShapeType.Dead, tween: false, Duration, Dead);
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
		Happy = reader.ReadSingle();
		Angry = reader.ReadSingle();
		Open = reader.ReadSingle();
		Surprised = reader.ReadSingle();
		None = reader.ReadSingle();
		Dead = reader.ReadSingle();
		Duration = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(HumanNetId);
		writer.WriteSingle(Happy);
		writer.WriteSingle(Angry);
		writer.WriteSingle(Open);
		writer.WriteSingle(Surprised);
		writer.WriteSingle(None);
		writer.WriteSingle(Dead);
		writer.WriteInt32(Duration);
	}
}
