namespace Assets.Scripts.Objects.Electrical;

public enum TraderInstruction : byte
{
	None,
	WriteTraderData,
	StrongestContactIdHash,
	StrongestContactMetaData,
	StrongestContactSignalData,
	WriteTraderBuyData,
	WriteTraderSellData,
	TraderBuyThingData,
	TraderBuyThingChildData,
	TraderBuyGasData,
	TraderSellThingData,
	TraderSellGasData,
	TraderSellThingChildData,
	FilterPrefabHashEquals,
	FilterPrefabHashNotEquals,
	FilterSortingClassCompare,
	FilterQuantityCompare,
	FilterGasContains,
	FilterGasNotContains
}
