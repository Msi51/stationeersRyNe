using Assets.Scripts.Networking;
using Objects.Rockets;
using Objects.Rockets.Mining;

namespace Assets.Scripts.Serialization;

public class NodeTransit : INetworkNullable
{
	public SpaceMapNode From;

	public SpaceMapNode Destination;

	private readonly float _distance = float.NaN;

	public float GetDistance()
	{
		return _distance * 2f;
	}

	public NodeTransit(SpaceMapNode from, SpaceMapNode destination, float distance)
	{
		From = from;
		Destination = destination;
		_distance = distance;
	}

	public NodeTransit(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		Network.ReadPackedId(reader, out var referenceId2);
		_distance = reader.ReadSingle();
		From = SpaceMapNode.Get(referenceId);
		Destination = SpaceMapNode.Get(referenceId2);
	}

	public void Write(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, From);
		Network.WritePackedId(writer, Destination);
		writer.WriteSingle(_distance);
	}
}
