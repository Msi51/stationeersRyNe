using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Trading;

namespace Assets.Scripts;

public class WorldAtmosphereSpawnData : IChecksum
{
	private const string GRID_X_ATTRIBUTE = "x";

	[XmlAttribute("x")]
	public int GridX;

	private const string GRID_Y_ATTRIBUTE = "y";

	[XmlAttribute("y")]
	public int GridY;

	private const string GRID_Z_ATTRIBUTE = "z";

	[XmlAttribute("z")]
	public int GridZ;

	private const string ROOM_ID_ATTRIBUTE = "RoomId";

	[XmlAttribute("RoomId")]
	public long RoomId;

	[XmlIgnore]
	public WorldGrid WorldGrid;

	[XmlElement("Gas", typeof(GasAction))]
	public List<ActionData> Actions = new List<ActionData>();

	public WorldAtmosphereSpawnData()
	{
	}

	public WorldAtmosphereSpawnData(Atmosphere atmosphere)
	{
		if (atmosphere.IsValidWorld() && atmosphere.Room != null)
		{
			GridX = atmosphere.WorldGrid.Value.x;
			GridY = atmosphere.WorldGrid.Value.y;
			GridZ = atmosphere.WorldGrid.Value.z;
			GasMixture.CreateGasActions(ref Actions, atmosphere.GasMixture);
			RoomId = atmosphere.Room.RoomId;
		}
	}

	public int GetChecksum()
	{
		int num = 0;
		foreach (ActionData action in Actions)
		{
			num = (num ^ action.GetChecksum()) * 41;
		}
		return num;
	}

	public void Initialize()
	{
		WorldGrid = new WorldGrid(GridX, GridY, GridZ);
		foreach (ActionData action in Actions)
		{
			action.Initialize();
		}
	}

	public bool Execute()
	{
		if (WorldGrid == WorldGrid.INVALID)
		{
			return false;
		}
		RoomController.World.RegisterRoomGridFromWorldSetting(RoomId, WorldGrid);
		(GridController.World.AtmosphericsController.GetAtmosphereLocal(WorldGrid) ?? new Atmosphere(WorldGrid, 0L)).GasMixture.Reset();
		foreach (ActionData action in Actions)
		{
			action.Execute(WorldGrid, null);
		}
		return true;
	}

	public void Add(ref XElement parent, string elementName)
	{
		XElement parentElement = XDocumentHelper.MakeElement(elementName, ref parent);
		XDocumentHelper.SetAttribute(parentElement, "x", GridX.ToString());
		XDocumentHelper.SetAttribute(parentElement, "y", GridY.ToString());
		XDocumentHelper.SetAttribute(parentElement, "z", GridZ.ToString());
		XDocumentHelper.SetAttribute(parentElement, "RoomId", RoomId.ToString());
		foreach (ActionData action in Actions)
		{
			action.Add(ref parentElement, action.XElementName);
		}
	}
}
