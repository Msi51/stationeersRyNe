using Assets.Scripts.Networking;

namespace Objects.Rockets.Scanning;

public class SurveyData : SpaceMapNodeActionData
{
	public SurveyData()
	{
	}

	public SurveyData(RocketBinaryReader reader)
		: base(reader)
	{
	}

	public override RocketAction ToInstance(SpaceMapNode node)
	{
		return new Survey(this, node);
	}
}
