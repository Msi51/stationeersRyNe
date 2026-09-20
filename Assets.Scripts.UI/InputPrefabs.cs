using System;
using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using Reagents;
using TMPro;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class InputPrefabs : InputWindowBase, IModal
{
	public delegate void InputEvent(DynamicThing prefab);

	public TextMeshProUGUI TitleText;

	public static InputPrefabs Instance;

	public static InputPanelState InputState = InputPanelState.None;

	public RectTransform GroupParents;

	public TMP_InputField SearchBar;

	public static PrefabReference CurrentPrefab;

	public PrefabReference PrefabReference;

	public ControlGroupItem ControlGroupPrefab;

	private Dictionary<SortingClass, RectTransform> _transformLookup = new Dictionary<SortingClass, RectTransform>();

	public static Dictionary<int, PrefabReference> PrefabReferences = new Dictionary<int, PrefabReference>();

	public static Dictionary<int, PrefabReference> ActivePrefabReferences = new Dictionary<int, PrefabReference>();

	public static List<ControlGroupItem> Groups = new List<ControlGroupItem>();

	private List<DynamicThing> ListOfAllowed = new List<DynamicThing>();

	public static SimpleFabricatorBase Fabricator { get; private set; }

	private static MachineTier RecentMachineTier => Fabricator?.CurrentTier ?? MachineTier.TierOne;

	public bool UnlockCursor => true;

	public static event InputEvent OnSubmit;

	public static event Event OnCancel;

	public void Update()
	{
		UITooltipCanvas.Instance?.DoUpdate();
	}

	public override void Initialize()
	{
		base.Initialize();
		Instance = this;
		SetVisible(isVisble: false);
		Array values = Enum.GetValues(typeof(SortingClass));
		List<SortingClass> list = new List<SortingClass>(values.Length);
		foreach (SortingClass item in values)
		{
			list.Add(item);
		}
		list.Sort((SortingClass x, SortingClass y) => string.Compare(x.ToString(), y.ToString(), StringComparison.Ordinal));
		foreach (SortingClass item2 in list)
		{
			ControlGroupItem controlGroupItem = UnityEngine.Object.Instantiate(ControlGroupPrefab, GroupParents);
			controlGroupItem.Title.text = item2.ToString().ToProper();
			_transformLookup.Add(item2, controlGroupItem.RectTransform);
			Groups.Add(controlGroupItem);
		}
		Localization.OnLanguageChanged = (Action)Delegate.Combine(Localization.OnLanguageChanged, new Action(OnLanguageChanged));
		Prefab.OnPrefabsLoaded += PopulateList;
	}

	private static void OnLanguageChanged()
	{
		foreach (PrefabReference value in PrefabReferences.Values)
		{
			value.RefreshString();
		}
	}

	public void SearchTextChanged()
	{
		foreach (int key in ActivePrefabReferences.Keys)
		{
			ActivePrefabReferences[key].SetVisible(isVisble: false);
		}
		ActivePrefabReferences.Clear();
		foreach (DynamicThing item in Instance.ListOfAllowed)
		{
			if (PrefabReferences.ContainsKey(item.PrefabHash))
			{
				PrefabReferences[item.PrefabHash].SetVisible(Instance.SearchBar.text.Equals("") || (item.SourcePrefab.DisplayName.ToLower().Contains(Instance.SearchBar.text.ToLower()) && PrefabReferences[item.PrefabHash].Prefab.RecipeTier <= RecentMachineTier));
				if (PrefabReferences[item.PrefabHash].IsVisible && !ActivePrefabReferences.ContainsKey(item.PrefabHash))
				{
					ActivePrefabReferences.Add(item.PrefabHash, PrefabReferences[item.PrefabHash]);
				}
			}
		}
		foreach (ControlGroupItem group in Groups)
		{
			group.SetVisible(isVisble: true);
			group.SetVisible(group.IsChildVisible);
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(GroupParents);
		SetInputKeyState(!string.IsNullOrWhiteSpace(Instance.SearchBar.text));
	}

	public void PopulateList()
	{
		foreach (int key in PrefabReferences.Keys)
		{
			UnityEngine.Object.Destroy(PrefabReferences[key].gameObject);
		}
		PrefabReferences.Clear();
		foreach (DynamicThing dynamicThingPrefab in DynamicThing.DynamicThingPrefabs)
		{
			if (!(dynamicThingPrefab == null) && !dynamicThingPrefab.CompareTag("NotSpawnable"))
			{
				_transformLookup.TryGetValue(dynamicThingPrefab.SortingClass, out var value);
				PrefabReference prefabReference = UnityEngine.Object.Instantiate(PrefabReference, value);
				prefabReference.SetPrefab(dynamicThingPrefab, Fabricator?.GetRecipeSafe(dynamicThingPrefab) ?? Recipe.INVALID);
				prefabReference.SetVisible(isVisble: false);
				PrefabReferences.Add(prefabReference.PrefabHash, prefabReference);
			}
		}
	}

	public static void GetAllDynamicThings(ref List<DynamicThing> list, Dictionary<MachineTier, List<DynamicThing>> dynamicThings)
	{
		if (list != null)
		{
			foreach (DynamicThing item in list)
			{
				if (PrefabReferences.ContainsKey(item.PrefabHash))
				{
					PrefabReferences[item.PrefabHash].SetVisible(isVisble: false);
				}
			}
			list.Clear();
		}
		foreach (KeyValuePair<MachineTier, List<DynamicThing>> dynamicThing in dynamicThings)
		{
			if (RecentMachineTier >= dynamicThing.Key && dynamicThing.Value != null)
			{
				list.AddRange(dynamicThing.Value);
			}
		}
	}

	public static bool ShowInputPanel(string title, DynamicThing dynamicThing, Dictionary<MachineTier, List<DynamicThing>> dynamicThings, SimpleFabricatorBase fabricator)
	{
		if (InputState != InputPanelState.None)
		{
			return false;
		}
		InputState = InputPanelState.Waiting;
		Instance.SetVisible(isVisble: true);
		MouseModeController.AddModal(Instance);
		SetChildWindowObjActive(isActive: true);
		Instance.TitleText.text = title;
		Fabricator = fabricator;
		GetAllDynamicThings(ref Instance.ListOfAllowed, dynamicThings);
		EventSystem.current.SetSelectedGameObject(Instance.SearchBar.gameObject);
		Instance.SearchTextChanged();
		return true;
	}

	public static bool ShowInputPanelAllDynamicThings(string title)
	{
		if (InputState != InputPanelState.None)
		{
			return false;
		}
		InputState = InputPanelState.Waiting;
		Instance.SetVisible(isVisble: true);
		MouseModeController.AddModal(Instance);
		SetChildWindowObjActive(isActive: true);
		Instance.TitleText.text = title;
		foreach (Thing allPrefab in Prefab.AllPrefabs)
		{
			if (allPrefab is DynamicThing item)
			{
				Instance.ListOfAllowed.Add(item);
			}
		}
		foreach (int key in PrefabReferences.Keys)
		{
			PrefabReferences[key].SetVisible(isVisble: true);
		}
		foreach (ControlGroupItem group in Groups)
		{
			group.SetVisible(group.IsChildVisible);
		}
		return true;
	}

	private static void SetChildWindowObjActive(bool isActive)
	{
		Instance.Transform.GetChild(0).gameObject.SetActive(isActive);
	}

	public void ClearFilter()
	{
		Instance.SearchBar.text = string.Empty;
		SearchTextChanged();
	}

	public static void CancelInput()
	{
		Instance.ButtonInputCancel();
	}

	public void ButtonInputCancel()
	{
		InputState = InputPanelState.Cancelled;
		if (InputPrefabs.OnCancel != null)
		{
			InputPrefabs.OnCancel = null;
		}
		foreach (int key in PrefabReferences.Keys)
		{
			PrefabReferences[key].SetVisible(isVisble: false);
		}
		foreach (ControlGroupItem group in Groups)
		{
			group.SetVisible(group.IsChildVisible);
		}
		Instance.SetVisible(isVisble: false);
		MouseModeController.RemoveModal(Instance);
		Instance.SearchBar.text = "";
		InputPrefabs.OnCancel = null;
		InputPrefabs.OnSubmit = null;
		InputState = InputPanelState.None;
	}

	public static void Submit()
	{
		InputState = InputPanelState.Submitted;
		Instance.SetVisible(isVisble: false);
		MouseModeController.RemoveModal(Instance);
		Instance.SearchBar.text = "";
		foreach (int key in PrefabReferences.Keys)
		{
			PrefabReferences[key].SetVisible(isVisble: false);
		}
		foreach (ControlGroupItem group in Groups)
		{
			group.SetVisible(group.IsChildVisible);
		}
		if (InputPrefabs.OnSubmit != null)
		{
			InputPrefabs.OnSubmit((CurrentPrefab != null) ? CurrentPrefab.Prefab : null);
		}
		CurrentPrefab = null;
		InputPrefabs.OnCancel = null;
		InputPrefabs.OnSubmit = null;
		InputState = InputPanelState.None;
	}
}
