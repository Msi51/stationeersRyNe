using Assets.Scripts;
using Assets.Scripts.Util;

namespace Trading;

internal static class ShuttleAudioHelper
{
	public static int MainEngineStartSound(ShuttleType type)
	{
		switch (type)
		{
		case ShuttleType.Small:
		case ShuttleType.SmallGas:
			return Defines.Sounds.ShuttleSmallMainEngineStart;
		case ShuttleType.Medium:
		case ShuttleType.MediumGas:
			return Defines.Sounds.ShuttleMediumMainEngineStart;
		default:
			return Defines.Sounds.ShuttleSmallMainEngineStart;
		case ShuttleType.Large:
		case ShuttleType.LargeGas:
		case ShuttleType.MediumPlane:
		case ShuttleType.LargePlane:
			return 0;
		}
	}

	public static int MainEngineEndSound(ShuttleType type)
	{
		switch (type)
		{
		case ShuttleType.Small:
		case ShuttleType.SmallGas:
			return Defines.Sounds.ShuttleSmallMainEngineEnd;
		case ShuttleType.Medium:
		case ShuttleType.MediumGas:
			return Defines.Sounds.ShuttleMediumMainEngineEnd;
		default:
			return Defines.Sounds.ShuttleSmallMainEngineEnd;
		case ShuttleType.Large:
		case ShuttleType.LargeGas:
		case ShuttleType.MediumPlane:
		case ShuttleType.LargePlane:
			return 0;
		}
	}

	public static int MainEngineSound(ShuttleType type)
	{
		switch (type)
		{
		case ShuttleType.Small:
		case ShuttleType.SmallGas:
			return Defines.Sounds.ShuttleSmallMainEngine;
		case ShuttleType.Medium:
		case ShuttleType.MediumGas:
			return Defines.Sounds.ShuttleMediumMainEngine;
		case ShuttleType.Large:
		case ShuttleType.LargeGas:
			return Defines.Sounds.ShuttleLargeMainEngine;
		default:
			return Defines.Sounds.ShuttleSmallMainEngine;
		case ShuttleType.MediumPlane:
		case ShuttleType.LargePlane:
			return 0;
		}
	}

	public static int MainEngineLSound(ShuttleType type)
	{
		switch (type)
		{
		case ShuttleType.Small:
		case ShuttleType.SmallGas:
		case ShuttleType.Medium:
		case ShuttleType.MediumGas:
			return 0;
		case ShuttleType.Large:
		case ShuttleType.LargeGas:
		case ShuttleType.MediumPlane:
		case ShuttleType.LargePlane:
			return Defines.Sounds.ShuttleLargeMainEngineL;
		default:
			return 0;
		}
	}

	public static int MainEngineLDepartSound(ShuttleType type)
	{
		switch (type)
		{
		case ShuttleType.Small:
		case ShuttleType.SmallGas:
		case ShuttleType.Medium:
		case ShuttleType.MediumGas:
			return 0;
		case ShuttleType.Large:
		case ShuttleType.LargeGas:
		case ShuttleType.MediumPlane:
		case ShuttleType.LargePlane:
			return Defines.Sounds.ShuttleLargeMainEngineLDepart;
		default:
			return 0;
		}
	}

	public static int MainEngineRSound(ShuttleType type)
	{
		switch (type)
		{
		case ShuttleType.Small:
		case ShuttleType.SmallGas:
		case ShuttleType.Medium:
		case ShuttleType.MediumGas:
			return 0;
		case ShuttleType.Large:
		case ShuttleType.LargeGas:
		case ShuttleType.MediumPlane:
		case ShuttleType.LargePlane:
			return Defines.Sounds.ShuttleLargeMainEngineR;
		default:
			return 0;
		}
	}

	public static int MainEngineRDepartSound(ShuttleType type)
	{
		switch (type)
		{
		case ShuttleType.Small:
		case ShuttleType.SmallGas:
		case ShuttleType.Medium:
		case ShuttleType.MediumGas:
			return 0;
		case ShuttleType.Large:
		case ShuttleType.LargeGas:
		case ShuttleType.MediumPlane:
		case ShuttleType.LargePlane:
			return Defines.Sounds.ShuttleLargeMainEngineRDepart;
		default:
			return 0;
		}
	}

	public static int MainEngineDepartSound(ShuttleType type)
	{
		switch (type)
		{
		case ShuttleType.Small:
		case ShuttleType.SmallGas:
			return Defines.Sounds.ShuttleSmallMainEngineDepart;
		case ShuttleType.Medium:
		case ShuttleType.MediumGas:
			return Defines.Sounds.ShuttleMediumMainEngineDepart;
		case ShuttleType.Large:
		case ShuttleType.LargeGas:
			return Defines.Sounds.ShuttleLargeMainEngineDepart;
		default:
			return Defines.Sounds.ShuttleSmallMainEngineDepart;
		case ShuttleType.MediumPlane:
		case ShuttleType.LargePlane:
			return 0;
		}
	}

	public static int MainEngineDistantSound(ShuttleType type)
	{
		switch (type)
		{
		case ShuttleType.Small:
		case ShuttleType.SmallGas:
			return Defines.Sounds.ShuttleSmallMainEngineDistant;
		case ShuttleType.Medium:
		case ShuttleType.MediumGas:
			return Defines.Sounds.ShuttleMediumMainEngineDistant;
		case ShuttleType.Large:
		case ShuttleType.LargeGas:
		case ShuttleType.MediumPlane:
		case ShuttleType.LargePlane:
			return Defines.Sounds.ShuttleLargeMainEngineDistant;
		default:
			return Defines.Sounds.ShuttleSmallMainEngineDistant;
		}
	}

	public static int MainEngineDistantDepartSound(ShuttleType type)
	{
		switch (type)
		{
		case ShuttleType.Small:
		case ShuttleType.SmallGas:
			return Defines.Sounds.ShuttleSmallMainEngineDistantDepart;
		case ShuttleType.Medium:
		case ShuttleType.MediumGas:
			return Defines.Sounds.ShuttleMediumMainEngineDistantDepart;
		case ShuttleType.Large:
		case ShuttleType.LargeGas:
		case ShuttleType.MediumPlane:
		case ShuttleType.LargePlane:
			return Defines.Sounds.ShuttleLargeMainEngineDistantDepart;
		default:
			return Defines.Sounds.ShuttleSmallMainEngineDistantDepart;
		}
	}
}
