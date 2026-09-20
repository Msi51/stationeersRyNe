using System;

namespace Assets.Scripts.Objects;

public struct CanMountResult
{
	public WallMountResult result;

	public Structure support;

	public Structure offending;

	public static readonly CanMountResult BasicValid = new CanMountResult
	{
		result = WallMountResult.Valid
	};

	public static implicit operator bool(CanMountResult c)
	{
		return c.result == WallMountResult.Valid;
	}

	public string ResultMessage()
	{
		switch (result)
		{
		case WallMountResult.Unknown:
		case WallMountResult.Valid:
			return string.Empty;
		case WallMountResult.InvalidMissingSupport:
			return InterfaceStrings.TooltipPlacementSnapFaceMountMissingSupport;
		case WallMountResult.InvalidRequiresFrame:
			return InterfaceStrings.TooltipPlacementSnapFaceMountMissingFrame;
		case WallMountResult.InvalidBlocked:
			return InterfaceStrings.TooltipPlacementSnapFaceMountBlocked(offending);
		case WallMountResult.InvalidNotMountable:
			return InterfaceStrings.TooltipPlacementSnapFaceNotMountable(offending);
		case WallMountResult.InvalidFacingMismatch:
			return InterfaceStrings.TooltipPlacementSnapFaceWrongFace(offending);
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
