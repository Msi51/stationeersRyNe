using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Networking;

public class UseStackableMessage : ProcessedMessage<UseStackableMessage>
{
	public long ReferenceId;

	public int QuantityUsed;

	public override void Process(long hostId)
	{
		Stackable stackable = Thing.Find<Stackable>(ReferenceId);
		QuantityUsed = Mathf.Min(QuantityUsed, stackable.Quantity);
		if (stackable.StackedGeneCollections != null && stackable.StackedGeneCollections.Count - QuantityUsed >= 0)
		{
			stackable.StackedGeneCollections.RemoveRange(stackable.StackedGeneCollections.Count - QuantityUsed, QuantityUsed);
		}
		stackable.Quantity -= QuantityUsed;
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ReferenceId = reader.ReadInt64();
		QuantityUsed = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ReferenceId);
		writer.WriteInt32(QuantityUsed);
	}
}
