using Assets.Scripts.Localization2;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Objects.Rockets.Mining;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Objects.Rockets.UI;

public class MapLocation : UserInterfaceBase, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler, IPointerClickHandler
{
	[Space(15f)]
	[Header("Map Location")]
	[SerializeField]
	private Image _image;

	[SerializeField]
	private Sprite _unchartedLocationSprite;

	private MapView _mapView;

	private Vector3 _offset;

	private bool _hovered;

	public static readonly float DefaultSize = 30f;

	public float SelectionSize;

	public SpaceMapNode Node { get; private set; }

	public void Initialize(SpaceMapNode node, MapView mapView)
	{
		Node = node;
		Node.MapLocation = this;
		SelectionSize = node.MapDisplayData.Icon.SelectionSize;
		_mapView = mapView;
		if (node.IsCharted)
		{
			_image.sprite = GetSpaceMapSprite(node);
			_image.color = node.MapDisplayData.Icon.TintColor;
			RectTransform.sizeDelta = Vector2.one * (DefaultSize * node.MapDisplayData.Icon.Size);
		}
		else
		{
			_image.sprite = _unchartedLocationSprite;
		}
		Vector3 position = node.MapDisplayData.Position;
		_offset = node.MapDisplayData.Offset;
		base.transform.localPosition = position + _offset;
		_image.enabled = node.NodeType != NodeType.LowOrbitHub;
	}

	private Sprite GetSpaceMapSprite(SpaceMapNode node)
	{
		if (node.Owner is LaunchMount launchMount)
		{
			Sprite mapIcon = launchMount.GetMapIcon();
			if (mapIcon != null)
			{
				return mapIcon;
			}
		}
		return node.MapDisplayData.Icon.IconSprite;
	}

	public Vector3 GetMapPosition()
	{
		return _mapView.GetLocalMapPosition(Transform.position) - _offset;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		_mapView.LocationSelected(this);
	}

	public new void OnPointerEnter(PointerEventData eventData)
	{
		_hovered = true;
	}

	public new void OnPointerExit(PointerEventData eventData)
	{
		_hovered = false;
	}

	public override void OnDisable()
	{
		base.OnDisable();
		_hovered = false;
	}

	private void Update()
	{
		if (_hovered && Node != null && Node.NodeType == NodeType.Generated && Node.Deposit != null)
		{
			UITooltipManager.SetTooltip(BuildTooltip());
		}
		if (_hovered && Node != null && !(_mapView == null) && _mapView.IsInvalidMannedDestination(Node))
		{
			UITooltipManager.SetTooltip(GameStrings.RocketCrewModuleMannedTravel.DisplayString);
		}
	}

	private string BuildTooltip()
	{
		MineableDeposit deposit = Node.Deposit;
		GameString gameString = (deposit.TargetReached(SurveyTarget.Composition) ? LocalizedEnumCollections.MineableDepositTypes.GetDisplayName(deposit.DepositType) : GameStrings.UnknownDepositValue);
		string value = (deposit.TargetReached(SurveyTarget.Density) ? StringManager.Get(deposit.Density) : ((string)GameStrings.UnknownDepositValue));
		string value2 = (deposit.TargetReached(SurveyTarget.Richness) ? StringManager.Get(deposit.Richness) : ((string)GameStrings.UnknownDepositValue));
		string value3 = (deposit.TargetReached(SurveyTarget.Size) ? StringManager.Get(deposit.Size) : ((string)GameStrings.UnknownDepositValue));
		StringManager.ReusableStringBuilder.Clear().Append("Type: ").AppendLine(gameString)
			.Append("Density: ")
			.AppendLine(value)
			.Append("Richness: ")
			.AppendLine(value2)
			.Append("Size: ")
			.AppendLine(value3)
			.Append("Composition: ");
		if (deposit.TargetReached(SurveyTarget.Composition))
		{
			StringManager.ReusableStringBuilder.AppendLine();
			DepositComposition depositComposition = deposit.DepositComposition;
			foreach (DepositMaterialReagentMix reagentMix in depositComposition.ReagentMixes)
			{
				StringManager.ReusableStringBuilder.AppendLine(reagentMix.Mixture.ToString());
			}
			foreach (DepositMaterialGas frozenGass in depositComposition.FrozenGasses)
			{
				frozenGass.AppendToString(ref StringManager.ReusableStringBuilder, asAtmosphere: false);
			}
			foreach (DepositMaterialGas gass in depositComposition.Gasses)
			{
				gass.AppendToString(ref StringManager.ReusableStringBuilder, asAtmosphere: true);
			}
		}
		else
		{
			StringManager.ReusableStringBuilder.AppendLine(GameStrings.UnknownDepositValue);
		}
		return StringManager.ReusableStringBuilder.ToString().Trim();
	}

	public void OnPointerUp(PointerEventData eventData)
	{
	}

	public void OnPointerDown(PointerEventData eventData)
	{
	}
}
