namespace Objects.Rockets.Scanning;

public class GatherSurveyInfoResultSaveData : ResultSaveData
{
	public GatherSurveyInfoResultSaveData()
	{
	}

	public GatherSurveyInfoResultSaveData(GatherSurveyInfoResult result)
	{
	}

	public override Result ToInstance()
	{
		return new GatherSurveyInfoResult(this);
	}
}
