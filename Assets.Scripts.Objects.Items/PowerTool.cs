using System.Text;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class PowerTool : Tool, IBatteryPowered, IPowered, IDensePoolable, IReferencable, IEvaluable, ILogicable
{
	[Header("Power Tool")]
	public float UsedPowerPassive;

	public float UsedPowerActive;

	private BatteryCell _batteryPrefab;

	protected static readonly int BASEPOWERUSAGE = 1000;

	protected bool _checkPower;

	public BatteryCell Battery => BatterySlot.Get<BatteryCell>();

	public Slot BatterySlot { get; private set; }

	public override bool IsOperable
	{
		get
		{
			if ((bool)Battery)
			{
				return !Battery.IsEmpty;
			}
			return false;
		}
	}

	public virtual int BasePowerUsage => BASEPOWERUSAGE;

	public void Recharge(float amount)
	{
		if ((bool)Battery)
		{
			Battery.PowerStored += amount;
		}
	}

	public override void Awake()
	{
		base.Awake();
		ElectricityManager.Register(this);
		BatterySlot = Slots.Find((Slot s) => s.Type == Slot.Class.Battery);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (GameManager.GameState != GameState.None)
		{
			ElectricityManager.Deregister(this);
		}
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (OnOff)
		{
			extendedText.AppendLine(GameStrings.ThingIsState.AsString(ToTooltip(), OnOff ? ActionStrings.On.AsColor("green") : ActionStrings.Off.AsColor("red")));
		}
		return extendedText;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		string text = InteractableType.Activate.ToString();
		foreach (InteractableState state in savedData.States)
		{
			if (Animator.StringToHash(state.StateName) == Animator.StringToHash(text))
			{
				state.State = 0;
			}
		}
	}

	public override void OnChildBatteryCellChange(BatteryCell batteryCell)
	{
		base.OnChildBatteryCellChange(batteryCell);
		CheckPower();
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 1f
		};
		BatteryCell batteryCell = attack.SourceItem as BatteryCell;
		if ((bool)batteryCell)
		{
			delayedActionInstance.ActionMessage = ActionStrings.Insert;
			delayedActionInstance.OverrideTitle = attack.SourceItem.GetPassiveTooltip(null).Title;
			if (!base.AllowInteraction)
			{
				return delayedActionInstance.Fail(GameStrings.ThingCanNotInsertInteractionsDisabled, attack.SourceItem.ToTooltip(), ToTooltip());
			}
			if ((bool)Battery)
			{
				return delayedActionInstance.Fail(GameStrings.ThingCanNotInsertAlreadyContains, attack.SourceItem.ToTooltip(), Battery.ToTooltip());
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			if (GameManager.RunSimulation && attack.CompletedRatio >= 1f)
			{
				OnServer.MoveToSlot(batteryCell, BatterySlot);
			}
			return delayedActionInstance.Succeed();
		}
		if ((bool)(attack.SourceItem as Screwdriver))
		{
			delayedActionInstance.ActionMessage = ActionStrings.Remove;
			delayedActionInstance.OverrideTitle = (BatterySlot.IsNotEmpty() ? BatterySlot.Get().GetPassiveTooltip(null).Title : BatterySlot.GetSafeName());
			if (!Battery)
			{
				return delayedActionInstance.Fail(GameStrings.CartridgeNoCartridgeInSlot, BatterySlot.ToTooltip(), BatterySlot.TypeTooltip());
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			if (GameManager.RunSimulation && attack.CompletedRatio >= 1f)
			{
				OnServer.MoveToSlotOrWorld(Battery, attack.OtherHand);
			}
			return delayedActionInstance.Succeed();
		}
		return base.AttackWith(attack, doAction);
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		Activate = 0;
	}

	public override bool OnUseItem(float quantity, Thing onUseThing)
	{
		if ((object)Battery == null || Battery.IsEmpty)
		{
			if (OnOff && GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractPowered, 0);
			}
			return false;
		}
		Battery.PowerStored -= BasePowerUsage;
		return true;
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.RunSimulation && !_checkPower)
		{
			_checkPower = true;
			CheckPowerNextFrame().Forget();
		}
	}

	public override void OnFinishedInteractionSync(Interactable interactable)
	{
		base.OnFinishedInteractionSync(interactable);
		if (GameManager.RunSimulation && !_checkPower)
		{
			_checkPower = true;
			CheckPowerNextFrame().Forget();
		}
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (OnOff && Powered && (object)Battery != null)
		{
			Battery.PowerStored -= UsedPowerPassive;
			if (Activate == 1)
			{
				Battery.PowerStored -= UsedPowerActive;
			}
		}
	}

	private async UniTask CheckPowerNextFrame()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		await UniTask.NextFrame();
		CheckPower();
	}

	public virtual void CheckPower()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		if ((object)Battery != null && !Battery.IsEmpty && OnOff)
		{
			if (!Powered)
			{
				OnServer.Interact(base.InteractPowered, 1);
			}
		}
		else if (Powered)
		{
			OnServer.Interact(base.InteractPowered, 0);
		}
		_checkPower = false;
	}

	public void AddBattery()
	{
		if ((object)_batteryPrefab == null)
		{
			_batteryPrefab = Prefab.Find<BatteryCell>("ItemBatteryCell");
		}
		BatteryCell batteryCell = Thing.Create<BatteryCell>(_batteryPrefab);
		batteryCell.PowerStored = batteryCell.PowerMaximum;
		OnServer.MoveToSlot(batteryCell, BatterySlot);
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		CheckPower();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		CheckPower();
	}
}
