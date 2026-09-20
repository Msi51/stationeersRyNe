using System;
using Assets.Scripts;
using Assets.Scripts.Emotes;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using CharacterCustomisation;
using UnityEngine;

namespace Util.Commands;

public class EmoteCommand : CommandBase
{
	public override string HelpText => "Triggers a facial expression on the local player's character. Optional duration is in milliseconds (default 5000) and intensity is a 0-1 float (default 1).";

	public override string[] Arguments => new string[3] { "<happy | surprised | open | none | angry | dead | reset>", "[duration]", "[intensity]" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("emote"))
		{
			return null;
		}
		if (InventoryManager.ParentHuman.State != EntityState.Alive)
		{
			ConsoleWindow.PrintError("You are dead and cannot perform emotes.", suppressStacktrace: true);
			return null;
		}
		if (args.Length < 1 || args.Length > 3)
		{
			return "Invalid syntax";
		}
		string text = args[0].ToLower();
		if (!Emote.Trigger(text))
		{
			switch (text)
			{
			default:
				ConsoleWindow.PrintError("No emote found with name '" + args[0] + "'.", suppressStacktrace: true);
				return null;
			case "angry":
			case "none":
			case "dead":
			case "open":
			case "happy":
			case "surprised":
			case "reset":
				break;
			}
		}
		int num = args.Length;
		if (num >= 1 && num <= 3)
		{
			switch (text)
			{
			case "angry":
			case "none":
			case "dead":
			case "open":
			case "happy":
			case "surprised":
			case "reset":
			{
				int result = 5000;
				float result2 = 1f;
				if (args.Length >= 2 && !CommandBase.Get(args, 1, "duration", out result))
				{
					return null;
				}
				if (args.Length == 3 && (!CommandBase.Get(args, 2, "intensity", out result2) || result2 < 0f || result2 > 1f))
				{
					ConsoleWindow.PrintError("Intensity must be a value between 0 and 1.", suppressStacktrace: true);
					return null;
				}
				BlendShapeType blendShapeType;
				switch (text)
				{
				case "angry":
					blendShapeType = BlendShapeType.Angry;
					break;
				case "none":
					blendShapeType = BlendShapeType.None;
					break;
				case "dead":
					blendShapeType = BlendShapeType.Dead;
					break;
				case "open":
					blendShapeType = BlendShapeType.Open;
					break;
				case "happy":
					blendShapeType = BlendShapeType.Happy;
					break;
				case "surprised":
					blendShapeType = BlendShapeType.Surprised;
					break;
				case "reset":
				{
					InventoryManager.ParentHuman.CosmeticsBehaviour.ResetExpressions();
					BlendShapeEmoteResetMessage blendShapeEmoteResetMessage = new BlendShapeEmoteResetMessage
					{
						HumanNetId = InventoryManager.ParentHuman.NetworkId
					};
					if (NetworkManager.IsClient)
					{
						blendShapeEmoteResetMessage.SendToServer();
					}
					else if (NetworkManager.IsServer)
					{
						blendShapeEmoteResetMessage.SendToClients();
					}
					return null;
				}
				default:
					blendShapeType = BlendShapeType.None;
					break;
				}
				InventoryManager.ParentHuman.CosmeticsBehaviour.SetExpression(blendShapeType, tween: true, Math.Abs(result), Mathf.Clamp01(result2));
				BlendShapeEmoteMessage blendShapeEmoteMessage = new BlendShapeEmoteMessage
				{
					HumanNetId = InventoryManager.ParentHuman.NetworkId,
					BlendShapeType = blendShapeType,
					Duration = Math.Abs(result),
					Intensity = Mathf.Clamp01(result2)
				};
				if (NetworkManager.IsClient)
				{
					blendShapeEmoteMessage.SendToServer();
				}
				else if (NetworkManager.IsServer)
				{
					blendShapeEmoteMessage.SendToClients();
				}
				break;
			}
			}
		}
		return null;
	}
}
