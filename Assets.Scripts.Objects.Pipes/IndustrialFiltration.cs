using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class IndustrialFiltration : DeviceInputOutputCircuit, IExtendableStructure, IReferencable, IEvaluable
{
	[SerializeField]
	private List<StructureExtensionInfo> extensions;

	private float _powerUsedDuringTick;

	public static readonly float EnergyPerAtmosphere = 100f;

	public static readonly float MinimumRatioToFilterAll = 0.001f;

	private const int FILTER_COUNT = 6;

	public List<StructureExtensionInfo> CompatibleExtensions => extensions;

	public List<IStructureExtension> Extensions { get; } = new List<IStructureExtension>(2);

	public IndustrialFiltrationFanExtension FanExtension { get; private set; }

	public bool FullyExtended
	{
		get
		{
			if (base.IsStructureCompleted && FanExtension != null)
			{
				return FanExtension.IsStructureCompleted;
			}
			return false;
		}
	}

	public bool IsFullyConnected
	{
		get
		{
			if (OutputNetwork != null)
			{
				return OutputNetwork2 != null;
			}
			return false;
		}
	}

	protected override Slot ProgrammableChipSlot => null;

	protected override bool IsOperable
	{
		get
		{
			if (!base.IsStructureCompleted)
			{
				return false;
			}
			bool flag = base.ProgrammableChip != null && (CodeErrorState != 0 || base.ProgrammableChip.CompilationError);
			bool flag2 = IsFullyConnected && !flag && FanExtension != null && GetRoom() == null;
			if (Error == 1)
			{
				if (!flag2)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (flag2)
			{
				return true;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			return false;
		}
	}

	public bool CanExtend(IStructureExtension extension)
	{
		foreach (IStructureExtension extension2 in Extensions)
		{
			if (extension2.SourcePrefab.PrefabHash == extension.SourcePrefab.PrefabHash)
			{
				return false;
			}
		}
		foreach (StructureExtensionInfo compatibleExtension in CompatibleExtensions)
		{
			if (compatibleExtension.Prefab.PrefabHash == extension.SourcePrefab.PrefabHash && RocketMath.Approximately(extension.Position, compatibleExtension.ExtensionPosition.position) && RocketMath.Approximately(extension.ThingTransformRotation.eulerAngles, Rotation.eulerAngles, 0.1f))
			{
				return true;
			}
		}
		return false;
	}

	public void Extend(IStructureExtension extension)
	{
		if (CanExtend(extension))
		{
			Extensions.Add(extension);
			extension.ExtendableParent = this;
			OnExtend(extension);
		}
	}

	public void RemoveExtension(IStructureExtension extension)
	{
		OnRemove(extension);
		if (Extensions.Contains(extension))
		{
			Extensions.Remove(extension);
		}
		extension.ExtendableParent = null;
	}

	private void OnExtend(IStructureExtension extension)
	{
		if (extension is IndustrialFiltrationFanExtension fanExtension)
		{
			FanExtension = fanExtension;
			return;
		}
		throw new NotImplementedException("extension");
	}

	private void OnRemove(IStructureExtension extension)
	{
		if (extension is IndustrialFiltrationFanExtension)
		{
			FanExtension = null;
			return;
		}
		throw new NotImplementedException("extension");
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (!OnOff || cableNetwork != base.PowerCableNetwork || base.PowerCableNetwork == null)
		{
			return 0f;
		}
		if (!IsOperable)
		{
			return UsedPower;
		}
		return UsedPower + _powerUsedDuringTick;
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.ReceivePower(cableNetwork, powerAdded);
		_powerUsedDuringTick = 0f;
	}

	protected override void ToggleInteractableColliders()
	{
		foreach (Interactable interactable in Interactables)
		{
			InteractableType action = interactable.Action;
			if (action == InteractableType.Slot3 || (uint)(action - 31) <= 1u)
			{
				interactable.Collider.enabled = IsOpen;
			}
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = new PassiveTooltip(true);
		passiveTooltip.Title = DisplayName;
		if (hitCollider == OutputConnection.Collider)
		{
			passiveTooltip = passiveTooltip.Populate(OutputConnection);
			passiveTooltip.Title = InterfaceStrings.ConnectionFiltered;
			return passiveTooltip;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	protected override StringBuilder GetInfoPanelOperationText()
	{
		StringBuilder infoPanelOperationText = base.GetInfoPanelOperationText();
		PressurekPa pressurekPa = RocketMath.Max(val2: PressurekPa.Zero, val1: PressurekPa.Zero);
		PressurekPa pressurekPa2 = pressurekPa * 1000.0;
		infoPanelOperationText.AppendLine(GameStrings.MachinePressureDifferential.AsString(pressurekPa2.ToFloat().ToStringPrefix("Pa", "yellow")));
		return infoPanelOperationText;
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, new VolumeLitres(400.0), 0L);
		}
	}

	public override void OnAtmosphericTick()
	{
		if (!OnOff || !Powered || Mode == 0 || !IsOperable)
		{
			_powerUsedDuringTick = 0f;
			base.ProcessedMoles = MoleQuantity.Zero;
			return;
		}
		_powerUsedDuringTick = 4000f;
		MultiGridAtmospherics.VentWorldToInternal(base.InternalAtmosphere, FanExtension.VentGrids, base.PressurePerTick, 6);
		MoleQuantity zero = MoleQuantity.Zero;
		for (int i = 0; i < Slots.Count; i++)
		{
			Slot slot = Slots[i];
			if (slot.Type != Slot.Class.GasFilter)
			{
				continue;
			}
			GasMixture fromMix = GasMixtureHelper.Create(base.InternalAtmosphere.GasMixture);
			if (fromMix.IsValid)
			{
				fromMix.Scale(0.1666666716337204);
				if (slot.Contains<GasFilter>(out var occupant) && !occupant.IsEmpty)
				{
					zero += fromMix.GetTotalMolesGassesAndLiquids;
					occupant.FilterGas(ref fromMix, ref OutputNetwork.Atmosphere.GasMixture, base.InternalAtmosphere, MinimumRatioToFilterAll);
				}
				OutputNetwork2.Atmosphere.Add(fromMix);
			}
		}
		base.InternalAtmosphere.GasMixture.Reset();
		base.ProcessedMoles = zero;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new IndustrialFiltrationSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
	}

	public override CanConstructInfo CanConstruct()
	{
		if (RoomController.World.GetRoom(base.Position) != null)
		{
			return CanConstructInfo.InvalidPlacement("Must be placed outside");
		}
		return base.CanConstruct();
	}

	public override bool HasFrameBelow(float upOffset = 0f)
	{
		return base.HasFrameBelow((0f - GridSize) / 2f + upOffset);
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		foreach (StructureExtensionInfo compatibleExtension in CompatibleExtensions)
		{
			if (base.GridController.Get<Structure>(compatibleExtension.ExtensionPosition.position, StructureElement.Center) is IStructureExtension extension)
			{
				Extend(extension);
			}
		}
	}

	public override void OnDestroy()
	{
		if (Singleton<GameManager>.IsQuitting)
		{
			return;
		}
		if (GameManager.GameState != GameState.None)
		{
			for (int num = Extensions.Count - 1; num >= 0; num--)
			{
				IStructureExtension structureExtension = Extensions[num];
				if (structureExtension == null)
				{
					Extensions.RemoveAt(num);
				}
				else
				{
					RemoveExtension(structureExtension);
				}
			}
		}
		base.OnDestroy();
	}
}
