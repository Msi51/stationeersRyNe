using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Items;

public class Stomach : Organ
{
	[SerializeField]
	[FormerlySerializedAs("Volume")]
	private float volume = 10f;

	private const float CONVERSION_MINUTES = 30f;

	private const float BODY_HEAT_RATE = 0.01f;

	private static readonly MoleQuantity AcidTargetQuantity = new MoleQuantity(1.0);

	private static readonly MoleQuantity AcidPerTickQuantity = new MoleQuantity(0.009999999776482582);

	private const float WASTE_CAPACITY = 0.5f;

	public VolumeLitres Volume => new VolumeLitres(volume);

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			Atmosphere atmosphere = (base.InternalAtmosphere = new Atmosphere(this, Volume, 0L));
		}
	}

	public override void OnLifeTick()
	{
		if (base.InternalAtmosphere == null)
		{
			return;
		}
		EqualizeToBodyTemperature();
		if (!DifficultySetting.Current.Sanitation || ParentEntity?.RootParent is ILifeSuspender { IsSuspendingLife: not false })
		{
			return;
		}
		SecreteAcid();
		if (GetWasteRatio() >= 1f)
		{
			WetSelf();
			return;
		}
		Mole water = base.InternalAtmosphere.GasMixture.Water;
		if (!(water.Quantity <= MoleQuantity.Zero))
		{
			Brain brain = ParentEntity?.OrganBrain;
			float num = (((object)brain != null && brain.IsOnline) ? 1f : ((float)DifficultySetting.Current.OfflineMetabolism));
			double num2 = (double)Volume.ToFloat() / 0.018;
			float num3 = 1f * GameManager.TicksPerThirtyMinutes;
			MoleQuantity moleQuantity = RocketMath.Min(water.Quantity, new MoleQuantity(num2 / (double)num3 * (double)num));
			if (!(moleQuantity <= MoleQuantity.Zero))
			{
				TemperatureKelvin temperature = base.InternalAtmosphere.Temperature;
				MoleEnergy energy = IdealGas.Energy(temperature, Mole.SpecificHeat(Chemistry.GasType.Water), moleQuantity);
				MoleEnergy energy2 = IdealGas.Energy(temperature, Mole.SpecificHeat(Chemistry.GasType.PollutedWater), moleQuantity);
				AtmosphericEventInstance.CreateRemove(base.InternalAtmosphere, new GasMixture(new Mole(Chemistry.GasType.Water, moleQuantity, energy)));
				AtmosphericEventInstance.CreateAdd(base.InternalAtmosphere, new GasMixture(new Mole(Chemistry.GasType.PollutedWater, moleQuantity, energy2)));
			}
		}
	}

	private void EqualizeToBodyTemperature()
	{
		if (base.InternalAtmosphere.TotalMoles <= MoleQuantity.Zero)
		{
			return;
		}
		float num = (Entity.BodyTemperature - base.InternalAtmosphere.Temperature).ToFloat() * 0.01f;
		if (!(Mathf.Abs(num) < 0.001f))
		{
			MoleEnergy energy = IdealGas.Energy(base.InternalAtmosphere.GasMixture.HeatCapacity, new TemperatureKelvin(Mathf.Abs(num)));
			if (num > 0f)
			{
				AtmosphericEventInstance.CreateAddEnergy(base.InternalAtmosphere, energy, spark: false);
			}
			else
			{
				AtmosphericEventInstance.CreateRemoveEnergy(base.InternalAtmosphere, energy);
			}
		}
	}

	private void SecreteAcid()
	{
		GasMixture gasMixture = base.InternalAtmosphere.GasMixture;
		MoleQuantity moleQuantity = gasMixture.LiquidHydrochloricAcid.Quantity + gasMixture.HydrochloricAcid.Quantity;
		if (!(moleQuantity >= AcidTargetQuantity))
		{
			MoleQuantity quantity = RocketMath.Min(AcidTargetQuantity - moleQuantity, AcidPerTickQuantity);
			MoleEnergy energy = IdealGas.Energy(Entity.BodyTemperature, Mole.SpecificHeat(Chemistry.GasType.LiquidHydrochloricAcid), quantity);
			AtmosphericEventInstance.CreateAdd(base.InternalAtmosphere, new GasMixture(new Mole(Chemistry.GasType.LiquidHydrochloricAcid, quantity, energy)));
		}
	}

	private void WetSelf()
	{
		Entity parentEntity = ParentEntity;
		if (parentEntity == null)
		{
			return;
		}
		Mole pollutedWater = base.InternalAtmosphere.GasMixture.PollutedWater;
		if (!(pollutedWater.Quantity <= MoleQuantity.Zero))
		{
			GasMixture gasMixture = new GasMixture(new Mole(Chemistry.GasType.PollutedWater, pollutedWater.Quantity, pollutedWater.Energy));
			AtmosphericEventInstance.CreateRemove(base.InternalAtmosphere, gasMixture);
			ISuit suit = (parentEntity as Human)?.Suit;
			if ((bool)suit?.AsThing && suit.InternalAtmosphere != null)
			{
				AtmosphericEventInstance.CreateAdd(suit.InternalAtmosphere, gasMixture);
			}
			else if (parentEntity.GridController.CanContainAtmos(parentEntity.WorldGrid))
			{
				AtmosphericEventInstance.CloneGlobalAddGasMix(parentEntity.WorldGrid, gasMixture);
			}
		}
	}

	private float GetWasteCapacity()
	{
		return Volume.ToFloat() * 0.5f;
	}

	public float GetWasteRatio()
	{
		if (!DifficultySetting.Current.Sanitation)
		{
			return 0f;
		}
		Atmosphere internalAtmosphere = base.InternalAtmosphere;
		if (internalAtmosphere == null)
		{
			return 0f;
		}
		return Mathf.Clamp01(internalAtmosphere.GasMixture.PollutedWater.Quantity.ToFloat() * Chemistry.MolarVolumeLiquid(Chemistry.GasType.PollutedWater).ToFloat() / GetWasteCapacity());
	}

	public override string ToConsoleString()
	{
		if (base.InternalAtmosphere == null)
		{
			return "nil";
		}
		MoleQuantity quantity = base.InternalAtmosphere.GasMixture.Water.Quantity;
		MoleQuantity quantity2 = base.InternalAtmosphere.GasMixture.PollutedWater.Quantity;
		MoleQuantity moleQuantity = base.InternalAtmosphere.GasMixture.LiquidHydrochloricAcid.Quantity + base.InternalAtmosphere.GasMixture.HydrochloricAcid.Quantity;
		return string.Format("{0:0.0}°C water:{1} waste:{2} ({3:0}%) acid:{4}", base.InternalAtmosphere.Temperature.ToCelsius(), quantity.ToFloat().ToStringPrefix("mol"), quantity2.ToFloat().ToStringPrefix("mol"), GetWasteRatio() * 100f, moleQuantity.ToFloat().ToStringPrefix("mol"));
	}
}
