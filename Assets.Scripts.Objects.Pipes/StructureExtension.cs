using Assets.Scripts.GridSystem;
using Assets.Scripts.Serialization;
using Trading;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Objects.Pipes;

public class StructureExtension : SmallGrid, IStructureExtension, IReferencable, IEvaluable
{
	public const float RENDER_DISTANCE = 100f;

	public const float SHADOW_DISTANCE = 60f;

	[SerializeField]
	private Direction parentDirection = Assets.Scripts.GridSystem.Direction.Down;

	private long _savedId;

	public virtual Direction ParentDirection => parentDirection;

	public IExtendableStructure ExtendableParent { get; set; }

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(60f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public override ShadowCastingMode GetShadowCastingMode()
	{
		return ShadowCastingMode.On;
	}

	public override void OnDestroy()
	{
		ExtendableParent?.RemoveExtension(this);
		base.OnDestroy();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new StructureExtensionSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is StructureExtensionSaveData structureExtensionSaveData)
		{
			structureExtensionSaveData.ExtendableParentId = ExtendableParent?.ReferenceId ?? 0;
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is StructureExtensionSaveData structureExtensionSaveData)
		{
			_savedId = structureExtensionSaveData.ExtendableParentId;
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (Thing.TryFind(_savedId, out var thing) && thing is IExtendableStructure extendableStructure)
		{
			extendableStructure.Extend(this);
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		Vector3 worldPosition = base.Position + Rotation * RocketGrid.Directions[(int)ParentDirection];
		if (base.GridController.Get<Structure>(worldPosition, StructureElement.Center) is IExtendableStructure extendableStructure)
		{
			extendableStructure.Extend(this);
		}
	}

	public override CanConstructInfo CanConstruct()
	{
		Vector3 worldPosition = base.Position + Rotation * RocketGrid.Directions[(int)ParentDirection];
		if (base.GridController.Get<Structure>(worldPosition, StructureElement.Center) is IExtendableStructure extendableStructure && extendableStructure.CanExtend(this))
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement("Must be placed on-top of a compatible structure");
	}
}
