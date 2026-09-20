using System.Globalization;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;

namespace Util.Commands;

public class EntityCommand : CommandBase
{
	public override string HelpText => "Runs entity debug functions. 'state' prints debug state for the targeted entity (defaults to the first human if no target is given).";

	public override string[] Arguments => new string[2] { "state", "[playerName | referenceId]" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("entity"))
		{
			return null;
		}
		if (args.Length == 0)
		{
			return "Invalid syntax";
		}
		Entity entity = Human.AllHumans[0];
		if (args.Length == 2)
		{
			long.TryParse(args[1], out var result);
			Entity entity2 = Thing.Find<Entity>(result);
			if ((object)entity2 != null)
			{
				entity = entity2;
			}
			if ((object)entity2 == null)
			{
				foreach (Human allHuman in Human.AllHumans)
				{
					if (string.Equals(allHuman.DisplayName.ToLower(CultureInfo.CurrentCulture), args[1].ToLower(CultureInfo.CurrentCulture)))
					{
						entity = allHuman;
					}
				}
			}
		}
		if (args[0] == "state")
		{
			return entity.PrintDebug();
		}
		return "Invalid syntax";
	}
}
