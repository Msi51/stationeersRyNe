using System;
using Assets.Scripts.Atmospherics;
using UnityEngine;

namespace Assets.Scripts.UI;

[Serializable]
public class GasThumbnail
{
	public string Name;

	public Chemistry.GasType GasType;

	public Sprite Thumbnail;
}
