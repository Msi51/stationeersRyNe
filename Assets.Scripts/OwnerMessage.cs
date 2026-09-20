using Assets.Scripts.Networking;
using Assets.Scripts.Objects;

namespace Assets.Scripts;

public class OwnerMessage : ProcessedMessage<OwnerMessage>
{
	public override void Deserialize(RocketBinaryReader reader)
	{
		if (reader.ReadBoolean())
		{
			Network.ReadPackedId(reader, out var referenceId);
			Thing.Find<Thing>(referenceId)?.ProcessOwnerUpdate(reader);
			if (reader.ReadBoolean())
			{
				Network.ReadPackedId(reader, out var referenceId2);
				Thing.Find<Thing>(referenceId2)?.ProcessOwnerUpdate(reader);
			}
		}
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		bool flag = NetworkClient.OwnerHuman;
		writer.WriteBoolean(flag);
		if (flag)
		{
			Network.WritePackedId(writer, NetworkClient.OwnerHuman);
			NetworkClient.OwnerHuman.BuildOwnerUpdate(writer);
			Slot parentSlot = NetworkClient.OwnerHuman.ParentSlot;
			bool flag2 = parentSlot != null && parentSlot.Parent is IPlayerVehicle;
			writer.WriteBoolean(flag2);
			if (flag2)
			{
				Network.WritePackedId(writer, NetworkClient.OwnerHuman.ParentSlot.Parent.ReferenceId);
				NetworkClient.OwnerHuman.ParentSlot.Parent.BuildOwnerUpdate(writer);
			}
		}
	}
}
