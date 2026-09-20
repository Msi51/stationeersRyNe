using Objects.Rockets.Mining;
using TMPro;
using UnityEngine;

namespace Objects.Rockets.UI;

public class SiteSurveyTargetItem : MonoBehaviour
{
	[SerializeField]
	private SurveyTarget _surveyTarget;

	[SerializeField]
	private TextMeshProUGUI _targetTMP;

	[SerializeField]
	private Color _targetReachedColor;

	[SerializeField]
	private Color _inActiveColor;

	public SurveyTarget SurveyTarget => _surveyTarget;

	public void Refresh(MineableDeposit deposit)
	{
		if (deposit.TargetReached(SurveyTarget))
		{
			_targetTMP.color = _targetReachedColor;
		}
		else
		{
			_targetTMP.color = _inActiveColor;
		}
	}
}
