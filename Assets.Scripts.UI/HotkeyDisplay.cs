using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class HotkeyDisplay : UserInterfaceBase
{
	public TMP_Text Key;

	public Image Button;

	public KeyItem KeyItem;

	public string Assignment;

	public KeyCode KeyCode;

	public UiComponentRenderer ButtonRenderer;

	public UiComponentRenderer KeyRenderer;

	private LayoutElement _layoutElement;

	private LayoutElement LayoutElement
	{
		get
		{
			if (!_layoutElement)
			{
				return _layoutElement = GetComponent<LayoutElement>();
			}
			return _layoutElement;
		}
	}

	public void Awake()
	{
		KeyManager.OnControlsChanged += Refresh;
		Localization.OnLanguageChanged = (Action)Delegate.Combine(Localization.OnLanguageChanged, new Action(Refresh));
		Refresh();
	}

	public void Assign(KeyItem keyItem)
	{
		KeyItem = keyItem;
		keyItem.OnChanged += Refresh;
	}

	public void Refresh()
	{
		if (KeyItem != null)
		{
			Assignment = KeyItem.Name;
			KeyCode = KeyItem.Key;
		}
		else
		{
			KeyCode = KeyManager.GetKey(Assignment);
		}
		ButtonReference buttonReference = KeyManager.GetButtonReference(KeyCode);
		if (buttonReference != null)
		{
			ButtonRenderer.SetVisible(isVisble: true);
			KeyRenderer.SetVisible(isVisble: false);
			Button.sprite = buttonReference.Sprite;
			return;
		}
		KeyRenderer.SetVisible(isVisble: true);
		ButtonRenderer.SetVisible(isVisble: false);
		string keyName = Localization.GetKeyName(KeyCode, stripSpecial: false);
		Key.text = keyName;
		if ((bool)LayoutElement && !string.IsNullOrEmpty(keyName))
		{
			LayoutElement.minWidth = Key.fontSize * (float)keyName.Length * 0.75f;
		}
	}
}
