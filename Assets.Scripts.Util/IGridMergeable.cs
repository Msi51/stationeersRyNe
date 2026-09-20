using Assets.Scripts.Objects;

namespace Assets.Scripts.Util;

public interface IGridMergeable : ISmartRotatable
{
	CanConstructInfo CanReplace(MultiConstructor constructor, Item inactiveHandItem);

	bool WillMergeWhenPlaced();
}
