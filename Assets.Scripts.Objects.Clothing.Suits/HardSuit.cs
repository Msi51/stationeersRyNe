using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects.Clothing.Suits;

public class HardSuit : SuitBase
{
	[SerializeField]
	[ReadOnly]
	protected int filterSlot4;

	public override float MovementSpeedMultiplier => 0.8f;

	public override float SuitVelocityAbsorbed => 12f;

	public override float HygieneReductionMultiplier => 1.5f;

	public override float BruteDamagePassthroughAsStun => 1f;

	public virtual Slot FilterSlot4 => Slots[filterSlot4];

	public override bool HasFilters => Slot.Contains<GasFilter>(FilterSlot1, FilterSlot2, FilterSlot3, FilterSlot4);
}
