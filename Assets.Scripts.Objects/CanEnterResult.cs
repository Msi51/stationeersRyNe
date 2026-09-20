using System.Text;
using Assets.Scripts.Localization2;

namespace Assets.Scripts.Objects;

public struct CanEnterResult
{
	private static readonly StringBuilder StateMessageBuilder = new StringBuilder();

	public bool Result;

	public string Reason;

	public static CanEnterResult Succeed = new CanEnterResult(result: true, string.Empty);

	public static CanEnterResult Fail(string reason)
	{
		return new CanEnterResult(result: false, reason);
	}

	private static void AppendStateMessage(Assets.Scripts.Localization2.GameString gameString, string arg0)
	{
		StateMessageBuilder.Append(gameString.AsString(arg0));
	}

	private static void AppendStateMessage(Assets.Scripts.Localization2.GameString gameString, string arg0, string arg1)
	{
		StateMessageBuilder.Append(gameString.AsString(arg0, arg1));
	}

	public static CanEnterResult Fail(Assets.Scripts.Localization2.GameString reason, string arg0)
	{
		StateMessageBuilder.Clear();
		AppendStateMessage(reason, arg0);
		return new CanEnterResult(result: false, StateMessageBuilder.ToString());
	}

	public static CanEnterResult Fail(Assets.Scripts.Localization2.GameString reason, string arg0, string arg1)
	{
		StateMessageBuilder.Clear();
		AppendStateMessage(reason, arg0, arg1);
		return new CanEnterResult(result: false, StateMessageBuilder.ToString());
	}

	private CanEnterResult(bool result, string reason)
	{
		Result = result;
		Reason = reason;
	}

	public static implicit operator bool(CanEnterResult canEnterResult)
	{
		return canEnterResult.Result;
	}
}
