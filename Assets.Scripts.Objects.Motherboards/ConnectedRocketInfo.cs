using System;
using System.Text;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Objects.Rockets;

namespace Assets.Scripts.Objects.Motherboards;

public class ConnectedRocketInfo : IEquatable<ConnectedRocketInfo>
{
	public RocketAvionicsDevice Avionics;

	public RocketDataDownLink DownLink;

	public ILogicable SelectedLogicable;

	public static ConnectedRocketInfo Invalid = new ConnectedRocketInfo(null, null, null);

	public bool IsValid
	{
		get
		{
			if (Avionics != null && DownLink != null)
			{
				return !Avionics.Rocket.BeingDestroyed;
			}
			return false;
		}
	}

	public static ConnectedRocketInfo Create(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		Network.ReadPackedId(reader, out var referenceId2);
		Network.ReadPackedId(reader, out var referenceId3);
		RocketAvionicsDevice avionics = Thing.Find<RocketAvionicsDevice>(referenceId);
		RocketDataDownLink downLink = Thing.Find<RocketDataDownLink>(referenceId2);
		ILogicable selectedLogicable = Thing.Find<ILogicable>(referenceId3);
		return new ConnectedRocketInfo(avionics, downLink, selectedLogicable);
	}

	public void Write(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, Avionics);
		Network.WritePackedId(writer, DownLink);
		Network.WritePackedId(writer, SelectedLogicable);
	}

	public ConnectedRocketInfo(ConnectedRocketReference rocketReference)
	{
		Avionics = Thing.Find<RocketAvionicsDevice>(rocketReference.Avionics);
		DownLink = Thing.Find<RocketDataDownLink>(rocketReference.DownLink);
		SelectedLogicable = Thing.Find<ILogicable>(rocketReference.SelectedLogicable);
	}

	public ConnectedRocketInfo(RocketAvionicsDevice avionics, RocketDataDownLink downLink, ILogicable selectedLogicable)
	{
		Avionics = avionics;
		DownLink = downLink;
		SelectedLogicable = selectedLogicable;
	}

	public string Tooltip()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < DownLink.ConnectedDataNetReceivers.Count; i++)
		{
			IReceiveDataNetworkDevices receiveDataNetworkDevices = DownLink.ConnectedDataNetReceivers[i];
			stringBuilder.Append("<color=").Append(receiveDataNetworkDevices.DataConnectionActive() ? "green" : "red").Append(">");
			stringBuilder.Append(receiveDataNetworkDevices.DisplayName);
			stringBuilder.Append("</color>, ");
			if (i < DownLink.ConnectedDataNetReceivers.Count - 1)
			{
				stringBuilder.Append(", ");
			}
			else
			{
				stringBuilder.AppendLine();
			}
		}
		stringBuilder.AppendLine(GameStrings.ConnectedRocketDownlink.AsString(DownLink.DisplayName.AsColor("green")));
		stringBuilder.AppendLine(GameStrings.ConnectedRocketAvionics.AsString(Avionics.DisplayName.AsColor("green")));
		return stringBuilder.ToString();
	}

	public bool Equals(ConnectedRocketInfo other)
	{
		if ((object)Avionics == null && (object)DownLink == null && (object)other?.Avionics == null && (object)other?.DownLink == null)
		{
			return true;
		}
		if (Avionics?.Rocket != null && other?.Avionics?.Rocket != null && Avionics.Rocket.ReferenceId == other.Avionics.Rocket.ReferenceId && (object)DownLink != null && (object)other.DownLink != null)
		{
			return DownLink.ReferenceId == other.DownLink.ReferenceId;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ConnectedRocketInfo other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Avionics, DownLink);
	}
}
