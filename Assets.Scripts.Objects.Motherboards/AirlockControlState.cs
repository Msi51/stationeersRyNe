namespace Assets.Scripts.Objects.Motherboards;

public enum AirlockControlState
{
	None = -1,
	Disabled,
	Pressurizing,
	Pressurized,
	Depressurizing,
	Depressurized,
	OverrideCount,
	Override
}
