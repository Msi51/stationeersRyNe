namespace Objects.Rockets.Scanning;

public abstract class RocketAction
{
	public readonly SpaceMapNode SpaceMapNode;

	protected readonly SpaceMapNodeActionData Data;

	protected RocketAction(SpaceMapNodeActionData actionData, SpaceMapNode node)
	{
		Data = actionData;
		SpaceMapNode = node;
	}

	public bool IsCompleted()
	{
		return false;
	}

	public abstract bool Evaluate(Rocket rocket, out RocketActionResult result);

	public abstract void Start(Rocket rocket);

	public abstract bool ProgressAction(float deltaTime, Rocket rocket, out RocketActionResult result);

	public virtual bool Complete(Rocket rocket, out RocketActionResult result)
	{
		Data?.Achievement?.Execute();
		Close(rocket);
		return result = RocketActionResult.Success;
	}

	public virtual void Close(Rocket rocket, bool clearAction = true)
	{
		rocket.CurrentAction = null;
		if (clearAction)
		{
			rocket.RocketMode = RocketMode.None;
		}
	}

	public abstract bool IsRocketMode(RocketMode rocketMode);

	public void OnCycleComplete()
	{
		Data?.Achievement?.Execute();
	}
}
