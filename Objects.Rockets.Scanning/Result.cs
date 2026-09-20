namespace Objects.Rockets.Scanning;

public abstract class Result
{
	public SpaceMapNode Node;

	public virtual void Do()
	{
	}

	public abstract ResultSaveData ToSaveData();
}
