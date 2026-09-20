namespace Assets.Scripts.Serialization;

public struct SaveResult
{
	public bool Success;

	public string Message;

	public static SaveResult Succeed = new SaveResult
	{
		Success = true,
		Message = ""
	};

	public static SaveResult Fail(string message)
	{
		return new SaveResult
		{
			Success = false,
			Message = message
		};
	}
}
