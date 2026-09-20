using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI.Motherboard;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Motherboards;

public class SorterMotherboard : MultiScreenMotherboard
{
	public VerticalLayoutGroup SorterGroup;

	[Header("Screen Prefabs")]
	public ScreenSorter ScreenSorterPrefab;

	public ScreenFilter ScreenFilterPrefab;

	public List<Sorter> AssignedSorters = new List<Sorter>();

	private List<ScreenSorter> _screenSorters = new List<ScreenSorter>();

	private bool _devicesChanged;

	public override bool IsError => AssignedSorters.Count == 0;

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		_ = savedData is SorterMotherboardSaveData;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new SorterMotherboardSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		_ = saveData is SorterMotherboardSaveData;
	}

	public virtual IEnumerator HandleDeviceListChange()
	{
		while (GameManager.GameState != GameState.Running)
		{
			yield return Yielders.EndOfFrame;
		}
		yield return Yielders.EndOfFrame;
		if (ParentComputer == null || !ParentComputer.AsThing().isActiveAndEnabled)
		{
			yield break;
		}
		if ((bool)ParentComputer.AsDevice())
		{
			List<ILogicable> list = ((!base.MasterMotherboard) ? ParentComputer.DeviceList() : new List<ILogicable> { base.MasterMotherboard.ParentComputer.AsDevice() });
			list.Remove(ParentComputer.AsDevice());
			int count = _screenSorters.Count;
			while (count-- > 0)
			{
				ScreenSorter screenSorter = _screenSorters[count];
				if (screenSorter.AssignedSorter == null || !list.Contains(screenSorter.AssignedSorter))
				{
					AssignedSorters.RemoveAt(count);
					Object.Destroy(screenSorter.gameObject);
					_screenSorters.RemoveAt(count);
					if (screenSorter.AssignedSorter != null)
					{
						screenSorter.AssignedSorter.ControllingSorterMotherboards.Remove(this);
					}
				}
				else
				{
					screenSorter.Title.text = screenSorter.AssignedSorter.DisplayName;
				}
			}
			foreach (ILogicable item in list)
			{
				Sorter sorter = item as Sorter;
				if (!(sorter == null) && !AssignedSorters.Contains(sorter))
				{
					ScreenSorter screenSorter2 = Object.Instantiate(ScreenSorterPrefab);
					screenSorter2.transform.SetParent(SorterGroup.transform, worldPositionStays: false);
					screenSorter2.Title.text = sorter.DisplayName;
					screenSorter2.AssignedSorter = sorter;
					AssignedSorters.Add(sorter);
					_screenSorters.Add(screenSorter2);
					screenSorter2.ButtonRename.onClick.AddListener(delegate
					{
						OnButtonRenameDevice(sorter);
					});
					screenSorter2.ButtonAddFilter.onClick.AddListener(delegate
					{
						OnButtonAddFilter(sorter);
					});
					sorter.ControllingSorterMotherboards.Add(this);
					RefreshSorterFilters(screenSorter2);
				}
			}
			_devicesChanged = false;
		}
		ParentComputer?.CheckStatus();
	}

	public void RefreshSorterFilters(Sorter sorter)
	{
		int num = AssignedSorters.FindIndex((Sorter s) => s == sorter);
		if (num >= 0)
		{
			RefreshSorterFilters(_screenSorters[num]);
		}
	}

	public void RefreshSorterFilters(ScreenSorter screenSorter)
	{
		int count = screenSorter.ScreenFilters.Count;
		while (count-- > 0)
		{
			ScreenFilter screenFilter = screenSorter.ScreenFilters[count];
			if (!screenSorter.AssignedSorter.FilterReferences.Contains(screenFilter.FilterReference))
			{
				Object.Destroy(screenFilter.gameObject);
				screenSorter.ScreenFilters.RemoveAt(count);
				screenSorter.DisplayedFilters.RemoveAt(count);
				continue;
			}
			int num = 0;
			num = ((!(screenFilter.FilterReference.PrefabName != string.Empty)) ? ScreenFilter.GetIndex(screenFilter.FilterReference.SlotType) : ScreenFilter.GetIndex(screenFilter.FilterReference.PrefabName));
			if (num != screenFilter.TypeList.value)
			{
				screenFilter.TypeList.value = num;
			}
		}
		foreach (FilterReference filterReference in screenSorter.AssignedSorter.FilterReferences)
		{
			if (!screenSorter.DisplayedFilters.Contains(filterReference))
			{
				ScreenFilter newFilterDisplay = Object.Instantiate(ScreenFilterPrefab);
				newFilterDisplay.transform.SetParent(screenSorter.WhitelistGrid.transform, worldPositionStays: false);
				screenSorter.DisplayedFilters.Add(filterReference);
				screenSorter.ScreenFilters.Add(newFilterDisplay);
				FilterReference reference = filterReference;
				newFilterDisplay.FilterReference = reference;
				if (filterReference.PrefabName != string.Empty)
				{
					newFilterDisplay.TypeList.value = ScreenFilter.GetIndex(filterReference.PrefabName);
				}
				else
				{
					newFilterDisplay.TypeList.value = ScreenFilter.GetIndex(filterReference.SlotType);
				}
				newFilterDisplay.ButtonDelete.onClick.AddListener(delegate
				{
					OnButtonDeleteFilter(screenSorter.AssignedSorter, reference);
				});
				newFilterDisplay.TypeList.onValueChanged.AddListener(delegate(int p)
				{
					OnDropdownChanged(newFilterDisplay, reference, p);
				});
			}
		}
	}

	public override void OnDeviceListChanged()
	{
		base.OnDeviceListChanged();
		if (!_devicesChanged)
		{
			_devicesChanged = true;
			StartCoroutine(HandleDeviceListChange());
		}
	}

	public override void OnInsertedToComputer(IComputer computer)
	{
		base.OnInsertedToComputer(computer);
		if (!_devicesChanged)
		{
			_devicesChanged = true;
			StartCoroutine(HandleDeviceListChange());
		}
	}

	public void OnDropdownChanged(ScreenFilter screenFilter, FilterReference reference, int filterIndex)
	{
		FilterReference filterReference = ScreenFilter.GetFilterReference(filterIndex);
		if (filterReference != null && filterIndex != screenFilter.LastValue)
		{
			screenFilter.LastValue = filterIndex;
			reference.SlotType = filterReference.SlotType;
			reference.PrefabName = filterReference.PrefabName;
			reference.SendUpdate();
		}
	}

	public void OnButtonAddFilter(Sorter sorter)
	{
		FilterReference filterReference = sorter.CreateFilter();
		RefreshSorterFilters(sorter);
		filterReference.SendUpdate();
	}

	public void OnButtonDeleteFilter(Sorter sorter, FilterReference filterReference)
	{
		filterReference.SendDelete();
		if (GameManager.RunSimulation)
		{
			sorter.RemoveFilter(filterReference);
			RefreshSorterFilters(sorter);
		}
	}
}
