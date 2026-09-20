using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Reagents;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class WaterPurifier : DeviceInputOutputImport, IResourceConsumer, IReferencable, IEvaluable, ISanitation
{
	[SerializeField]
	protected Collider infoPanel;

	public static int CharcoalPrefabHash = Animator.StringToHash("ItemCharcoal");

	private static readonly VolumeLitres FLOW_RATE_L_MIN = new VolumeLitres(0.025);

	private static readonly VolumeLitres FLOW_RATE_L_MAX = new VolumeLitres(0.15);

	private const float PROCESSED_MOLES_PER_UNIT_CARBON = 40f;

	private MoleQuantity _molesProcessedLastTick;

	public List<OreResource> Resources = new List<OreResource>();

	public override bool CanBeginImport
	{
		get
		{
			if (base.CanBeginImport)
			{
				return OnOff;
			}
			return false;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			bool flag = base.IsInputValid && base.IsOutputValid && HasCharcoal() && base.IsStructureCompleted;
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

	public MoleQuantity MolesProcessedLastTick
	{
		get
		{
			return _molesProcessedLastTick;
		}
		set
		{
			if (!RocketMath.Approximately(value, _molesProcessedLastTick))
			{
				base.NetworkUpdateFlags |= 512;
			}
			_molesProcessedLastTick = value;
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.Sanitation);
	}

	public override void OnAllPrefabsLoaded()
	{
		base.OnAllPrefabsLoaded();
		foreach (OreResource resource in Resources)
		{
			resource.Initialize();
		}
	}

	public override CanConstructInfo CanConstruct()
	{
		if (HasFrameBelow())
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
	}

	protected StringBuilder GetInfoPanelOperationText()
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (Error == 1 && !HasCharcoal())
		{
			stringBuilder.Append(GameStrings.WaterPurifierCharcoalErrorMsg.AsString(Prefab.Find<Thing>(CharcoalPrefabHash).DisplayName));
		}
		if (Error == 0)
		{
			stringBuilder.Append(GameStrings.ProcessedMoles.AsString(MolesProcessedLastTick.ToFloat().ToStringPrefix("mol", "yellow")));
		}
		return stringBuilder;
	}

	public override void Awake()
	{
		base.Awake();
		ReagentMixture = new ReagentMixture(this);
	}

	private void CollectResource(Ore ore)
	{
		ReagentMixture.Add(ore.CreatedReagentMixture);
		ore.Quantity--;
	}

	public override void OnImportClosingComplete()
	{
		base.OnImportClosingComplete();
		if (GameManager.RunSimulation && ImportingThing != null && ImportingThing.PrefabHash != CharcoalPrefabHash)
		{
			OnServer.Interact(base.InteractImport, 0);
		}
	}

	protected override void OnServerImportTick()
	{
		if (!base.IsStructureCompleted)
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
		if (base.IsImportClosed && ImportingThing is Ore ore && ImportingThing.PrefabHash == CharcoalPrefabHash)
		{
			lock (ReagentMixture)
			{
				if (ReagentMixture.Carbon.Quantity <= 0.0)
				{
					CollectResource(ore);
				}
			}
		}
		if (!OnOff)
		{
			OnServer.Interact(base.InteractImport, 0);
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (OnOff && Powered && IsOperable)
		{
			VolumeLitres value = new VolumeLitres(RocketMath.MapToScale(0f, Chemistry.Limits.MAXPressureLiquidPipe.ToFloat(), FLOW_RATE_L_MIN.ToFloat(), FLOW_RATE_L_MAX.ToFloat(), RocketMath.Max(PressurekPa.Zero, InputNetwork.Atmosphere.PressureGasses - OutputNetwork.Atmosphere.PressureGasses).ToFloat()));
			value = RocketMath.Clamp(value, FLOW_RATE_L_MIN, FLOW_RATE_L_MAX);
			MoleQuantity moleQuantity = MoleQuantity.Zero;
			if (value > VolumeLitres.Zero)
			{
				GasMixture gasMixture = AtmosphereHelper.RemoveLiquidVolume(InputNetwork.Atmosphere, value);
				moleQuantity = gasMixture.GetTotalMolesLiquids;
				Mole mole = gasMixture.Remove(Chemistry.GasType.PollutedWater, MoleQuantity.MaxValue);
				gasMixture.Add(new Mole(Chemistry.GasType.Water, mole.Quantity, mole.Energy));
				OutputNetwork.Atmosphere.Add(gasMixture);
				AtmosphereHelper.MoveToEqualize(InputNetwork.Atmosphere, OutputNetwork.Atmosphere, base.PressurePerTick, AtmosphereHelper.MatterState.Gas);
				lock (ReagentMixture)
				{
					ReagentMixture.Carbon.Quantity -= mole.Quantity.ToFloat() / 40f;
				}
			}
			MolesProcessedLastTick = moleQuantity;
			if (moleQuantity > MoleQuantity.One)
			{
				if (Activate == 0)
				{
					OnServer.Interact(base.InteractActivate, 1);
				}
			}
			else if (Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
		}
		else if (Activate == 1)
		{
			OnServer.Interact(base.InteractActivate, 0);
		}
	}

	private bool HasCharcoal()
	{
		if (ImportingThing != null && ImportingThing.PrefabHash == CharcoalPrefabHash)
		{
			return true;
		}
		if (ReagentMixture.Carbon.Quantity > 0.0)
		{
			return true;
		}
		return false;
	}

	public override void AssessError()
	{
		bool flag = base.IsInputValid && base.IsOutputValid && HasCharcoal() && base.IsStructureCompleted;
		if (GameManager.RunSimulation && HasErrorState && Error == 0 && !flag)
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if ((GameManager.RunSimulation && HasErrorState && Error == 1 && flag) || !OnOff || !Powered)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		Tooltip.ToolTipStringBuilder.Clear();
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (passiveTooltip.Title.Equals(string.Empty))
		{
			passiveTooltip.Title = DisplayName;
		}
		if (infoPanel != null && hitCollider == infoPanel && Powered)
		{
			passiveTooltip.Extended = GetInfoPanelOperationText().ToString();
		}
		return passiveTooltip;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		AssessError();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		AssessError();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteFloatHalf(MolesProcessedLastTick.ToFloat());
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			MolesProcessedLastTick = new MoleQuantity(reader.ReadFloatHalf());
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

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new WaterPurifierSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		_ = savedData is WaterPurifierSaveData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		_ = savedData is WaterPurifierSaveData;
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Activate || logicType == LogicType.Setting || logicType - 23 <= LogicType.Power)
		{
			return false;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Activate || logicType == LogicType.Setting)
		{
			return false;
		}
		return base.CanLogicWrite(logicType);
	}

	public List<Item> GetResourcesUsed()
	{
		List<Item> list = new List<Item>(Resources.Count);
		foreach (OreResource resource in Resources)
		{
			Item generatorOre = resource.GeneratorOre;
			if (!(generatorOre == null))
			{
				list.Add(generatorOre);
			}
		}
		return list;
	}

	public bool CanProcess(Recipe recipe)
	{
		foreach (OreResource resource in Resources)
		{
			if (resource.GeneratorOre.CreatedReagentMixture.ContainsSome(recipe))
			{
				return true;
			}
		}
		return false;
	}

	public bool CanProcess(Reagent reagentType)
	{
		foreach (OreResource resource in Resources)
		{
			if (resource.GeneratorOre.CreatedReagentMixture.Contains(reagentType))
			{
				return true;
			}
		}
		return false;
	}
}
