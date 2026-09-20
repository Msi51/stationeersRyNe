using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;

namespace Objects.Rockets.Log.RocketEvents;

public class CargoStorageFullEvent : RocketEvent
{
	private readonly RocketChuteStorage _chuteStorage;

	private readonly string _fullReferencableDisplayName;

	public override string TextColor => "yellow";

	public override RocketEventType RocketEventType => RocketEventType.CargoStorageFull;

	public CargoStorageFullEvent(IReferencable eventOrigin, RocketChuteStorage chuteStorage)
		: base(eventOrigin)
	{
		_chuteStorage = chuteStorage;
		_fullReferencableDisplayName = _chuteStorage.DisplayName;
		Hash = (Hash ^ 9) * 41;
		Hash = (Hash ^ (int)(_chuteStorage?.ReferenceId ?? 0)) * 41;
	}

	public CargoStorageFullEvent(RocketBinaryReader reader)
		: base(reader)
	{
		if (reader.ReadBoolean())
		{
			Network.ReadPackedId(reader, out var referenceId);
			_chuteStorage = Referencable.Find<RocketChuteStorage>(referenceId);
			_fullReferencableDisplayName = _chuteStorage?.DisplayName ?? string.Empty;
		}
		else
		{
			_fullReferencableDisplayName = reader.ReadString();
		}
	}

	protected override void Write(RocketBinaryWriter writer)
	{
		base.Write(writer);
		bool flag = _chuteStorage != null && !_chuteStorage.BeingDestroyed;
		writer.WriteBoolean(flag);
		if (flag)
		{
			Network.WritePackedId(writer, _chuteStorage.ReferenceId);
		}
		else
		{
			writer.WriteString(_fullReferencableDisplayName);
		}
	}

	public override string GetText()
	{
		return GameStrings.RocketLogDeviceFull.AsString(_fullReferencableDisplayName);
	}
}
