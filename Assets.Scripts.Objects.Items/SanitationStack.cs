using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class SanitationStack : Stackable, ISanitation
{
	[Header("Sanitation Stack")]
	[Tooltip("Filled packet created when a folded packet is used")]
	public SanitationPacket PacketPrefab;

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.Sanitation);
	}

	public override DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletedRatio = 1f)
	{
		if (!(RootParent is Human human))
		{
			return base.OnUseSecondary(doAction, actionCompletedRatio);
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 1f,
			ActionMessage = GameStrings.Use
		};
		if (SanitationPacket.HumanChecks(human, delayedActionInstance) != SanitationResult.Allowed)
		{
			return delayedActionInstance.Fail(GameStrings.DefecatePacketFull, ToTooltip());
		}
		if (!doAction || actionCompletedRatio < 1f)
		{
			return delayedActionInstance;
		}
		if (GameManager.RunSimulation)
		{
			UnfoldAndFill(human);
		}
		return delayedActionInstance.Succeed();
	}

	private void UnfoldAndFill(Human human)
	{
		SanitationPacket sanitationPacket = OnServer.Create<SanitationPacket>(PacketPrefab, GetSafeDropPosition(base.Position, human.CharacterRotationY.rotation * Vector3.forward, 0.5f), Rotation);
		if (sanitationPacket == null)
		{
			return;
		}
		sanitationPacket.DestroyAtZero = false;
		sanitationPacket.SetQuantity(0f);
		if (!sanitationPacket.OnUseItem(1f, human))
		{
			OnServer.Destroy(sanitationPacket);
			return;
		}
		Slot slot = PickFreeHand(human);
		if (slot == null && base.Quantity <= 1 && base.ParentSlot != null)
		{
			slot = base.ParentSlot;
			OnServer.MoveToWorld(this, base.Position, Rotation, Vector3.zero, Vector3.zero);
		}
		if (slot != null)
		{
			OnServer.MoveToSlotOrWorld(sanitationPacket, slot);
		}
		DecrementQuantity();
	}

	private Slot PickFreeHand(Human human)
	{
		Slot parentSlot = base.ParentSlot;
		if (parentSlot == human.RightHandSlot || parentSlot == human.LeftHandSlot)
		{
			Slot slot = ((parentSlot == human.RightHandSlot) ? human.LeftHandSlot : human.RightHandSlot);
			if (!slot.IsEmpty())
			{
				return null;
			}
			return slot;
		}
		if (human.RightHandSlot.IsEmpty())
		{
			return human.RightHandSlot;
		}
		if (human.LeftHandSlot.IsEmpty())
		{
			return human.LeftHandSlot;
		}
		return null;
	}
}
