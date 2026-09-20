using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Trading;

public interface ISmallGrid : ITooltip, IReferencable, IEvaluable
{
	List<Connection> AccessOpenEnds { get; }

	bool IsConnected(Connection otherEnd);

	void OnNeighborPlaced(SmallGrid neighbor);

	void OnNeighborRemoved(SmallGrid neighbor);

	void FillConnected<T>(Span<SmallCellRef> buf, ref int count) where T : ISmallGrid;
}
