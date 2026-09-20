using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.UI;

public class ChatCanvas : ManagerBase
{
	public Transform StartPos;

	public Transform EndPos;

	public Transform Grid;

	private Vector3 LocalPos;

	private Vector3 TargetPos;

	private ChatWindow TargetWindow;

	public override void ManagerStart()
	{
		base.ManagerStart();
		LocalPos = (TargetPos = StartPos.localPosition);
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		if (WorldManager.IsGamePaused || (object)Camera.main == null)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < Grid.childCount; i++)
		{
			if (Grid.GetChild(i).gameObject.activeInHierarchy)
			{
				num++;
			}
		}
		if (!Settings.CurrentData.PopupChat || num == 0)
		{
			base.gameObject.SetActive(value: false);
		}
		if (StartPos.gameObject.activeInHierarchy)
		{
			StartPos.localPosition = Vector3.Lerp(StartPos.localPosition, TargetPos, Time.deltaTime * 0.06f);
		}
		base.transform.LookAt(Camera.main.transform);
	}

	private void OnEnable()
	{
		ChangeTarget().Forget();
	}

	private void OnDisable()
	{
		ChangeTarget().Forget();
		HideAllPopups();
		TargetWindow = null;
	}

	public void SetText(string text)
	{
		if (Settings.CurrentData.PopupChat && text.Length > 0)
		{
			TargetPos = LocalPos;
			ChatWindow chatWindow = (TargetWindow ? TargetWindow : Grid.GetChild(0).GetComponent<ChatWindow>());
			TargetWindow = null;
			chatWindow.SetText(text);
			if (!chatWindow.gameObject.activeInHierarchy)
			{
				chatWindow.gameObject.SetActive(value: false);
				chatWindow.transform.SetAsFirstSibling();
				chatWindow.gameObject.SetActive(value: true);
			}
		}
	}

	public void HideAllPopups()
	{
		for (int i = 0; i < Grid.childCount; i++)
		{
			Grid.GetChild(i).gameObject.SetActive(value: false);
		}
	}

	public void ShowStatus(bool status)
	{
		if (Settings.CurrentData.PopupChat)
		{
			if (status)
			{
				TargetWindow = Grid.GetChild(0).GetComponent<ChatWindow>();
				TargetWindow.gameObject.SetActive(value: false);
				TargetWindow.transform.SetAsLastSibling();
				TargetWindow.gameObject.SetActive(value: true);
				TargetWindow.Typing.SetActive(value: true);
			}
			else if (TargetWindow != null && TargetWindow.Typing.activeInHierarchy)
			{
				TargetWindow.Typing.SetActive(value: false);
				TargetWindow.TargetText = " ";
				TargetWindow.DeactiveTime = 0.5f;
			}
		}
	}

	private async UniTaskVoid ChangeTarget()
	{
		while (true)
		{
			TargetPos = LocalPos + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
			await UniTask.Delay(500);
		}
	}
}
