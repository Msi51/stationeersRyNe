using Assets.Scripts.Localization2;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;

namespace Objects.Rockets.UI;

public class NavInfoRow : UserInterfaceBase
{
	[SerializeField]
	private TextMeshProUGUI _nameTextMesh;

	[SerializeField]
	private TextMeshProUGUI _distance;

	[SerializeField]
	private TextMeshProUGUI _chartProgress;

	public void SetValues(SpaceMapNode node, NodeConnection nodeConnection)
	{
		if (node == null || nodeConnection == null)
		{
			Clear();
			return;
		}
		_nameTextMesh.text = nodeConnection.Child.DisplayName;
		_distance.text = (nodeConnection.Child.IsCharted ? (StringManager.Get(nodeConnection.Distance()) + "Δv") : ((string)GameStrings.SpaceMapUknownDeltaV));
		_chartProgress.text = (nodeConnection.Child.IsCharted ? GameStrings.Charted.DisplayString : (StringManager.Get(node.ChartPoints) + " / " + StringManager.Get(nodeConnection.Difficulty)));
	}

	public void Clear()
	{
		_nameTextMesh.text = string.Empty;
		_distance.text = string.Empty;
		_chartProgress.text = string.Empty;
	}

	public void HideAndClear()
	{
		SetVisible(isVisble: false);
		Clear();
	}
}
