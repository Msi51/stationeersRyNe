namespace Weather;

public interface IWeatherDamagable
{
	bool CanBeWeathered();

	void DoWeatherDamage(float damageMultiplier);
}
