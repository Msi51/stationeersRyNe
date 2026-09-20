using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.LoadGame;

public class SteamPublishedIcon : UserInterfaceBase
{
	[Header("Steam Published Icon")]
	[SerializeField]
	private Color _workshopMatchFoundColorTint;

	[SerializeField]
	private Color _workshopMatchNotFoundColorTint;

	[SerializeField]
	private Image _isPublishedImage;

	private bool _matchFound;

	private bool _tooltipShowing;

	public void SetMatchFound(bool found)
	{
		_matchFound = found;
		_isPublishedImage.color = (found ? _workshopMatchFoundColorTint : _workshopMatchNotFoundColorTint);
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		string description = (_matchFound ? "This has been published.\nClicking publish will update the existing workshop item." : "No matching workshop item found.\nClicking publish will create a new workshop item.");
		_tooltipShowing = true;
		PanelToolTip.Instance.SetUpTooltip("Steam Workshop", description);
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		_tooltipShowing = false;
		PanelToolTip.Instance.ClearToolTip();
	}

	public override void OnDisable()
	{
		if (_tooltipShowing)
		{
			PanelToolTip.Instance.ClearToolTip();
		}
	}
}
