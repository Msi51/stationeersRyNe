namespace Assets.Scripts.Objects.Electrical;

public enum SorterInstruction : byte
{
	None,
	FilterPrefabHashEquals,
	FilterPrefabHashNotEquals,
	FilterSortingClassCompare,
	FilterSlotTypeCompare,
	FilterQuantityCompare,
	LimitNextExecutionByCount
}
