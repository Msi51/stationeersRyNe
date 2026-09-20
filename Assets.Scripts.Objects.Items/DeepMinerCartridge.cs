using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Util;
using TerrainSystem;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class DeepMinerCartridge : Cartridge
{
	public TextMeshProUGUI PositionTextX;

	public TextMeshProUGUI PositionTextY;

	public TextMeshProUGUI PositionTextZ;

	public TextMeshProUGUI RegionText;

	public TextMeshProUGUI InfoText;

	public Vector3 GetLocation()
	{
		if (RootParent != this)
		{
			return RootParent.ThingTransformPosition;
		}
		return base.ThingTransformPosition;
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		Redraw();
	}

	public void Redraw()
	{
		if (Tablet == null || (Tablet.ParentSlot != null && Tablet.ParentSlot != InventoryManager.Instance.ActiveHand.Slot) || PositionTextX == null || PositionTextY == null || PositionTextZ == null)
		{
			return;
		}
		Vector3Int vector3Int = GetLocation().RoundToInt();
		PositionTextX.text = "<color=grey>X</color> " + StringManager.Get(vector3Int.x);
		PositionTextY.text = "<color=grey>Y</color> " + StringManager.Get(vector3Int.y);
		PositionTextZ.text = "<color=grey>Z</color> " + StringManager.Get(vector3Int.z);
		string arg = "None";
		GeographicRegionData geographicRegionData = WorldSetting.Current.Data.GeographicRegionData;
		if (geographicRegionData != null)
		{
			arg = geographicRegionData.DefaultRegionName;
			if (RegionManager.TryGetRegionAtWorldPosition(geographicRegionData.RegionSet, vector3Int, out var region))
			{
				arg = region.Name;
			}
		}
		RegionText.text = GameStrings.RegionName.AsString(arg);
		List<DeepMinablesGenerationData> deepMinablesData = WorldSetting.Current.Data.DeepMinablesData;
		StringBuilder stringBuilder = new StringBuilder();
		foreach (DeepMinablesGenerationData item in deepMinablesData)
		{
			if (item.Evaluate(this))
			{
				item.ReagentAction.ToolTip(stringBuilder, 0);
				break;
			}
		}
		InfoText.text = stringBuilder.ToString();
	}

	public override void OnScreenUpdate()
	{
		base.OnScreenUpdate();
		Redraw();
	}
}
