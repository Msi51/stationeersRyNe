using System.Text;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using CharacterCustomisation;
using UnityEngine;

namespace Util.Commands;

public class CustomFacialExpressionCommand : CommandBase
{
	public override string HelpText => "Sets a custom facial expression on the local player's character. Pass 'list' to print all available expressions.";

	public override string[] Arguments => new string[1] { "<list | expressionname>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("customfacialexpression"))
		{
			return null;
		}
		if (InventoryManager.ParentHuman.State != EntityState.Alive)
		{
			ConsoleWindow.PrintError("You are dead and cannot perform emotes.", suppressStacktrace: true);
			return null;
		}
		if (args.Length != 1)
		{
			return "Invalid syntax";
		}
		if (PlayerCosmeticsBehaviour.FacialExpressions.TryGet(Animator.StringToHash(args[0].ToLower()), out var expression))
		{
			InventoryManager.ParentHuman.CosmeticsBehaviour.SetFacialExpression(expression);
			return "Facial expression set to '" + args[0] + "'.";
		}
		if (args[0] == "list")
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("Available facial expressions:");
			foreach (string expressionName in PlayerCosmeticsBehaviour.FacialExpressions.ExpressionNames)
			{
				stringBuilder.AppendLine(expressionName);
			}
			return stringBuilder.ToString();
		}
		ConsoleWindow.PrintError("Facial expression '" + args[0] + "' does not exist in data.", suppressStacktrace: true);
		return null;
	}
}
