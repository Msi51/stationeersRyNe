using System.Collections.Generic;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class ToolBelt : WearableItem
{
	protected List<Item> _toolBeltTools;

	public int SlotCount = 8;

	public MeshRenderer SuitRenderer;

	public MeshRenderer HumanRenderer;

	private new const float RENDER_DISTANCE = 100f;

	protected override float GetRenderMaxDistanceSquared()
	{
		if (base.ParentSlot != null && base.ParentHuman?.HelmetSlot == base.ParentSlot)
		{
			return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
		}
		return base.GetRenderMaxDistanceSquared();
	}

	public void SwapMesh(bool suitEquip)
	{
		if (!(HumanRenderer == null))
		{
			HumanRenderer.material = SuitRenderer.material;
			HumanRenderer.enabled = !suitEquip;
			SuitRenderer.enabled = suitEquip;
		}
	}

	public void CheckSuitSlot()
	{
		if (base.ParentSlot?.Parent is Human human)
		{
			SwapMesh(human.SuitSlot?.Occupant);
		}
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		SwapMesh(suitEquip: true);
	}

	public override void OnEnterInventory(Thing oldParent)
	{
		base.OnEnterInventory(oldParent);
		CheckSuitSlot();
		if ((bool)HumanRenderer)
		{
			Slot parentSlot = base.ParentSlot;
			if (parentSlot != null && parentSlot.HidesOccupant)
			{
				HumanRenderer.enabled = false;
			}
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.PersonalToolbelt);
	}
}
