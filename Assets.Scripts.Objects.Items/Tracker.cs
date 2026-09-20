using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using TerrainSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Items;

public class Tracker : Cartridge
{
	public TextMeshProUGUI DistanceText;

	public Image Arrow;

	public int CurrentIndex;

	public Thing CurrentlyTracked;

	public TextMeshProUGUI PositionTextX;

	public TextMeshProUGUI PositionTextY;

	public TextMeshProUGUI PositionTextZ;

	public TextMeshProUGUI RegionText;

	private Vector3 _baseRotation;

	private string _cachedDistance;

	private float _cachedDirection;

	private static List<ITrackable> Trackables => ITrackable.Trackables;

	public override void Awake()
	{
		base.Awake();
		_baseRotation = Arrow.transform.localEulerAngles;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		SetupWhenReady().Forget();
	}

	private async UniTaskVoid SetupWhenReady()
	{
		while (GameManager.GameState != GameState.Running)
		{
			await UniTask.NextFrame();
		}
		await UniTask.NextFrame();
		CurrentIndex = 0;
		Redraw();
	}

	public override void OnTabletScrollUp()
	{
		base.OnTabletScrollUp();
		if ((bool)CurrentlyTracked)
		{
			CurrentIndex = Trackables.FindIndex((ITrackable t) => t as Thing == CurrentlyTracked);
		}
		CurrentIndex++;
		if (CurrentIndex >= Trackables.Count)
		{
			CurrentIndex = 0;
		}
		CurrentIndex = Mathf.Clamp(CurrentIndex, 0, Trackables.Count);
		if (Trackables.Count > 1 && Tablet.OnOff && Tablet.Powered)
		{
			Tablet.PlaySound(Tablet.ScrollUpHash);
		}
		Redraw();
	}

	public override void OnTabletScrollDown()
	{
		base.OnTabletScrollDown();
		if ((bool)CurrentlyTracked)
		{
			CurrentIndex = Trackables.FindIndex((ITrackable t) => t as Thing == CurrentlyTracked);
		}
		CurrentIndex--;
		if (CurrentIndex < 0)
		{
			CurrentIndex = Trackables.Count - 1;
		}
		CurrentIndex = Mathf.Clamp(CurrentIndex, 0, Trackables.Count);
		if (Trackables.Count > 1 && Tablet.OnOff && Tablet.Powered)
		{
			Tablet.PlaySound(Tablet.ScrollDownHash);
		}
		Redraw();
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		Redraw();
	}

	public void Redraw()
	{
		if (!Tablet || base.BeingDestroyed || (Tablet.ParentSlot != null && Tablet.ParentSlot != InventoryManager.Instance.ActiveHand.Slot))
		{
			return;
		}
		Vector3 vector = base.Position;
		PositionTextX.text = StringGenerator.GetString(Mathf.RoundToInt(vector.x));
		PositionTextY.text = StringGenerator.GetString(Mathf.RoundToInt(vector.y));
		PositionTextZ.text = StringGenerator.GetString(Mathf.RoundToInt(vector.z));
		string arg = GameStrings.NotApplicableString;
		GeographicRegionData geographicRegionData = WorldSetting.Current.Data.GeographicRegionData;
		if (geographicRegionData != null)
		{
			arg = geographicRegionData.DefaultRegionName;
			if (RegionManager.TryGetRegionAtWorldPosition(geographicRegionData.RegionSet, position, out var region))
			{
				arg = region.Name;
			}
		}
		RegionText.text = GameStrings.RegionName.AsString(arg);
		if (Trackables.Count <= 0)
		{
			return;
		}
		if (CurrentIndex >= Trackables.Count)
		{
			CurrentIndex = 0;
		}
		CurrentlyTracked = Trackables[CurrentIndex] as Thing;
		if ((bool)CurrentlyTracked)
		{
			SelectedTitle.text = CurrentlyTracked.TrackableName;
			SelectedTitle.gameObject.SetActive(value: true);
		}
		else
		{
			SelectedTitle.text = "None";
			SelectedTitle.gameObject.SetActive(value: false);
		}
		if (!CurrentlyTracked || (CurrentlyTracked.HasPowerState && !CurrentlyTracked.Powered) || CurrentlyTracked == InventoryManager.ParentBrain)
		{
			if (Arrow.isActiveAndEnabled)
			{
				Arrow.enabled = false;
			}
		}
		else if (!Arrow.isActiveAndEnabled)
		{
			Arrow.enabled = true;
		}
	}

	public override void OnPreScreenUpdate()
	{
		base.OnPreScreenUpdate();
		if ((bool)CurrentlyTracked && (bool)RootParent)
		{
			_cachedDistance = Vector3.Distance(CurrentlyTracked.RootParent.Position, RootParent.Position).ToStringPrefix("m", "yellow");
			_cachedDirection = Vector3.Angle(CurrentlyTracked.RootParent.Position, RootParent.Position);
		}
		else
		{
			_cachedDistance = "-";
		}
	}

	public override void OnScreenUpdate()
	{
		base.OnScreenUpdate();
		base.Position = Transform.position;
		Redraw();
		if (!CurrentlyTracked || (CurrentlyTracked.HasPowerState && !CurrentlyTracked.Powered))
		{
			DistanceText.text = "?";
			return;
		}
		DistanceText.text = _cachedDistance;
		Vector3 vector = CurrentlyTracked.RootParent.ThingTransformPosition - RootParent.ThingTransformPosition;
		Vector3 vector2 = ((RootParent is Entity entity) ? entity.EntityForward : RootParent.ThingTransform.forward);
		float num = Mathf.Atan2(vector.z, vector.x) * 57.29578f;
		float num2 = Mathf.Atan2(vector2.z, vector2.x) * 57.29578f;
		Arrow.transform.localRotation = Quaternion.AngleAxis(num - num2, Vector3.forward);
	}
}
