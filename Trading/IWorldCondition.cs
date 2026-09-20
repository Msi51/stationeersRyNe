namespace Trading;

public interface IWorldCondition : IChecksum
{
	bool Evaluate();
}
