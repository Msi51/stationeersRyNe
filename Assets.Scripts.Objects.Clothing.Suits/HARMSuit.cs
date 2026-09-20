using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Clothing.Suits;

public class HARMSuit : SuitBase
{
	[SerializeField]
	[ReadOnly]
	protected int filterSlot4;

	public override TemperatureKelvin MaxCoolantTemperatureK => new TemperatureKelvin(323.15);

	public override TemperatureKelvin MinCoolantTemperatureK => Chemistry.FREEZING_TEMPERATURE_NITROGEN_K;

	public override float MaxACEnergy => 1000f;

	public override float EnergyCoolingPowerCostPercent => 0.2f;

	public override float EnergyHeatingPowerCostPercent => 0.7f;

	public override float RadiationFactor => 0.025f;

	public override float MovementSpeedMultiplier => 0.6f;

	public override float StormMovementSpeedMultiplier => 1f;

	public override float ToolSpeedMultiplier => 0.95f;

	public override float SuitVelocityAbsorbed => 16f;

	public override float SuitVelocityScale => 2f;

	public override float BruteDamagePassthroughAsStun => 0f;

	public override float HygieneReductionMultiplier => 2.5f;

	public override float LavaDamage => base.LavaDamage * 0.25f;

	public virtual Slot FilterSlot4 => Slots[filterSlot4];

	public override bool HasFilters => Slot.Contains<GasFilter>(FilterSlot1, FilterSlot2, FilterSlot3, FilterSlot4);

	public override StringBuilder GetSlotTooltip()
	{
		StringBuilder slotTooltip = base.GetSlotTooltip();
		if (base.ParentSlot?.Parent is SuitStorage && (bool)base.CoolantTank)
		{
			slotTooltip.AppendLine(GameStrings.ContainsItemInSlotAtQuantity.AsString(base.CoolantTank.ToTooltip(), base.CoolantTankSlot.ToTooltip(), base.CoolantTank.GetQuantityText().AsColor("yellow")));
		}
		return slotTooltip;
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (GameManager.RunSimulation && base.InternalAtmosphere != null)
		{
			base.InternalAtmosphere.Volume = base.Volume;
		}
	}
}
