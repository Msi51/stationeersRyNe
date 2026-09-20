using System;
using System.Text;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects;

[Serializable]
public class Connection
{
	public NetworkType ConnectionType = NetworkType.Pipe;

	public Transform Transform;

	public Collider Collider;

	public ConnectionRole ConnectionRole;

	[ReadOnly]
	public Renderer HelperRenderer;

	public SmallGrid Parent;

	public Grid3 LocalGrid;

	public Grid3 FacingGrid;

	private bool _isInitialized;

	private bool _isValid;

	public Vector3 TransformUp { get; set; }

	public bool IsValid
	{
		get
		{
			if (ThreadedManager.IsThread)
			{
				return _isValid;
			}
			if (Transform != null)
			{
				return Parent != null;
			}
			return false;
		}
		private set
		{
			_isValid = value;
		}
	}

	public static implicit operator ConnectionRef(Connection connection)
	{
		return new ConnectionRef(connection);
	}

	public Connection(SmallGrid parent)
	{
		Parent = parent;
	}

	public void Validate()
	{
		IsValid = Transform != null && Parent != null;
	}

	private T GetSmallGridOccupant<T>(bool connected = true) where T : SmallGrid
	{
		Initialize();
		if (!_isInitialized || !Transform || !Parent || Parent.GridController == null)
		{
			return null;
		}
		T val = SmallCell.Get<T>(LocalGrid);
		if ((object)val == null)
		{
			return null;
		}
		if (connected)
		{
			if (!val.IsConnected(this))
			{
				return null;
			}
			return val;
		}
		return val;
	}

	public Device GetDevice(bool connected = true)
	{
		return GetSmallGridOccupant<Device>(connected);
	}

	public Chute GetChute(bool connected = true)
	{
		return GetSmallGridOccupant<Chute>(connected);
	}

	public Pipe GetPipe(bool connected = true)
	{
		return GetSmallGridOccupant<Pipe>(connected);
	}

	public INetworkedPipe GetINetworkedPipe(bool connected = true)
	{
		INetworkedPipe smallGridOccupant = GetSmallGridOccupant<Pipe>(connected);
		return smallGridOccupant ?? (GetSmallGridOccupant<Device>(connected) as INetworkedPipe);
	}

	public Cable GetCable(bool connected = true)
	{
		return GetSmallGridOccupant<Cable>(connected);
	}

	public SmallGrid GetOther(bool connected = true)
	{
		return GetSmallGridOccupant<SmallGrid>(connected);
	}

	public SmallGrid GetChuteOrDevice(bool connected = true)
	{
		Chute chute = GetChute(connected);
		if ((bool)chute)
		{
			return chute;
		}
		return GetDevice(connected);
	}

	public void CacheTransformUp()
	{
		if (IsValid)
		{
			TransformUp = Transform.up;
		}
	}

	public bool Initialize()
	{
		if (_isInitialized && !Parent.IsCursor)
		{
			return true;
		}
		if (ThreadedManager.IsThread)
		{
			return _isInitialized;
		}
		Validate();
		SetGrids();
		CacheTransformUp();
		return IsValid;
	}

	public void SetGrids()
	{
		if (IsValid)
		{
			if (!Parent.IsCursor)
			{
				_isInitialized = true;
			}
			Vector3 position = Transform.position;
			LocalGrid = Parent.GridController.WorldToLocalGrid(position, SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
			FacingGrid = Parent.GridController.WorldToLocalGrid(position + Transform.forward * SmallGrid.SmallGridSize, SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
		}
	}

	public string ToStationpediaName()
	{
		if (ConnectionRole != ConnectionRole.None)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(ConnectionType.GetName()).Append(" ");
			stringBuilder.Append(ConnectionRole.GetName());
			return stringBuilder.ToString();
		}
		if (ConnectionRole == ConnectionRole.None)
		{
			return GameStrings.ConnectionGeneric.DisplayString;
		}
		return EnumCollections.NetworkType.GetName(ConnectionType);
	}

	private string GetTitle()
	{
		if (ConnectionRole == ConnectionRole.None)
		{
			return GameStrings.ConnectionGeneric.DisplayString;
		}
		return ConnectionRole.GetName();
	}

	private void CheckConnectedTo(StringBuilder sb, NetworkType typeToCheck)
	{
		if ((ConnectionType & typeToCheck) != NetworkType.None)
		{
			sb.AppendLine(GameStrings.CanConnectTo.AsString(typeToCheck.GetName().AsColor("yellow")));
		}
	}

	private string GetExtendedText()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(GameStrings.ConnectionForThing.AsString(Parent.ToTooltip()));
		CheckConnectedTo(stringBuilder, NetworkType.Pipe);
		CheckConnectedTo(stringBuilder, NetworkType.PipeLiquid);
		CheckConnectedTo(stringBuilder, NetworkType.Power);
		CheckConnectedTo(stringBuilder, NetworkType.Data);
		CheckConnectedTo(stringBuilder, NetworkType.Chute);
		CheckConnectedTo(stringBuilder, NetworkType.LandingPad);
		CheckConnectedTo(stringBuilder, NetworkType.Elevator);
		ISmallGrid smallGrid = SmallCell.Get<ISmallGrid>(LocalGrid, this);
		if (smallGrid != null)
		{
			stringBuilder.AppendLine(GameStrings.ConnectionToThing.AsString(smallGrid.ToTooltip()));
		}
		return stringBuilder.ToString();
	}

	public Grid3 GetFacingGrid()
	{
		if (_isInitialized)
		{
			return FacingGrid;
		}
		return (Transform.position + Transform.forward * SmallGrid.SmallGridSize).ToGrid(SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
	}

	public Grid3 GetLocalGrid()
	{
		if (_isInitialized)
		{
			return LocalGrid;
		}
		return Transform.position.ToGrid(SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
	}

	public PassiveTooltip Populate(PassiveTooltip passiveTooltip)
	{
		passiveTooltip.Title = GetTitle();
		passiveTooltip.Extended = GetExtendedText();
		return passiveTooltip;
	}
}
