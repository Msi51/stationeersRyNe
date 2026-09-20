using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Networks;
using Objects.RoboticArm;
using UnityEngine;

namespace Assets.Scripts.GridSystem;

public class SmallCell
{
	public Grid3 SmallGrid;

	public Chute Chute;

	public Pipe Pipe;

	public Device Device;

	public Cable Cable;

	public SmallGrid Other;

	public ISmallGridOwner Owner;

	public IRoboticArmRail Rail;

	public static T Get<T>(Vector3 worldPosition) where T : IReferencable
	{
		return Get<T>(GridController.World.WorldToLocalGrid(worldPosition, Assets.Scripts.Objects.SmallGrid.SmallGridSize, Assets.Scripts.Objects.SmallGrid.SmallGridOffset));
	}

	public static T Get<T>(Grid3 localPosition) where T : IReferencable
	{
		SmallCell smallCell = GridController.World.GetSmallCell(localPosition);
		Chute chute = smallCell?.Chute;
		if (chute is T)
		{
			return (T)(object)((chute is T) ? chute : null);
		}
		Pipe pipe = smallCell?.Pipe;
		if (pipe is T)
		{
			return (T)(object)((pipe is T) ? pipe : null);
		}
		Device device = smallCell?.Device;
		if (device is T)
		{
			return (T)(object)((device is T) ? device : null);
		}
		Cable cable = smallCell?.Cable;
		if (cable is T)
		{
			return (T)(object)((cable is T) ? cable : null);
		}
		IRoboticArmRail roboticArmRail = smallCell?.Rail;
		if (roboticArmRail is T)
		{
			return (T)roboticArmRail;
		}
		SmallGrid smallGrid = smallCell?.Other;
		if (smallGrid is T)
		{
			return (T)(object)((smallGrid is T) ? smallGrid : null);
		}
		return default(T);
	}

	public static T Get<T>(Grid3 localPosition, Connection connection) where T : ISmallGrid
	{
		SmallCell smallCell = GridController.World.GetSmallCell(localPosition);
		if (smallCell?.Chute is T result && result.IsConnected(connection))
		{
			return result;
		}
		if (smallCell?.Pipe is T result2 && result2.IsConnected(connection))
		{
			return result2;
		}
		if (smallCell?.Device is T result3 && result3.IsConnected(connection))
		{
			return result3;
		}
		if (smallCell?.Cable is T result4 && result4.IsConnected(connection))
		{
			return result4;
		}
		if (smallCell?.Rail is T result5 && result5.IsConnected(connection))
		{
			return result5;
		}
		if (smallCell?.Other is T result6 && result6.IsConnected(connection))
		{
			return result6;
		}
		return default(T);
	}

	public static T Get<T>(Vector3 worldPosition, Connection connection) where T : ISmallGrid
	{
		return Get<T>(GridController.World.WorldToLocalGrid(worldPosition, Assets.Scripts.Objects.SmallGrid.SmallGridSize, Assets.Scripts.Objects.SmallGrid.SmallGridOffset), connection);
	}

	public SmallCell(Grid3 key)
	{
		SmallGrid = key;
	}

	public SmallCell(Grid3 key, GridController gridController)
	{
		SmallGrid = key;
	}

	public bool IsValid()
	{
		if (!(Device != null) && !(Cable != null) && !(Pipe != null) && !(Chute != null) && !(Other != null) && Owner == null)
		{
			return Rail != null;
		}
		return true;
	}

	public void Add(SmallGrid smallGridObjectGrid)
	{
		Cable cable = smallGridObjectGrid as Cable;
		if ((bool)cable)
		{
			Cable = cable;
			return;
		}
		Chute chute = smallGridObjectGrid as Chute;
		if ((bool)chute)
		{
			Chute = chute;
			return;
		}
		Pipe pipe = smallGridObjectGrid as Pipe;
		if ((bool)pipe)
		{
			Pipe = pipe;
			return;
		}
		Device device = smallGridObjectGrid as Device;
		if ((bool)device && device.SmallCollisionType != SmallGridBlock.Covers)
		{
			Device = device;
		}
		else if (smallGridObjectGrid is IRoboticArmRail rail)
		{
			Rail = rail;
		}
		else
		{
			Other = smallGridObjectGrid;
		}
	}

	public void RemoveCellObjectReferences(SmallGrid smallGrid)
	{
		if (smallGrid == Device)
		{
			Device.SmallCell = null;
			Device = null;
		}
		if (smallGrid == Chute)
		{
			Chute.SmallCell = null;
			Chute = null;
		}
		if (smallGrid == Pipe)
		{
			Pipe.SmallCell = null;
			Pipe = null;
		}
		if (smallGrid == Cable)
		{
			Cable.SmallCell = null;
			Cable = null;
		}
		if (smallGrid == (SmallGrid)Rail)
		{
			Rail.SmallCell = null;
			Rail = null;
		}
		if (smallGrid == Other)
		{
			Other.SmallCell = null;
			Other = null;
		}
	}
}
