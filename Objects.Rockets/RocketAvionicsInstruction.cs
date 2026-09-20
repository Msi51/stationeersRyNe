namespace Objects.Rockets;

public enum RocketAvionicsInstruction : byte
{
	None,
	StackPointer,
	JumpToAddress,
	ResourceSite,
	SurveySite,
	ChildResourceSite,
	ChildSurveySite
}
