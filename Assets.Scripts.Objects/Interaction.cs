namespace Assets.Scripts.Objects;

public readonly struct Interaction(Thing sourceThing, Slot sourceSlot, Thing destinationThing, bool altKey)
{
	public Thing SourceThing { get; } = sourceThing;

	public Slot SourceSlot { get; } = sourceSlot;

	public Thing DestinationThing { get; } = destinationThing;

	public bool AltKey { get; } = altKey;
}
