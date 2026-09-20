using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Items;

public class Lungs : Organ, IThermal
{
	private const float TEMPERATURE_DAMAGE_SCALAR = 200f;

	private const float VACUUM_DAMAGE_PER_TICK = 0.4f;

	private const float TEMP_DAMAGE_PER_TICK = 3f;

	private const float DAMAGE_TICK_NO_OXYGEN = 0.2f;

	private const float REPAIR_SCALE = 0.2f;

	[SerializeField]
	[FormerlySerializedAs("Volume")]
	private float volume = 6f;

	public Chemistry.GasType BreathedType = Chemistry.GasType.Oxygen;

	public Chemistry.GasType ToxicTypes = Chemistry.GasType.Methane | Chemistry.GasType.Pollutant;

	protected virtual TemperatureKelvin TemperatureMin => Chemistry.Temperature.ZeroDegrees - new TemperatureKelvin(10.0);

	protected virtual TemperatureKelvin TemperatureMax => Chemistry.Temperature.ZeroDegrees + new TemperatureKelvin(50.0);

	public VolumeLitres Volume => new VolumeLitres(volume);

	public override bool HasReadableAtmosphere => true;

	public virtual float AtmosphericEfficiency => Mathf.Clamp((base.InternalAtmosphere.PartialPressureO2 / Chemistry.MinimumOxygenPartialPressure).ToFloat(), 0f, 1.5f);

	public float DamageEfficiency => Mathf.Max(0f, 100f - DamageState.Total) / 100f;

	protected virtual PressurekPa ToxinLevel => base.InternalAtmosphere.PartialPressureHumanToxins;

	private float BreathingEfficiency => AtmosphericEfficiency * DamageEfficiency;

	public override string ToConsoleString()
	{
		if (base.InternalAtmosphere == null)
		{
			return "nil";
		}
		float num = AtmosphericEfficiency * DamageEfficiency;
		return $"{base.InternalAtmosphere.PressureGassesAndLiquids.ToFloat():0.0}kPa " + $"{base.InternalAtmosphere.Temperature.ToCelsius():0.0}°C O2:{base.InternalAtmosphere.PartialPressureO2.ToFloat():0.0}kPa " + $"tox:{ToxinLevel.ToFloat():0.0}kPa eff:{num * 100f:0}%";
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, Volume, 0L);
		}
	}

	public override void OnLifeTick()
	{
		float num = 1f / (float)DifficultySetting.Current.LungDamageRate * 4f;
		float b = DamageState.MaxDamage / num;
		Atmosphere internalAtmosphere = base.InternalAtmosphere;
		if (internalAtmosphere != null && internalAtmosphere.PressureGassesAndLiquids > Chemistry.ResetThreshold)
		{
			PressurekPa toxinLevel = ToxinLevel;
			float toxic;
			if (toxinLevel > Entity.ToxicPartialPressureForDamage)
			{
				float num2 = Mathf.Min(toxinLevel.ToFloat() * 0.2f, b);
				DamageState.Damage(ChangeDamageType.Increment, num2, DamageUpdateType.Toxic);
				Chemistry.GasType[] values = EnumCollections.GasTypes.Values;
				foreach (Chemistry.GasType gasType in values)
				{
					if (gasType != Chemistry.GasType.Undefined && (ToxicTypes & gasType) != Chemistry.GasType.Undefined)
					{
						base.InternalAtmosphere.GasMixture.Remove(gasType, new MoleQuantity(num2));
					}
				}
			}
			else
			{
				toxic = DamageState.Toxic;
				if (toxic > 0f && toxic < 50f)
				{
					DamageState.Damage(ChangeDamageType.Decrement, 0.1f, DamageUpdateType.Toxic);
				}
			}
			if (base.InternalAtmosphere.Temperature < TemperatureMin)
			{
				float num3 = (TemperatureKelvin.One - base.InternalAtmosphere.Temperature / TemperatureMin).ToFloat();
				float value = Mathf.Min(3f * num3, b);
				DamageState.Damage(ChangeDamageType.Increment, value, DamageUpdateType.Burn);
				return;
			}
			if (base.InternalAtmosphere.Temperature > TemperatureMax)
			{
				float num4 = Mathf.Clamp01((base.InternalAtmosphere.Temperature - TemperatureMax).ToFloat() / 200f);
				float value2 = Mathf.Min(3f * num4, b);
				DamageState.Damage(ChangeDamageType.Increment, value2, DamageUpdateType.Burn);
				return;
			}
			toxic = DamageState.Oxygen;
			if (toxic > 0f && toxic < 50f)
			{
				DamageState.Damage(ChangeDamageType.Decrement, 0.2f, DamageUpdateType.Oxygen);
			}
			toxic = DamageState.Burn;
			if (toxic > 0f && toxic < 10f)
			{
				DamageState.Damage(ChangeDamageType.Decrement, 0.6f, DamageUpdateType.Burn);
			}
			toxic = DamageState.Brute;
			if (toxic > 0f && toxic < 10f)
			{
				DamageState.Damage(ChangeDamageType.Decrement, 0.6f, DamageUpdateType.Brute);
			}
		}
		else
		{
			float num5 = Mathf.Min(0.4f, b);
			DamageState.Damage(ChangeDamageType.Increment, num5 * 0.6f, DamageUpdateType.Brute);
			DamageState.Damage(ChangeDamageType.Increment, num5 * 0.4f, DamageUpdateType.Burn);
		}
	}

	public MoleQuantity TakeBreath(Atmosphere breathingAtmosphere, MoleQuantity molesPerBreath)
	{
		MoleQuantity moleQuantity = RocketMath.Min(molesPerBreath, base.InternalAtmosphere.GasMixture.Oxygen.Quantity * BreathingEfficiency);
		breathingAtmosphere.GasMixture.CarbonDioxide.AddAtTemperature(Entity.BodyTemperature, base.InternalAtmosphere.GasMixture.Oxygen.Remove(moleQuantity).Quantity * 0.5);
		base.InternalAtmosphere.GasMixture.AddEnergy(Entity.EnergyReleasedPerTick);
		return moleQuantity;
	}
}
