using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class SpawnReference : UserInterfaceBase, IScreenSpaceTooltip
{
	public Image Thumbnail;

	public Image MainImage;

	public Thing Prefab;

	public TextMeshProUGUI Text;

	public Button Button;

	public RectTransform ChildContainer;

	public VerticalLayoutGroup ChildContainerLayoutGroup;

	private SpawnReference _parentSpawnReference;

	public LayoutElement LayoutElement;

	public Image BgImage;

	public Image InventoryIcon;

	public Animator Animator;

	private ThingSpawnData _thingSpawnData;

	private string _tooltipName;

	private string _tooltipDescription;

	private bool _noExpand;

	private bool _isExpanded;

	private const int DEEP_TOOLTIP_DEPTH = 4;

	public int PrefabHash => Prefab.PrefabHash;

	public bool TooltipIsVisible => IsVisible;

	private bool HasChildren
	{
		get
		{
			if (_thingSpawnData.DynamicThings.Count <= 0 && _thingSpawnData.Items.Count <= 0)
			{
				return _thingSpawnData.Spawns.Count > 0;
			}
			return true;
		}
	}

	public void ToggleSize()
	{
		_isExpanded = !_isExpanded;
		if (_noExpand)
		{
			_isExpanded = false;
		}
		UpdateContentScale();
		RefreshLayoutRecursiveAsync().Forget();
	}

	public void UpdateContentScale()
	{
		SetContentScale(_isExpanded ? 1 : 0);
	}

	private void SetContentScale(float scale)
	{
		ChildContainer.localScale = Vector3.one * scale;
		float num = ChildContainerLayoutGroup.preferredHeight * scale;
		float size = LayoutElement.minHeight + num;
		RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
	}

	private async UniTaskVoid RefreshLayoutRecursiveAsync()
	{
		await UniTask.WaitForEndOfFrame();
		UpdateContentScale();
		if ((object)_parentSpawnReference != null)
		{
			_parentSpawnReference.RefreshLayoutRecursiveAsync().Forget();
		}
	}

	private void InstantiateChildren(SpawnReference prefab)
	{
		if (_thingSpawnData != null)
		{
			if (_thingSpawnData.DynamicThings.Count <= 0 && _thingSpawnData.Items.Count <= 0 && _thingSpawnData.Spawns.Count <= 0)
			{
				_noExpand = true;
				Button.enabled = false;
			}
			else
			{
				AddAll(_thingSpawnData.DynamicThings, prefab);
				AddAll(_thingSpawnData.Items, prefab);
				AddAll(_thingSpawnData.Spawns, prefab);
			}
		}
	}

	private void AddAll(List<SpawnData> spawnDatas, SpawnReference prefab)
	{
		foreach (SpawnData spawnData in spawnDatas)
		{
			AddSpawns(spawnData, prefab);
		}
	}

	private void AddSpawns(SpawnData spawnData, SpawnReference prefab)
	{
		if (!spawnData.IsValid())
		{
			SpawnData spawnData2 = DataCollection.Get<SpawnData>(spawnData.IdHash);
			if (spawnData2.IsValid())
			{
				AddSpawns(spawnData2, prefab);
			}
		}
		else if (!spawnData.HideInStartScreen)
		{
			AddAll(spawnData.DynamicThings, prefab);
			AddAll(spawnData.Items, prefab);
			AddAll(spawnData.Spawns, prefab);
		}
	}

	private void AddAll(List<DynamicSpawnData> dynamicSpawnDatas, SpawnReference prefab)
	{
		foreach (DynamicSpawnData dynamicSpawnData in dynamicSpawnDatas)
		{
			if (!dynamicSpawnData.HideInStartScreen)
			{
				CreateInstance(dynamicSpawnData, prefab, ChildContainer, this);
			}
		}
	}

	public static SpawnReference CreateInstance(ThingSpawnData data, SpawnReference prefab, Transform parent, SpawnReference parentSpawnRef = null, bool expand = false, bool addChildren = true)
	{
		SpawnReference spawnReference = UnityEngine.Object.Instantiate(prefab, parent);
		spawnReference.SetPrefab(data);
		spawnReference._parentSpawnReference = parentSpawnRef;
		spawnReference._isExpanded = expand;
		if (addChildren)
		{
			spawnReference.InstantiateChildren(prefab);
		}
		else
		{
			spawnReference.Button.enabled = false;
		}
		spawnReference.UpdateContentScale();
		return spawnReference;
	}

	private void SetPrefab(ThingSpawnData spawnData)
	{
		_thingSpawnData = spawnData;
		Prefab = spawnData.Prefab;
		Text.text = spawnData.DisplayName;
		base.name = "~SpawnReference_" + Prefab.PrefabName;
		try
		{
			Thumbnail.sprite = ((spawnData.colorSwatch != null) ? Prefab.Thumbnails[spawnData.colorSwatch.GetIndex()] : Prefab.Thumbnail);
		}
		catch (IndexOutOfRangeException innerException)
		{
			throw new ArgumentOutOfRangeException("invalid color on " + Prefab.PrefabName, innerException);
		}
		_tooltipName = _thingSpawnData.GetTooltipName();
		StringBuilder stringBuilder = new StringBuilder(1024);
		_thingSpawnData.ToolTip(stringBuilder, 0, spawnData.ShowDeepStartScreenTooltip ? 4 : 2, includeName: false);
		_tooltipDescription = stringBuilder.ToString();
		Animator.enabled = HasChildren;
		if (!HasChildren)
		{
			BgImage.color.SetAlpha(0f);
		}
		InventoryIcon.enabled = HasChildren;
	}

	public void ShowTooltip()
	{
		PanelToolTip.Instance.SetUpTooltip(_tooltipName, _tooltipDescription, this);
	}

	public void HideTooltip()
	{
		PanelToolTip.Instance.ClearToolTip();
	}

	public void DoUpdate()
	{
		PanelToolTip.Instance.SetInfoText(_tooltipDescription);
	}
}
