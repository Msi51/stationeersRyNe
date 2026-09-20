using UI.ElementCollection;
using UnityEngine;

namespace UI.Motherboard;

public struct MapMotherboardPanelTrackableIconData : IUIElementData
{
	public Vector3 WorldPosition;

	public Color Color;

	public float Scale;

	public long Id { get; set; }
}
