using System.Collections.Generic;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using Assets.Scripts.UI.CustomScrollPanel;
using Assets.Scripts.Util;
using Objects.Electrical;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Items;

public class Cartridge : CharacterItem, ISpatial, IPhysical, IProfile, IDensePoolable
{
	[Header("Cartridge")]
	[ReadOnly]
	public Tablet Tablet;

	public RectTransform RectTransform;

	public Scrollbar Scrollbar;

	public TextMeshProUGUI SelectedTitle;

	public static List<Cartridge> AllCartridgePrefabs = new List<Cartridge>();

	public static List<Cartridge> AllCartridges = new List<Cartridge>();

	[SerializeField]
	protected ScrollPanel _scrollPanel;

	private const float SCREEN_UPDATE_DISTANCE = 4f;

	private bool InUpdateRange { get; set; }

	public virtual float ScanTime => 0f;

	public virtual string ScanActionText => string.Empty;

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		AllCartridges.Add(this);
	}

	public override void OnDestroy()
	{
		AllCartridges.Remove(this);
		base.OnDestroy();
	}

	public new static void ClearAll()
	{
		AllCartridges.Clear();
	}

	public void OnScroll(Vector2 scrollDelta)
	{
		if ((bool)_scrollPanel && !(scrollDelta == Vector2.zero))
		{
			_scrollPanel.OnScroll(scrollDelta);
		}
	}

	public virtual void OnMainTick()
	{
		InUpdateRange = CheckUpdateRange();
	}

	private bool CheckUpdateRange()
	{
		if (Tablet == null)
		{
			return false;
		}
		if (Tablet.CurrentCameraDistanceSquared > 16f)
		{
			return false;
		}
		int num = 0;
		for (int num2 = AllCartridges.Count - 1; num2 >= 0; num2--)
		{
			Cartridge cartridge = AllCartridges[num2];
			if (!(cartridge.Tablet == null))
			{
				if (cartridge != this && cartridge.Tablet.CurrentCameraDistanceSquared < Tablet.CurrentCameraDistanceSquared)
				{
					num++;
				}
				if (num > 3)
				{
					return false;
				}
			}
		}
		return true;
	}

	public virtual void OnTabletScrollUp()
	{
	}

	public virtual void OnTabletScrollDown()
	{
	}

	public override string GetQuantityText()
	{
		return string.Empty;
	}

	public override string GetSecondaryNameText()
	{
		return Localization.GetThingName(PrefabName);
	}

	public virtual void OnInsertedInTablet()
	{
		if (!(Tablet == null))
		{
			Tablet.TransferScreen();
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.CartridgeCategory);
	}

	public virtual void OnRemovedFromTablet(Tablet oldParent)
	{
		oldParent.RemoveScreen(this);
	}

	public override void UpdateEachFrame()
	{
		if (!WorldManager.IsGamePaused)
		{
			base.UpdateEachFrame();
			if (!GameManager.IsBatchMode && (bool)Tablet && !Tablet.IsOccluded && Tablet.OnOff && Tablet.Powered && (InPlayerHand() || InUpdateRange))
			{
				OnScreenUpdate();
			}
		}
	}

	protected bool InPlayerHand()
	{
		Human rootParentHuman = RootParentHuman;
		if (rootParentHuman != null && rootParentHuman == InventoryManager.ParentHuman && Tablet != null && Tablet.ParentSlot != null)
		{
			return Tablet.ParentSlot.IsHandSlot;
		}
		return false;
	}

	public override void OnThreadUpdate()
	{
		base.OnThreadUpdate();
		if ((bool)Tablet && !Tablet.IsOccluded && Tablet.OnOff && Tablet.Powered)
		{
			OnPreScreenUpdate();
		}
	}

	public virtual void OnScreenUpdate()
	{
	}

	protected void UpdateCachedPosition()
	{
		Rotation = base.ActiveRigidbody.rotation;
		base.Position = base.ActiveRigidbody.position;
		Forward = ThingTransform.forward;
	}

	public virtual void OnPreScreenUpdate()
	{
	}

	public virtual void OnTabletScanned(Thing thing)
	{
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		Tablet tablet = parent as Tablet;
		if ((bool)tablet)
		{
			Tablet = tablet;
			OnInsertedInTablet();
		}
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		Tablet tablet = oldParent as Tablet;
		if ((bool)tablet)
		{
			Tablet = null;
			OnRemovedFromTablet(tablet);
		}
	}
}
