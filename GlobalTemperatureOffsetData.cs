public abstract class GlobalTemperatureOffsetData
{
	public const float DAY_TEMPERATURE_EASE_ANGLE = 70f;

	public const float NIGHT_TEMPERATURE_EASE_ANGLE = 110f;

	public abstract float GetOffset(float solarAngleDegrees, float evaluateX);

	public abstract float GetOffset(float solarAngleDegrees);

	public virtual void Init()
	{
	}
}
