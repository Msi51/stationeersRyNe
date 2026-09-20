using System;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;

namespace Objects.Rockets.Log.RocketEvents;

public class ReachedDestinationEvent : RocketEvent
{
	private readonly SpaceMapNode _currentNode;

	private readonly string _currentNodeName;

	public override RocketEventType RocketEventType => RocketEventType.ReachedDestination;

	public override string TextColor => "green";

	public override string GetText()
	{
		return GameStrings.RocketLogArrived.AsString(EventOriginName, _currentNodeName);
	}

	public ReachedDestinationEvent(IReferencable eventOrigin, SpaceMapNode currentNode)
		: base(eventOrigin)
	{
		_currentNode = currentNode;
		_currentNodeName = currentNode?.DisplayName ?? string.Empty;
		Hash = (Hash ^ 3) * 41;
		Hash = (Hash ^ (int)(_currentNode?.ReferenceId ?? 0)) * 41;
		int hash = Hash;
		DateTime dateTime = _dateTime;
		Hash = (hash ^ dateTime.GetHashCode()) * 41;
	}

	protected override void Write(RocketBinaryWriter writer)
	{
		base.Write(writer);
		SpaceMapNode currentNode = _currentNode;
		bool flag = currentNode != null && !currentNode.BeingDestroyed;
		writer.WriteBoolean(flag);
		if (flag)
		{
			Network.WritePackedId(writer, _currentNode);
		}
		else
		{
			writer.WriteString(_currentNodeName);
		}
	}

	public ReachedDestinationEvent(RocketBinaryReader reader)
		: base(reader)
	{
		if (reader.ReadBoolean())
		{
			Network.ReadPackedId(reader, out var referenceId);
			_currentNode = Referencable.Find<SpaceMapNode>(referenceId);
			_currentNodeName = _currentNode?.DisplayName ?? string.Empty;
		}
		else
		{
			_currentNodeName = reader.ReadString();
		}
	}
}
