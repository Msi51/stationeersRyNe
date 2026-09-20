using System;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class ConstructionPanel : UserInterfaceBase
{
	public GridLayoutGroup GridGroup;

	public int BuildIndex;

	public Image Slot;

	public Image SelectedImage;

	[ReadOnly]
	public MultiConstructor Parent;

	private static readonly int SwapVariantHash = Animator.StringToHash("SwapVariant");

	private int _attempt;

	public void SelectUp()
	{
		if (Parent?.Constructables == null)
		{
			return;
		}
		BuildIndex++;
		if (BuildIndex >= Parent.Constructables.Count)
		{
			BuildIndex = 0;
		}
		Parent.LastSelectedIndex = BuildIndex;
		if (Parent.Constructables[BuildIndex] == null)
		{
			_attempt++;
			if (_attempt < Parent.Constructables.Count)
			{
				SelectUp();
			}
		}
		else
		{
			_attempt = 0;
			InventoryManager.UpdatePlacement(Parent.Constructables[BuildIndex]);
			UIAudioManager.Play(SwapVariantHash);
		}
	}

	public void SelectDown()
	{
		if (Parent?.Constructables == null)
		{
			return;
		}
		BuildIndex--;
		if (BuildIndex < 0)
		{
			BuildIndex = Math.Max(Parent.Constructables.Count - 1, 0);
		}
		Parent.LastSelectedIndex = BuildIndex;
		if (Parent.Constructables[BuildIndex] == null)
		{
			_attempt++;
			if (_attempt < Parent.Constructables.Count)
			{
				SelectDown();
			}
		}
		else
		{
			_attempt = 0;
			InventoryManager.UpdatePlacement(Parent.Constructables[BuildIndex]);
			UIAudioManager.Play(SwapVariantHash);
		}
	}

	public void Assign(MultiConstructor parent)
	{
		Parent = parent;
		GridGroup.constraintCount = Math.Min(3, Parent.Slots.Count);
		BuildIndex = Parent.LastSelectedIndex;
		InventoryManager.UpdatePlacement(Parent.Constructables[BuildIndex]);
		UIAudioManager.Play(SwapVariantHash);
	}
}
