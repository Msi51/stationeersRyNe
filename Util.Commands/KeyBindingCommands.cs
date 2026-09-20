using System;
using InputSystem;

namespace Util.Commands;

public class KeyBindingCommands : CommandBase
{
	public override string HelpText => "Prints all key bindings registered against the local human. Pass 'reset' to clear the keybinding stack, which can resolve stuck-input issues.";

	public override string[] Arguments => new string[1] { "[reset]" };

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		return KeyWrapBindings.Print(args ?? Array.Empty<string>());
	}
}
