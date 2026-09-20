using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class DevicePipeMounted : Device, ISmartRotatable, IPipeMounted, IMounted, IRocketInternals, IRocketComponent
{
	[Header("DevicePipeMounted")]
	public Pipe.ContentType contentType = Pipe.ContentType.Gas;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	public AtmosphereHelper.MatterState MatterState => contentType switch
	{
		Pipe.ContentType.Unknown => AtmosphereHelper.MatterState.All, 
		Pipe.ContentType.Gas => AtmosphereHelper.MatterState.Gas, 
		Pipe.ContentType.Liquid => AtmosphereHelper.MatterState.Liquid, 
		Pipe.ContentType.All => AtmosphereHelper.MatterState.All, 
		_ => AtmosphereHelper.MatterState.All, 
	};

	public Atmosphere NetworkAtmosphere
	{
		get
		{
			if (!HasReadableAtmosphere)
			{
				return null;
			}
			return base.SmallCell.Pipe.PipeNetwork.Atmosphere;
		}
	}

	public override bool HasReadableAtmosphere
	{
		get
		{
			if (base.SmallCell != null && base.SmallCell.Pipe != null)
			{
				return base.SmallCell.Pipe.PipeNetwork?.Atmosphere != null;
			}
			return false;
		}
	}

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(20f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(6f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.AtmosDevices);
	}

	public bool IsValidPipe()
	{
		if ((bool)base.SmallCell?.Pipe && base.SmallCell.Pipe.IsBurst == PipeBurst.None && IsSameOrientation(base.SmallCell.Pipe))
		{
			return IsContentMatch(base.SmallCell.Pipe.PipeContentType);
		}
		return false;
	}

	public override CanConstructInfo CanConstruct()
	{
		SmallCell smallCell = base.GridController.GetSmallCell(base.ThingTransformPosition);
		if (smallCell == null || !smallCell.Pipe || !smallCell.Pipe.IsStraight)
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.MustMountToPipe.DisplayString);
		}
		if (smallCell.Pipe.IsBurst != PipeBurst.None)
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.CannotPlaceOnBurstPipe.DisplayString);
		}
		if (!IsSameOrientation(smallCell.Pipe))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedBySmallGrid.AsString(smallCell.Pipe.DisplayName));
		}
		if (!IsContentMatch(smallCell.Pipe.PipeContentType))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PipeContentTypeIncorrect.AsString(smallCell.Pipe.DisplayName));
		}
		if (smallCell.Device != null)
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedBySmallGrid.AsString(smallCell.Device.DisplayName));
		}
		if (smallCell.Owner != null && smallCell.Owner.IsCollision(this, smallCell.SmallGrid))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementConnectingNeedsFuselage.DisplayString);
		}
		return CanConstructInfo.ValidPlacement;
	}

	public override bool IsSameOrientation(SmallGrid smallGrid)
	{
		if ((bool)smallGrid)
		{
			return RocketMath.CompareVectors(RocketMath.Abs(smallGrid.ThingTransform.forward), RocketMath.Abs(ThingTransform.forward), 0.1f);
		}
		return false;
	}

	public virtual bool IsContentMatch(Pipe.ContentType inContentType)
	{
		if (inContentType != contentType)
		{
			return contentType == Pipe.ContentType.All;
		}
		return true;
	}

	public virtual void CheckForPipe()
	{
	}

	public override void OnGridUpdated(SmallGrid updatingOccupant)
	{
		base.OnGridUpdated(updatingOccupant);
		CheckForPipe();
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		CheckForPipe();
	}

	public override void OnGridPlaced(SmallGrid newOccupant)
	{
		base.OnGridPlaced(newOccupant);
		CheckForPipe();
	}

	public override void OnGridRemoved(SmallGrid newOccupant)
	{
		base.OnGridRemoved(newOccupant);
		CheckForPipe();
	}

	public void Mount(Grid3 grid)
	{
		Pipe pipe = base.GridController.GetPipe(grid);
		if ((object)pipe != null && !RocketMath.Approximately(pipe.ThingTransform.forward, ThingTransform.forward, 1f) && !RocketMath.Approximately(-pipe.ThingTransform.forward, ThingTransform.forward, 1f))
		{
			if (Vector3.Distance(pipe.ThingTransform.forward, ThingTransform.forward) < Vector3.Distance(-pipe.ThingTransform.forward, ThingTransform.forward))
			{
				ThingTransform.rotation = Quaternion.FromToRotation(Vector3.forward, pipe.ThingTransform.forward);
			}
			else
			{
				ThingTransform.rotation = Quaternion.FromToRotation(Vector3.forward, -pipe.ThingTransform.forward);
			}
		}
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}
}
