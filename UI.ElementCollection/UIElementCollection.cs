using System.Collections.Generic;
using Assets.Scripts.UI;
using UnityEngine;

namespace UI.ElementCollection;

public abstract class UIElementCollection<TElement, TData> where TElement : UserInterfaceBase, IUIElement<TData> where TData : IUIElementData
{
	private Dictionary<long, TElement> _elementLookup = new Dictionary<long, TElement>();

	private List<TElement> _elementList = new List<TElement>();

	public void Update(List<TData> data, TElement prefab, Transform parent)
	{
		Prepare();
		foreach (TData datum in data)
		{
			Update(datum, prefab, parent);
		}
		CleanUp();
	}

	private void Prepare()
	{
		foreach (TElement element in _elementList)
		{
			element.ShowingThisFrame = false;
		}
	}

	private void Update(TData data, TElement prefab, Transform parent)
	{
		if (!_elementLookup.TryGetValue(data.Id, out var value))
		{
			value = AddElement(data, prefab, parent);
		}
		value.ShowingThisFrame = true;
		value.DoUpdate(data);
	}

	private void CleanUp()
	{
		for (int num = _elementList.Count - 1; num >= 0; num--)
		{
			TElement val = _elementList[num];
			if (!val.ShowingThisFrame)
			{
				_elementList.RemoveAt(num);
				_elementLookup.Remove(val.Id);
				Destroy(val);
			}
		}
	}

	private TElement AddElement(TData data, TElement prefab, Transform parent)
	{
		TElement val = Instantiate(prefab, parent);
		val.Id = data.Id;
		_elementLookup.Add(data.Id, val);
		_elementList.Add(val);
		return val;
	}

	protected virtual TElement Instantiate(TElement prefab, Transform parent)
	{
		return Object.Instantiate(prefab, parent);
	}

	protected virtual void Destroy(TElement element)
	{
		Object.Destroy(element.GameObject);
	}
}
