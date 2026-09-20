using System;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Electrical;

public static class CircuitHolders
{
	private const int MAX_CIRCUIT_HOLDERS = 4096;

	public static DensePool<ICircuitHolder> AllCircuitHolders = new DensePool<ICircuitHolder>("AllCircuitHolders", 4096);

	private static readonly Action<ICircuitHolder> CircuitHolderAction = delegate(ICircuitHolder iCircuitHolder)
	{
		iCircuitHolder?.Execute();
	};

	public static void Register(ICircuitHolder iCircuitHolder)
	{
		if (iCircuitHolder != null)
		{
			AllCircuitHolders.Add(iCircuitHolder);
		}
	}

	public static void Deregister(ICircuitHolder iCircuitHolder)
	{
		if (iCircuitHolder != null)
		{
			AllCircuitHolders.Remove(iCircuitHolder);
		}
	}

	public static void Execute()
	{
		AllCircuitHolders.ForEach(CircuitHolderAction);
	}
}
