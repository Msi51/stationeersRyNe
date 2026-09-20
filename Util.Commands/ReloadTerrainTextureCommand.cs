using System;

namespace Util.Commands;

public class ReloadTerrainTextureCommand : CommandBase
{
	public override string HelpText => "Reloads the terrain textures from streaming assets.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("reloadterraintexture"))
		{
			return null;
		}
		WorldManager.ReloadTerrainTextures(out var message);
		return message;
	}
}
