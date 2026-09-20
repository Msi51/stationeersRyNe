using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

[Serializable]
public class SlotHotkeyIndicator
{
	public GameObject Base;

	public UiComponentRenderer Renderer;

	public Image Background;

	public Image Outline;

	public Image Icon;

	public Image Button;

	public Animator Animator;

	public TMP_Text ControlText;

	public bool ActiveSelf
	{
		get
		{
			if (!Renderer)
			{
				return Base.activeSelf;
			}
			return Renderer.IsVisibleSelf;
		}
	}

	public void SetVisible(bool isVisible)
	{
		if (Renderer != null)
		{
			Renderer.SetVisible(isVisible);
		}
		else
		{
			Base.SetActive(isVisible);
		}
	}

	public void Animate(int value)
	{
		Animator.SetTrigger(value);
	}
}
