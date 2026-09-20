using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class Cladding : Structure, ISmartRotatable
{
	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public override Vector3 CenterPosition => base.ThingTransformPosition + ThingTransform.rotation * Bounds.center;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(60f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public override Grid3 GetLocalGrid()
	{
		return base.GridController.WorldToLocalGrid(CenterPosition, GridSize, GridOffset);
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.WallFloorCategory);
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

	public List<Connection> GetOpenEnds()
	{
		return new List<Connection>(0);
	}

	public int ConnectedCount()
	{
		return 0;
	}

	public int GetOpenEndsCount()
	{
		return 0;
	}

	public float GetGridSize()
	{
		return 2f;
	}
}
