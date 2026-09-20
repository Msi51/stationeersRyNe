using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class SpawnPointAtmospherics : DeviceInputOutput, IExitable, IThermal
{
	public MoleEnergy MaxEnergyPull = new MoleEnergy(250.0);

	private const int STUN_PER_TICK = 10;

	private const int STUN_MAX = 100;

	public float HealAmount = 1f;

	private float _powerUsedDuringTick;

	private TemperatureKelvin _maxHeatKelvin = new TemperatureKelvin(297.1499938964844);

	protected Slot SleeperSlot => Slots[0];

	public bool FreeLook => false;

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, new VolumeLitres(10.0), 0L);
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.EntityState => true, 
			LogicType.HealthDamage => true, 
			LogicType.StunDamage => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		Entity occupant;
		Entity occupant2;
		Entity occupant3;
		return logicType switch
		{
			LogicType.EntityState => SleeperSlot.Contains<Entity>(out occupant) ? ((double)(int)occupant.State) : (-1.0), 
			LogicType.HealthDamage => SleeperSlot.Contains<Entity>(out occupant2) ? ((double)occupant2.DamageState.Total) : (-1.0), 
			LogicType.StunDamage => SleeperSlot.Contains<Entity>(out occupant3) ? ((double)occupant3.DamageState.Stun) : (-1.0), 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public virtual Vector3 GetExitPosition(Entity entity)
	{
		return base.ThingTransformPosition + ThingTransform.forward;
	}

	public virtual void Exit(Human parent)
	{
		Human human = parent ?? Slots[0].Get<Human>();
		if ((bool)human)
		{
			human.MoveToWorld(GetExitPosition(human), human.ParentSlot.Parent.Rotation, Vector3.zero, Vector3.zero);
		}
	}

	public virtual Transform GetCameraPoint(Entity entity)
	{
		return null;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		AddOccupantString(extendedText);
		return extendedText;
	}

	public StringBuilder AddOccupantString(StringBuilder sb = null, bool healthInfo = false, bool indent = false, bool alwaysShow = false, bool organInfo = false)
	{
		if (SleeperSlot.IsEmpty())
		{
			if (!alwaysShow)
			{
				return null;
			}
			if (sb == null)
			{
				sb = new StringBuilder();
			}
			if (indent)
			{
				sb.Append(StringManager.Indent);
			}
			sb.AppendLine(GameStrings.SlotVacant);
			return null;
		}
		Entity entity = SleeperSlot.Get<Entity>();
		if (sb == null)
		{
			sb = new StringBuilder();
		}
		if (indent)
		{
			sb.Append(StringManager.Indent);
		}
		sb.AppendLine(GameStrings.SlotOccupiedBy.AsString(SleeperSlot.Get().ToTooltip()));
		if (entity?.IsSleeping ?? false)
		{
			if (indent)
			{
				sb.Append(StringManager.Indent);
			}
			sb.AppendLine(GameStrings.EntityIsCurrentlyState.AsString(entity.ToTooltip(), EnumCollections.EntityStates.GetName(entity.State)));
		}
		else if (entity?.IsDead ?? false)
		{
			if (indent)
			{
				sb.Append(StringManager.Indent);
			}
			if (indent)
			{
				sb.Append(StringManager.Indent);
			}
			sb.AppendLine(GameStrings.EntityIsDead.AsString(entity.ToTooltip()));
		}
		if (healthInfo && (object)entity != null)
		{
			if (entity.DamageState.TotalRounded > 0)
			{
				entity.AddNamedDamageString(sb, indent);
			}
			int num = Mathf.RoundToInt(entity.HydrationRatio * 100f);
			StringManager.AddKeyValueLine(sb, GameStrings.Hydration, num.ToStringPrefix("%", GetStateColor(num)), indent);
			int num2 = Mathf.RoundToInt(entity.NutritionRatio * 100f);
			StringManager.AddKeyValueLine(sb, GameStrings.Nutrition, num2.ToStringPrefix("%", GetStateColor(num2)), indent);
			int num3 = Mathf.RoundToInt(Mathf.Clamp01(entity.BreathingEfficiency) * 100f);
			StringManager.AddKeyValueLine(sb, GameStrings.BreathingEfficiency, num3.ToStringPrefix("%", GetStateColor(num3)), indent);
			int num4 = Mathf.RoundToInt(entity.SanitationRatio * 100f);
			StringManager.AddKeyValueLine(sb, GameStrings.Waste, num4.ToStringPrefix("%", GetStateColorInverted(num4)), indent);
			if (organInfo)
			{
				foreach (Organ organ in entity.Organs)
				{
					sb.AppendLine(organ.DisplayName.AsColor("white"));
					organ.AddNamedDamageString(sb, indent);
					if (organ.InternalAtmosphere != null)
					{
						AtmosphericsManager.MakeGasTooltip(organ.InternalAtmosphere, sb, indent);
					}
				}
			}
		}
		return sb;
	}

	private string GetStateColor(float percent)
	{
		if (percent >= 60f)
		{
			if (!(percent >= 99.9f))
			{
				if (percent >= 90f)
				{
					return "green";
				}
				return "yellow";
			}
			return "lightblue";
		}
		if (percent >= 25f)
		{
			return "orange";
		}
		return "red";
	}

	private string GetStateColorInverted(float percent)
	{
		if (percent >= 60f)
		{
			if (!(percent > 99.9f))
			{
				if (percent >= 90f)
				{
					return "orange";
				}
				return "yellow";
			}
			return "red";
		}
		if (percent >= 25f)
		{
			return "green";
		}
		return "lightblue";
	}

	private DelayedActionInstance AddOccupantString(DelayedActionInstance result)
	{
		if ((object)SleeperSlot?.Occupant == null)
		{
			return result;
		}
		result.ExtendedMessage = AddOccupantString()?.ToString();
		return result;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action != InteractableType.Activate)
		{
			return AddOccupantString(base.InteractWith(interactable, interaction, doAction));
		}
		DynamicThing occupant = interaction.SourceSlot.Occupant;
		if (occupant is Human human)
		{
			if ((bool)SleeperSlot.Occupant)
			{
				return AddOccupantString(DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.SlotFull));
			}
			DelayedActionInstance delayedActionInstance = CheckAllowedInside(interactable, human);
			if (delayedActionInstance.IsDisabled)
			{
				return delayedActionInstance;
			}
			if (GameManager.RunSimulation && doAction)
			{
				OnServer.MoveToSlot(human, SleeperSlot);
				OnServer.Interact(base.InteractOpen, 0);
				CheckConnections();
			}
			return new DelayedActionInstance
			{
				Duration = 0.5f,
				ActionMessage = ActionStrings.Insert,
				ExtendedMessage = AddOccupantString()?.ToString()
			}.Succeed();
		}
		if (SleeperSlot.IsNotEmpty())
		{
			if ((bool)occupant)
			{
				return AddOccupantString(DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.HandSlotOccupied));
			}
			if (GameManager.RunSimulation && doAction)
			{
				OnServer.MoveToWorld(SleeperSlot.Occupant, GetExitPosition(null), Quaternion.identity, Vector3.zero, Vector3.zero);
			}
			return new DelayedActionInstance
			{
				Duration = 0.5f,
				ActionMessage = ActionStrings.Take,
				ExtendedMessage = AddOccupantString()?.ToString()
			}.Succeed();
		}
		Human human2 = interaction.SourceThing as Human;
		if (human2 == null)
		{
			return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.CannotInteract);
		}
		DelayedActionInstance delayedActionInstance2 = CheckAllowedInside(interactable, human2);
		if (delayedActionInstance2.IsDisabled)
		{
			return delayedActionInstance2;
		}
		if (GameManager.RunSimulation && doAction)
		{
			OnServer.MoveToSlot(human2, SleeperSlot);
			OnServer.Interact(base.InteractOpen, 0);
			CheckConnections();
		}
		return new DelayedActionInstance
		{
			Duration = 0.5f,
			ActionMessage = ActionStrings.GetIn,
			ExtendedMessage = AddOccupantString()?.ToString()
		}.Succeed();
	}

	public virtual DelayedActionInstance CheckAllowedInside(Interactable interactable, Human human)
	{
		if (human?.Suit != null)
		{
			return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DeviceDoesNotAllowInternals, human.Suit.AsThing.ToTooltip());
		}
		if ((object)human?.HeadAsSpaceHelmet != null)
		{
			return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DeviceDoesNotAllowInternals, human.HeadAsSpaceHelmet.ToTooltip());
		}
		if ((object)human?.LeftHandSlot.Occupant != null)
		{
			return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DeviceDoesNotAllowItemsInHands, human.LeftHandSlot.Occupant.ToTooltip(), human.LeftHandSlot.ToTooltip());
		}
		if ((object)human?.RightHandSlot.Occupant != null)
		{
			return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DeviceDoesNotAllowItemsInHands, human.RightHandSlot.Occupant.ToTooltip(), human.RightHandSlot.ToTooltip());
		}
		return DelayedActionInstance.Success(string.Empty);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Open)
		{
			SetActivateEnabled();
		}
	}

	private void SetActivateEnabled()
	{
		foreach (Interactable interactable in Interactables)
		{
			if (interactable.Action == InteractableType.Activate)
			{
				interactable.Collider.enabled = IsOpen;
				break;
			}
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		if (previousChild != null)
		{
			base.OnChildExitInventory(previousChild);
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractOpen, 1);
			}
		}
	}

	public override void OnAtmosphericTick()
	{
		if (base.InternalAtmosphere == null || InputNetwork?.Atmosphere == null)
		{
			return;
		}
		AtmosphereHelper.Mix(base.InternalAtmosphere, IsOpen ? GetWorldAtmosphere() : InputNetwork.Atmosphere, AtmosphereHelper.MatterState.Gas);
		if (!IsOpen)
		{
			AtmosphereHelper.DrainLiquids(base.InternalAtmosphere, InputNetwork.Atmosphere, Chemistry.PipeVolume);
		}
		if (!OnOff || !Powered || base.PowerCable == null)
		{
			return;
		}
		if (InputNetwork != null && SleeperSlot.Occupant is Human human)
		{
			if (!Powered)
			{
				bool flag = false;
				if ((bool)base.PowerCable && base.PowerCable.CableNetwork.PowerTick != null)
				{
					flag = base.PowerCable.CableNetwork.PowerTick.Potential > 90f;
					SetPowered(flag).Forget();
				}
			}
			else if (human.DamageState.Stun + 10f >= 100f)
			{
				human.DamageState.Damage(ChangeDamageType.Set, 100f, DamageUpdateType.Stun);
			}
			else if (human.DamageState.Stun < 100f)
			{
				human.DamageState.Damage(ChangeDamageType.Increment, 10f, DamageUpdateType.Stun);
			}
		}
		if (base.InternalAtmosphere != null && base.InternalAtmosphere.IsAboveArmstrong() && base.InternalAtmosphere.GasMixture.Temperature <= _maxHeatKelvin && _maxHeatKelvin - base.InternalAtmosphere.GasMixture.Temperature > TemperatureKelvin.Zero)
		{
			MoleEnergy val = IdealGas.Energy(base.InternalAtmosphere.GasMixture.HeatCapacity, _maxHeatKelvin - base.InternalAtmosphere.GasMixture.Temperature);
			base.InternalAtmosphere.GasMixture.AddEnergy(RocketMath.Min(MaxEnergyPull, val));
			_powerUsedDuringTick = RocketMath.Min(MaxEnergyPull, val).ToFloat();
		}
		base.OnAtmosphericTick();
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (cableNetwork != base.PowerCableNetwork || base.PowerCableNetwork == null)
		{
			return 0f;
		}
		float num = 0f;
		if (!OnOff)
		{
			return num;
		}
		num += UsedPower;
		if (IsOpen)
		{
			num += _powerUsedDuringTick;
		}
		return num;
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.ReceivePower(cableNetwork, powerAdded + _powerUsedDuringTick);
		_powerUsedDuringTick = 0f;
	}

	private async UniTask SetPowered(bool powered)
	{
		if (GameManager.RunSimulation)
		{
			if (GameManager.IsThread)
			{
				await UniTask.SwitchToMainThread();
			}
			OnServer.Interact(base.InteractOnOff, powered ? 1 : 0);
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		_ = savedData is CryoTubeSaveData;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new CryoTubeSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		_ = saveData is CryoTubeSaveData;
	}
}
