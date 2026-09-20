namespace Assets.Scripts.Objects.Electrical;

public enum PrinterInstruction : byte
{
	None,
	StackPointer,
	ExecuteRecipe,
	WaitUntilNextValid,
	JumpIfNextInvalid,
	JumpToAddress,
	DeviceSetLock,
	EjectReagent,
	EjectAllReagents,
	MissingRecipeReagent
}
