using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Networks;
using Objects.Items;
using Objects.Rockets;
using Objects.Rockets.Mining;
using Objects.Rockets.Scanning;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class RocketMiner : DeviceImportExport, IRocketInternals, IRocketComponent, IRocketActionProgressable, IReferencable, IEvaluable, IRocketMassContributor, IRocketMiner
{
	private RocketMiningDrillHead _miningHead;

	[Header("Miner Speed Multipliers")]
	[SerializeField]
	private float oreSpeedMultiplier = 1f;

	[SerializeField]
	private float iceSpeedMultiplier = 1f;

	[SerializeField]
	private float junkSpeedMultiplier = 1f;

	private float _miningProgress;

	private ushort _nextYield;

	private int _quantityMined;

	public float MassContribution => 500f;

	public Rocket Rocket => RocketNetwork?.Rocket;

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

	public override bool CanIceMelt => false;

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

	public Thing LastMinedPrefab { get; private set; }

	public int QuantityMined
	{
		get
		{
			return _quantityMined;
		}
		private set
		{
			_quantityMined = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 2048;
			}
		}
	}

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => true;

	public RocketNetwork RocketNetwork { get; set; }

	public float GetActionProgress => MiningProgress;

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.RocketPayloadCategory);
	}

	public float MinerSpeedMultiplier(MineableDeposit deposit)
	{
		float num = 1f;
		if ((deposit.DepositType & MineableDepositType.Ore) != MineableDepositType.None || (deposit.DepositType & MineableDepositType.ReagentMix) != MineableDepositType.None)
		{
			num *= oreSpeedMultiplier;
		}
		if ((deposit.DepositType & MineableDepositType.Ice) != MineableDepositType.None)
		{
			num *= iceSpeedMultiplier;
		}
		if ((deposit.DepositType & MineableDepositType.Junk) != MineableDepositType.None)
		{
			num *= junkSpeedMultiplier;
		}
		if ((bool)_miningHead)
		{
			num *= _miningHead.SpeedMultiplier;
		}
		return num;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		extendedText.AppendLine(GameStrings.TooltipInternalQuantity.AsString(StringManager.Get(QuantityMined).AsColor("yellow")));
		return extendedText;
	}

	public override PassiveUITooltip GetPassiveUITooltip()
	{
		return PassiveUITooltip.Make(DisplayName, GetExtendedText().ToString());
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (newChild is RocketMiningDrillHead miningHead)
		{
			_miningHead = miningHead;
		}
		Error = (((object)_miningHead == null) ? 1 : 0);
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (previousChild is RocketMiningDrillHead rocketMiningDrillHead && _miningHead == rocketMiningDrillHead)
		{
			_miningHead = null;
		}
		Error = (((object)_miningHead == null) ? 1 : 0);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Quantity => true, 
			LogicType.DrillCondition => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Quantity:
			return QuantityMined;
		case LogicType.DrillCondition:
			if (!(_miningHead != null))
			{
				return -1.0;
			}
			return (double)_miningHead.Quantity / (double)_miningHead.MaxQuantity;
		default:
			return base.GetLogicValue(logicType);
		}
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		float num = base.GetUsedPower(cableNetwork);
		if ((object)_miningHead != null)
		{
			num *= _miningHead.PowerConsumptionMultiplier;
		}
		return num;
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (Rocket?.CurrentNode?.Deposit == null)
		{
			MiningProgress = 0f;
			LastMinedPrefab = null;
			QuantityMined = 0;
			return;
		}
		if (!(Rocket.CurrentAction is RocketMine))
		{
			if (LastMinedPrefab != null && QuantityMined > 0)
			{
				if (OnOff && Powered && IsNextExportReady)
				{
					CreateMinedPrefab(QuantityMined);
					MiningProgress = 0f;
					QuantityMined = 0;
					LastMinedPrefab = null;
				}
			}
			else
			{
				MiningProgress = 0f;
				QuantityMined = 0;
				LastMinedPrefab = null;
			}
			return;
		}
		if (OnOff && Powered && MiningProgress >= 1f && QuantityMined < SpaceOre.SpaceOreStackSize && IsNextExportReady)
		{
			Thing prefab;
			int num = Rocket.CurrentNode.Deposit.MineDeposit(out prefab, _miningHead);
			if (prefab == LastMinedPrefab)
			{
				QuantityMined += num;
			}
			else
			{
				QuantityMined = num;
				LastMinedPrefab = prefab;
			}
			_miningHead.OnResourceCollected();
			MiningProgress = 0f;
		}
		if (!(LastMinedPrefab == null))
		{
			float num2 = 1f;
			if (LastMinedPrefab is IQuantity quantity)
			{
				num2 = quantity.GetMaxQuantity;
			}
			if ((float)QuantityMined >= num2 && IsNextExportReady)
			{
				CreateMinedPrefab((int)num2);
			}
		}
	}

	private void CreateMinedPrefab(int quantity)
	{
		Thing thing = OnServer.Create<Thing>(LastMinedPrefab, ExportSlot);
		Rocket.CurrentNode.Deposit.SetPrefabValues(thing);
		if (thing is IQuantity quantity2)
		{
			quantity2.SetQuantity(quantity);
		}
		QuantityMined -= quantity;
	}

	protected override void OnServerExportTick()
	{
		if (CanBeginExport)
		{
			OnServer.Interact(base.InteractExport, 1);
		}
	}

	public bool CanProgressAction(out RocketActionResult result)
	{
		if (_miningHead == null)
		{
			return result = RocketActionResult.Failure(GameStrings.RocketMinerHasNoDrillHead, this);
		}
		if (_miningHead.Quantity <= 0f)
		{
			return result = RocketActionResult.Failure(GameStrings.RocketMinerDrillHeadWornOut, this);
		}
		if (!OnOff || !Powered)
		{
			return result = RocketActionResult.Failure(GameStrings.DeviceOffOrUnpowered, this);
		}
		if (MiningProgress >= 1f && !IsNextExportReady)
		{
			return result = RocketActionResult.Failure(GameStrings.ExportSlotBlocked, this);
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
		if (deposit != null && deposit.DepositType != MineableDepositType.Gas)
		{
			MiningProgress += deltaTime * MinerSpeedMultiplier(deposit) / deposit.TimeToMine();
			MiningProgress = Mathf.Min(MiningProgress, 1f);
			int num = deposit.OreQuantity();
			if (deposit.DepositType == MineableDepositType.Ice)
			{
				num = Mathf.RoundToInt((float)num * _miningHead.IceYieldMultiplier);
			}
			if (deposit.DepositType == MineableDepositType.Ore)
			{
				num = Mathf.RoundToInt((float)num * _miningHead.ReagentYieldMultiplier);
			}
			NextYield = (ushort)num;
		}
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RocketMinerSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is RocketMinerSaveData rocketMinerSaveData)
		{
			MiningProgress = rocketMinerSaveData.MiningProgress;
			if (!string.IsNullOrEmpty(rocketMinerSaveData.LastMinedPrefab))
			{
				LastMinedPrefab = Prefab.Find(rocketMinerSaveData.LastMinedPrefab);
			}
			QuantityMined = rocketMinerSaveData.QuantityMined;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is RocketMinerSaveData rocketMinerSaveData)
		{
			rocketMinerSaveData.MiningProgress = MiningProgress;
			rocketMinerSaveData.LastMinedPrefab = ((LastMinedPrefab != null) ? LastMinedPrefab.PrefabName : string.Empty);
			rocketMinerSaveData.QuantityMined = QuantityMined;
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(MiningProgress);
		writer.WriteUInt16(NextYield);
		writer.WriteUInt16((ushort)QuantityMined);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		MiningProgress = reader.ReadSingle();
		NextYield = reader.ReadUInt16();
		QuantityMined = reader.ReadUInt16();
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
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			writer.WriteUInt16((ushort)QuantityMined);
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
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			QuantityMined = reader.ReadUInt16();
		}
	}

	public string GetActionInfoText()
	{
		if (_miningHead == null)
		{
			return GameStrings.RocketMineNoHeadInfo.AsString();
		}
		string arg = (OnOff ? GameStrings.On.DisplayString : GameStrings.Off.DisplayString);
		string arg2 = StringManager.Get(Mathf.RoundToInt(_miningHead.Quantity / _miningHead.MaxQuantity * 100f)) + "%";
		string arg3 = StringManager.Get(NextYield);
		return GameStrings.RocketMineActionInfo.AsString(arg, arg2, arg3);
	}
}
