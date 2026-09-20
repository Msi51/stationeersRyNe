using System.Collections.Generic;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Objects.Electrical;

public class PylonNode
{
	public byte Index;

	public IPylonNodeOwner Owner;

	public readonly List<PylonConnection> Connections = new List<PylonConnection>();

	public Transform Transform { get; }

	public int MaxConnections { get; }

	public bool CanConnect => Connections.Count < MaxConnections;

	public bool IsValid
	{
		get
		{
			Structure structure = Owner.AsStructure();
			if (!structure.BeingDestroyed)
			{
				return structure.IsStructureCompleted;
			}
			return false;
		}
	}

	public bool IsConnectedTo(PylonNode other)
	{
		foreach (PylonConnection connection in Connections)
		{
			if (connection.GetOther(this) == other)
			{
				return true;
			}
		}
		return false;
	}

	public PylonNode(byte index, IPylonNodeOwner owner, Transform transform, int maxConnections)
	{
		Index = index;
		Owner = owner;
		Transform = transform;
		MaxConnections = maxConnections;
	}
}
