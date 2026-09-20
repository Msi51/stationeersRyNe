using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI.Motherboard;
using Reagents;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Motherboards;

public class ManufacturingMotherboard : MultiScreenMotherboard
{
	public VerticalLayoutGroup FabriactorGroup;

	[Header("Screen Prefabs")]
	public ScreenFabricator ScreenFabricatorPrefab;

	public ScreenConstructionJob ScreenConstructionJobPrefab;

	public List<Fabricator> AssignedFabricators = new List<Fabricator>();

	private List<ScreenFabricator> _screenFabricators = new List<ScreenFabricator>();

	private bool _devicesChanged;

	public override bool IsError => AssignedFabricators.Count == 0;

	public virtual IEnumerator HandleDeviceListChange()
	{
		while (GameManager.GameState != GameState.Running)
		{
			yield return Yielders.EndOfFrame;
		}
		yield return Yielders.EndOfFrame;
		if (ParentComputer == null || !ParentComputer.AsDevice() || !ParentComputer.AsDevice().isActiveAndEnabled)
		{
			yield break;
		}
		List<ILogicable> list = ((!base.MasterMotherboard) ? ParentComputer.DeviceList() : new List<ILogicable> { base.MasterMotherboard.ParentComputer.AsDevice() });
		list.Remove(ParentComputer.AsDevice());
		int count = _screenFabricators.Count;
		while (count-- > 0)
		{
			ScreenFabricator screenFabricator = _screenFabricators[count];
			if (screenFabricator.AssignedFabricator == null || !list.Contains(screenFabricator.AssignedFabricator))
			{
				AssignedFabricators.RemoveAt(count);
				Object.Destroy(screenFabricator.gameObject);
				_screenFabricators.RemoveAt(count);
				if (screenFabricator.AssignedFabricator != null)
				{
					screenFabricator.AssignedFabricator.ControllingManufacturingMotherboards.Remove(this);
				}
			}
			else
			{
				screenFabricator.Title.text = screenFabricator.AssignedFabricator.DisplayName;
				RefreshActivate(screenFabricator);
				RefreshJobs(screenFabricator);
				RefreshCurrentJob(screenFabricator);
			}
		}
		foreach (ILogicable item in list)
		{
			Fabricator fabricator = item as Fabricator;
			if (!(fabricator == null) && !AssignedFabricators.Contains(fabricator))
			{
				ScreenFabricator screenFabricator2 = Object.Instantiate(ScreenFabricatorPrefab);
				screenFabricator2.transform.SetParent(FabriactorGroup.transform, worldPositionStays: false);
				screenFabricator2.Title.text = fabricator.DisplayName;
				screenFabricator2.AssignedFabricator = fabricator;
				AssignedFabricators.Add(fabricator);
				_screenFabricators.Add(screenFabricator2);
				screenFabricator2.ButtonRename.onClick.AddListener(delegate
				{
					OnButtonRenameDevice(fabricator);
				});
				screenFabricator2.ButtonAddJob.onClick.AddListener(delegate
				{
					OnButtonAddJob(fabricator);
				});
				screenFabricator2.ButtonPlay.onClick.AddListener(delegate
				{
					OnButtonPlay(fabricator);
				});
				screenFabricator2.ButtonIncrease.onClick.AddListener(delegate
				{
					OnButtonCurrentJobIncrease(fabricator);
				});
				screenFabricator2.ButtonDecrease.onClick.AddListener(delegate
				{
					OnButtonCurrentJobDecrease(fabricator);
				});
				screenFabricator2.ButtonDelete.onClick.AddListener(delegate
				{
					OnButtonCurrentJobDelete(fabricator);
				});
				fabricator.ControllingManufacturingMotherboards.Add(this);
				RefreshJobs(screenFabricator2);
				RefreshCurrentJob(screenFabricator2);
			}
		}
		ParentComputer?.CheckStatus();
		_devicesChanged = false;
	}

	public void OnButtonAddJob(Fabricator fabricator)
	{
		FabricatorJob fabricatorJob = fabricator.CreateJob();
		RefreshJobs(fabricator);
		fabricatorJob.SendUpdate();
	}

	public void OnButtonPlay(Fabricator fabricator)
	{
		Motherboard.UseComputer(2, base.netId, fabricator.netId, 0, sendToAll: false);
	}

