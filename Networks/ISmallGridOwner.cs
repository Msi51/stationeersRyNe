using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;

namespace Networks;

public interface ISmallGridOwner
{
	bool IsCollision(SmallGrid gridObject, Grid3 grid);

	void OnGridPlaced(SmallGrid occupant);

	void OnGridUpdated(SmallGrid occupant);

	void OnGridRemoved(SmallGrid occupant);
}
