using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class TaskItem : MonoBehaviour
{
	public TMP_Text TaskText;

	public string TaskDescription;

	public Image TaskState;

	public Sprite TaskComplete;

	public Sprite TaskIncomplete;

	public Animator anim;

	public HelpLinkHandler LinkHandler;

	private bool _isComplete;

	public string Text
	{
		set
		{
			TaskText.text = value;
		}
	}

	public string Description
	{
		get
		{
			return TaskDescription;
		}
		set
		{
			TaskDescription = value;
		}
	}

	public bool Completed
	{
		get
		{
			return _isComplete;
		}
		set
		{
			if (TaskState == null || !this || !TaskState.sprite)
			{
				return;
			}
			_isComplete = value;
			UnityMainThreadDispatcher.Instance().Enqueue(delegate
			{
				if ((bool)TaskState)
				{
					TaskState.sprite = (value ? TaskComplete : TaskIncomplete);
				}
				if ((bool)anim)
				{
					anim.SetBool("TaskCompleted", _isComplete);
				}
			});
		}
	}
}
