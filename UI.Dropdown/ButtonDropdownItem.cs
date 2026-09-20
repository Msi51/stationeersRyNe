using Assets.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Dropdown;

public class ButtonDropdownItem : UserInterfaceBase
{
	[SerializeField]
	private TextMeshProUGUI _textMesh;

	[SerializeField]
	private Button _button;

	private ButtonDropdown _owner;

	public int Index { get; private set; }

	public string Title { get; private set; }

	public void Initialize(string title, int index, ButtonDropdown owner)
	{
		Title = title;
		_textMesh.text = title;
		Index = index;
		_button.onClick.AddListener(OnClick);
		_owner = owner;
	}

	private void OnDestroy()
	{
		_button.onClick.RemoveListener(OnClick);
	}

	private void OnClick()
	{
		_owner.ItemClicked(Index);
	}
}
