namespace Objects.Rockets.Scanning;

public class GatherSurveyInfoResultData : ResultData
{
	public override Result ToInstance()
	{
		return new GatherSurveyInfoResult(this);
	}
}
