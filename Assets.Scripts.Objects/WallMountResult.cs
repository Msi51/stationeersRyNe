namespace Assets.Scripts.Objects;

public enum WallMountResult
{
	Unknown,
	Valid,
	InvalidMissingSupport,
	InvalidRequiresFrame,
	InvalidBlocked,
	InvalidNotMountable,
	InvalidFacingMismatch
}
