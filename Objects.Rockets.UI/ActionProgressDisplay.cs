using Objects.Rockets.Scanning;
using TMPro;
using UnityEngine;

namespace Objects.Rockets.UI;

public class ActionProgressDisplay : GameBase
{
	[SerializeField]
	private TextMeshProUGUI _actionName;

	[SerializeField]
	private TextMeshProUGUI _progressRatio;

	[SerializeField]
	private RectTransform _progressBar;

	[SerializeField]
	private TextMeshProUGUI _optionalText;

	protected ProgressDisplayData _data;

	public virtual void Apply(ProgressDisplayData data)
	{
		_data = data;
		_actionName.text = data.Title;
		_progressRatio.text = data.RatioString();
		_progressBar.localScale = new Vector3(data.Ratio, 1f, 1f);
		_optionalText.text = data.ActionInfoText;
	}

	public void Clear()
	{
		_actionName.text = string.Empty;
		_progressRatio.text = string.Empty;
	}
}