	public void RefreshCurrentJob(Fabricator fabricator)
	{
		int num = AssignedFabricators.FindIndex((Fabricator s) => s == fabricator);
		if (num >= 0)
		{
			RefreshCurrentJob(_screenFabricators[num]);
		}
	}

	public void RefreshCurrentJob(ScreenFabricator screenFabricator)
	{
		FabricatorJob currentJob = screenFabricator.AssignedFabricator.CurrentJob;
		screenFabricator.ConstructingPanel.SetActive(currentJob != null);
		if (currentJob != null && (bool)currentJob.Prefab)
		{
			screenFabricator.ConstructingImage.sprite = currentJob.Prefab.Thumbnail;
			screenFabricator.ConstructingTitle.text = $"<color=yellow><b>Making</b></color> {currentJob.Prefab.DisplayName} x {currentJob.Quantity}";
			screenFabricator.ConstructingProgress.value = screenFabricator.AssignedFabricator.Progress;
			screenFabricator.ConstructingResources.text = currentJob.Recipe.GetComparisonResult(screenFabricator.AssignedFabricator.ReadableReagentMixture);
		}
	}

	public void RefreshActivate(Fabricator fabricator)
	{
		int num = AssignedFabricators.FindIndex((Fabricator s) => s == fabricator);
		if (num >= 0)
		{
			RefreshActivate(_screenFabricators[num]);
		}
	}

	public void RefreshActivate(ScreenFabricator screenFabricator)
	{
		screenFabricator.ButtonPlayImage.sprite = ((screenFabricator.AssignedFabricator.Activate == 1) ? screenFabricator.ImagePause : screenFabricator.ImagePlay);
	}

	public void RefreshProgress(Fabricator fabricator)
	{
		int num = AssignedFabricators.FindIndex((Fabricator s) => s == fabricator);
		if (num >= 0)
		{
			RefreshProgress(_screenFabricators[num]);
		}
	}

	public void RefreshProgress(ScreenFabricator screenFabricator)
	{
		screenFabricator.ConstructingProgress.value = screenFabricator.AssignedFabricator.Progress;
	}

	public void RefreshJobs(Fabricator fabricator)
	{
		int num = AssignedFabricators.FindIndex((Fabricator s) => s == fabricator);
		if (num >= 0)
		{
			RefreshJobs(_screenFabricators[num]);
		}
	}

	public void RefreshJobs(ScreenFabricator screenFabricator)
	{
		int count = screenFabricator.ScreenJobs.Count;
		while (count-- > 0)
		{
			ScreenConstructionJob screenConstructionJob = screenFabricator.ScreenJobs[count];
			if (!screenFabricator.AssignedFabricator.JobReferences.Contains(screenConstructionJob.JobReference))
			{
				Object.Destroy(screenConstructionJob.gameObject);
				screenFabricator.ScreenJobs.RemoveAt(count);
				screenFabricator.JobReferences.RemoveAt(count);
				continue;
			}
			int num = 0;
			num = (screenConstructionJob.JobReference.Prefab ? ScreenConstructionJob.GetIndex(screenConstructionJob.JobReference.Prefab.name) : 0);
			if (num != screenConstructionJob.TypeList.value)
			{
				screenConstructionJob.TypeList.value = num;
			}
			screenConstructionJob.UpdateQuantity();
		}
		foreach (FabricatorJob jobReference in screenFabricator.AssignedFabricator.JobReferences)
		{
			if (!screenFabricator.JobReferences.Contains(jobReference))
			{
				ScreenConstructionJob newFilterDisplay = Object.Instantiate(ScreenConstructionJobPrefab);
				newFilterDisplay.transform.SetParent(screenFabricator.JobGrid.transform, worldPositionStays: false);
				screenFabricator.JobReferences.Add(jobReference);
				screenFabricator.ScreenJobs.Add(newFilterDisplay);
				FabricatorJob reference = jobReference;
				newFilterDisplay.JobReference = reference;
				newFilterDisplay.AssignedFabricator = screenFabricator.AssignedFabricator;
				newFilterDisplay.UpdateQuantity();
				newFilterDisplay.TypeList.value = (jobReference.Prefab ? ScreenFilter.GetIndex(jobReference.Prefab.name) : 0);
				newFilterDisplay.ButtonDelete.onClick.AddListener(delegate
				{
					OnButtonDeleteFilter(screenFabricator.AssignedFabricator, reference);
				});
				newFilterDisplay.TypeList.onValueChanged.AddListener(delegate(int p)
				{
					OnDropdownChanged(newFilterDisplay, p);
				});
				newFilterDisplay.ButtonAdd.onClick.AddListener(delegate
				{
					OnButtonQuantityAdd(screenFabricator.AssignedFabricator, reference);
				});
				newFilterDisplay.ButtonRemove.onClick.AddListener(delegate
				{
					OnButtonQuantityRemove(screenFabricator.AssignedFabricator, reference);
				});
			}
		}
	}

