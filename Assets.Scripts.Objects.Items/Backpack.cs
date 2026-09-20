using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Backpack : WearableItem, ILogicable, IReferencable, IEvaluable
{
	private new const float RENDER_DISTANCE = 100f;

	[SerializeField]
	private int slotCount = 9;

	protected override float GetRenderMaxDistanceSquared()
	{
		if (base.ParentSlot != null && base.ParentHuman?.BackpackSlot == base.ParentSlot)
		{
			return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
		}
		return base.GetRenderMaxDistanceSquared();
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.PersonalBackpacks);
	}

	public override CanEnterResult CanEnter(Slot destinationSlot)
	{
		if (destinationSlot.Parent is SuitBase suitBase && destinationSlot == suitBase.BackSlot)
		{
			return suitBase.IsAllowedBackpackPrefab(this);
		}
		return base.CanEnter(destinationSlot);
	}
}
