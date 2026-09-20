using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets;
using Objects.Rockets.Mining;
using Objects.Rockets.Scanning;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class RocketGasCollector : DeviceMixAtmosphere, IRocketInternals, IRocketComponent, IRocketMassContributor, IRocketMiner, IRocketActionProgressable, IReferencable, IEvaluable
{
	public const float INTERNAL_VOLUME = 200f;

	private float _miningProgress;

	private ushort _nextYield;

	public float RocketMass = 20f;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => true;

	public override VolumeLitres Volume => new VolumeLitres(200.0);

	private bool IsConstructed
	{
		get
		{
			BuildState currentBuildState = base.CurrentBuildState;
			List<BuildState> buildStates = BuildStates;
			return currentBuildState == buildStates[buildStates.Count - 1];
		}
	}

	public float MiningProgress
	{
		get
		{
			return _miningProgress;
		}
		set
		{
			_miningProgress = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 512;
			}
		}
	}

	public ushort NextYield
	{
		get
		{
			return _nextYield;
		}
		set
		{
			_nextYield = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 1024;
			}
		}
	}

	public RocketNetwork RocketNetwork { get; set; }

	public Rocket Rocket => RocketNetwork?.Rocket;

	public float MassContribution => RocketMass;

	public float GetActionProgress => MiningProgress;

	protected override bool IsOperable => ConnectedPipeNetworks.Count > 0;

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		if ((bool)base.SwitchOnOff)
		{
			base.SwitchOnOff.RefreshState(skipAnimation);
		}
	}

	public override void OnAtmosphericTick()
	{
		if (OnOff && Powered && IsOperable && IsConstructed)
		{
			base.OnAtmosphericTick();
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.RocketPayloadCategory);
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		AtmosphericsManager.MakeGasTooltip(base.InternalAtmosphere, extendedText);
		return extendedText;
	}

	public override PassiveUITooltip GetPassiveUITooltip()
	{
		return PassiveUITooltip.Make(DisplayName, GetExtendedText().ToString());
	}

	public string GetActionInfoText()
	{
		string arg = (OnOff ? GameStrings.On.DisplayString : GameStrings.Off.DisplayString);
		string arg2 = StringManager.Get(NextYield);
		return GameStrings.RocketGasCollectActionInfo.AsString(arg, arg2);
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (Rocket?.CurrentNode?.Deposit == null)
		{
			MiningProgress = 0f;
		}
		else if (!(Rocket.CurrentAction is RocketMine))
		{
			MiningProgress = 0f;
		}
		else if (OnOff && Powered && MiningProgress >= 1f)
		{
			GasMixture gasMixture = Rocket.CurrentNode.Deposit.CollectGas();
			base.InternalAtmosphere.Add(gasMixture);
			MiningProgress = 0f;
		}
	}

	public bool CanProgressAction(out RocketActionResult result)
	{
		if (!IsConstructed)
		{
			result = RocketActionResult.Failure(GameStrings.DeviceNotConstructed);
			return false;
		}
		if (ConnectedPipeNetworks.Count == 0)
		{
			result = RocketActionResult.Failure(GameStrings.DeviceOutputNetworkInvalid);
			return false;
		}
		return result = RocketActionResult.Success;
	}

	public void ClearAction()
	{
		MiningProgress = 0f;
	}

	public void ProgressMineAction(float deltaTime, RocketMine mineAction)
	{
		MineableDeposit deposit = mineAction.SpaceMapNode.Deposit;
		if (deposit != null)
		{
			MiningProgress += deltaTime / deposit.TimeToMine();
			MiningProgress = Mathf.Min(MiningProgress, 1f);
			int num = deposit.OreQuantity();
			NextYield = (ushort)num;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RocketGasCollectorSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is RocketGasCollectorSaveData rocketGasCollectorSaveData)
		{
			MiningProgress = rocketGasCollectorSaveData.MiningProgress;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is RocketGasCollectorSaveData rocketGasCollectorSaveData)
		{
			rocketGasCollectorSaveData.MiningProgress = MiningProgress;
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(MiningProgress);
		writer.WriteUInt16(NextYield);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		MiningProgress = reader.ReadSingle();
		NextYield = reader.ReadUInt16();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(MiningProgress);
		}
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteUInt16(NextYield);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			MiningProgress = reader.ReadSingle();
		}
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			NextYield = reader.ReadUInt16();
		}
	}
}
