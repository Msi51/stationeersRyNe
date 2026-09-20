using System.Collections.Generic;

namespace Objects;

public static class SubmergedHandler
{
	private static readonly List<ISubmergeable> _submergedHandlers = new List<ISubmergeable>();

	public static void Register(ISubmergeable handler)
	{
		_submergedHandlers.Add(handler);
	}

	public static void DeRegister(ISubmergeable handler)
	{
		_submergedHandlers.Remove(handler);
	}

	public static void Tick()
	{
		for (int num = _submergedHandlers.Count - 1; num >= 0; num--)
		{
			ISubmergeable submergeable = _submergedHandlers[num];
			if (submergeable != null && submergeable.IsValid && submergeable.DoSubmergableTick)
			{
				submergeable.OnSubmergeableTick();
			}
		}
	}

	public static void Clear()
	{
		_submergedHandlers.Clear();
	}
}
