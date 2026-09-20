using Assets.Scripts.GridSystem;

namespace Assets.Scripts.Objects.Pipes;

public interface IMounted
{
	CanConstructInfo CanConstruct();

	void Mount(Grid3 grid);
}
