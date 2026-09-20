using System;

namespace Util.Commands;

public class BasicCommand : CommandBase
{
	private readonly Func<string[], string> _execute;

	public override string HelpText { get; }

	public override string[] Arguments { get; }

	public override bool IsLaunchCmd { get; }

	public BasicCommand(Func<string[], string> execute, string helpText = null, string[] arguments = null, bool isLaunchCmd = false)
	{
		HelpText = helpText;
		Arguments = arguments;
		IsLaunchCmd = isLaunchCmd;
		_execute = execute;
	}

	public override string Execute(string[] args)
	{
		return _execute?.Invoke(args);
	}
}
