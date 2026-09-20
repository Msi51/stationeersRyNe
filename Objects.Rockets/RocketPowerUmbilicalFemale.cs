using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using JetBrains.Annotations;
using Objects.Rockets.Scanning;
using Trading;
using UnityEngine;

namespace Objects.Rockets;

public class RocketPowerUmbilicalFemale : RocketPowerUmbilical, IRocketInternals, IRocketComponent, IRocketTransferActionProgressable, IRocketActionProgressable, IReferencable, IEvaluable
{
	private BatteryCellState _batteryState;

	[SerializeField]
	private Vector3 startingGrid = new Vector3(0f, 0f, 0.5f);

	public static string[] BatteryModeStrings = Enum.GetNames(typeof(BatteryCellState));

	public override Vector3 FirstPartnerSearchPosition => base.Position + Forward * SmallGrid.SmallGridSize;

	public override UmbilicalType UmbilicalType => UmbilicalType.Socket;

	public Vector3 StartingGrid => startingGrid;

	private bool PartnerValid
	{
		get
		{
			if ((object)base.PartnerUmbilical != null)
			{
				return base.PartnerUmbilical.CanTransfer;
			}
			return false;
		}
	}

	public override string[] ModeStrings => BatteryModeStrings;

	protected override bool IsOperable
	{
		get
		{
			if (OutputNetwork == null)
			{
				return false;
			}
			return base.IsOperable;
		}
	}

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Umbilical;

	public bool StrictlyInternal => true;

	public new Thing AsThing => this;

	public Type PartnerType => typeof(RocketPowerUmbilicalMale);

