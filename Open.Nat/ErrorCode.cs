namespace Open.Nat;

public enum ErrorCode
{
	None = 0,
	InvalidArguments = 402,
	ActionFailed = 501,
	Unauthorized = 606,
	SpecifiedArrayIndexInvalid = 713,
	NoSuchEntryInArray = 714,
	WildCardNotPermittedInSourceIp = 715,
	WildCardNotPermittedInExternalPort = 716,
	ConflictInMappingEntry = 718,
	SamePortValuesRequired = 724,
	OnlyPermanentLeasesSupported = 725,
	RemoteHostOnlySupportsWildcard = 726,
	ExternalPortOnlySupportsWildcard = 727,
	NoPortMapsAvailable = 728,
	ConflictWithOtherMechanisms = 729,
	WildCardNotPermittedInIntPort = 732
}
