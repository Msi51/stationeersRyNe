using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class WallVent : SmallSingleGrid, ISmartRotatable
{
	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.FlatExhaustive;

	public int[] OpenEndsPermutation = new int[4] { 0, 1, 2, 3 };

	private WorldGrid _facingGrid;

	private WorldGrid _rearGrid;

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		AtmosphericsManager.Instance.Register(this);
		Vector3 forward = ThingTransform.forward;
		Vector3 vector = ThingTransform.position;
		_facingGrid = new WorldGrid(vector + forward * 0.1f);
		_rearGrid = new WorldGrid(vector + forward * -0.1f);
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		AtmosphericsManager.Instance.Deregister(this);
	}

	public override void OnAtmosphericTick()
	{
		if (base.GridController.CanContainAtmos(_facingGrid) && base.GridController.CanContainAtmos(_rearGrid))
		{
			Atmosphere inputAtmos = base.AtmosphericsController.SampleGlobalAtmosphere(_facingGrid);
			Atmosphere outputAtmos = base.AtmosphericsController.SampleGlobalAtmosphere(_rearGrid);
			AtmosphereHelper.Mix(inputAtmos, outputAtmos, AtmosphereHelper.MatterState.Gas);
		}
	}

	private bool CanVentAtmos()
	{
		if (base.GridController.CanContainAtmos(_facingGrid))
		{
			return base.GridController.CanContainAtmos(_rearGrid);
		}
		return false;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (CanVentAtmos())
		{
			return passiveTooltip;
		}
		passiveTooltip.Title = DisplayName;
		passiveTooltip.Extended = GetExtendedText().ToString();
		return passiveTooltip;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (base.GridController.CanContainAtmos(_facingGrid) && base.GridController.CanContainAtmos(_rearGrid))
		{
			return extendedText;
		}
		extendedText.AppendLine(GameStrings.DeviceWorldGridBlocked.AsString(ToTooltip()));
		return extendedText;
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

	public new List<Connection> GetOpenEnds()
	{
		return null;
	}

	public override int ConnectedCount()
	{
		return 0;
	}

	public new int GetOpenEndsCount()
	{
		return 0;
	}

	public new float GetGridSize()
	{
		return 2f;
	}
}
