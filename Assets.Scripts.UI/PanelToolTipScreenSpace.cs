using TMPro;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class PanelToolTipScreenSpace : UserInterfaceBase
{
	public TMP_Text ToolTipItemName;

	public TMP_Text Information;

	public GraphicRaycaster GraphicRayCast;

	public HorizontalLayoutGroup LayoutGroup;

	[SerializeField]
	private Canvas _canvas;

	[SerializeField]
	private float _offset;

	protected IScreenSpaceTooltip _tooltipToUpdate;

	public virtual void Initialize()
	{
		SetVisible(isVisble: false);
	}

	public virtual void ClearToolTip()
	{
		_tooltipToUpdate = null;
		SetVisible(isVisble: false);
		ToolTipItemName.SetText(string.Empty);
		Information.SetText(string.Empty);
		GraphicRayCast.enabled = true;
	}

	private void LateUpdate()
	{
		if (IsVisible)
		{
			if (_tooltipToUpdate != null && !_tooltipToUpdate.TooltipIsVisible)
			{
				ClearToolTip();
				return;
			}
			_tooltipToUpdate?.DoUpdate();
			Vector3 mousePosition = Input.mousePosition;
			Vector3 quadrant = UITooltipPanel.GetQuadrant(mousePosition);
			Vector3 vector = new Vector3(quadrant.x * (LayoutGroup.preferredWidth * 0.5f + _offset), quadrant.y * (LayoutGroup.preferredHeight * 0.5f + _offset));
			Transform.position = mousePosition - vector * _canvas.scaleFactor;
			RefreshText();
		}
	}

	public virtual void RefreshText()
	{
	}
}
