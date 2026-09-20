using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Util;

namespace Util.Commands;

public class CelestialCommand : CommandBase
{
	private const string ARG_ECCENTRICITY = "eccentricity";

	private const string ARG_SEMI_MAJOR_AXIS_AU = "semimajoraxisau";

	private const string ARG_SEMI_MAJOR_AXIS_KM = "semimajoraxiskm";

	private const string ARG_INCLINATION = "inclination";

	private const string ARG_PERIAPSIS = "periapsis";

	private const string ARG_PERIOD = "period";

	private const string ARG_ROTATION = "rotation";

	private const string ARG_ASCENDING_NODE = "ascendingnode";

	public override string HelpText => "Inspects or edits a celestial body's orbital parameters; with only a celestial name prints its current values, with three arguments sets the named property. Editing is disabled in multiplayer.";

	public override string[] Arguments => new string[8] { "eccentricity", "semimajoraxisau", "semimajoraxiskm", "inclination", "periapsis", "period", "ascendingnode", "rotation" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("celestial"))
		{
			return null;
		}
		if (args.Length == 0)
		{
			ConsoleWindow.PrintError("Invalid syntax", suppressStacktrace: true);
			return null;
		}
		if (OrbitalSimulation.System == null)
		{
			ConsoleWindow.PrintError("No orbital simulation system loaded.", suppressStacktrace: true);
			return null;
		}
		Celestial celestial = null;
		celestial = OrbitalSimulation.System.Find<Celestial>(args[0]) ?? OrbitalSimulation.System.Find<Celestial>(args[0].ToProper());
		if (celestial == null)
		{
			ConsoleWindow.PrintError("Celestial '" + args[0] + "' not found.", suppressStacktrace: true);
			return null;
		}
		if (args.Length == 1)
		{
			celestial.PrintDebug();
			return null;
		}
		if (NetworkManager.IsActive)
		{
			ConsoleWindow.PrintError("Cannot change celestials in multiplayer.", suppressStacktrace: true);
			return null;
		}
		if (args.Length != 3)
		{
			ConsoleWindow.PrintError("Invalid syntax", suppressStacktrace: true);
			return null;
		}
		if (!(celestial is CelestialBody celestialBody))
		{
			return null;
		}
		switch (args[1])
		{
		case "eccentricity":
		{
			if (!CommandBase.Get(args, 2, "eccentricity", out float result7))
			{
				return null;
			}
			celestialBody.Orbit.Eccentricity = result7;
			break;
		}
		case "semimajoraxiskm":
		{
			if (!double.TryParse(args[2], out var result4))
			{
				ConsoleWindow.PrintError("Invalid value '" + args[2] + "' for semimajoraxiskm.", suppressStacktrace: true);
				return null;
			}
			celestialBody.Orbit.SemiMajorAxis = result4 * 6.6845871222684464E-09;
			break;
		}
		case "semimajoraxisau":
		{
			if (!double.TryParse(args[2], out var result8))
			{
				ConsoleWindow.PrintError("Invalid value '" + args[2] + "' for semimajoraxisau.", suppressStacktrace: true);
				return null;
			}
			celestialBody.Orbit.SemiMajorAxis = result8;
			break;
		}
		case "inclination":
		{
			if (!CommandBase.Get(args, 2, "inclination", out float result3))
			{
				return null;
			}
			celestialBody.Orbit.Inclination = result3;
			break;
		}
		case "periapsis":
		{
			if (!CommandBase.Get(args, 2, "periapsis", out float result5))
			{
				return null;
			}
			celestialBody.Orbit.ArgumentOfPeriapsis = result5;
			break;
		}
		case "period":
		{
			if (!CommandBase.Get(args, 2, "period", out float result2))
			{
				return null;
			}
			celestialBody.Orbit.Period = result2;
			break;
		}
		case "ascendingnode":
		{
			if (!double.TryParse(args[2], out var result6))
			{
				ConsoleWindow.PrintError("Invalid value '" + args[2] + "' for ascendingnode.", suppressStacktrace: true);
				return null;
			}
			celestialBody.Orbit.LongitudeOfAscendingNode = result6;
			break;
		}
		case "rotation":
		{
			if (!(celestialBody is RotatingCelestialBody rotatingCelestialBody))
			{
				ConsoleWindow.PrintAction(celestialBody.Name + " is not a rotating body.");
				return null;
			}
			if (!CommandBase.Get(args, 2, "rotation", out float result))
			{
				return null;
			}
			rotatingCelestialBody._baseRotationSpeed = result;
			break;
		}
		default:
			return "Invalid syntax";
		}
		ConsoleWindow.PrintAction(celestialBody.Name + " " + args[1] + " set to " + args[2] + ".");
		return null;
	}
}
