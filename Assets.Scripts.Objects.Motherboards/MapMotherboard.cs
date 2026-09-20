using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Objects.Items;
using UI.Motherboard;
using UnityEngine;

namespace Assets.Scripts.Objects.Motherboards;

public class MapMotherboard : Motherboard
{
	[SerializeField]
	private MapMotherboardPanel _panel;

	public override bool IsOperable => true;

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		_panel.Initialise();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		_panel.CleanUp();
	}

	public override void Update1000MS(float deltaTime)
	{
		base.Update1000MS(deltaTime);
		_panel.UpdateTrackables();
	}

	public void MapUpdate(float orbitalPosition)
	{
		Thing thing = ParentComputer.AsThing();
		if (thing.OnOff && thing.Powered && thing.Error == 0)
		{
			(int, int) mapPos = RocketDeepScanningHead.GetMapPos(orbitalPosition * 0.1f, 64, 0.9f);
			_panel.UpdateMask(mapPos.Item1, mapPos.Item2);
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		string maskTextureSaveData = _panel.GetMaskTextureSaveData();
		writer.WriteString(maskTextureSaveData);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		string data = reader.ReadString();
		_panel.LoadMaskTextureSaveData(data);
		_panel.LoadToggles(deepMinables: false, players: false);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new MapMotherboardSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is MapMotherboardSaveData mapMotherboardSaveData)
		{
			_panel.LoadMaskTextureSaveData(mapMotherboardSaveData.MaskTexture);
			_panel.LoadToggles(mapMotherboardSaveData.DeepMinablesToggle, mapMotherboardSaveData.PlayersToggle);
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is MapMotherboardSaveData mapMotherboardSaveData)
		{
			mapMotherboardSaveData.MaskTexture = _panel.GetMaskTextureSaveData();
			mapMotherboardSaveData.DeepMinablesToggle = _panel.DeepMinablesToggle.On;
			mapMotherboardSaveData.PlayersToggle = _panel.TrackablesToggle.On;
		}
	}
}
