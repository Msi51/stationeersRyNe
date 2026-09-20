using Cysharp.Threading.Tasks;
using UI.Tooltips;
using UnityEngine.EventSystems;

namespace Assets.Scripts.UI;

public class ToolTipBase : UserInterfaceBase
{
	private static ToolTipBase _hoverTooltip;

	public override void OnPointerEnter(PointerEventData eventData)
	{
		bool num = (object)_hoverTooltip == null;
		if ((object)_hoverTooltip == null)
		{
			_hoverTooltip = this;
		}
		if (num)
		{
			ShowTooltip().Forget();
		}
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		if (_hoverTooltip == this)
		{
			_hoverTooltip = null;
		}
	}

	private static async UniTaskVoid ShowTooltip()
	{
		while ((object)_hoverTooltip != null && _hoverTooltip.isActiveAndEnabled && _hoverTooltip.gameObject.activeInHierarchy)
		{
			UITooltipManager.SetTooltip(_hoverTooltip.GetString());
			UITooltipCanvas.Instance.DoUpdate();
			await UniTask.NextFrame();
		}
		UITooltipManager.ClearTooltip();
		UITooltipCanvas.Instance.DoUpdate();
	}

	public virtual string GetString()
	{
		return string.Empty;
	}
}
