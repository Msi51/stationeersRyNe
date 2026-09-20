using Assets.Scripts.Objects.Electrical;

namespace Assets.Scripts.Objects.Pipes;

public interface IMemoryWritable : IMemory
{
	void WriteMemory(int address, double value);

	void ClearMemory();
}
