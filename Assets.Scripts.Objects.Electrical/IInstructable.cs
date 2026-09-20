namespace Assets.Scripts.Objects.Electrical;

public interface IInstructable : IMemory
{
	IEnumCollection GetInstructions();

	string GetInstructionDescription(int i);
}
