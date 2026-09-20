namespace Assets.Scripts.Objects;

public readonly struct InteractionInstance(Thing thing, InteractableType action, int state, bool skipAnimation)
{
	public readonly InteractableType Action = action;

	public readonly int State = state;

	public readonly bool SkipAnimation = skipAnimation;

	public readonly Thing Thing = thing;
}
