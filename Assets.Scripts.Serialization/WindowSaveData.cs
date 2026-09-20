using System.Xml.Serialization;
using UnityEngine;

namespace Assets.Scripts.Serialization;

[XmlRoot]
public class WindowSaveData
{
	public int SlotId;

	public int StringHash;

	public bool IsOpen = true;

	public bool IsUndocked;

	public Vector2 Position;
}
