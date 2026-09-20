using System.Collections.Generic;
using System.Xml.Serialization;

namespace Assets.Scripts.Serialization;

[XmlRoot]
public class UserInterfaceSaveData
{
	public List<WindowSaveData> OpenSlots = new List<WindowSaveData>();

	public int SelectedButton;

	public int ActiveHandSlot;
}
