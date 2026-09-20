using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Objects.Rockets.UI;

[ExecuteInEditMode]
public class MapView : UserInterfaceBase, IScrollHandler, IEventSystemHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
	[Header("Map View")]
	[SerializeField]
	private MapLocation _mapLocationPrefab;

	[SerializeField]
	private MapSprite _mapSpritePrefab;

	[SerializeField]
	private RectTransform _mapConnectionPrefab;

	[SerializeField]
	private DynamicLocationPanel _dynamicLocationPanelPrefab;

	[SerializeField]
	private Material _gridMaterial;

	[SerializeField]
	private Material _starMaterial;

	[Space(15f)]
	[SerializeField]
	private Transform _locationParent;

	[SerializeField]
	private RectTransform _mapRectTransform;

	[SerializeField]
	private RectTransform _selectionIcon;

	[SerializeField]
	private Material _selectionIconMaterial;

	[SerializeField]
	private RectTransform _destinationIcon;

	[SerializeField]
	private Material _destinationIconMaterial;

	[Space(15f)]
	[SerializeField]
	private RocketIcon _rocketIconPrefab;

	[SerializeField]
	private Transform _rocketIconParent;

	[SerializeField]
	private float _rocketIconOffset;

	[Space(15f)]
	[SerializeField]
	private float _zoomSpeed;

	[SerializeField]
	private float _minScale;

	[SerializeField]
	private float _maxScale;

	private bool _isDragging;

	private Vector3 _dragOffset;

	private MapPanel _mapPanel;

	private Vector2 _pointerDownPosition;

	private Vector2 _starPosition;

	private List<RocketIcon> _rocketIcons = new List<RocketIcon>();

	private Dictionary<long, DynamicLocationPanel> _dynamicLocationPanelLookup = new Dictionary<long, DynamicLocationPanel>();

	private static readonly int OFFSET_X = Shader.PropertyToID("_OffsetX");

	private static readonly int OFFSET_Y = Shader.PropertyToID("_OffsetY");

	private static readonly int MAP_SCALE = Shader.PropertyToID("_MapScale");

	private static readonly int MAP_WIDTH = Shader.PropertyToID("_MapWidth");

	private static readonly int MAP_HEIGHT = Shader.PropertyToID("_MapHeight");

	private Dictionary<long, float> _locationOffsets = new Dictionary<long, float>();

	public void Initialize(MapPanel mapPanel)
	{
		InitializeRocketIcons(mapPanel.Motherboard);
		_mapPanel = mapPanel;
		_mapPanel.SetSelectedRocketNameText(_mapPanel.Motherboard.SelectedRocket()?.Avionics?.Rocket?.DisplayName ?? string.Empty);
		AddLocationsToMap();
		AddStaticElementsToMap();
	}

	private void RocketChangedRefresh()
	{
		InitializeRocketIcons(_mapPanel.Motherboard);
		_mapPanel.SetSelectedRocketNameText(_mapPanel.Motherboard.SelectedRocket()?.Avionics?.Rocket?.DisplayName ?? string.Empty);
	}

	public void AddDynamicNodeToSpaceMap(SpaceMapNode parent, SpaceMapNode child)
	{
		if (_dynamicLocationPanelLookup.TryGetValue(parent.ReferenceId, out var value))
		{
			value.AddNewNode(child, this);
		}
		else
		{
			AddDynamicLocations(parent, child.AsList(), this);
		}
	}

	private void AddLocationsToMap()
	{
		foreach (SpaceMapNode node in SpaceMap.Current.Nodes)
		{
			NodeType nodeType = node.NodeType;
			if ((nodeType == NodeType.Static || nodeType == NodeType.Entry || nodeType == NodeType.LowOrbitHub) && (node.IsCharted || (node.ParentConnection != null && node.ParentConnection.Parent.IsCharted)))
			{
				AddMapLocation(node);
			}
		}
	}

	private void AddStaticElementsToMap()
	{
		foreach (SpaceMapSpriteData spriteDatum in SpaceMap.Current.Data.SpriteData)
		{
			MapSprite mapSprite = UnityEngine.Object.Instantiate(_mapSpritePrefab, _locationParent);
			mapSprite.Transform.SetAsFirstSibling();
			mapSprite.Initialize(spriteDatum);
		}
	}

	private bool IsIconAlreadyAdded(ConnectedRocketInfo info)
	{
		foreach (RocketIcon rocketIcon in _rocketIcons)
		{
			if (rocketIcon.ConnectedRocketInfo.Equals(info))
			{
				return true;
			}
		}
		return false;
	}

	private void InitializeRocketIcons(RocketMotherboard motherboard)
	{
		ConnectedRocketInfo[] connectedRockets = motherboard.ConnectedRockets;
		int selectedIndex = motherboard.SelectedIndex;
		foreach (Transform item in _rocketIconParent)
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
		_rocketIcons.Clear();
		for (int i = 0; i < connectedRockets.Length; i++)
		{
			ConnectedRocketInfo connectedRocketInfo = connectedRockets[i];
			if (connectedRocketInfo.Avionics.GetIsOperable() && !IsIconAlreadyAdded(connectedRocketInfo))
			{
				bool selected = i == selectedIndex;
				RocketIcon rocketIcon = UnityEngine.Object.Instantiate(_rocketIconPrefab, _rocketIconParent);
				rocketIcon.Initialize(this);
				rocketIcon.ConnectedRocketInfo = connectedRocketInfo;
				rocketIcon.SetState(selected, connected: true);
				rocketIcon.SetTooltip(GameStrings.RocketMapIconTooltip.AsString(connectedRocketInfo.Avionics.Rocket.DisplayName));
				ApplyRocketThumbnail(rocketIcon, connectedRocketInfo.Avionics.Rocket);
				_rocketIcons.Add(rocketIcon);
			}
		}
		foreach (Rocket allRocket in Rocket.AllRockets)
		{
			if (!RocketIconAdded(allRocket))
			{
				RocketIcon rocketIcon2 = UnityEngine.Object.Instantiate(_rocketIconPrefab, _rocketIconParent);
				rocketIcon2.Initialize(this);
				rocketIcon2.Rocket = allRocket;
				rocketIcon2.SetState(selected: false, connected: false);
				string text = (RocketIsConnected(connectedRockets, allRocket) ? GameStrings.RocketNoOperableAvionics.AsColor("red") : GameStrings.RocketNoDataConnection.AsColor("red"));
				rocketIcon2.SetTooltip(GameStrings.RocketMapIconTooltip.AsString(allRocket.DisplayName) + Environment.NewLine + text);
				ApplyRocketThumbnail(rocketIcon2, allRocket);
				_rocketIcons.Add(rocketIcon2);
			}
		}
	}

	private void ApplyRocketThumbnail(RocketIcon icon, Rocket rocket)
	{
		if (icon == null || rocket == null)
		{
			return;
		}
		if (rocket.CurrentNode?.Owner is Thing { CustomColor: { IsSet: not false } } thing)
		{
			int colorIndex = GameManager.GetColorIndex(thing.CustomColor);
			if (colorIndex != rocket.LaunchMountColorIndex)
			{
				rocket.LaunchMountColorIndex = colorIndex;
				rocket.MapIconDirty = true;
			}
		}
		Sprite icon2 = RocketMapIconRenderer.Instance.GetIcon(rocket, rocket.LaunchMountColorIndex);
		icon.SetMapSprite(icon2, rocket.MapHighlight);
	}

	private void RefreshDirtyThumbnails()
	{
		foreach (RocketIcon rocketIcon in _rocketIcons)
		{
			if (!(rocketIcon == null))
			{
				Rocket rocket = rocketIcon.Rocket ?? rocketIcon.ConnectedRocketInfo?.Avionics?.Rocket;
				if (rocket != null && rocket.MapIconDirty)
				{
					ApplyRocketThumbnail(rocketIcon, rocket);
				}
			}
		}
	}

	private bool RocketIconAdded(Rocket rocket)
	{
		foreach (RocketIcon rocketIcon in _rocketIcons)
		{
			if (rocketIcon.Rocket == rocket || rocketIcon.ConnectedRocketInfo?.Avionics?.Rocket == rocket)
			{
				return true;
			}
		}
		return false;
	}

	private bool RocketIsConnected(ConnectedRocketInfo[] connectedRockets, Rocket rocket)
	{
		foreach (ConnectedRocketInfo connectedRocketInfo in connectedRockets)
		{
			if (rocket == connectedRocketInfo.Avionics.Rocket)
			{
				return true;
			}
		}
		return false;
	}

	public void FocusOnSelectedRocket()
	{
		foreach (RocketIcon rocketIcon in _rocketIcons)
		{
			if (rocketIcon.Selected)
			{
				float num = 4f;
				Vector3 vector = GetLocalMapPosition(rocketIcon.Transform.position) * (0f - num);
				SetMapTransform(vector, num);
				UpdateGrid();
				break;
			}
		}
	}

	public void RecenterMap()
	{
		MapLocation mapLocation = SpaceMap.Current.EntryNode.MapLocation;
		float num = 2f;
		Vector3 vector = mapLocation.GetMapPosition() * (0f - num);
		SetMapTransform(vector, num);
		UpdateGrid();
	}

	public Vector3 GetLocalMapPosition(Vector3 position)
	{
		return _locationParent.InverseTransformPoint(position);
	}

	public void LocationSelected(MapLocation location)
	{
		_selectionIcon.gameObject.SetActive(value: true);
		_selectionIcon.position = location.Transform.position;
		SetSelectionIconSize(location, _selectionIcon, _selectionIconMaterial);
		_mapPanel.LocationSelected(location);
	}

	private void SetSelectionIconSize(MapLocation location, RectTransform iconRectTransform, Material iconMaterial)
	{
		int num = 20;
		float num2 = location.RectTransform.sizeDelta.x * location.SelectionSize + (float)num;
		iconRectTransform.sizeDelta = new Vector2(num2, num2);
		iconMaterial.SetFloat("_ImageSize", num2);
	}

	public void SetTargetDestination(SpaceMapNode node)
	{
		_mapPanel.Motherboard.SelectedRocket().Avionics.SetTargetDestination(node);
	}

	public bool IsInvalidMannedDestination(SpaceMapNode node)
	{
		Rocket rocket = _mapPanel?.Motherboard?.SelectedRocket()?.Avionics?.Rocket;
		if (rocket != null && rocket.IsManned)
		{
			return !Rocket.IsValidMannedTarget(node);
		}
		return false;
	}

	public void RocketSelected(ConnectedRocketInfo connectedRocket)
	{
		_mapPanel.Motherboard.RocketSelected(connectedRocket);
		RocketChangedRefresh();
		Refresh();
	}

	public void Refresh()
	{
		RefreshDirtyThumbnails();
		SetRocketIconPositions();
		SetDestinationMarkerPosition();
	}

	public void Clear()
	{
		foreach (Transform item in _locationParent)
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
		foreach (Transform item2 in _rocketIconParent)
		{
			UnityEngine.Object.Destroy(item2.gameObject);
		}
		_dynamicLocationPanelLookup.Clear();
		_rocketIcons.Clear();
	}

	private void SetDestinationMarkerPosition()
	{
		SpaceMapNode spaceMapNode = _mapPanel.Motherboard?.SelectedRocket()?.Avionics?.GetTarget();
		if (spaceMapNode == null)
		{
			_destinationIcon.gameObject.SetActive(value: false);
			return;
		}
		_destinationIcon.gameObject.SetActive(value: true);
		_destinationIcon.position = spaceMapNode.MapLocation.Transform.position;
		SetSelectionIconSize(spaceMapNode.MapLocation, _destinationIcon, _destinationIconMaterial);
	}

	private void OffsetIcon(SpaceMapNode node, RocketIcon icon, float scale, int rocketsHere)
	{
		float num = (float)(rocketsHere - 1) / 2f;
		if (_locationOffsets.TryGetValue(node.ReferenceId, out var value))
		{
			value += 1f;
			_locationOffsets[node.ReferenceId] = value;
		}
		else
		{
			value = 0f - num;
			_locationOffsets.Add(node.ReferenceId, value);
		}
		icon.Offset(value * _rocketIconOffset * scale, 0f);
	}

	private int RocketsStationaryAtNode(SpaceMapNode node)
	{
		int num = 0;
		foreach (Rocket item in node.RocketsHere)
		{
			if (item.Progress == 0f)
			{
				num++;
			}
		}
		return num;
	}

	private void SetRocketIconPositions()
	{
		_locationOffsets.Clear();
		for (int num = _rocketIcons.Count - 1; num >= 0; num--)
		{
			RocketIcon rocketIcon = _rocketIcons[num];
			if (rocketIcon == null)
			{
				_rocketIcons.RemoveAt(num);
			}
			else
			{
				rocketIcon.Offset(0f, 0f);
				bool flag = false;
				SpaceMapNode spaceMapNode = null;
				SpaceMapNode spaceMapNode2 = null;
				float num2 = 0f;
				if (rocketIcon.Connected && rocketIcon.ConnectedRocketInfo != null && rocketIcon.ConnectedRocketInfo.IsValid)
				{
					ConnectedRocketInfo connectedRocketInfo = rocketIcon.ConnectedRocketInfo;
					spaceMapNode = connectedRocketInfo.Avionics.GetCurrentNode();
					spaceMapNode2 = connectedRocketInfo.Avionics.GetNextNode();
					num2 = connectedRocketInfo.Avionics.GetProgress();
					flag = connectedRocketInfo.Avionics.OnOff && connectedRocketInfo.Avionics.Powered;
				}
				else if (rocketIcon.Rocket != null)
				{
					Rocket rocket = rocketIcon.Rocket;
					spaceMapNode = rocket.CurrentNode;
					spaceMapNode2 = rocket.CurrentTransit?.Destination;
					num2 = rocket.GetMapProgress();
					flag = spaceMapNode != null;
				}
				if (spaceMapNode == null)
				{
					rocketIcon.SetActive(active: false);
				}
				else
				{
					rocketIcon.SetActive(flag);
					if (flag)
					{
						int num3 = RocketsStationaryAtNode(spaceMapNode);
						if (spaceMapNode2 == null || num2 == 0f)
						{
							if (num3 > 1)
							{
								float b = Mathf.Min(30f, spaceMapNode.MapLocation.RectTransform.sizeDelta.x) / 30f;
								float num4 = Mathf.Lerp(1f, b, (float)num3 / 4f);
								rocketIcon.SetBaseScale(num4);
								OffsetIcon(spaceMapNode, rocketIcon, num4, num3);
							}
							else
							{
								rocketIcon.SetBaseScale(1f);
							}
							rocketIcon.Transform.localPosition = (spaceMapNode.IsDestroyedLaunchPadNode ? LaunchMountEstimatedPosition() : spaceMapNode.MapLocation.GetMapPosition());
						}
						else
						{
							rocketIcon.SetBaseScale(1f);
							Vector3 b2 = (spaceMapNode2.IsDestroyedLaunchPadNode ? LaunchMountEstimatedPosition() : spaceMapNode2.MapLocation.GetMapPosition());
							Vector3 a = (spaceMapNode.IsDestroyedLaunchPadNode ? LaunchMountEstimatedPosition() : spaceMapNode.MapLocation.GetMapPosition());
							rocketIcon.Transform.localPosition = Vector3.Lerp(a, b2, num2);
						}
					}
				}
			}
		}
	}

	private Vector3 LaunchMountEstimatedPosition()
	{
		SpaceMapNode entryNode = SpaceMap.Current.EntryNode;
		int offset = entryNode.MapDisplayData.DynamicPanel.Offset;
		return entryNode.MapLocation.GetMapPosition() + Vector3.down * offset;
	}

	private void SetMapTransform(Vector2 position, float scale)
	{
		_starPosition = position;
		_mapRectTransform.localPosition = position;
		_mapRectTransform.localScale = Vector3.one * scale;
	}

	private void AddMapLocation(SpaceMapNode node)
	{
		UnityEngine.Object.Instantiate(_mapLocationPrefab, _locationParent).Initialize(node, this);
		List<SpaceMapNode> list = new List<SpaceMapNode>();
		if (!node.IsCharted)
		{
			return;
		}
		foreach (NodeConnection childConnection in node.ChildConnections)
		{
			if (childConnection.Child.SpaceMap != node.SpaceMap)
			{
				continue;
			}
			if (childConnection.Child.NodeType == NodeType.Static)
			{
				AddConnectionToStaticNode(childConnection, node);
				continue;
			}
			NodeType nodeType = childConnection.Child.NodeType;
			if (nodeType == NodeType.Generated || nodeType == NodeType.LaunchPad || nodeType == NodeType.LowOrbitLaunchPad)
			{
				list.Add(childConnection.Child);
			}
		}
		if (list.Count > 0 || node.NodeType == NodeType.Entry)
		{
			AddDynamicLocations(node, list, this);
		}
	}

	private void AddDynamicLocations(SpaceMapNode fromNode, List<SpaceMapNode> nodes, MapView mapView)
	{
		DynamicLocationPanel dynamicLocationPanel = UnityEngine.Object.Instantiate(_dynamicLocationPanelPrefab, _locationParent);
		dynamicLocationPanel.Initialize(fromNode, nodes, mapView);
		_dynamicLocationPanelLookup.Add(fromNode.ReferenceId, dynamicLocationPanel);
	}

	private void AddConnectionToStaticNode(NodeConnection connection, SpaceMapNode fromNode)
	{
		SpaceMapNode child = connection.Child;
		Vector3 position = fromNode.MapDisplayData.Position;
		Vector3 position2 = connection.Child.MapDisplayData.Position;
		Vector3 incident = position2 - position;
		Vector3 normalized = incident.normalized;
		position += normalized * (fromNode.MapDisplayData.Icon.Size * 0.5f * MapLocation.DefaultSize);
		Vector3 vector = position2 - normalized * (child.MapDisplayData.Icon.Size * 0.5f * MapLocation.DefaultSize);
		float magnitude = (vector - position).magnitude;
		Vector3 localPosition = (vector + position) * 0.5f;
		float z = FindVectorClockwiseAngleDegrees(incident);
		RectTransform rectTransform = UnityEngine.Object.Instantiate(_mapConnectionPrefab, _locationParent);
		rectTransform.localPosition = localPosition;
		rectTransform.sizeDelta = new Vector2(magnitude, 1f);
		rectTransform.rotation = Quaternion.Euler(0f, 0f, z);
	}

	private float FindVectorClockwiseAngleDegrees(Vector3 incident)
	{
		incident.Normalize();
		float f = Vector3.Dot(incident, Vector3.left);
		if (!(incident.y <= 0f))
		{
			return 360f - 57.29578f * Mathf.Acos(f);
		}
		return 57.29578f * Mathf.Acos(f);
	}

	private void UpdateGrid()
	{
		Vector2 vector = (_mapRectTransform.pivot - Vector2.one * 0.5f) * _mapRectTransform.localScale.x;
		float value = (vector.x - _mapRectTransform.localPosition.x) / RectTransform.rect.width;
		float value2 = (vector.y - _mapRectTransform.localPosition.y) / RectTransform.rect.height;
		_gridMaterial.SetFloat(OFFSET_X, value);
		_gridMaterial.SetFloat(OFFSET_Y, value2);
		_gridMaterial.SetFloat(MAP_SCALE, _mapRectTransform.localScale.x);
		_gridMaterial.SetFloat(MAP_WIDTH, RectTransform.rect.width);
		_gridMaterial.SetFloat(MAP_HEIGHT, RectTransform.rect.height);
		_starMaterial.SetFloat(OFFSET_X, _starPosition.x);
		_starMaterial.SetFloat(OFFSET_Y, _starPosition.y);
	}

	public void ClearSelected()
	{
		_selectionIcon.gameObject.SetActive(value: false);
		_mapPanel?.ClearLocationSelection();
	}

	public void ClearAll()
	{
		ClearSelected();
	}

	public void OnScroll(PointerEventData eventData)
	{
		if (!_isDragging)
		{
			float x = _mapRectTransform.localScale.x;
			float num = eventData.scrollDelta.y * _zoomSpeed;
			float num2 = Mathf.Clamp(x + num, _minScale, _maxScale);
			Vector3 vector = _mapRectTransform.InverseTransformPoint(eventData.position);
			_mapRectTransform.localScale = Vector3.one * num2;
			Vector3 vector2 = _mapRectTransform.InverseTransformPoint(eventData.position) - vector;
			_mapRectTransform.localPosition += vector2 * num2;
			UpdateGrid();
		}
	}

	public void OnDrag(PointerEventData eventData)
	{
		_starPosition += eventData.delta;
		_mapRectTransform.position = (Vector3)eventData.position - _dragOffset;
		UpdateGrid();
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		_dragOffset = (Vector3)eventData.position - _mapRectTransform.position;
		_isDragging = true;
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		_isDragging = false;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		_pointerDownPosition = eventData.position;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (RocketMath.Approximately(_pointerDownPosition, eventData.position))
		{
			ClearSelected();
		}
	}
}
