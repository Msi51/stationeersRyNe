using System.Collections.Generic;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Objects.Electrical;

public static class Transmitters
{
	public static List<ILogicable> AllTransmitters = new List<ILogicable>();

	public static void ClearAll()
	{
		AllTransmitters.Clear();
	}
}
