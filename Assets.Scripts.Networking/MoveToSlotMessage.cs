using System;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using UnityEngine;

namespace Assets.Scripts.Networking;

public class MoveToSlotMessage : ProcessedMessage<MoveToSlotMessage>
{
	public long ChildId;

	public long ParentId;

	public int SlotId;

	public bool Drag;

	public Vector3 Offset;

	public int InteractionIndex;

	public override void Process(long hostId)
	{
		DynamicThing childThing = Thing.Find<DynamicThing>(ChildId);
		Thing parentThing = Thing.Find<Thing>(ParentId);
		if (childThing == null || parentThing == null)
		{
			if (!GameManager.RunSimulation)
			{
				DeferredMessageQueue.DeferUntilExists(this, hostId, ChildId, ParentId, 3f, "MoveToSlotMessage");
			}
		}
		else if (Drag)
		{
			childThing.DragInSlot(parentThing.Slots[SlotId], Offset);
			if (GameManager.RunSimulation && childThing is Human)
			{
				NetworkServer.SendToClients(this, NetworkChannel.GeneralTraffic, -1L);
			}
		}
		else if (InventoryManager.Instance == null)
		{
			InventoryManager.OnInitialize = (InventoryManager.Event)Delegate.Combine(InventoryManager.OnInitialize, (InventoryManager.Event)delegate
			{
				childThing.MoveToSlot(parentThing.Slots[SlotId], parentThing);
			});
		}
		else
		{
			OnServer.MoveToSlot(childThing, parentThing.Slots[SlotId]);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ChildId = reader.ReadInt64();
		ParentId = reader.ReadInt64();
		SlotId = reader.ReadInt32();
		Drag = reader.ReadBoolean();
		Offset = reader.ReadVector3();
		InteractionIndex = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ChildId);
		writer.WriteInt64(ParentId);
		writer.WriteInt32(SlotId);
		writer.WriteBoolean(Drag);
		writer.WriteVector3(Offset);
		writer.WriteInt32(InteractionIndex);
	}
}
