using System.Collections.Generic;
using System.Xml.Serialization;

namespace Assets.Scripts.GridSystem;

[XmlRoot]
public class RoomData
{
	[XmlElement]
	public long RoomId;

	public bool WillSave = true;

	[XmlArray("Grids")]
	[XmlArrayItem("Grid")]
	public List<Grid3> Grids = new List<Grid3>();

	public RoomData()
	{
	}

	public RoomData(Room room)
	{
		RoomId = room.RoomId;
		WillSave = room.WillSave;
		foreach (WorldGrid grid in room.Grids)
		{
			Grids.Add(grid.Value);
		}
	}
}
