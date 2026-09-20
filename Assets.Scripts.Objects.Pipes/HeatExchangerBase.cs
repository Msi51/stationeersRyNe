using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;

namespace Assets.Scripts.Objects.Pipes;

public class HeatExchangerBase : DeviceInputOutput
{
	public static float HeatExchangeRatio(Atmosphere leftSide, Atmosphere rightSide)
	{
		return leftSide.HeatExchangeRatio() * rightSide.HeatExchangeRatio();
	}

	public override CanConstructInfo CanConstruct()
	{
		if (!base.RequiresFrame || HasFrameBelow())
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
	}
}
