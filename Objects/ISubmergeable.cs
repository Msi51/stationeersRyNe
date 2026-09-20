namespace Objects;

public interface ISubmergeable
{
	bool IsValid { get; }

	bool DoSubmergableTick { get; }

	void OnSubmergeableTick();
}
