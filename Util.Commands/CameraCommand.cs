using Assets.Scripts;
using Assets.Scripts.Util;

namespace Util.Commands;

public class CameraCommand : CommandBase
{
	private const string ARG_SHAKE = "shake";

	private const string ARG_CINEMATIC = "cinematic";

	public override string HelpText => "Provides camera tools. Use 'shake [intensity]' to set camera shake, or 'cinematic [on|off|toggle|tether <m>|help]' to drive the free-fly trailer/screenshot camera (Tab opens the settings panel, the ScreenShot key captures 4K).";

	public override string[] Arguments => new string[2] { "shake [intensity]", "cinematic [on | off | toggle | tether <m> | help]" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("camera"))
		{
			return null;
		}
		if (args.Length < 1 || args.Length > 3)
		{
			return "Invalid syntax";
		}
		string text = args[0];
		if (!(text == "shake"))
		{
			if (text == "cinematic")
			{
				return Cinematic(args);
			}
			return "Invalid syntax";
		}
		return Shake(args);
	}

	private string Cinematic(string[] args)
	{
		if (args.Length == 1)
		{
			CinematicCamera.Toggle();
			return "Cinematic camera: " + (CinematicCamera.IsActive ? "on" : "off") + ". Type 'camera cinematic help' for controls.";
		}
		switch (args[1])
		{
		case "on":
			CinematicCamera.SetActive(on: true);
			return "Cinematic camera: on.";
		case "off":
			CinematicCamera.SetActive(on: false);
			return "Cinematic camera: off.";
		case "toggle":
			CinematicCamera.Toggle();
			return "Cinematic camera: " + (CinematicCamera.IsActive ? "on" : "off") + ".";
		case "tether":
		{
			if (args.Length != 3 || !float.TryParse(args[2], out var result))
			{
				return "Invalid syntax";
			}
			CinematicCamera.GetOrCreate().TetherRadius = result;
			return "Cinematic tether radius: " + ((result <= 0f) ? "unlimited" : (result + "m")) + ".";
		}
		case "help":
		case "?":
			PrintCinematicHelp();
			return null;
		default:
			return "Invalid syntax";
		}
	}

	private static void PrintCinematicHelp()
	{
		ConsoleWindow.PrintAction("=== Cinematic Camera ===");
		ConsoleWindow.Print("Free-fly camera for trailers, screenshots, and recording. Local-only (invisible to other clients). Player Human stays put while active.");
		ConsoleWindow.Print("");
		ConsoleWindow.PrintAction("Console:");
		ConsoleWindow.Print("  camera cinematic                - toggle on/off");
		ConsoleWindow.Print("  camera cinematic on | off       - explicit");
		ConsoleWindow.Print("  camera cinematic tether <m>     - soft-clamp to within N metres of player (0 = unlimited)");
		ConsoleWindow.Print("  camera cinematic help           - this text");
		ConsoleWindow.Print("");
		ConsoleWindow.PrintAction("Movement:");
		ConsoleWindow.Print("  WASD                            - horizontal");
		ConsoleWindow.Print("  Space / Ctrl                    - up / down (uses Ascend/Descend keybinds)");
		ConsoleWindow.Print("  Q / E                           - roll left / right (uses RotateRoll* keybinds, default Insert/PageUp)");
		ConsoleWindow.Print("  Shift                           - fast modifier (5x by default)");
		ConsoleWindow.Print("  Alt                             - slow / fine-tune (0.2x by default)");
		ConsoleWindow.Print("  Mouse wheel                     - adjust base speed (multiplicative)");
		ConsoleWindow.Print("");
		ConsoleWindow.PrintAction("Look & FOV:");
		ConsoleWindow.Print("  Mouse                           - look (respects Settings.InvertMouse)");
		ConsoleWindow.Print("  FoVUp / FoVDown / FovReset      - adjust FOV (same keybinds as normal play)");
		ConsoleWindow.Print("");
		ConsoleWindow.PrintAction("Capture & UI:");
		ConsoleWindow.Print("  ScreenShot key (default P)      - 4K PNG to {SavePath}/screenshots/");
		ConsoleWindow.Print("  Tab                             - open/close live settings panel (speed, smoothing, FOV, screenshot res)");
		ConsoleWindow.Print("  Esc                             - close panel first, then exit cinematic");
		ConsoleWindow.Print("");
		ConsoleWindow.PrintAction("Notes:");
		ConsoleWindow.Print("  - Translation, mouse-look, and roll are all low-pass smoothed for trailer-grade footage.");
		ConsoleWindow.Print("  - Camera registers as an ILodRequester so terrain streams in around it at player quality.");
		ConsoleWindow.Print("  - Local player renders in third-person while active so you can see your own character.");
	}

	private string Shake(string[] args)
	{
		if (args.Length == 1)
		{
			ConsoleWindow.PrintAction("Camera Shake is " + StringManager.Get(CameraController.CameraShake));
			return null;
		}
		if (args.Length != 2)
		{
			return "Invalid syntax";
		}
		if (float.TryParse(args[1], out var result))
		{
			CameraController.SetCameraShake(result);
			return null;
		}
		return "Invalid syntax";
	}
}
