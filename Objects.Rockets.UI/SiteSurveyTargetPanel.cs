using Objects.Rockets.Mining;
using UnityEngine;

namespace Objects.Rockets.UI;

public class SiteSurveyTargetPanel : MonoBehaviour
{
	[SerializeField]
	private SiteSurveyTargetItem[] _siteSurveyTargetItems;

	private LocationPanel _locationPanel;

	[SerializeField]
	private GameObject _content;

	public void Show(LocationPanel locationPanel)
	{
		_content.SetActive(value: true);
		_locationPanel = locationPanel;
		Refresh();
	}

	public void Hide()
	{
		_content.SetActive(value: false);
	}

	public void Refresh()
	{
		if (!_content.gameObject.activeSelf)
		{
			return;
		}
		MineableDeposit mineableDeposit = _locationPanel?.CurrentNode?.Deposit;
		if (_siteSurveyTargetItems == null || mineableDeposit == null)
		{
			Hide();
			return;
		}
		SiteSurveyTargetItem[] siteSurveyTargetItems = _siteSurveyTargetItems;
		for (int i = 0; i < siteSurveyTargetItems.Length; i++)
		{
			siteSurveyTargetItems[i].Refresh(mineableDeposit);
		}
	}
}
