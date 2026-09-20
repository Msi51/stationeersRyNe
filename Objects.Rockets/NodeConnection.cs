using Assets.Scripts;
using Assets.Scripts.Networking;
using Objects.Rockets.Mining;

namespace Objects.Rockets;

public class NodeConnection : INetworkNullable
{
	private float _distance;

	public SpaceMapNode Parent { get; }

	public SpaceMapNode Child { get; }

	public int Difficulty { get; }

	public ConnectionType ConnectionType { get; }

	public float Distance()
	{
		return _distance * ((ConnectionType == ConnectionType.Standard) ? ((float)DifficultySetting.Current.SpaceMapDistanceMultiplier) : 1f);
	}

	public NodeConnection(SpaceMapNode child, SpaceMapNode parent, float distance, ConnectionType type, int difficulty = 0)
	{
		Parent = parent;
		Child = child;
		_distance = distance;
		ConnectionType = type;
		Difficulty = difficulty;
	}

	public NodeConnection(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		Network.ReadPackedId(reader, out var referenceId2);
		Parent = Referencable.Find<SpaceMapNode>(referenceId);
		Child = Referencable.Find<SpaceMapNode>(referenceId2);
		_distance = reader.ReadSingle();
		Difficulty = reader.ReadUInt16();
		ConnectionType = (ConnectionType)reader.ReadByte();
		Parent.ChildConnections.Add(this);
	}

	public void Write(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, Parent);
		Network.WritePackedId(writer, Child);
		writer.WriteSingle(_distance);
		writer.WriteUInt16((ushort)Difficulty);
		writer.WriteByte((byte)ConnectionType);
	}
}
