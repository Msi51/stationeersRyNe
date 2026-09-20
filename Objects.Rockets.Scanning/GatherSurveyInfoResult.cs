namespace Objects.Rockets.Scanning;

public class GatherSurveyInfoResult : Result
{
	public GatherSurveyInfoResult()
	{
	}

	public GatherSurveyInfoResult(GatherSurveyInfoResultData data)
	{
	}

	public GatherSurveyInfoResult(GatherSurveyInfoResultSaveData data)
	{
	}

	public override ResultSaveData ToSaveData()
	{
		return new GatherSurveyInfoResultSaveData(this);
	}
}
