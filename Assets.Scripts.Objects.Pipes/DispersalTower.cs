using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class DispersalTower : DeviceInputOutputImportExportCircuit, IExtendableStructure, IReferencable, IEvaluable
{
	private static readonly VolumeLitres InternalVolume = new VolumeLitres(1000.0);

	private const double TARGET_PRESSURE = 10000.0;

	public static readonly PressurekPa TargetPressure = new PressurekPa(10000.0);

	[SerializeField]
	private List<StructureExtensionInfo> extensions;

	private DispersalTowerVentExtension VentExtension;

	protected override Slot ProgrammableChipSlot => Slots[1];

	public bool FullyExtended
	{
		get
		{
			if (VentExtension != null)
			{
				return VentExtension.IsStructureCompleted;
			}
			return false;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			bool flag = base.IsInputValid && FullyExtended && base.IsStructureCompleted;
			if (Error == 1)
			{
				if (!flag)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (flag)
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

	public bool IsReadyToOpen => base.InternalAtmosphere?.PressureGasses >= TargetPressure;

	public override bool IsNextImportReady
	{
		get
		{
			if (base.IsImportOpen)
			{
				return ImportingThing == null;
			}
			return false;
		}
	}

	public override bool CanBeginImport
	{
		get
		{
			if (base.IsImportOpen && ImportingThing != null && !IsOpen)
			{
				return OnOff;
			}
			return false;
		}
	}

	public List<StructureExtensionInfo> CompatibleExtensions => extensions;

	public List<IStructureExtension> Extensions { get; } = new List<IStructureExtension>(2);

	public override Slot ImportSlot => base.ImportSlot;

	public override void InitInternalAtmosphere()
	{
		base.InitInternalAtmosphere();
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, InternalVolume, 0L);
		}
		base.InternalAtmosphere.Volume = InternalVolume;
	}

	public override void Awake()
	{
		base.Awake();
	}

	public override bool HasFrameBelow(float upOffset = 0f)
	{
		return base.HasFrameBelow((0f - GridSize) / 2f + upOffset);
	}

	protected override string GetInfoPanelOperationText()
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (ImportingThing != null)
		{
			stringBuilder.AppendLine(ImportingThing.ToTooltip());
		}
		AtmosphericsManager.DisplayBasicAtmosphere(base.InternalAtmosphere, stringBuilder);
		return stringBuilder.ToString();
	}

	protected override void CheckConnections()
	{
		InputNetwork = (InputConnection?.GetINetworkedPipe())?.PipeNetwork;
		AssessError();
	}

	public override void AssessError()
	{
		bool flag = !base.IsInputValid;
		if (GameManager.RunSimulation && HasErrorState && Error == 0 && flag)
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if ((GameManager.RunSimulation && HasErrorState && Error == 1 && !flag) || !OnOff || !Powered)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (IsOpen)
		{
			Atmosphere outputAtmos = base.AtmosphericsController.CloneGlobalAtmosphere(VentExtension.VentGrid, 0L);
			AtmosphereHelper.MoveToEqualize(base.InternalAtmosphere, outputAtmos, PressurekPa.MaxValue, AtmosphereHelper.MatterState.All);
		}
		if (OnOff && Powered && Error != 1 && base.IsStructureCompleted && FullyExtended && !IsOpen)
		{
			AtmosphereHelper.MoveToEqualize(InputNetwork.Atmosphere, base.InternalAtmosphere, PressurekPa.MaxValue, AtmosphereHelper.MatterState.Gas);
		}
	}

	protected override void OnServerImportTick()
	{
		base.OnServerImportTick();
		if (!base.IsStructureCompleted || !FullyExtended)
		{
			return;
		}
		if (IsNextImportReady)
		{
			TryChuteImport();
		}
		if (CanBeginImport)
		{
			OnServer.Interact(base.InteractImport, 1);
		}
		if (base.IsImportClosed && ImportingThing != null && IsOpen)
		{
			if (ImportingThing is Plant plant)
			{
				TerraForming.AddPlantsToWorld(plant);
				OnServer.Destroy(plant);
			}
			else
			{
				OnServer.MoveToWorld(ImportingThing, VentExtension.VentGrid.Value.ToVector3(), Quaternion.identity, new Vector3(Random.Range(0f, 1f), 10f, Random.Range(0f, 1f)), Vector3.zero);
			}
		}
		if ((!OnOff && base.IsImportClosed) || (base.IsImportClosed && (object)ImportingThing == null))
		{
			OnServer.Interact(base.InteractImport, 0);
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Open)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			if (!IsReadyToOpen && !IsOpen)
			{
				return delayedActionInstance.Fail(GameStrings.DispersalTowerNotEnoughPressure.AsString(TargetPressure.ToFloat().ToStringPrefix("Pa", "yellow")));
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnDestroy()
	{
		if (Singleton<GameManager>.IsQuitting)
		{
			return;
		}
		base.OnDestroy();
		if (GameManager.GameState == GameState.None)
		{
			return;
		}
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

	public override void SetLogicValue(LogicType logicType, double value)
	{
		int num = (int)Mathf.Clamp((float)value, 0f, 1f);
		if (logicType == LogicType.Open)
		{
			if (IsReadyToOpen || num == 0)
			{
				OnServer.Interact(base.InteractOpen, num);
			}
		}
		else
		{
			base.SetLogicValue(logicType, value);
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
		if (extension is DispersalTowerVentExtension ventExtension)
		{
			VentExtension = ventExtension;
		}
	}

	private void OnRemove(IStructureExtension extension)
	{
		if (extension is DispersalTowerVentExtension)
		{
			VentExtension = null;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new DispersalTowerSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		_ = savedData is DispersalTowerSaveData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		_ = savedData is DispersalTowerSaveData;
	}
}
