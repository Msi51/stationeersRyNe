using Assets.Scripts.Atmospherics;
using Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class SprayCan : Consumable, ISprayer, IUsedAmount, IUsed
{
	[Header("Spray Can")]
	public Material PaintMaterial;

	private static readonly GasMixture PollutionMixture = new GasMixture(new Mole(Chemistry.GasType.Pollutant, new MoleQuantity(0.009999999776482582), new MoleEnergy(73.0)));

	public override int ConstructingSoundHash => Animator.StringToHash("SprayPaintLong");

	public override int FinishedConstructingSoundHash => Animator.StringToHash("SprayPaintFinished");

	public float TimeToUse()
	{
		return 0.5f;
	}

	public Material GetPaintMaterial()
	{
		return PaintMaterial;
	}

	public override bool UseDefaultUiUsingSounds()
	{
		return false;
	}

	public override bool OnUseItem(float quantity, Thing onUseThing)
	{
		base.Quantity -= quantity;
		AtmosphericEventInstance.CloneGlobalAddGasMix(base.WorldGrid, PollutionMixture);
		return true;
	}
}
