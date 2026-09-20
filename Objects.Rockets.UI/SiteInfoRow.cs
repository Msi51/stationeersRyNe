using System.Text;
using Assets.Scripts.Util;
using Objects.Rockets.Mining;
using TMPro;
using UnityEngine;

namespace Objects.Rockets.UI;

public class SiteInfoRow : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI _nameTextMesh;

	[SerializeField]
	private TextMeshProUGUI _sizeTextMesh;

	[SerializeField]
	private TextMeshProUGUI _densityTextMesh;

	[SerializeField]
	private TextMeshProUGUI _richnessTextMesh;

	[SerializeField]
	private TextMeshProUGUI _compositionTextMesh;

	private static StringBuilder _stringBuilder = new StringBuilder();

	public void SetValues(SpaceMapNodeData siteData)
	{
		_nameTextMesh.text = siteData.Name;
		_sizeTextMesh.text = GetRangeText(siteData.DepositData.SizeData.Min, siteData.DepositData.SizeData.Max);
		_densityTextMesh.text = GetRangeText(siteData.DepositData.DensityData.Min, siteData.DepositData.DensityData.Max);
		_richnessTextMesh.text = GetRangeText(siteData.DepositData.RichnessData.Min, siteData.DepositData.RichnessData.Max);
		_stringBuilder.Clear();
		foreach (DepositMaterialReagentMixData reagentMix in siteData.DepositData.DepositCompositionData.ReagentMixes)
		{
			reagentMix.ToolTip(_stringBuilder, 0);
		}
		foreach (DepositMaterialGasData frozenGasMix in siteData.DepositData.DepositCompositionData.FrozenGasMixes)
		{
			frozenGasMix.AppendToString(ref _stringBuilder, asAtmosphere: false);
		}
		foreach (DepositMaterialGasData gasMix in siteData.DepositData.DepositCompositionData.GasMixes)
		{
			gasMix.AppendToString(ref _stringBuilder, asAtmosphere: true);
		}
		_compositionTextMesh.text = _stringBuilder.ToString();
	}

	private string GetRangeText(int min, int max)
	{
		string text = StringManager.Get(min);
		string text2 = StringManager.Get(max);
		return text + " - " + text2;
	}
}
