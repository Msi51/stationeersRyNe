using System.Collections.Generic;
using Assets.Scripts.Inventory;
using Assets.Scripts.UI.Motherboard;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Items;

public class AccessController : Cartridge
{
	private string _selectedText = string.Empty;

	private string _unSelectedText = "NONE";

	public GridLayoutGroup ColorGrid;

	private Dictionary<int, ScreenColorItem> _moleDisplay = new Dictionary<int, ScreenColorItem>();

	private HashSet<ScreenColorItem> _gasItems = new HashSet<ScreenColorItem>();

	public ScreenColorItem ColorItemPrefab;

	public Image ColorSelected;

	public Text ColorText;

	public Image ButtonAdd;

	public Image ButtomRemove;

	private GameObject _gridGroupGameObject;

	private int MaxColors;

	private int _selectedColor;

	private int _selectedColorBit;

	private string _selectedColorText;

	public Thing ScannedThing
	{
		get
		{
			if (!RootParent || !RootParent.HasAuthority || !CursorManager.CursorThing || !CursorManager.CursorThing.HasAccessState)
			{
				return null;
			}
			return CursorManager.CursorThing;
		}
	}

	public override float ScanTime => 0.5f;

	public override string ScanActionText
	{
		get
		{
			if (!ScannedThing)
			{
				return string.Empty;
			}
			return $"{(ScannedThing.HasAccess(_selectedColorBit) ? ActionStrings.Remove : ActionStrings.Add)} {_selectedColorText}";
		}
	}

	public override void Awake()
	{
		base.Awake();
		List<ColorSwatch> customColors = Singleton<GameManager>.Instance.CustomColors;
		for (int i = 0; i < customColors.Count; i++)
		{
			ColorSwatch colorSwatch = customColors[i];
			ScreenColorItem screenColorItem = Object.Instantiate(ColorItemPrefab, ColorGrid.transform);
			screenColorItem.Image.color = colorSwatch.Color;
			screenColorItem.Parent.SetActive(value: true);
			screenColorItem.Bit = colorSwatch.Bit;
			_moleDisplay.Add(colorSwatch.Index, screenColorItem);
			_gasItems.Add(screenColorItem);
		}
		MaxColors = customColors.Count;
		RefreshColor();
		_gridGroupGameObject = ColorGrid.gameObject;
	}

	public override void OnPreScreenUpdate()
	{
		base.OnPreScreenUpdate();
		if (ScannedThing == null)
		{
			Clear();
		}
		else
		{
			_selectedText = ScannedThing.DisplayName.ToUpper();
		}
	}

	private void Clear()
	{
		_selectedText = _unSelectedText;
	}

	public override void OnScreenUpdate()
	{
		base.OnScreenUpdate();
		SelectedTitle.text = _selectedText;
		if ((bool)ScannedThing)
		{
			ButtonAdd.enabled = !ScannedThing.HasAccess(_selectedColorBit);
			ButtomRemove.enabled = ScannedThing.HasAccess(_selectedColorBit);
			_gridGroupGameObject.SetActive(value: true);
			{
				foreach (ScreenColorItem gasItem in _gasItems)
				{
					gasItem.Parent.SetActive(ScannedThing.HasAccess(gasItem.Bit));
				}
				return;
			}
		}
		ButtonAdd.enabled = false;
		ButtomRemove.enabled = false;
		_gridGroupGameObject.SetActive(value: false);
	}

	public override void OnTabletScrollUp()
	{
		base.OnTabletScrollUp();
		_selectedColor++;
		if (_selectedColor >= MaxColors)
		{
			_selectedColor = 0;
		}
		if (Tablet.OnOff && Tablet.Powered)
		{
			Tablet.PlaySound(Tablet.ScrollUpHash);
		}
		RefreshColor();
	}

	public override void OnTabletScrollDown()
	{
		base.OnTabletScrollDown();
		_selectedColor--;
		if (_selectedColor < 0)
		{
			_selectedColor = MaxColors - 1;
		}
		if (Tablet.OnOff && Tablet.Powered)
		{
			Tablet.PlaySound(Tablet.ScrollDownHash);
		}
		RefreshColor();
	}

	public override void OnTabletScanned(Thing thing)
	{
		base.OnTabletScanned(thing);
		if (thing.HasAccess(_selectedColorBit))
		{
			thing.RemoveAccess(_selectedColorBit);
		}
		else
		{
			thing.GiveAccess(_selectedColorBit);
		}
	}

	private void RefreshColor()
	{
		ColorSwatch colorSwatch = GameManager.GetColorSwatch(_selectedColor);
		ColorSelected.color = colorSwatch.Color;
		ColorText.text = colorSwatch.DisplayName;
		_selectedColorBit = colorSwatch.Bit;
		_selectedColorText = colorSwatch.DisplayName;
	}
}
