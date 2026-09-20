using System.Text;
using Assets.Scripts.Localization2;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Objects.Rockets.Mining;
using TMPro;
using UI.Tooltips;
using UnityEngine;

namespace Objects.Rockets.UI;

public class LocationResourcesPanel : UserInterfaceBase
{
	[Space(18f)]
	[Header("Location Resources Panel")]
	[SerializeField]
	private TextMeshProUGUI _sizeText;

	[SerializeField]
	private TextMeshProUGUI _densityText;

	[SerializeField]
	private TextMeshProUGUI _richnessText;

	[SerializeField]
	private TextMeshProUGUI _depositTypeText;

	[SerializeField]
	private TextMeshProUGUI _compositionText;

	[SerializeField]
	private UITooltip _densityTooltip;

	[SerializeField]
	private UITooltip _richnessTooltip;

	[SerializeField]
	private UITooltip _sizeTooltip;

	private string _baseLineCollectionQuantity;

	private string _collectionQuantityFromRichness;

	private string _richnessMultiplier;

	private string _collectionSpeed;

	private string _densityReduction;

	private string _richnessReduction;

	private string _estTotalResources;

	private LocationPanel _locationPanel;

	public void Show(LocationPanel locationPanel)
	{
		SetVisible(isVisble: true);
		_locationPanel = locationPanel;
		Refresh();
	}

	public void Hide()
	{
		GameObject.SetActive(value: false);
	}

	public void Refresh()
	{
		MineableDeposit mineableDeposit = _locationPanel?.CurrentNode?.Deposit;
		if (mineableDeposit == null)
		{
			Clear();
			return;
		}
		if (mineableDeposit.TargetReached(SurveyTarget.Size))
		{
			_sizeText.text = StringManager.Get(mineableDeposit.Size);
			_baseLineCollectionQuantity = StringManager.Get(Mathf.RoundToInt(mineableDeposit.MineBaseLine() * mineableDeposit.DepositTypeMultiplier()));
			_densityReduction = StringManager.Get(MineableDeposit.DensityReduction(mineableDeposit.Size));
			_richnessReduction = StringManager.Get(MineableDeposit.RichnessReduction(mineableDeposit.Richness, mineableDeposit.Size));
		}
		else
		{
			_sizeText.text = GameStrings.UnknownDepositValue;
			_baseLineCollectionQuantity = string.Empty;
			_densityReduction = string.Empty;
			_richnessReduction = string.Empty;
		}
		if (mineableDeposit.TargetReached(SurveyTarget.Density))
		{
			_densityText.text = StringManager.Get(mineableDeposit.Density);
			_collectionSpeed = new TimeLength(mineableDeposit.TimeToMine()).ToNearestString();
			_estTotalResources = StringManager.Get(mineableDeposit.TotalOreAtLocation);
		}
		else
		{
			_densityText.text = GameStrings.UnknownDepositValue;
			_collectionSpeed = string.Empty;
		}
		if (mineableDeposit.TargetReached(SurveyTarget.Richness))
		{
			_richnessText.text = StringManager.Get(mineableDeposit.Richness);
			_richnessMultiplier = (MineableDeposit.RichnessMultiplier(mineableDeposit.Richness) * 100f).ToStringPercent("green");
			_collectionQuantityFromRichness = "<color=green>" + StringManager.Get(Mathf.RoundToInt(mineableDeposit.MineBaseLine() * mineableDeposit.DepositTypeMultiplier() * MineableDeposit.RichnessMultiplier(mineableDeposit.Richness))) + "</color>";
		}
		else
		{
			_richnessText.text = GameStrings.UnknownDepositValue;
			_collectionQuantityFromRichness = string.Empty;
		}
		StringBuilder sb = new StringBuilder();
		if (mineableDeposit.TargetReached(SurveyTarget.Composition))
		{
			DepositComposition depositComposition = mineableDeposit.DepositComposition;
			sb.Clear();
			foreach (DepositMaterialReagentMix reagentMix in depositComposition.ReagentMixes)
			{
				sb.AppendLine(reagentMix.Mixture.ToString());
			}
			foreach (DepositMaterialGas frozenGass in depositComposition.FrozenGasses)
			{
				frozenGass.AppendToString(ref sb, asAtmosphere: false);
			}
			foreach (DepositMaterialGas gass in depositComposition.Gasses)
			{
				gass.AppendToString(ref sb, asAtmosphere: true);
			}
			_depositTypeText.text = LocalizedEnumCollections.MineableDepositTypes.GetDisplayName(mineableDeposit.DepositType);
			_compositionText.text = sb.ToString();
		}
		else
		{
			_depositTypeText.text = GameStrings.UnknownDepositValue;
			_compositionText.text = GameStrings.UnknownDepositValue;
		}
		_densityTooltip.TooltipText = GetDensityToolTip(sb);
		_richnessTooltip.TooltipText = GetRichnessToolTip(sb);
		_sizeTooltip.TooltipText = GetSizeTooltip(sb);
	}

	public string GetDensityToolTip(StringBuilder sb)
	{
		sb.Clear();
		sb.Append(GameStrings.DepositDensityToolTip);
		if (!string.IsNullOrEmpty(_collectionSpeed))
		{
			sb.Append("\n");
			sb.Append(GameStrings.EstimatedTotalResources);
			sb.Append("<color=green>").Append(_estTotalResources).Append("</color>");
			sb.Append("\n");
			sb.Append(GameStrings.MineOperationSpeedToolTip);
			sb.Append("<color=green>").Append(_collectionSpeed).Append("</color>");
		}
		return sb.ToString();
	}

	public string GetRichnessToolTip(StringBuilder sb)
	{
		sb.Clear();
		sb.Append(GameStrings.DepositRichnessToolTip);
		if (!string.IsNullOrEmpty(_collectionQuantityFromRichness))
		{
			sb.Append("\n");
			sb.Append(GameStrings.RichnessOperationAmountToolTip.AsString(_richnessMultiplier, "<color=yellow>" + _baseLineCollectionQuantity + "</color>", _collectionQuantityFromRichness));
		}
		return sb.ToString();
	}

	public string GetSizeTooltip(StringBuilder sb)
	{
		sb.Clear();
		sb.Append(GameStrings.DepositSizeToolTip);
		if (!string.IsNullOrEmpty(_baseLineCollectionQuantity))
		{
			sb.Append("\n");
			sb.Append(GameStrings.CollectionQuantityToolTip);
			sb.Append("<color=yellow>").Append(_baseLineCollectionQuantity).Append("</color>");
		}
		if (!string.IsNullOrEmpty(_densityReduction))
		{
			sb.Append("\n");
			sb.Append(GameStrings.DensityReductionToolTip);
			sb.Append("<color=orange>").Append(_densityReduction).Append("</color>");
		}
		if (!string.IsNullOrEmpty(_richnessReduction))
		{
			sb.Append("\n");
			sb.Append(GameStrings.RichnessReductionToolTip);
			sb.Append("<color=orange>").Append(_richnessReduction).Append("</color>");
		}
		return sb.ToString();
	}

	public void Clear()
	{
		_sizeText.text = string.Empty;
		_densityText.text = string.Empty;
		_richnessText.text = string.Empty;
		_compositionText.text = string.Empty;
		_depositTypeText.text = string.Empty;
	}
}
