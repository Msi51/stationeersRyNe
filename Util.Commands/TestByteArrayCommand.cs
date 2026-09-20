namespace Util.Commands;

public class TestByteArrayCommand : CommandBase
{
	public override string HelpText => "Tests every item in the world to verify its network read and write functions are symmetrical. Editor-only. Supply a reference id to check a single item. Currently not implemented.";

	public override string[] Arguments => new string[1] { "[referenceId]" };

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		return "Not implemented.";
	}
}
