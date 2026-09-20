using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Networks;
using Objects.Structures;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class Wall : LargeStructure, ISmartRotatable
{
	public enum WallMaterial
	{
		Default,
		Metal,
		Composite,
		Glass
	}

	[Header("Walls")]
	public WallBlockMode AdditionalBlocking;

	public WallMaterial WallMaterialType;

	private MeshFilter _wallMesh;

	private Mesh _originalMesh;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	private int _frameLastChecked;

	private static readonly int MetalWallFailHash = Animator.StringToHash("MetalWallFail");

	private static readonly int CompositeWallFailHash = Animator.StringToHash("CompositeWallFail");

	private static readonly int GlassWallFailHash = Animator.StringToHash("GlassWallFail");

	public override bool Stressed
	{
		get
		{
			return _stressed;
		}
		set
		{
			if (value != _stressed)
			{
				if (value)
				{
					AtmosphericAudioHandler.Instance.AddStressedWall(this);
				}
				else
				{
					AtmosphericAudioHandler.Instance.RemoveStressedWall(this);
				}
				_stressed = value;
			}
		}
	}

	public override Vector3 CenterPosition => base.ThingTransformPosition + ThingTransform.rotation * Bounds.center;

	public override Material SelectColorSwatchMaterial(bool emissive)
	{
		if (!emissive)
		{
			return CustomColor.Normal;
		}
		return CustomColor.Emissive;
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.WallFloorCategory);
	}

	public override string GetStationpediaCategoryKey()
	{
		return StationpediaCategoryStrings.WallFloorCategory;
	}

	public override void Awake()
	{
		base.Awake();
		AddRocketRenderer();
		if (CustomColor != null)
		{
			SetCustomColor(CustomColor.Index);
		}
	}

	public bool GetBlockFlag(WallBlockMode setting)
	{
		return (AdditionalBlocking & setting) != 0;
	}

	private Structure IsAnyCollidingStructurual(Cell cell)
	{
		if (cell == null)
		{
			return null;
		}
		Grid3 grid = ThingTransform.position.ToGridPosition();
		Grid3 grid2 = base.GridController.WorldToLocalGrid(ThingTransform.position, GridSize, GridOffset) - grid;
		StructuralArray.Enumerator enumerator = cell.Lookup.GetEnumerator();
		while (enumerator.MoveNext())
		{
			Structure current = enumerator.Current;
			if (!(cell.Grid - current.WorldGrid != grid2) && current.IsDoor)
			{
				return current;
			}
		}
		return null;
	}

	public override CanConstructInfo CanConstruct()
	{
		Structure structure = IsSideBlocked();
		if ((object)structure != null)
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedByStructure.AsString(structure.DisplayName));
		}
		CanConstructInfo result = IsBlockClear();
		if (!result.CanConstruct)
		{
			return result;
		}
		return base.CanConstruct();
	}

	private Structure IsSideBlocked()
	{
		Cell cell = base.GridController.GetCell(base.ThingTransformPosition + ThingTransform.forward * 1f);
		if (cell != null)
		{
			foreach (Structure allStructure in cell.AllStructures)
			{
				if (allStructure is INetworkedLandingPad || allStructure is Frame)
				{
					return allStructure;
				}
			}
		}
		Structure structure = IsAnyCollidingStructurual(cell);
		if ((object)structure != null)
		{
			return structure;
		}
		Cell cell2 = base.GridController.GetCell(base.ThingTransformPosition + -ThingTransform.forward * 1f);
		Structure structure2 = IsAnyCollidingStructurual(cell2);
		if ((object)structure2 != null)
		{
			return structure2;
		}
		return null;
	}

	public override object GetLocalGridBounds()
	{
		return new Grid3[1] { Grid3.zero };
	}

	private CanConstructInfo IsBlockClear()
	{
		Cell cell = base.GridController.GetCell(GetGrid());
		if (cell == null)
		{
			return CanConstructInfo.ValidPlacement;
		}
		if (GetBlockFlag(WallBlockMode.Top))
		{
			CanConstructInfo result = IsBlockPosition(cell, Quaternion.AngleAxis(90f, ThingTransform.right));
			if (!result.CanConstruct)
			{
				return result;
			}
		}
		if (GetBlockFlag(WallBlockMode.Bottom))
		{
			CanConstructInfo result2 = IsBlockPosition(cell, Quaternion.AngleAxis(-90f, ThingTransform.right));
			if (!result2.CanConstruct)
			{
				return result2;
			}
		}
		if (GetBlockFlag(WallBlockMode.Left))
		{
			CanConstructInfo result3 = IsBlockPosition(cell, Quaternion.AngleAxis(90f, ThingTransform.up));
			if (!result3.CanConstruct)
			{
				return result3;
			}
		}
		if (GetBlockFlag(WallBlockMode.Right))
		{
			CanConstructInfo result4 = IsBlockPosition(cell, Quaternion.AngleAxis(-90f, ThingTransform.up));
			if (!result4.CanConstruct)
			{
				return result4;
			}
		}
		if (GetBlockFlag(WallBlockMode.Front))
		{
			CanConstructInfo result5 = IsBlockPosition(cell, Quaternion.AngleAxis(180f, ThingTransform.right));
			if (!result5.CanConstruct)
			{
				return result5;
			}
		}
		if (GetBlockFlag(WallBlockMode.Back))
		{
			CanConstructInfo result6 = IsBlockPosition(cell, Quaternion.AngleAxis(-180f, ThingTransform.right));
			if (!result6.CanConstruct)
			{
				return result6;
			}
		}
		return CanConstructInfo.ValidPlacement;
	}

	private CanConstructInfo IsBlockPosition(Cell cell, Quaternion offsetRotation)
	{
		return CanConstructCell(cell, GetBlockPosition(offsetRotation));
	}

	private Vector3 GetBlockPosition(Quaternion offsetRotation)
	{
		return GetGrid() + ThingTransform.rotation * Bounds.center + offsetRotation * -ThingTransform.forward;
	}

	public override Grid3 GetLocalGrid()
	{
		return base.GridController.WorldToLocalGrid(CenterPosition, GridSize, GridOffset);
	}

	public static void PlayWallFailSound(Wall wall)
	{
		switch (wall.WallMaterialType)
		{
		case WallMaterial.Default:
			OnServer.PlayWorldAudioClipsData(MetalWallFailHash, wall.Position).Forget();
			break;
		case WallMaterial.Metal:
			OnServer.PlayWorldAudioClipsData(MetalWallFailHash, wall.Position).Forget();
			break;
		case WallMaterial.Composite:
			OnServer.PlayWorldAudioClipsData(CompositeWallFailHash, wall.Position).Forget();
			break;
		case WallMaterial.Glass:
			OnServer.PlayWorldAudioClipsData(GlassWallFailHash, wall.Position).Forget();
			break;
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

	public List<Connection> GetOpenEnds()
	{
		return null;
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

	public List<ThingRenderer> GetThingRenderers()
	{
		return Renderers;
	}
}
