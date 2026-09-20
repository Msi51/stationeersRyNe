using System;
using System.Collections.Generic;
using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Objects.Rockets.UI;

public class DynamicLocationPanel : UserInterfaceBase
{
	[Header("Dynamic Location Panel")]
	[SerializeField]
	private Transform _locationParent;

	[SerializeField]
	private MapLocation _locationPrefab;

	[Space(15f)]
	[SerializeField]
	private GridLayoutGroup _gridLayout;

	[SerializeField]
	private RectTransform _gridTransform;

	[SerializeField]
	private FitDynamicPanelWhenResize _panelFitter;

	[SerializeField]
	private Image _backgroundImage;

	[SerializeField]
	private Sprite _gradientSprite;

	[SerializeField]
	private Color _gradientTint;

	private List<MapLocation> _locationList = new List<MapLocation>();

	private static int CompareNodes(SpaceMapNode a, SpaceMapNode b)
	{
		return string.Compare(a.DisplayName, b.DisplayName, StringComparison.InvariantCultureIgnoreCase);
	}

	public void Initialize(SpaceMapNode fromNode, List<SpaceMapNode> nodes, MapView mapView)
	{
		if (nodes != null)
		{
			NodeType nodeType = fromNode.NodeType;
			if (nodeType == NodeType.Entry || nodeType == NodeType.LowOrbitHub)
			{
				nodes.Sort(CompareNodes);
			}
			foreach (SpaceMapNode node in nodes)
			{
				MapLocation mapLocation = UnityEngine.Object.Instantiate(_locationPrefab, _locationParent);
				mapLocation.Initialize(node, mapView);
				_locationList.Add(mapLocation);
			}
		}
		SetOrientation(fromNode.MapDisplayData);
		int constraintCount = GetConstraintCount(fromNode);
		_gridLayout.constraintCount = Mathf.Min(_locationList.Count, constraintCount);
	}

	public void AddNewNode(SpaceMapNode node, MapView mapView)
	{
		MapLocation mapLocation = UnityEngine.Object.Instantiate(_locationPrefab, _locationParent);
		mapLocation.Initialize(node, mapView);
		_locationList.Add(mapLocation);
		int constraintCount = GetConstraintCount(node);
		_gridLayout.constraintCount = Mathf.Min(_locationList.Count, constraintCount);
	}

	private int GetConstraintCount(SpaceMapNode node)
	{
		int result = 3;
		int result2 = 4;
		int num = 9;
		if (node != null && node.MapDisplayData?.DynamicPanel?.Grow == true)
		{
			if (_locationList.Count <= num)
			{
				return result;
			}
			return result2;
		}
		return result;
	}

	private void SetOrientation(MapDisplayData mapDisplayData)
	{
		switch (mapDisplayData.DynamicPanel.Orientation)
		{
		case 0:
			SetOrientationBelow(mapDisplayData);
			break;
		case 1:
			SetOrientationLeft(mapDisplayData);
			break;
		case 2:
			SetOrientationAbove(mapDisplayData);
			break;
		case 3:
			SetOrientationRight(mapDisplayData);
			break;
		}
		if (mapDisplayData.DynamicPanel.ExtraSize > 0)
		{
			_backgroundImage.sprite = _gradientSprite;
			_backgroundImage.color = _gradientTint;
		}
	}

	private void SetOrientationBelow(MapDisplayData data)
	{
		_gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
		_gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
		_gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		RectTransform.pivot = new Vector2(0.5f, 1f);
		RectTransform.localPosition = data.Position + Vector3.down * data.DynamicPanel.Offset;
		SetGridTransformAnchorsAndPivot(new Vector2(0.5f, 0f));
		_panelFitter.SetExtraSize(new Vector2(0f, data.DynamicPanel.ExtraSize));
	}

	private void SetOrientationLeft(MapDisplayData data)
	{
		_gridLayout.startCorner = GridLayoutGroup.Corner.UpperRight;
		_gridLayout.startAxis = GridLayoutGroup.Axis.Vertical;
		_gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
		RectTransform.pivot = new Vector2(1f, 0.5f);
		RectTransform.localPosition = data.Position + Vector3.left * data.DynamicPanel.Offset;
		SetGridTransformAnchorsAndPivot(new Vector2(0f, 0.5f));
		_panelFitter.SetExtraSize(new Vector2(data.DynamicPanel.ExtraSize, 0f));
	}

	private void SetOrientationAbove(MapDisplayData data)
	{
		_gridLayout.startCorner = GridLayoutGroup.Corner.LowerLeft;
		_gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
		_gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		RectTransform.pivot = new Vector2(0.5f, 0f);
		RectTransform.localPosition = data.Position + Vector3.up * data.DynamicPanel.Offset;
		SetGridTransformAnchorsAndPivot(new Vector2(0.5f, 1f));
		_panelFitter.SetExtraSize(new Vector2(0f, data.DynamicPanel.ExtraSize));
	}

	private void SetOrientationRight(MapDisplayData data)
	{
		_gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
		_gridLayout.startAxis = GridLayoutGroup.Axis.Vertical;
		_gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
		RectTransform.pivot = new Vector2(0f, 0.5f);
		RectTransform.localPosition = data.Position + Vector3.right * data.DynamicPanel.Offset;
		SetGridTransformAnchorsAndPivot(new Vector2(1f, 0.5f));
		_panelFitter.SetExtraSize(new Vector2(data.DynamicPanel.ExtraSize, 0f));
	}

	private void SetGridTransformAnchorsAndPivot(Vector2 value)
	{
		_gridTransform.pivot = value;
		_gridTransform.anchorMax = value;
		_gridTransform.anchorMin = value;
	}

	public void Hide()
	{
		Clear();
		base.gameObject.SetActive(value: false);
	}

	private void Clear()
	{
		foreach (Transform item in _locationParent)
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
		_locationList.Clear();
	}
}
