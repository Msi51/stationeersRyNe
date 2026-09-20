using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.UI;

public class IconEffect : MonoBehaviour
{
	public Entity Parent;

	public Animator Animator;

	public List<GameObject> Effects = new List<GameObject>();

	private Coroutine _rotateIcon;

	[ReadOnly]
	public bool IsActive;

	public void DisplayIcon(float seconds, int iconType)
	{
		if (IsActive)
		{
			Debug.LogWarning("Trying to display an icon while one already showing", Parent);
			return;
		}
		IsActive = true;
		Effects[iconType].SetActive(value: true);
		Animator.SetBool("Rotate", value: true);
		_rotateIcon = StartCoroutine(RotateIcon(seconds));
	}

	public void DisplayIcon(int iconType)
	{
		if (!IsActive)
		{
			IsActive = true;
			Effects[iconType].SetActive(value: true);
			Animator.SetBool("Rotate", value: true);
		}
	}

	public void CancelIcon()
	{
		Animator.SetBool("Rotate", value: false);
	}

	public void OnFinishedAnimation()
	{
		foreach (GameObject effect in Effects)
		{
			effect.SetActive(value: false);
		}
		IsActive = false;
	}

	private IEnumerator RotateIcon(float seconds)
	{
		yield return Yielders.WaitForSeconds(seconds);
		Animator.SetBool("Rotate", value: false);
	}
}
