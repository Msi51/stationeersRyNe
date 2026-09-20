using Assets.Scripts.Networking;

namespace Assets.Scripts.Objects.Motherboards;

public readonly struct ConnectedRocketReference
{
	public readonly long Avionics;

	public readonly long DownLink;

	public readonly long SelectedLogicable;

	public static ConnectedRocketReference Invalid = new ConnectedRocketReference(0L, 0L, 0L);

	public bool IsValid
	{
		get
		{
			if (Avionics != 0L && DownLink != 0L)
			{
				return SelectedLogicable != 0;
			}
			return false;
		}
	}

	private ConnectedRocketReference(long avionics, long downLink, long selectedLogicable)
	{
		Avionics = avionics;
		DownLink = downLink;
		SelectedLogicable = selectedLogicable;
	}

	public static ConnectedRocketReference Create(ConnectedRocketInfo rocketInfo)
	{
		return new ConnectedRocketReference(rocketInfo.Avionics.ReferenceId, rocketInfo.DownLink.ReferenceId, rocketInfo.SelectedLogicable.ReferenceId);
	}

	public static ConnectedRocketReference Create(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		Network.ReadPackedId(reader, out var referenceId2);
		Network.ReadPackedId(reader, out var referenceId3);
		return new ConnectedRocketReference(referenceId, referenceId2, referenceId3);
	}

	public void Write(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, Avionics);
		Network.WritePackedId(writer, DownLink);
		Network.WritePackedId(writer, SelectedLogicable);
	}
}
