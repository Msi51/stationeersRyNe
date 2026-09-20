using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class ChatWindow : MonoBehaviour
{
	public GameObject Typing;

	public Text ChatText;

	[HideInInspector]
	public float DeactiveTime;

	[HideInInspector]
	public string TargetText = "";

	private static readonly float TypingSpeed = 0.02f;

	private void OnEnable()
	{
		StartCoroutine(ShowText());
	}

	private void OnDisable()
	{
		StopCoroutine(ShowText());
		Typing.gameObject.SetActive(value: false);
		ChatText.gameObject.SetActive(value: false);
		ChatText.text = (TargetText = "");
		base.transform.SetAsFirstSibling();
	}

	private void Update()
	{
		if (!WorldManager.IsGamePaused && Time.time >= DeactiveTime && !Typing.activeInHierarchy && TargetText.Length > 0)
		{
			base.gameObject.SetActive(value: false);
			TargetText = "";
		}
	}

	public void SetText(string text)
	{
		text.Replace(" ", "");
		TargetText = text;
		for (int i = 0; i < TargetText.Length; i += 30)
		{
			if (i > 0 && i < TargetText.Length - 1)
			{
				TargetText = TargetText.Insert(i, "\n");
			}
		}
		ChatText.gameObject.SetActive(value: true);
		ChatText.text = "";
		Typing.SetActive(value: false);
		DeactiveTime = Time.time + (float)text.Length * TypingSpeed + 8f;
	}

	private IEnumerator ShowText()
	{
		while (true)
		{
			if (!ChatText.text.Equals(TargetText) && TargetText != null)
			{
				for (int i = 0; i < TargetText.Length; i++)
				{
					ChatText.text = TargetText.Substring(0, i + 1);
					yield return Yielders.WaitForSeconds(TypingSpeed);
				}
			}
			yield return null;
		}
	}
}
