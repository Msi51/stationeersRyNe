using Assets.Scripts.Objects.Electrical;

namespace Assets.Scripts.Objects.Pipes;

public interface IMemoryReadable : IMemory
{
	double ReadMemory(int address);
}