	public IRocketActionProgressableTarget CurrentTarget => base.PartnerUmbilical;

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.UmbilicalCategory);
	}

	private void CheckError()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		if (!IsOperable)
		{
			if (Error == 0)
			{
				base.LastPowerAdded = 0f;
				base.LastPowerRemoved = 0f;
				OnServer.Interact(base.InteractError, 1, skipAnimation: true);
			}
		}
		else if (Error == 1)
		{
			OnServer.Interact(base.InteractError, 0, skipAnimation: true);
		}
	}

	public override void OnAddCableNetwork(CableNetwork newNetwork)
	{
		base.OnAddCableNetwork(newNetwork);
		CheckError();
	}

	public override void OnRemoveCableNetwork(CableNetwork oldNetwork)
	{
		base.OnRemoveCableNetwork(oldNetwork);
		CheckError();
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			base.OnDestroy();
			if ((bool)base.PartnerUmbilical)
			{
				base.PartnerUmbilical.PartnerRemoved();
			}
			base.PartnerUmbilical = null;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RocketPowerUmbilicalFemaleSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is RocketPowerUmbilicalFemaleSaveData rocketPowerUmbilicalFemaleSaveData)
		{
			base.PowerStored = rocketPowerUmbilicalFemaleSaveData.PowerStored;
			base.PartnerDistance = rocketPowerUmbilicalFemaleSaveData.PartnerDistance;
			_savedPartnerId = rocketPowerUmbilicalFemaleSaveData.PartnerUmbilicalId;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is RocketPowerUmbilicalFemaleSaveData rocketPowerUmbilicalFemaleSaveData)
		{
			if (float.IsNaN(base.PowerStored))
			{
				base.PowerStored = 0f;
			}
			rocketPowerUmbilicalFemaleSaveData.PowerStored = base.PowerStored;
			rocketPowerUmbilicalFemaleSaveData.PartnerDistance = base.PartnerDistance;
			rocketPowerUmbilicalFemaleSaveData.PartnerUmbilicalId = base.PartnerUmbilical?.ReferenceId ?? 0;
		}
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		RocketUmbilicalHelper.FindAndSetOtherUmbilical(this);
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		SetPartner(Thing.Find<IUmbilical>(_savedPartnerId));
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		if ((bool)base.PartnerUmbilical)
		{
			base.PartnerUmbilical.PartnerRemoved();
		}
		RetractUmbilical();
		base.PartnerUmbilical = null;
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (base.TransferProgress >= 1f && PartnerValid)
		{
			MovePowerToUmbilical();
			base.TransferProgress = 0f;
		}
	}

	private void MovePowerToUmbilical()
	{
		float num = Mathf.Clamp(base.PartnerUmbilical.PowerMaximum - base.PartnerUmbilical.PowerStored, 0f, PowerMaximum);
		float a = num;
		foreach (Battery battery in base.RocketNetwork.Batteries)
		{
			if (!battery.IsEmpty && battery.OnOff && battery.Error != 1)
			{
				float num2 = Mathf.Min(num, battery.PowerStored);
				battery.PowerStored -= num2;
				num -= num2;
				base.PowerStored += num2;
				if (num <= 0f)
				{
					break;
				}
			}
		}
		float num3 = Mathf.Min(a, base.PowerStored);
		base.PartnerUmbilical.ReceivePower(null, num3);
		base.PowerStored -= num3;
	}

	public override void UsePower(CableNetwork cableNetwork, float powerUsed)
	{
		base.LastPowerRemoved = powerUsed;
		base.PowerStored = Mathf.Clamp(base.PowerStored - powerUsed, 0f, PowerMaximum);
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.LastPowerAdded = powerAdded;
		base.PowerStored = Mathf.Clamp(powerAdded + base.PowerStored, 0f, PowerMaximum);
	}

	public override float GetUsedPower([NotNull] CableNetwork cableNetwork)
	{
		if (InputNetwork == null || Error == 1 || cableNetwork != InputNetwork)
		{
			return 0f;
		}
		return UsedPower + Mathf.Clamp(PowerMaximum - base.PowerStored, 0f, PowerMaximum);
	}

	public override float GetGeneratedPower(CableNetwork cableNetwork)
	{
		if (OutputNetwork == null || Error == 1 || cableNetwork != OutputNetwork)
		{
			return 0f;
		}
		if (base.PartnerUmbilical is RocketPowerUmbilicalFemale)
		{
			return 0f;
		}
		return Mathf.Max(base.PowerStored, 0f);
	}

	public void OnLaunch(bool immediate = false)
	{
		base.PowerStored = 0f;
		if ((bool)base.PartnerUmbilical)
		{
			if (base.PartnerUmbilical.IsOpen)
			{
				base.PartnerUmbilical.DamageState.Damage(ChangeDamageType.Set, base.PartnerUmbilical.DamageState.MaxDamage, DamageUpdateType.Brute);
			}
			else
			{
				base.PartnerUmbilical.PartnerRemoved();
			}
			base.PartnerUmbilical = null;
		}
	}

	public void OnLanded(bool immediate = false)
	{
		RocketUmbilicalHelper.FindAndSetOtherUmbilical(this);
	}

	public override void SetPartner(IUmbilical partner)
	{
		base.PartnerUmbilical = partner as RocketPowerUmbilical;
	}

	public override bool IsCompatibleWith(IUmbilical other)
	{
		return other is RocketPowerUmbilical;
	}

	public override void PartnerRemoved()
	{
		base.PartnerUmbilical = null;
	}

	public override void RetractUmbilical()
	{
		base.PartnerUmbilical?.RetractUmbilical();
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action != InteractableType.Open)
		{
			return base.GetContextualName(interactable);
		}
		if (!IsOpen)
		{
			return ActionStrings.Extend;
		}
		return ActionStrings.Retract;
	}

	public override bool CanProgressAction(out RocketActionResult result)
	{
		if (!base.PartnerUmbilical)
		{
			result = RocketActionResult.Failure(GameStrings.NoPartnerUmbilical);
			return false;
		}
		if (base.RocketNetwork?.Rocket?.CurrentNode == null || base.PartnerUmbilical?.RocketNetwork?.Rocket?.CurrentNode != base.RocketNetwork.Rocket.CurrentNode)
		{
			result = RocketActionResult.Failure(GameStrings.NoPartnerUmbilical);
			return false;
		}
		foreach (Battery battery in OutputNetwork.BatteryList)
		{
			if (battery.OnOff && !battery.IsEmpty && battery.Error != 1)
			{
				result = RocketActionResult.Success;
				return true;
			}
		}
		result = RocketActionResult.Failure(GameStrings.NoAvailableBattery);
		return false;
	}

	public List<IRocketActionProgressableTarget> GetValidTargets()
	{
		List<IRocketActionProgressableTarget> list = new List<IRocketActionProgressableTarget>(4);
		Rocket rocket = base.RocketNetwork?.Rocket;
		List<Rocket> list2 = (rocket?.CurrentNode)?.RocketsHere;
		if (list2 == null)
		{
			return list;
		}
		foreach (Rocket item2 in list2)
		{
			if (item2 == rocket || item2 == null)
			{
				continue;
			}
			foreach (IUmbilical umbilical in item2.GetUmbilicals())
			{
				if (umbilical is RocketPowerUmbilical item)
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	public void SetTarget(IRocketActionProgressableTarget selectedTarget)
	{
		RocketPowerUmbilicalFemale rocketPowerUmbilicalFemale = selectedTarget as RocketPowerUmbilicalFemale;
		SetPartner(rocketPowerUmbilicalFemale);
		base.TransferProgress = 0f;
		if ((bool)rocketPowerUmbilicalFemale)
		{
			rocketPowerUmbilicalFemale.SetPartner(null);
		}
	}
}
