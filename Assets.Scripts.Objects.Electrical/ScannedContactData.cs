using Assets.Scripts.Networking;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Electrical;

public class ScannedContactData
{
	private TraderContact _contact;

	private float _lastScannedDegreeOffset;

	private float _currentTimeTillResolve;

	private float _startTimeTillResolve;

	public const float MAX_TIME_TO_RESOLVE = 99999f;

	public byte NetworkUpdateFlags;

	public TraderContact Contact
	{
		get
		{
			return _contact;
		}
		set
		{
			if (NetworkManager.IsServer)
			{
				NetworkUpdateFlags |= 1;
			}
			_contact = value;
		}
	}

	public float LastScannedDegreeOffset
	{
		get
		{
			return _lastScannedDegreeOffset;
		}
		set
		{
			if (NetworkManager.IsServer && !RocketMath.Approximately(LastScannedDegreeOffset, value))
			{
				NetworkUpdateFlags |= 2;
			}
			_lastScannedDegreeOffset = value;
		}
	}

	public float CurrentTimeTillResolve
	{
		get
		{
			return _currentTimeTillResolve;
		}
		set
		{
			if (NetworkManager.IsServer && !RocketMath.Approximately(CurrentTimeTillResolve, value))
			{
				NetworkUpdateFlags |= 4;
			}
			_currentTimeTillResolve = value;
		}
	}

	public float StartTimeTillResolve
	{
		get
		{
			return _startTimeTillResolve;
		}
		set
		{
			if (NetworkManager.IsServer && !RocketMath.Approximately(StartTimeTillResolve, value))
			{
				NetworkUpdateFlags |= 8;
			}
			_startTimeTillResolve = value;
		}
	}

	public float ResolutionPercentage => 1f - CurrentTimeTillResolve / StartTimeTillResolve;

	public static bool IsNetworkUpdate(byte toCheck, byte networkUpdateType)
	{
		return (toCheck & networkUpdateType) != 0;
	}

	public void Write(RocketBinaryWriter writer)
	{
		byte networkUpdateFlags = NetworkUpdateFlags;
		NetworkUpdateFlags = 0;
		writer.WriteByte(networkUpdateFlags);
		if (IsNetworkUpdate(networkUpdateFlags, 1))
		{
			writer.WriteInt64(Contact.ReferenceId);
		}
		if (IsNetworkUpdate(networkUpdateFlags, 2))
		{
			writer.WriteSingle(LastScannedDegreeOffset);
		}
		if (IsNetworkUpdate(networkUpdateFlags, 4))
		{
			writer.WriteSingle(CurrentTimeTillResolve);
		}
		if (IsNetworkUpdate(networkUpdateFlags, 8))
		{
			writer.WriteSingle(StartTimeTillResolve);
		}
	}

	public void Read(RocketBinaryReader reader)
	{
		byte toCheck = reader.ReadByte();
		if (IsNetworkUpdate(toCheck, 1))
		{
			Contact = Referencable.Find<TraderContact>(reader.ReadInt64());
		}
		if (IsNetworkUpdate(toCheck, 2))
		{
			LastScannedDegreeOffset = reader.ReadSingle();
		}
		if (IsNetworkUpdate(toCheck, 4))
		{
			CurrentTimeTillResolve = reader.ReadSingle();
		}
		if (IsNetworkUpdate(toCheck, 8))
		{
			StartTimeTillResolve = reader.ReadSingle();
		}
	}
}
