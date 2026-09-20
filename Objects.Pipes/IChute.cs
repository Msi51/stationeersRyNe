using System.Collections.Generic;
using Assets.Scripts.Objects;

namespace Objects.Pipes;

public interface IChute
{
	Slot TransportSlot { get; }

	List<Connection> SmallGridOpenEnds { get; }

	void SetNeighbor(SmallGrid sendingNeighbor);
}
