using Assets.Scripts.Inventory;
using Assets.Scripts.Util;
using TerrainSystem;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Gps : Cartridge
{
	public TextMeshProUGUI PositionTextX;

	public TextMeshProUGUI PositionTextY;

	public TextMeshProUGUI PositionTextZ;

	public TextMeshProUGUI RegionText;

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
		Vector3 location = GetLocation();
		PositionTextX.text = StringGenerator.GetString(Mathf.RoundToInt(location.x));
		PositionTextY.text = StringGenerator.GetString(Mathf.RoundToInt(location.y));
		PositionTextZ.text = StringGenerator.GetString(Mathf.RoundToInt(location.z));
		string text = "None";
		GeographicRegionData geographicRegionData = WorldSetting.Current.Data.GeographicRegionData;
		if (geographicRegionData != null)
		{
			text = geographicRegionData.DefaultRegionName;
			if (RegionManager.TryGetRegionAtWorldPosition(geographicRegionData.RegionSet, location, out var region))
			{
				text = region.Name;
			}
		}
		RegionText.text = "Region: " + text;
	}

	public override void OnScreenUpdate()
	{
		base.OnScreenUpdate();
		Redraw();
	}
}
