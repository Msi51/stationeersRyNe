using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts;

[Serializable]
public class DigitGameObject
{
	public string Name;

	public int Digit;

	public List<GameObject> Digits = new List<GameObject>();

	public void IsVisible(bool isVisible)
	{
		foreach (GameObject digit in Digits)
		{
			digit.SetActive(isVisible);
		}
	}
}
