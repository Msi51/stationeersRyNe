using UnityEngine;

namespace Networks;

public class RocketData
{
	public RocketNetwork Network;

	public Vector3 Offset;

	public RocketData(RocketNetwork network, Vector3 offset)
	{
		Network = network;
		Offset = offset;
	}
}
