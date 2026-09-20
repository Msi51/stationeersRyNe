using System.Collections;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.UI;

[RequireComponent(typeof(Animator))]
public class UserInterfaceAnimated : UserInterfaceBase
{
	public Animator Animator;

	public bool shouldShowForDuration;

	[Range(0f, 30f)]
	public float showDuration = 5f;

	private static readonly int IsVisibleState = Animator.StringToHash("IsVisible");

	private bool IsAnimVisible
	{
		get
		{
			if (GameObject.activeSelf)
			{
				return Animator.GetBool(IsVisibleState);
			}
			return false;
		}
	}

	public void Show()
	{
		if (!IsAnimVisible)
		{
			SetVisible(isVisble: true);
			Animator.SetBool(IsVisibleState, value: true);
			if (shouldShowForDuration && IsVisible)
			{
				this.StartCoroutineOnMainThread(HideAfterDuration());
			}
		}
	}

	public void Hide()
	{
		if (IsAnimVisible)
		{
			Animator.SetBool(IsVisibleState, value: false);
		}
	}

	public void Toggle()
	{
		if (IsAnimVisible)
		{
			Hide();
		}
		else
		{
			Show();
		}
	}

	public void SetIsShown(bool isShown)
	{
		if (IsAnimVisible != isShown)
		{
			if (isShown)
			{
				Show();
			}
			else
			{
				Hide();
			}
		}
	}

	public void OnFadeOutComplete()
	{
		SetVisible(isVisble: false);
	}

	private IEnumerator HideAfterDuration()
	{
		yield return Yielders.WaitForSeconds(showDuration);
		Hide();
	}
}
