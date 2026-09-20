using System.Collections.Generic;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class QuestItem : DraggableWindow
{
	public GameObject Parent;

	public Image QuestThumbail;

	public TMP_Text QuestTitle;

	public TMP_Text QuestDescription;

	public VerticalLayoutGroup QuestTaskGrid;

	public TaskItem TaskItemPrefab;

	public Canvas MasterCanvas;

	public Image Background;

	public List<TaskItem> CurrentTaskItems = new List<TaskItem>();

	private bool _isOpaque;

	public string Title
	{
		set
		{
			QuestTitle.text = Localization.ParseTooltip(value);
		}
	}

	public string Description
	{
		set
		{
			QuestDescription.text = Localization.ParseTooltip(value);
		}
	}

	public bool IsOpaque
	{
		get
		{
			return _isOpaque;
		}
		set
		{
			_isOpaque = value;
			Background.color = Background.color.SetAlpha(_isOpaque ? 1f : 0.75f);
		}
	}

	public TaskItem NewTask(string taskText, bool isHidden = false)
	{
		TaskItem taskItem = null;
		if (!isHidden)
		{
			taskItem = Object.Instantiate(TaskItemPrefab, QuestTaskGrid.transform);
			taskItem.LinkHandler.Canvas = MasterCanvas;
		}
		else
		{
			taskItem = Object.Instantiate(TaskItemPrefab);
		}
		taskItem.Text = Localization.ParseTooltip(taskText);
		CurrentTaskItems.Add(taskItem);
		return taskItem;
	}

	public void ClearTasks()
	{
		int count = CurrentTaskItems.Count;
		while (count-- > 0)
		{
			TaskItem taskItem = CurrentTaskItems[count];
			if ((bool)taskItem)
			{
				Object.Destroy(taskItem.gameObject);
			}
			CurrentTaskItems.RemoveAt(count);
		}
	}

	public void DestroyTask(TaskItem taskItem)
	{
		CurrentTaskItems.RemoveAll((TaskItem t) => taskItem == t);
	}

	public void ButtonOpacity()
	{
		IsOpaque = !IsOpaque;
	}
}
