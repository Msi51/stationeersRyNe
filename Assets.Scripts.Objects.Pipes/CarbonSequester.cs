using System.Collections.Generic;
using System.Text;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Reagents;
using Trading;
using UnityEngine;
using Util;

namespace Assets.Scripts.Objects.Pipes;

public class CarbonSequester : DeviceInputOutputImportExport, IExtendableStructure, IReferencable, IEvaluable
{
	public const float POWER_PER_UNIT_CARBON = 45000f;

	private MoleQuantity _processedMoles;

	public const int MAX_REAGENTS = 1000;

	private CancellationTokenWrapper AddCarbonCancellation = new CancellationTokenWrapper();

	private const float INPUT_PRESSURE_DENOMINATOR = 4.9f;

	[SerializeField]
	private List<StructureExtensionInfo> extensions;

	private CarbonSequesterVentExtension VentExtension;

	private CarbonSequesterTopExtension TopExtension;

	public MoleQuantity ProcessedMoles
	{
		get
		{
			return _processedMoles;
		}
		set
		{
			if (NetworkManager.IsServer && !RocketMath.Approximately(value, ProcessedMoles))
			{
				base.NetworkUpdateFlags |= 512;
			}
			_processedMoles = value;
		}
	}

	public bool FullyExtended
	{
		get
		{
			if (VentExtension != null && VentExtension.IsStructureCompleted && TopExtension != null)
			{
				return TopExtension.IsStructureCompleted;
			}
			return false;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			bool flag = base.IsInputValid && ReagentMixture.TotalReagents < 1000.0;
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

	public override Slot ExportSlot
	{
		get
		{
			List<Slot> slots = Slots;
			if (slots == null || slots.Count <= 0)
			{
				return null;
			}
			return Slots[0];
		}
	}

	public List<StructureExtensionInfo> CompatibleExtensions => extensions;

	public List<IStructureExtension> Extensions { get; } = new List<IStructureExtension>(2);

	public override void InitInternalAtmosphere()
	{
		base.InitInternalAtmosphere();
	}

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None)
		{
			ReagentMixture = new ReagentMixture(this);
		}
	}

	protected StringBuilder GetInfoPanelOperationText(StringBuilder sb)
	{
		sb.AppendLine(GameStrings.ProcessedMoles.AsString(ProcessedMoles.ToFloat().ToStringPrefix("mol", "yellow")));
		return sb;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		Tooltip.ToolTipStringBuilder.Clear();
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (passiveTooltip.Title.Equals(string.Empty))
		{
			passiveTooltip.Title = DisplayName;
		}
		if (_infoScreen != null && hitCollider == _infoScreen.InfoTrigger && Powered)
		{
			if (Error == 0)
			{
				GetInfoPanelOperationText(Tooltip.ToolTipStringBuilder);
				ReagentMixture.BuildReagentString(Tooltip.ToolTipStringBuilder);
				passiveTooltip.Extended = Tooltip.ToolTipStringBuilder.ToString();
				passiveTooltip.Title = Localization.GetInterface("Contents");
			}
			else
			{
				passiveTooltip.Extended = ActionStrings.Error;
			}
		}
		return passiveTooltip;
	}

	public Ore GetPrefabForNextReagent()
	{
		ReagentMixture nextMix = ReagentMixture.GetNextMix();
		Centrifuge.RecipeComparable.AllRecipes.TryGetValue(new Recipe(nextMix, null), out var value);
		return value;
	}

	protected override void OnServerExportTick(float deltaTime)
	{
		if (!base.IsStructureCompleted)
		{
			return;
		}
		if (IsNextExportReady && IsOpen && ReagentMixture.TotalReagents > 0.0)
		{
			ReagentMixture reagentMixture = new ReagentMixture();
			Ore prefabForNextReagent = GetPrefabForNextReagent();
			if (prefabForNextReagent != null)
			{
				int quantity = ReagentMixture.AddNextReagent(reagentMixture, prefabForNextReagent.MaxQuantity);
				Ore ore = Centrifuge.CreateOutput(prefabForNextReagent, quantity, ExportSlot);
				if ((object)ore != null)
				{
					ore.CreatedReagentMixture = reagentMixture;
					ExportingThing = ore;
				}
			}
		}
		else if (CanBeginExport)
		{
			OnServer.Interact(base.InteractExport, 1);
		}
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
		if (OnOff && Powered && Error != 1 && base.IsStructureCompleted && FullyExtended)
		{
			AddCarbonCancellation.CancelAndInitialize();
			float num = UsedPower / 45000f;
			MoleQuantity quantity = new MoleQuantity((double)num * 100.0);
			GasMixture gasMixture = InputNetwork.Atmosphere.Remove(IdealGas.Quantity(InputNetwork.Atmosphere.PressureGasses / 4.900000095367432, Chemistry.PipeVolume, InputNetwork.Atmosphere.Temperature), AtmosphereHelper.MatterState.Gas);
			Mole mole = gasMixture.Remove(Chemistry.GasType.CarbonDioxide, quantity);
			Atmosphere atmosphere = AtmosphericsController.World.CloneGlobalAtmosphere(VentExtension.VentGrid, 0L);
			atmosphere.Add(gasMixture);
			atmosphere.Add(new Mole(energy: IdealGas.Energy(mole.Temperature, Mole.SpecificHeat(Chemistry.GasType.CarbonDioxide), mole.Quantity), gasType: Chemistry.GasType.Oxygen, quantity: mole.Quantity));
			ProcessedMoles = mole.Quantity;
			AddCarbonCancellation.CancelAndInitialize();
			AddCarbonNextFrame(AddCarbonCancellation.Token, mole.Quantity.ToDouble() / 100.0).Forget();
		}
	}

	private async UniTaskVoid AddCarbonNextFrame(CancellationToken token, double carbonAdded)
	{
		await UniTask.SwitchToMainThread(token);
		if (carbonAdded > 0.0)
		{
			ReagentMixture.Set(ReagentMixture.Carbon.ReagentId, ReagentMixture.Carbon.Quantity + carbonAdded);
		}
	}

	public override void OnDestroy()
	{
		if (Singleton<GameManager>.IsQuitting)
		{
			return;
		}
		base.OnDestroy();
		AddCarbonCancellation.Cancel();
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
		if (!(extension is CarbonSequesterVentExtension ventExtension))
		{
			if (extension is CarbonSequesterTopExtension topExtension)
			{
				TopExtension = topExtension;
			}
		}
		else
		{
			VentExtension = ventExtension;
		}
	}

	private void OnRemove(IStructureExtension extension)
	{
		if (!(extension is CarbonSequesterVentExtension))
		{
			if (extension is CarbonSequesterTopExtension)
			{
				TopExtension = null;
			}
		}
		else
		{
			VentExtension = null;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(ProcessedMoles.ToFloat());
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			ProcessedMoles = new MoleQuantity(reader.ReadSingle());
		}
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
		ThingSaveData result = (savedData = new CarbonSequesterSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		_ = savedData is CarbonSequesterSaveData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		_ = savedData is CarbonSequesterSaveData;
	}
}
