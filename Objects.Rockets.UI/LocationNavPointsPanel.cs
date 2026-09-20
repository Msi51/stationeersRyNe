using System.Collections.Generic;
using Assets.Scripts.UI;
using Assets.Scripts.UI.CustomScrollPanel;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Objects.Rockets.UI;

public class LocationNavPointsPanel : UserInterfaceBase
{
	[SerializeField]
	private NavInfoRow _navInfoRowPrefab;

	[SerializeField]
	private Transform _navInfoRowParent;

	[SerializeField]
	private ScrollPanel _scrollPanel;

	[SerializeField]
	private VerticalLayoutGroup _verticalLayout;

	private LocationPanel _locationPanel;

	private const int MAX_ROWS = 10;

	private List<NavInfoRow> _infoRows = new List<NavInfoRow>(10);

	private bool _initialised;

	public void Hide()
	{
		SetVisible(isVisble: false);
		Clear();
	}

	public void Init()
	{
		if (!_initialised)
		{
			_initialised = true;
			for (int i = 0; i < 10; i++)
			{
				_infoRows.Add(Object.Instantiate(_navInfoRowPrefab, _navInfoRowParent));
			}
		}
	}

	public void Show(LocationPanel locationPanel)
	{
		Init();
		SetVisible(isVisble: true);
		_locationPanel = locationPanel;
		ShowNavInfo();
		ResizeScrollPanel().Forget();
	}

	private async UniTaskVoid ResizeScrollPanel()
	{
		await UniTask.WaitForEndOfFrame();
		_scrollPanel.SetScrollPosition(0f);
		_scrollPanel.SetContentHeight(_verticalLayout.preferredHeight);
	}

	public void Refresh()
	{
		SpaceMapNode spaceMapNode = _locationPanel?.CurrentNode;
		if (spaceMapNode == null)
		{
			ClearInfoRows();
			return;
		}
		for (int i = 0; i < _infoRows.Count && spaceMapNode.ChildConnections.Count > i; i++)
		{
			NodeConnection nodeConnection = spaceMapNode.ChildConnections[i];
			_infoRows[i].SetValues(spaceMapNode, nodeConnection);
		}
	}

	public void Clear()
	{
		ClearInfoRows();
	}

	private void ShowNavInfo()
	{
		SpaceMapNode spaceMapNode = _locationPanel?.CurrentNode;
		if (spaceMapNode == null)
		{
			ClearInfoRows();
			return;
		}
		for (int i = 0; i < _infoRows.Count; i++)
		{
			if (spaceMapNode.ChildConnections.Count <= i || spaceMapNode.ChildConnections[i].Child.NodeType != NodeType.Static)
			{
				_infoRows[i].Clear();
				_infoRows[i].SetVisible(isVisble: false);
			}
			else
			{
				NodeConnection nodeConnection = spaceMapNode.ChildConnections[i];
				_infoRows[i].SetVisible(isVisble: true);
				_infoRows[i].SetValues(spaceMapNode, nodeConnection);
			}
		}
	}

	private void ClearInfoRows()
	{
		foreach (NavInfoRow infoRow in _infoRows)
		{
			infoRow.Clear();
			infoRow.SetVisible(isVisble: false);
		}
	}
}
