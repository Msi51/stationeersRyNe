namespace Assets.Scripts.Objects.Electrical;

public interface ILogicTick : ILogicStack, IMemory
{
	void OnLogicTick();
}
