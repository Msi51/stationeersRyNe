using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using Networks;
using Objects.RoboticArm;
using Objects.Rockets;
using TerrainSystem;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class SmallGrid : Structure, ISmallGrid, ITooltip, IReferencable, IEvaluable
{
	private const float RENDER_DISTANCE = 20f;

	private const float SHADOW_DISTANCE = 10f;

	[Header("Small Grids")]
	public List<Connection> OpenEnds = new List<Connection>();

	[Tooltip("Percentage of bounds to use when calculating pipe grid network size")]
	[Range(0.1f, 1f)]
	public static float SmallGridSize = 0.5f;

	public static float SmallGridOffset = 0.25f;

	public SmallGridBlock SmallCollisionType;

	public bool DualRegister;

	[SerializeField]
	[Tooltip("When true, can only be placed if there's a supporting frame behind")]
	private bool requiresFrame;

	[ReadOnly]
	public bool HasDataConnection;

	private static readonly List<SmallGrid> FoundSmallGrid = new List<SmallGrid>();

	private static readonly List<INetworkedStructure> FoundRoboticArms = new List<INetworkedStructure>();

	public List<Connection> AccessOpenEnds => OpenEnds;

	public SmallCell SmallCell { get; set; }

	public bool RequiresFrame
	{
		get
		{
			return requiresFrame;
		}
		protected set
		{
			requiresFrame = value;
		}
	}

	public override Vector3 CenterPosition => base.Position + Forward * 0.2f;

	public override bool IsBurnable
	{
		get
		{
			if (base.AutoignitionTemperature > TemperatureKelvin.Zero || base.FlashPointTemperature > TemperatureKelvin.Zero)
			{
				return !base.Indestructable;
			}
			return false;
		}
	}

	public bool HasOpenGrid => base.GridController.CanContainAtmos(base.WorldGrid);

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(20f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(10f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public virtual bool IsSameOrientation(SmallGrid smallGrid)
	{
		if (!smallGrid || !RocketMath.CompareVectors(smallGrid.ThingTransform.forward, ThingTransform.forward))
		{
			return RocketMath.CompareVectors(smallGrid.ThingTransform.forward, -ThingTransform.forward);
		}
		return true;
	}

	public override void Awake()
	{
		base.Awake();
		foreach (Connection openEnd in OpenEnds)
		{
			openEnd.Validate();
			if (openEnd.ConnectionType == NetworkType.Data || openEnd.ConnectionType == NetworkType.PowerAndData)
			{
				HasDataConnection = true;
			}
		}
	}

	protected bool GetConnection(NetworkType type, ConnectionRole role, out Connection connection)
	{
		foreach (Connection openEnd in OpenEnds)
		{
			if (openEnd.ConnectionType == type && openEnd.ConnectionRole == role)
			{
				connection = openEnd;
				return true;
			}
		}
		connection = null;
		return false;
	}

	public override void RebuildGridState()
	{
		base.RebuildGridState();
		foreach (Connection openEnd in OpenEnds)
		{
			openEnd?.SetGrids();
		}
	}

	public virtual bool HasFrameBelow(float upOffset = 0f)
	{
		if (this is IRocketInternals { StrictlyInternal: not false })
		{
			return true;
		}
		Vector3 vector = base.ThingTransformPosition + upOffset * ThingTransform.up;
		if (Cell.IsInCrewModule(new WorldGrid(vector), out var crewModule) && crewModule.AllowMounting)
		{
			return true;
		}
		Vector3 registeredPosition = vector;
		registeredPosition += 0.99f * ThingTransform.forward;
		registeredPosition += 0.01f * ThingTransform.up;
		if (base.GridController.IsBlockedGrid(registeredPosition))
		{
			return false;
		}
		WorldGrid worldGrid = new WorldGrid(vector - ThingTransform.up * GridSize / 2f);
		Structure structure = base.GridController.Get<Structure>(worldGrid);
		if (!structure || ((bool)structure && !structure.AllowMounting))
		{
			return false;
		}
		return true;
	}

	public virtual bool HasVoxelBelow()
	{
		return VoxelTerrain.GetDensityWorldSpace(base.Position - Vector3.up * 1f) > 0.49803922f;
	}

	public override void OnFireTick()
	{
		if (!IsBurnable)
		{
			return;
		}
		Atmosphere atmos = base.AtmosphericsController?.SampleGlobalAtmosphere(new WorldGrid(base.Position));
		if (!base.IsBurning && ShouldIgnite(atmos))
		{
			base.IsBurning = true;
			OnFireStart();
		}
		if (!base.IsBurning)
		{
			return;
		}
		float num = ThingHealth * AtmosphericsManager.Instance.TickSpeedSeconds / BurnTime;
		float heatEnergyReleased = EnergyReleasedWhenBurned * num / 100f;
		DamageState.Damage(ChangeDamageType.Increment, num, DamageUpdateType.Burn);
		Atmosphere atmosphere = AtmosphericsManager.CloneGlobalAtmosphereThreadSafe(base.WorldGrid);
		if (OnFireConsume(atmosphere, heatEnergyReleased))
		{
			lock (atmosphere)
			{
				atmosphere.Sparked = true;
				return;
			}
		}
		Extinguish();
	}

	public override void WillJoinNetwork(Span<ConnectionRef> connBuf, ref int connCount)
	{
		Span<SmallCellRef> span = stackalloc SmallCellRef[32];
		int count = 0;
		FillConnected(span, ref count);
		if (count == 0)
		{
			return;
		}
		Span<SmallCellRef> span2 = span;
		span2 = span2.Slice(0, count);
		for (int i = 0; i < span2.Length; i++)
		{
			SmallCellRef smallCellRef = span2[i];
			foreach (Connection accessOpenEnd in smallCellRef.Get().AccessOpenEnds)
			{
				Connection connection = FindConnectingEnd(accessOpenEnd);
				if (connection != null)
				{
					connBuf[connCount++] = connection;
				}
			}
		}
	}

	public virtual void OnGridPlaced(SmallGrid newOccupant)
	{
	}

	public virtual void OnGridUpdated(SmallGrid updatingOccupant)
	{
	}

	public virtual void OnGridRemoved(SmallGrid oldOccupant)
	{
	}

	public virtual void OnGridAdjacentPlaced(SmallGrid neighbor)
	{
	}

	public virtual void OnGridAdjacentRemoved(SmallGrid neighbor)
	{
	}

	public virtual void OnNeighborPlaced(SmallGrid neighbor)
	{
	}

	public virtual void OnNeighborRemoved(SmallGrid neighbor)
	{
	}

	public void RegisterGridUpdate()
	{
		if (GameManager.GameState == GameState.Running)
		{
			if (SmallCell.Pipe != null)
			{
				SmallCell.Pipe.OnGridUpdated(this);
			}
			if (SmallCell.Cable != null)
			{
				SmallCell.Cable.OnGridUpdated(this);
			}
			if (SmallCell.Device != null)
			{
				SmallCell.Device.OnGridUpdated(this);
			}
			if (SmallCell.Chute != null)
			{
				SmallCell.Chute.OnGridUpdated(this);
			}
			if (SmallCell.Other != null)
			{
				SmallCell.Other.OnGridUpdated(this);
			}
			if (SmallCell.Rail != null)
			{
				((SmallGrid)SmallCell.Rail).OnGridUpdated(this);
			}
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		foreach (Connection openEnd in OpenEnds)
		{
			openEnd.Initialize();
		}
		if (GameManager.GameState == GameState.Loading || GameManager.GameState == GameState.None)
		{
			return;
		}
		if (base.WorldGrid == WorldGrid.INVALID)
		{
			throw new NullReferenceException("grid for " + DisplayName + " is invalid when registering");
		}
		Span<SmallCellRef> span = stackalloc SmallCellRef[32];
		int count = 0;
		FillConnected(span, ref count);
		Span<SmallCellRef> span2 = span;
		Span<SmallCellRef> span3 = span2.Slice(0, count);
		for (int i = 0; i < span3.Length; i++)
		{
			SmallCellRef smallCellRef = span3[i];
			smallCellRef.Get().OnNeighborPlaced(this);
		}
		Span<Grid3> span4 = stackalloc Grid3[6];
		int count2 = 0;
		base.GridController.PopulateSmallGridNeighbours(base.Position, span4, ref count2);
		Span<Grid3> span5 = span4;
		Span<Grid3> span6 = span5.Slice(0, count2);
		for (int i = 0; i < span6.Length; i++)
		{
			Grid3 localGrid = span6[i];
			SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
			if (smallCell != null)
			{
				if (smallCell.Pipe != null)
				{
					smallCell.Pipe.OnGridAdjacentPlaced(this);
				}
				if (smallCell.Cable != null)
				{
					smallCell.Cable.OnGridAdjacentPlaced(this);
				}
				if (smallCell.Device != null)
				{
					smallCell.Device.OnGridAdjacentPlaced(this);
				}
				if (smallCell.Chute != null)
				{
					smallCell.Chute.OnGridAdjacentPlaced(this);
				}
				if (smallCell.Other != null)
				{
					smallCell.Other.OnGridAdjacentPlaced(this);
				}
				if (smallCell.Rail != null)
				{
					((SmallGrid)smallCell.Rail).OnGridAdjacentPlaced(this);
				}
			}
		}
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		SmallCell smallCell = base.GridController.GetSmallCell(base.ThingTransformPosition);
		if (smallCell != null)
		{
			if (smallCell.Pipe != null)
			{
				smallCell.Pipe.OnGridRemoved(this);
			}
			if (smallCell.Cable != null)
			{
				smallCell.Cable.OnGridRemoved(this);
			}
			if (smallCell.Device != null)
			{
				smallCell.Device.OnGridRemoved(this);
			}
			if (smallCell.Chute != null)
			{
				smallCell.Chute.OnGridRemoved(this);
			}
			if (smallCell.Other != null)
			{
				smallCell.Other.OnGridRemoved(this);
			}
			if (smallCell.Rail != null)
			{
				((SmallGrid)smallCell.Rail).OnGridRemoved(this);
			}
		}
		Span<SmallCellRef> span = stackalloc SmallCellRef[32];
		int count = 0;
		FillConnected(span, ref count);
		Span<SmallCellRef> span2 = span;
		Span<SmallCellRef> span3 = span2.Slice(0, count);
		for (int i = 0; i < span3.Length; i++)
		{
			SmallCellRef smallCellRef = span3[i];
			smallCellRef.Get().OnNeighborRemoved(this);
		}
		if (!base.IsDoor)
		{
			Vector3 vector = base.ThingTransformPosition + base.transform.forward * 1f;
			WorldGrid worldGrid = new WorldGrid(vector - ThingTransform.forward * 2f);
			Structure structure = base.GridController.Get<Structure>(worldGrid);
			if ((bool)structure && structure.AttachedDevices.Contains(this))
			{
				structure.AttachedDevices.Remove(this);
			}
		}
		Span<Grid3> span4 = stackalloc Grid3[6];
		int count2 = 0;
		base.GridController.PopulateSmallGridNeighbours(base.Position, span4, ref count2);
		Span<Grid3> span5 = span4;
		Span<Grid3> span6 = span5.Slice(0, count2);
		for (int i = 0; i < span6.Length; i++)
		{
			Grid3 localGrid = span6[i];
			SmallCell smallCell2 = base.GridController.GetSmallCell(localGrid);
			if (smallCell2 != null)
			{
				if (smallCell2.Pipe != null)
				{
					smallCell2.Pipe.OnGridAdjacentRemoved(this);
				}
				if (smallCell2.Cable != null)
				{
					smallCell2.Cable.OnGridAdjacentRemoved(this);
				}
				if (smallCell2.Device != null)
				{
					smallCell2.Device.OnGridAdjacentRemoved(this);
				}
				if (smallCell2.Chute != null)
				{
					smallCell2.Chute.OnGridAdjacentRemoved(this);
				}
				if (smallCell2.Other != null)
				{
					smallCell2.Other.OnGridAdjacentRemoved(this);
				}
				if (smallCell2.Rail != null)
				{
					((SmallGrid)smallCell2.Rail).OnGridAdjacentRemoved(this);
				}
			}
		}
	}

	private Structure IsOnPlane(Structure structure, Vector3 thingPosition, Vector3 thingForward)
	{
		Vector3 vector = Vector3.Scale(structure.Position - thingPosition, thingForward).Abs();
		if (vector.x < 0.005f && vector.y < 0.005f && vector.z < 0.005f)
		{
			return structure;
		}
		return null;
	}

	public override CanMountResult CanMountOnWall()
	{
		CanMountResult result = new CanMountResult
		{
			result = WallMountResult.Unknown
		};
		Vector3 thingTransformPosition = base.ThingTransformPosition;
		Vector3 forward = ThingTransform.forward;
		Vector3 vector = thingTransformPosition + GridSize * forward / 2f;
		Vector3 rearPosition = thingTransformPosition - GridSize * forward / 2f;
		switch (RocketGrid.GridLocation(vector))
		{
		case Location.Face:
		case Location.Edge:
		{
			for (int i = -1; i <= 1; i += 2)
			{
				for (int j = -1; j <= 1; j += 2)
				{
					WorldGrid worldGrid = new WorldGrid(vector + 0.1f * (i * ThingTransform.right + j * ThingTransform.up));
					if (base.GridController.IsBlockedGrid(worldGrid))
					{
						result.result = WallMountResult.InvalidBlocked;
						result.offending = base.GridController.Get<Structure>(worldGrid);
						return result;
					}
				}
			}
			break;
		}
		default:
			if (base.GridController.IsBlockedGrid(vector))
			{
				result.result = WallMountResult.InvalidBlocked;
				return result;
			}
			break;
		}
		Vector3 thingBackward = forward * -1f;
		Vector3 rearGrid = thingTransformPosition - forward * GridSize / 2f;
		List<Structure> list = new List<Structure> { null, null, null, null };
		bool[] array = new bool[4];
		Structure structure = null;
		bool flag = false;
		int num = 0;
		switch (RocketGrid.GridLocation(thingTransformPosition))
		{
		case Location.Edge:
		case Location.Corner:
		{
			for (int k = -1; k <= 1; k += 2)
			{
				for (int l = -1; l <= 1; l += 2)
				{
					Vector3 offset = 0.1f * (k * ThingTransform.right + l * ThingTransform.up);
					bool isWall2;
					Structure support2 = GetSupport(out isWall2, rearGrid, vector, thingBackward, thingTransformPosition, forward, rearPosition, offset);
					if (!support2 || !list.Contains(support2))
					{
						list[num] = support2;
						if (isWall2)
						{
							flag = (array[num] = true);
						}
						if (!structure && (bool)support2)
						{
							structure = support2;
						}
						num++;
					}
				}
			}
			break;
		}
		case Location.Face:
		{
			bool isWall;
			Structure support = GetSupport(out isWall, rearGrid, vector, thingBackward, thingTransformPosition, forward, rearPosition);
			if (isWall)
			{
				flag = (array[0] = true);
			}
			structure = (list[0] = support);
			num = 1;
			break;
		}
		default:
			result.result = WallMountResult.InvalidMissingSupport;
			return result;
		}
		if (Cell.IsInCrewModule(new WorldGrid(vector), out var crewModule) && crewModule.AllowMounting)
		{
			structure = crewModule;
			flag = true;
		}
		if (!structure)
		{
			result.result = WallMountResult.InvalidMissingSupport;
			return result;
		}
		if (RequiresFrame && flag)
		{
			result.result = WallMountResult.InvalidRequiresFrame;
			return result;
		}
		result.support = structure;
		bool flag2 = false;
		for (int m = 0; m < num; m++)
		{
			Structure structure3 = list[m];
			if ((bool)structure3 && structure3.AllowMounting)
			{
				flag2 = true;
			}
		}
		if (!flag2)
		{
			result.result = WallMountResult.InvalidNotMountable;
			return result;
		}
		Grid3[] array2 = (Grid3[])GridBounds.GetLocalSmallGrid(base.ThingTransformPosition, base.ThingTransformRotation);
		foreach (Grid3 localGrid in array2)
		{
			SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
			if (smallCell != null)
			{
				if (smallCell.Cable != null && _IsCollision(smallCell.Cable))
				{
					result.result = WallMountResult.InvalidBlocked;
					result.offending = smallCell.Cable;
					return result;
				}
				if (smallCell.Device != null && _IsCollision(smallCell.Device))
				{
					result.result = WallMountResult.InvalidBlocked;
					result.offending = smallCell.Device;
					return result;
				}
				if (smallCell.Pipe != null && _IsCollision(smallCell.Pipe))
				{
					result.result = WallMountResult.InvalidBlocked;
					result.offending = smallCell.Pipe;
					return result;
				}
				if (smallCell.Chute != null && _IsCollision(smallCell.Chute))
				{
					result.result = WallMountResult.InvalidBlocked;
					result.offending = smallCell.Chute;
					return result;
				}
				if (smallCell.Rail != null && _IsCollision((SmallGrid)smallCell.Rail))
				{
					result.result = WallMountResult.InvalidBlocked;
					result.offending = (SmallGrid)smallCell.Rail;
					return result;
				}
				if (smallCell.Other != null && _IsCollision(smallCell.Other))
				{
					result.result = WallMountResult.InvalidBlocked;
					result.offending = smallCell.Other;
					return result;
				}
			}
		}
		result.result = WallMountResult.Valid;
		return result;
	}

	private Structure GetSupport(out bool isWall, Vector3 rearGrid, Vector3 forwardPosition, Vector3 thingBackward, Vector3 thingPosition, Vector3 thingForward, Vector3 rearPosition, Vector3 offset = default(Vector3))
	{
		isWall = false;
		WorldGrid worldGrid = new WorldGrid(rearGrid + offset);
		Structure structure = base.GridController.Get<Structure>(worldGrid);
		if (structure != null)
		{
			return structure;
		}
		structure = base.GridController.GetFaceStructure(forwardPosition + offset, thingBackward);
		if (structure != null && (bool)IsOnPlane(structure, thingPosition, thingForward))
		{
			isWall = true;
			return structure;
		}
		structure = base.GridController.GetFaceStructure(rearPosition + offset, thingForward);
		if (structure != null && (bool)IsOnPlane(structure, thingPosition, thingBackward))
		{
			isWall = true;
			return structure;
		}
		return null;
	}

	public virtual bool IsPipeEndCollision(SmallGrid smallGrid)
	{
		foreach (Connection openEnd in OpenEnds)
		{
			foreach (Connection openEnd2 in smallGrid.OpenEnds)
			{
				if ((openEnd.Transform.position - openEnd2.Transform.position).sqrMagnitude < 0.010000001f)
				{
					return true;
				}
			}
		}
		return false;
	}

	protected virtual bool _IsCollision(SmallGrid smallGrid)
	{
		if ((smallGrid.SmallCollisionType & SmallCollisionType) != SmallGridBlock.None)
		{
			return true;
		}
		return IsPipeEndCollision(smallGrid);
	}

	public override CanConstructInfo CanConstruct()
	{
		Grid3[] array = (Grid3[])GridBounds.GetLocalSmallGrid(base.ThingTransformPosition, base.ThingTransformRotation);
		for (int i = 0; i < array.Length; i++)
		{
			Grid3 localGrid = array[i];
			SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
			if (this is IRocketInternals { StrictlyInternal: not false } && !(smallCell?.Owner is RocketNetwork))
			{
				return CanConstructInfo.InvalidPlacement(GameStrings.CannotPlaceOutsideRocket);
			}
			if (smallCell?.Owner == null && this is IRocketInternals)
			{
				Grid3 localGrid2 = new Grid3(localGrid.x, (float)localGrid.y + SmallGridSize * 10f, localGrid.z);
				Grid3 localGrid3 = new Grid3(localGrid.x, (float)localGrid.y - SmallGridSize * 10f, localGrid.z);
				SmallCell smallCell2 = base.GridController.GetSmallCell(localGrid2);
				SmallCell smallCell3 = base.GridController.GetSmallCell(localGrid3);
				if (smallCell2?.Owner != null || smallCell3?.Owner != null)
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.PlacementConnectingNeedsFuselage.DisplayString);
				}
			}
			if (smallCell != null)
			{
				if ((bool)smallCell.Cable && _IsCollision(smallCell.Cable))
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedBySmallGrid.AsString(smallCell.Cable.DisplayName));
				}
				if ((bool)smallCell.Device && _IsCollision(smallCell.Device))
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedBySmallGrid.AsString(smallCell.Device.DisplayName));
				}
				if ((bool)smallCell.Pipe && _IsCollision(smallCell.Pipe))
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedBySmallGrid.AsString(smallCell.Pipe.DisplayName));
				}
				if ((bool)smallCell.Chute && _IsCollision(smallCell.Chute))
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedBySmallGrid.AsString(smallCell.Chute.DisplayName));
				}
				if (smallCell.Rail != null && _IsCollision((SmallGrid)smallCell.Rail))
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedBySmallGrid.AsString(smallCell.Rail.DisplayName));
				}
				if ((bool)smallCell.Other && _IsCollision(smallCell.Other))
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedBySmallGrid.AsString(smallCell.Other.DisplayName));
				}
				if (smallCell.Owner != null && smallCell.Owner.IsCollision(this, smallCell.SmallGrid))
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.PlacementConnectingNeedsFuselage.DisplayString);
				}
			}
		}
		if (!DualRegister)
		{
			return CanConstructInfo.ValidPlacement;
		}
		return base.CanConstruct();
	}

	public virtual bool IsConnected(Connection otherEnd)
	{
		Grid3 facingGrid = otherEnd.GetFacingGrid();
		foreach (Connection openEnd in OpenEnds)
		{
			if ((openEnd.ConnectionType & otherEnd.ConnectionType) != NetworkType.None)
			{
				Grid3 localGrid = openEnd.GetLocalGrid();
				if (facingGrid == localGrid)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsConnected(Connection otherEnd, out Connection connectedEnd)
	{
		Grid3 facingGrid = otherEnd.GetFacingGrid();
		foreach (Connection openEnd in OpenEnds)
		{
			if ((openEnd.ConnectionType & otherEnd.ConnectionType) != NetworkType.None)
			{
				Grid3 localGrid = openEnd.GetLocalGrid();
				if (facingGrid == localGrid)
				{
					connectedEnd = openEnd;
					return true;
				}
			}
		}
		connectedEnd = null;
		return false;
	}

	public virtual int ConnectedCount()
	{
		int num = 0;
		foreach (Connection openEnd in OpenEnds)
		{
			Grid3 localGrid = base.GridController.WorldToLocalGrid(openEnd.Transform.position, SmallGridSize, SmallGridOffset);
			SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
			if (smallCell != null)
			{
				if (smallCell.Pipe != null && smallCell.Pipe != this && smallCell.Pipe.IsConnected(openEnd))
				{
					num++;
				}
				if (smallCell.Device != null && smallCell.Device != this && smallCell.Device.IsConnected(openEnd))
				{
					num++;
				}
				if (smallCell.Cable != null && smallCell.Cable != this && smallCell.Cable.IsConnected(openEnd))
				{
					num++;
				}
				if (smallCell.Chute != null && smallCell.Chute != this && smallCell.Chute.IsConnected(openEnd))
				{
					num++;
				}
				if (smallCell.Rail != null && (SmallGrid)smallCell.Rail != this && ((SmallGrid)smallCell.Rail).IsConnected(openEnd))
				{
					num++;
				}
				if (smallCell.Other != null && smallCell.Other != this && smallCell.Other.IsConnected(openEnd))
				{
					num++;
				}
			}
		}
		return num;
	}

	public virtual void FillConnected<T>(Span<SmallCellRef> buf, ref int count) where T : ISmallGrid
	{
		foreach (Connection openEnd in OpenEnds)
		{
			Grid3 localGrid = base.GridController.WorldToLocalGrid(openEnd.Transform.position, SmallGridSize, SmallGridOffset);
			SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
			if (smallCell != null)
			{
				if (IsValidConnection(smallCell.Cable, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Cable);
				}
				if (IsValidConnection(smallCell.Chute, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Chute);
				}
				if (IsValidConnection(smallCell.Device, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Device);
				}
				if (IsValidConnection(smallCell.Pipe, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Pipe);
				}
				if (IsValidConnection(smallCell.Rail, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Rail);
				}
				if (IsValidConnection(smallCell.Other, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Other);
				}
			}
		}
		bool IsValidConnection(ISmallGrid sibling, Connection end)
		{
			if (!(sibling is T val))
			{
				return false;
			}
			if (val.ReferenceId != base.ReferenceId)
			{
				return val.IsConnected(end);
			}
			return false;
		}
	}

	public void FillConnected(Span<SmallCellRef> buf, ref int count)
	{
		foreach (Connection openEnd in OpenEnds)
		{
			Grid3 localGrid = base.GridController.WorldToLocalGrid(openEnd.Transform.position, SmallGridSize, SmallGridOffset);
			SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
			if (smallCell != null)
			{
				if (IsValidConnection(smallCell.Cable, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Cable);
				}
				if (IsValidConnection(smallCell.Chute, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Chute);
				}
				if (IsValidConnection(smallCell.Device, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Device);
				}
				if (IsValidConnection(smallCell.Pipe, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Pipe);
				}
				if (IsValidConnection(smallCell.Rail, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Rail);
				}
				if (IsValidConnection(smallCell.Other, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Other);
				}
			}
		}
		bool IsValidConnection(ISmallGrid sibling, Connection end)
		{
			if (sibling != null && sibling.ReferenceId != base.ReferenceId)
			{
				return sibling.IsConnected(end);
			}
			return false;
		}
	}

	public void FillConnected<T>(NetworkType networkType, Span<SmallCellRef> buf, ref int count) where T : SmallGrid
	{
		foreach (Connection openEnd in OpenEnds)
		{
			if ((openEnd.ConnectionType & networkType) == 0)
			{
				continue;
			}
			Grid3 localGrid = base.GridController.WorldToLocalGrid(openEnd.Transform.position, SmallGridSize, SmallGridOffset);
			SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
			if (smallCell != null)
			{
				if (IsValidConnection(smallCell.Cable, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Cable);
				}
				if (IsValidConnection(smallCell.Chute, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Chute);
				}
				if (IsValidConnection(smallCell.Device, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Device);
				}
				if (IsValidConnection(smallCell.Pipe, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Pipe);
				}
				if (IsValidConnection(smallCell.Rail, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Rail);
				}
				if (IsValidConnection(smallCell.Other, openEnd))
				{
					buf[count++] = new SmallCellRef(smallCell, SmallCellType.Other);
				}
			}
		}
		bool IsValidConnection(ISmallGrid sibling, Connection end)
		{
			if (sibling is T val && val != null && val != this)
			{
				return val.IsConnected(end);
			}
			return false;
		}
	}

	public List<INetworkedStructure> ConnectedRoboticArms()
	{
		FoundRoboticArms.Clear();
		foreach (Connection openEnd in OpenEnds)
		{
			Grid3 localGrid = base.GridController.WorldToLocalGrid(openEnd.Transform.position, SmallGridSize, SmallGridOffset);
			SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
			if (smallCell != null)
			{
				if (smallCell.Rail != null && smallCell.Rail != this as IRoboticArmRail && ((SmallGrid)smallCell.Rail).IsConnected(openEnd) && smallCell.Rail is INetworkedRoboticArm item)
				{
					FoundRoboticArms.Add(item);
				}
				else if (smallCell.Device != null && smallCell.Device != this && smallCell.Device.IsConnected(openEnd) && smallCell.Device is INetworkedRoboticArm item2)
				{
					FoundRoboticArms.Add(item2);
				}
			}
		}
		return FoundRoboticArms;
	}

	public Connection FindConnectingEnd(Connection destinationEnd)
	{
		Grid3 grid = base.GridController.WorldToLocalGrid(destinationEnd.Transform.position, SmallGridSize, SmallGridOffset);
		foreach (Connection openEnd in OpenEnds)
		{
			Grid3 grid2 = base.GridController.WorldToLocalGrid(openEnd.Transform.position + openEnd.Transform.forward * SmallGridSize, SmallGridSize, SmallGridOffset);
			if (grid == grid2 && RocketMath.CompareVectors(openEnd.Transform.forward, -destinationEnd.Transform.forward, 1f))
			{
				return openEnd;
			}
		}
		return null;
	}

	public List<Connection> GetOpenEnds()
	{
		List<Connection> list = new List<Connection>(OpenEnds.Count);
		for (int i = 0; i < OpenEnds.Count; i++)
		{
			list.Add(OpenEnds[i]);
		}
		return list;
	}

	public int GetOpenEndsCount()
	{
		return OpenEnds.Count;
	}

	public float GetGridSize()
	{
		return GridSize;
	}

	protected bool IsConnectingToUmbilical(out IUmbilical found)
	{
		foreach (Connection openEnd in OpenEnds)
		{
			Grid3 localGrid = base.GridController.WorldToLocalGrid(openEnd.Transform.position, SmallGridSize, SmallGridOffset);
			if (base.GridController.GetSmallCell(localGrid)?.Device is IUmbilical umbilical && Vector3.Dot(openEnd.Transform.forward, umbilical.AsThing.Transform.forward) > 0.5f)
			{
				found = umbilical;
				return true;
			}
		}
		found = null;
		return false;
	}
}
