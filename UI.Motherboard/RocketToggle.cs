using Assets.Scripts;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Motherboard;

public class RocketToggle : GameBase
{
	[SerializeField]
	[ReadOnly]
	protected Button button;

	[SerializeField]
	protected Image selected;

	[SerializeField]
	[ReadOnly]
	protected RocketToggleGroup parentToggleGroup;

	[SerializeField]
	[ReadOnly]
	private int index;

	[SerializeField]
	private UITooltip _tooltip;

	private bool _isSelected;

	public int Index => index;

	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			_isSelected = value;
			Color color = selected.color;
			color.a = (IsSelected ? 1 : 0);
			selected.color = color;
		}
	}

	public void SetTooltip(string text)
	{
		if ((bool)_tooltip)
		{
			_tooltip.TooltipText = text;
		}
	}

	public virtual void OnClicked()
	{
		parentToggleGroup.Select(Index);
	}

	public void Init(RocketToggleGroup parent)
	{
		parentToggleGroup = parent;
	}
}