	public void OnButtonCurrentJobIncrease(Fabricator fabricator)
	{
		Motherboard.UseComputer(3, base.netId, fabricator.netId, -1, sendToAll: false);
	}

	public void OnButtonCurrentJobDecrease(Fabricator fabricator)
	{
		Motherboard.UseComputer(4, base.netId, fabricator.netId, -1, sendToAll: false);
	}

	public void OnButtonCurrentJobDelete(Fabricator fabricator)
	{
		if (fabricator.CurrentJob != null)
		{
			fabricator.CurrentJob.SendDelete();
			fabricator.CurrentJob = null;
			RefreshCurrentJob(fabricator);
		}
	}

	public void OnButtonQuantityAdd(Fabricator fabricator, FabricatorJob fabricatorJob)
	{
		Motherboard.UseComputer(3, base.netId, fabricator.netId, fabricatorJob.Index, sendToAll: false);
	}

	public void OnButtonQuantityRemove(Fabricator fabricator, FabricatorJob fabricatorJob)
	{
		Motherboard.UseComputer(4, base.netId, fabricator.netId, fabricatorJob.Index, sendToAll: false);
	}

	public override void MotherboardCommand(int command, Thing reference, int referenceInt, string text)
	{
		base.MotherboardCommand(command, reference, referenceInt, text);
		if ((uint)(command - 3) > 1u)
		{
			return;
		}
		Fabricator fabricator = reference as Fabricator;
		if (fabricator == null)
		{
			return;
		}
		if (referenceInt >= 0)
		{
			FabricatorJob fabricatorJob = fabricator.JobReferences[referenceInt];
			int value = ((command == 3) ? (fabricatorJob.Quantity + 1) : (fabricatorJob.Quantity - 1));
			fabricatorJob.Quantity = Mathf.Clamp(value, 1, 999);
			fabricatorJob.SendUpdate();
			{
				foreach (ManufacturingMotherboard controllingManufacturingMotherboard in fabricator.ControllingManufacturingMotherboards)
				{
					controllingManufacturingMotherboard.RefreshJobs(fabricator);
				}
				return;
			}
		}
		if (fabricator.CurrentJob == null)
		{
			return;
		}
		int value2 = ((command == 3) ? (fabricator.CurrentJob.Quantity + 1) : (fabricator.CurrentJob.Quantity - 1));
		fabricator.CurrentJob.Quantity = Mathf.Clamp(value2, 1, 999);
		fabricator.CurrentJob.CurrentJobMessage().SendToClients();
		foreach (ManufacturingMotherboard controllingManufacturingMotherboard2 in fabricator.ControllingManufacturingMotherboards)
		{
			controllingManufacturingMotherboard2.RefreshCurrentJob(fabricator);
		}
	}

	public void OnDropdownChanged(ScreenConstructionJob screenJob, int value)
	{
		if (screenJob.LastValue != value)
		{
			if (value == 0)
			{
				screenJob.JobReference.Prefab = null;
				screenJob.JobReference.Recipe = default(Recipe);
			}
			else
			{
				DynamicThing prefab = ScreenConstructionJob.DynamicThings[value - 1];
				screenJob.JobReference.Prefab = prefab;
				screenJob.JobReference.Recipe = ScreenConstructionJob.GetRecipe(value - 1);
			}
			screenJob.LastValue = value;
			screenJob.JobReference.SendUpdate();
		}
	}

	public void OnButtonDeleteFilter(Fabricator fabricator, FabricatorJob fabricatorJob)
	{
		fabricatorJob.SendDelete();
		if (GameManager.RunSimulation)
		{
			if (fabricatorJob == fabricator.CurrentJob)
			{
				fabricator.CurrentJob = null;
			}
			if (fabricatorJob.Index >= 0)
			{
				fabricator.JobReferences.Remove(fabricatorJob);
			}
			RefreshJobs(fabricator);
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
}
