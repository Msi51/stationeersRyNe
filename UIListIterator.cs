using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIListIterator : MonoBehaviour
{
	public int Index;

	[Tooltip("Length of iteration. Exclusive. i.e 10 length will go from 0-9")]
	public int Length = 10;

	public bool Loop = true;

	[SerializeField]
	private Button _increaseButton;

	[SerializeField]
	private Button _decreaseButton;

	[SerializeField]
	private TextMeshProUGUI _indexText;

	[SerializeField]
	private string _indexFormat = "00";

	[SerializeField]
	private TextMeshProUGUI _warningText;

	[HideInInspector]
	public UnityEvent<int> onValueChanged = new UnityEvent<int>();

	public Func<int, string> TextOverride;

	private void Start()
	{
		_increaseButton.onClick.AddListener(Increase);
		_decreaseButton.onClick.AddListener(Decrease);
	}

	public void SetIndexWithoutInvoke(int index)
	{
		if (Loop)
		{
			Index = (int)Mathf.Repeat(index, Length);
		}
		Index = Mathf.Clamp(Index, 0, Length - 1);
		_indexText.text = TextOverride?.Invoke(Index) ?? Index.ToString(_indexFormat);
	}

	public void SetIndex(int index)
	{
		SetIndexWithoutInvoke(index);
		onValueChanged?.Invoke(Index);
	}

	public void Increase()
	{
		SetIndex(Index + 1);
	}

	public void Decrease()
	{
		SetIndex(Index - 1);
	}

	public void RandomIndex(bool invokeEvent = false)
	{
		int num = UnityEngine.Random.Range(0, Length);
		if (invokeEvent)
		{
			SetIndex(num);
		}
		else
		{
			SetIndexWithoutInvoke(num);
		}
	}

	public void SetInteractivity(bool isInteractive)
	{
		_increaseButton.interactable = isInteractive;
		_decreaseButton.interactable = isInteractive;
	}

	public void ShowWarningText(bool active)
	{
		_warningText.gameObject.SetActive(active);
	}
}
