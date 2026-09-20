using System;
using Assets.Scripts;
using Assets.Scripts.Networking;

namespace Objects.Rockets.Log.RocketEvents;

public abstract class RocketEvent : ISyncListable
{
	public static SyncList<RocketEvent> NewToSend = new SyncList<RocketEvent>(Create);

	public readonly IReferencable EventOrigin;

	public readonly string EventOriginName;

	public readonly DateTime _dateTime;

	public int Hash;

	public const int ChecksumPrime = 41;

	public abstract string TextColor { get; }

	public abstract RocketEventType RocketEventType { get; }

	public abstract string GetText();

	public override string ToString()
	{
		return $"<color=grey>{_dateTime:hh:mm dd/MM/yyyy} {EventOriginName}</color> <color={TextColor}>{GetText()}</color>";
	}

	public RocketEvent(IReferencable eventOrigin)
	{
		EventOrigin = eventOrigin;
		EventOriginName = eventOrigin?.DisplayName ?? string.Empty;
		_dateTime = DateTime.Now;
		Hash = (Hash ^ (int)(EventOrigin?.ReferenceId ?? 0)) * 41;
	}

	public RocketEvent(RocketBinaryReader reader)
	{
		bool num = reader.ReadBoolean();
		Hash = reader.ReadInt32();
		if (num)
		{
			Network.ReadPackedId(reader, out var referenceId);
			EventOrigin = Referencable.Find<Rocket>(referenceId);
			EventOriginName = EventOrigin?.DisplayName ?? string.Empty;
		}
		else
		{
			EventOriginName = reader.ReadString();
		}
		Network.ReadDateTime(reader, out var dateTime);
		_dateTime = dateTime;
	}

	protected virtual void Write(RocketBinaryWriter writer)
	{
		IReferencable eventOrigin = EventOrigin;
		bool flag = eventOrigin != null && !eventOrigin.BeingDestroyed;
		writer.WriteBoolean(flag);
		writer.WriteInt32(Hash);
		if (flag)
		{
			Network.WritePackedId(writer, EventOrigin);
		}
		else
		{
			writer.WriteString(EventOriginName);
		}
		Network.WriteDateTime(writer, _dateTime);
	}

	public void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteByte((byte)RocketEventType);
		Write(writer);
	}

	public static void Create(RocketBinaryReader reader)
	{
		RocketEventType rocketEventType = (RocketEventType)reader.ReadByte();
		RocketEvent rocketEvent = null;
		RocketLog.Append(rocketEventType switch
		{
			RocketEventType.FuelDepleted => new FuelDepletedEvent(reader), 
			RocketEventType.BatteryDepleted => new BatteryDepletedEvent(reader), 
			RocketEventType.ReachedDestination => new ReachedDestinationEvent(reader), 
			RocketEventType.ActionReport => new ActionReportEvent(reader), 
			RocketEventType.NavPointChart => new NavPointChartEvent(reader), 
			RocketEventType.PipeFail => new PipeFailEvent(reader), 
			RocketEventType.RocketLandAborted => new RocketLandAbortedEvent(reader), 
			RocketEventType.RocketCrashed => new RocketCrashedEvent(reader), 
			RocketEventType.CargoStorageFull => new CargoStorageFullEvent(reader), 
			RocketEventType.Deploy => new RocketDeployEvent(reader), 
			_ => throw new NullReferenceException("Unable to deserialize rocket event"), 
		}).Forget();
	}
}
