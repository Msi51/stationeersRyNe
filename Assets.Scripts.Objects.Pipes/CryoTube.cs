using System;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using CharacterCustomisation;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class CryoTube : OccupantAtmospherics, ILifeSuspender, ICryogenicRegenerator
{
	[SerializeField]
	private Transform _exitPoint;

	private static readonly TemperatureKelvin _temperatureRequirement = new TemperatureKelvin(130.0);

	private static readonly VolumeLitres _volumeRequirement = new VolumeLitres(10.0);

	[SerializeField]
	protected InfoScreenComponent _infoScreen2;

	private static CryoTubeMask _maskPrefab;

	public GameBase Liquid;

	private Transform _cameraRig;

	public Transform CameraPoint;

	private static readonly TemperatureKelvin MAXTempDelta = new TemperatureKelvin(100.0);

	private const float HEAT_PUMP_EFFICIENCY = 100f;

	public CryoTubeMask Mask { get; private set; }

	public Slot MaskSlot => Slots[1];

	private Vector3 ExitPosition
	{
		get
		{
			if (!_exitPoint)
			{
				return Transform.position + Transform.forward * 1.5f;
			}
			return _exitPoint.position;
		}
	}

	private TemperatureKelvin InternalTemperature
	{
		get
		{
			if (!(base.InternalAtmosphere.TotalMoles > MoleQuantity.Zero))
			{
				return TemperatureKelvin.Zero;
			}
			return base.InternalAtmosphere.Temperature;
		}
	}

	private PressurekPa InternalPressure => base.InternalAtmosphere.PressureGassesAndLiquids;

	public bool LiquidTooHot
	{
		get
		{
			if (InputNetwork2?.Atmosphere == null)
			{
				return false;
			}
			return InputNetwork2.Atmosphere.Temperature > _temperatureRequirement;
		}
	}

	public bool LiquidVolumeTooLow
	{
		get
		{
			if (InputNetwork2?.Atmosphere == null)
			{
				return true;
			}
			return InputNetwork2.Atmosphere.TotalVolumeLiquids < _volumeRequirement;
		}
	}

	public bool NotPureLiquidNitrogen
	{
		get
		{
			if (InputNetwork2?.Atmosphere == null)
			{
				return true;
			}
			MoleQuantity quantity = InputNetwork2.Atmosphere.GasMixture.LiquidNitrogen.Quantity;
			MoleQuantity getTotalMolesLiquids = InputNetwork2.Atmosphere.GasMixture.GetTotalMolesLiquids;
			return quantity.ToDouble() / getTotalMolesLiquids.ToDouble() < 1.0;
		}
	}

	public bool WillRevive
	{
		get
		{
			if (Powered)
			{
				return HasLiquid;
			}
			return false;
		}
	}

	public bool HasLiquid
	{
		get
		{
			if (!LiquidTooHot && !LiquidVolumeTooLow)
			{
				return !NotPureLiquidNitrogen;
			}
			return false;
		}
	}

	private Atmosphere CoolantAtmosphere => InputNetwork2?.Atmosphere;

	protected override bool IsOperable
	{
		get
		{
			bool flag = base.IsInputValid && base.IsInput2Valid && WillRevive && !base.IsUnsafeAtmosphere;
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

	public bool IsSuspendingLife => IsOperable;

	public bool IsCryogenicActive => IsOperable;

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		if ((object)_maskPrefab == null)
		{
			_maskPrefab = Prefab.Find<CryoTubeMask>("ItemCryoMask");
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (_infoScreen2 != null)
		{
			_infoScreen2.RefreshState(this);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (MaskSlot.Contains<CryoTubeMask>(out var occupant))
		{
			Mask = occupant;
		}
		if (base.BedSlot.Contains<Human>(out var occupant2))
		{
			if (!Mask)
			{
				Mask = occupant2.HelmetSlot.Get<CryoTubeMask>();
			}
			if (!Mask)
			{
				ConsoleWindow.Print($"CryoTube.OnFinishedLoad: occupied by {occupant2.DisplayName} but no mask found, helmet slot has {occupant2.HelmetSlot.Get()}");
			}
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (!(newChild is Human human) || !GameManager.RunSimulation || !GameManager.IsRunning)
		{
			return;
		}
		if (!Mask)
		{
			Mask = MaskSlot.Get<CryoTubeMask>();
		}
		if (!Mask)
		{
			if ((object)_maskPrefab == null)
			{
				_maskPrefab = Prefab.Find<CryoTubeMask>("ItemCryoMask");
			}
			Mask = Thing.Create<CryoTubeMask>(_maskPrefab);
		}
		OnServer.MoveToSlot(Mask, human.HelmetSlot);
	}

	public override void OnChildExitInventory(DynamicThing oldChild)
	{
		base.OnChildExitInventory(oldChild);
		if (!(oldChild is Human human) || !GameManager.RunSimulation)
		{
			return;
		}
		CryoTubeMask cryoTubeMask = human.HelmetSlot.Get<CryoTubeMask>();
		if ((bool)cryoTubeMask)
		{
			Mask = cryoTubeMask;
		}
		if (!Mask)
		{
			return;
		}
		CryoTubeMask cryoTubeMask2 = MaskSlot.Get<CryoTubeMask>();
		if (!(cryoTubeMask2 == Mask))
		{
			if (cryoTubeMask2 != null)
			{
				cryoTubeMask2.DestroyItem();
				MaskSlot.Empty();
			}
			if (!Mask.MoveToSlot(MaskSlot, this))
			{
				ConsoleWindow.Print($"CryoTube mask return failed: occupant={MaskSlot.Get()}, canEnter={Mask.CanEnter(MaskSlot).Reason}");
			}
		}
	}

	public override DelayedActionInstance CheckAllowedInside(Interactable interactable, Human human)
	{
		if ((object)human == null)
		{
			throw new NullReferenceException("error human is null entering cryo tube");
		}
		if (human.SpeciesClass == SpeciesClass.Robot)
		{
			return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DeviceNotForSpecies, EnumCollections.Species.GetName(human.SpeciesClass));
		}
		if (human.HelmetSlot.Contains<Item>(out var occupant))
		{
			return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DeviceDoesNotAllowInternals, occupant.ToTooltip());
		}
		if (human.SuitSlot.Contains<Item>(out var occupant2))
		{
			return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DeviceDoesNotAllowInternals, occupant2.ToTooltip());
		}
		if (human.UniformSlot.Contains<Item>(out var occupant3))
		{
			return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DeviceDoesNotAllowInternals, occupant3.ToTooltip());
		}
		if (human.ToolbeltSlot.Contains<Item>(out var occupant4))
		{
			return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DeviceDoesNotAllowInternals, occupant4.ToTooltip());
		}
		if (human.BackpackSlot.Contains<Item>(out var occupant5))
		{
			return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DeviceDoesNotAllowInternals, occupant5.ToTooltip());
		}
		if (human.GlassesSlot.Contains<Item>(out var occupant6))
		{
			return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DeviceDoesNotAllowInternals, occupant6.ToTooltip());
		}
		return base.CheckAllowedInside(interactable, human);
	}

	public override Vector3 GetExitPosition(Entity entity)
	{
		return ExitPosition;
	}

	public override Transform GetCameraPoint(Entity entity)
	{
		return CameraPoint;
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			Atmosphere atmosphere = (base.InternalAtmosphere = new Atmosphere(this, new VolumeLitres(800.0), 0L));
		}
	}

	public override void Update100MS(float deltaTime)
	{
		base.Update100MS(deltaTime);
		Liquid.SetActive(HasLiquid);
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		Tooltip.ToolTipStringBuilder.Clear();
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (passiveTooltip.Title.Equals(string.Empty))
		{
			passiveTooltip.Title = DisplayName;
		}
		if (Powered)
		{
			if (_infoScreen != null && hitCollider == _infoScreen.InfoTrigger)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine(GameStrings.CryoState.AsColor("white"));
				AddErrorStateStrings(stringBuilder, indent: true);
				if (WillRevive)
				{
					stringBuilder.Append(StringManager.Indent);
					stringBuilder.AppendLine(GameStrings.CryoWillRevive.AsColor("green"));
					stringBuilder.Append(StringManager.Indent);
					stringBuilder.AppendLine(GameStrings.CryoWillHeal.AsColor("green"));
				}
				stringBuilder.AppendLine(GameStrings.CryogenicLiquid.AsColor("white"));
				AtmosphericsManager.MakeGasTooltip(InputNetwork2?.Atmosphere, stringBuilder, indent: true);
				stringBuilder.AppendLine(GameStrings.BreathingAtmosphere.AsColor("white"));
				AtmosphericsManager.MakeGasTooltip(base.InternalAtmosphere, stringBuilder, indent: true);
				passiveTooltip.Extended = stringBuilder.ToString();
			}
			else if (_infoScreen2 != null && hitCollider == _infoScreen2.InfoTrigger)
			{
				StringBuilder stringBuilder2 = new StringBuilder();
				stringBuilder2.AppendLine(GameStrings.SlotOccupant.AsColor("white"));
				AddOccupantString(stringBuilder2, healthInfo: true, indent: true, alwaysShow: true, organInfo: true);
				passiveTooltip.Extended = stringBuilder2.ToString();
			}
		}
		return passiveTooltip;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (Error == 1)
		{
			extendedText.AppendLine(GameStrings.CryoConditionsNotMet.AsColor("red"));
		}
		return extendedText;
	}

	private void AddErrorStateStrings(StringBuilder sb, bool indent = false)
	{
		if (InputNetwork == null)
		{
			if (indent)
			{
				sb.Append(StringManager.Indent);
			}
			sb.AppendLine(GameStrings.CryoNoAtmosphericInput.AsColor("red"));
		}
		if (IsOpen)
		{
			if (indent)
			{
				sb.Append(StringManager.Indent);
			}
			sb.AppendLine(GameStrings.SleeperDoorIsOpen.AsColor("red"));
		}
		if (base.IsUnsafeAtmosphere)
		{
			if (indent)
			{
				sb.Append(StringManager.Indent);
			}
			sb.AppendLine(GameStrings.CryoAtmosphereIsUnsafe.AsColor("red"));
		}
		if (InputNetwork2 == null)
		{
			if (indent)
			{
				sb.Append(StringManager.Indent);
			}
			sb.AppendLine(GameStrings.CryoNoLiquidInput.AsColor("red"));
		}
		if (LiquidTooHot)
		{
			if (indent)
			{
				sb.Append(StringManager.Indent);
			}
			sb.AppendLine(GameStrings.CryoLiquidTemperatureTooHigh.AsString(_temperatureRequirement.ToFloat().ToStringPrefix("K")).AsColor("red"));
		}
		if (LiquidVolumeTooLow)
		{
			if (indent)
			{
				sb.Append(StringManager.Indent);
			}
			sb.AppendLine(GameStrings.CryoLiquidVolumeTooLow.AsString(_volumeRequirement.ToFloat().ToStringPrefix("L")).AsColor("red"));
		}
		if (NotPureLiquidNitrogen)
		{
			if (indent)
			{
				sb.Append(StringManager.Indent);
			}
			sb.AppendLine(GameStrings.NotPureLiquidNitrogen.AsColor("red"));
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		Liquid?.SetVisible(WillRevive);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Activate)
		{
			return InteractWithActivate(interactable, interaction, doAction);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	private DelayedActionInstance InteractWithActivate(Interactable interactable, Interaction interaction, bool doAction)
	{
		if (interaction.SourceSlot.Contains<DynamicBodyBag>(out var occupant))
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0.5f,
				ActionMessage = ActionStrings.Insert
			};
			if (base.SleeperSlot.IsNotEmpty())
			{
				return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.SlotFull);
			}
			if (occupant.PlayerHasRespawned)
			{
				return delayedActionInstance.Fail(GameStrings.PlayerHasAlreadyRespawned.DisplayString);
			}
			if (!GameManager.RunSimulation || !doAction)
			{
				return delayedActionInstance.Succeed();
			}
			Brain brain = occupant.BrainSlot.Get<Brain>();
			Lungs lungs = occupant.LungsSlot.Get<Lungs>();
			Stomach stomach = occupant.StomachSlot.Get<Stomach>();
			MoveBodyBagToSlot(brain, lungs, stomach, occupant, base.SleeperSlot);
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	private void MoveBodyBagToSlot(Brain brain, Lungs lungs, Stomach stomach, DynamicBodyBag bodyBag, Slot slot)
	{
		GameManager.ClientInfo.TryGetValue(brain.ClientId, out var value);
		StartLocationData startLocation = ((value != null) ? DataCollection.Get<StartLocationData>(value.StartLocationHash) : null);
		Human human = Human.CreateEmptyHuman(slot, startLocation);
		string text = Client.Find(brain.ClientId)?.name ?? "Unknown";
		OnServer.SetCustomName(human, text);
		human.SetBasicsData(brain.ClientId, text);
		human.CosmeticData.Copy(bodyBag.BodyCosmeticData);
		human.UpdateCosmeticIdentity();
		human.DamageState.Copy(bodyBag.BodyDamageState);
		OnServer.MoveToSlot(brain, human.BrainSlot);
		OnServer.MoveToSlot(lungs, human.LungsSlot);
		OnServer.MoveToSlot(stomach, human.StomachSlot);
		if (!human.OrganLungs)
		{
			human.CreateLungs();
		}
		if (!human.OrganStomach)
		{
			human.CreateStomach();
		}
		human.State = EntityState.Dead;
		OnServer.Destroy(bodyBag);
		OnServer.Interact(base.InteractOpen, 0);
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (IsOpen || !Powered)
		{
			return;
		}
		try
		{
			if (base.InternalAtmosphere != null && CoolantAtmosphere != null)
			{
				float num = 1f;
				if (InputNetwork2.Atmosphere.Temperature < InternalTemperature && InternalTemperature > Chemistry.Temperature.ZeroDegrees)
				{
					num = ((MAXTempDelta - (CoolantAtmosphere.Temperature - InternalTemperature)) / MAXTempDelta).ToFloat();
					num = Mathf.Clamp(num, 0.01f, 1f);
				}
				MoleEnergy energy = new MoleEnergy(100f * num);
				CoolantAtmosphere.GasMixture.AddEnergy(base.InternalAtmosphere.GasMixture.RemoveEnergy(energy));
			}
			if (!WillRevive || base.BedSlot.IsEmpty() || !base.BedSlot.Contains<Human>(out var occupant))
			{
				return;
			}
			if (occupant.DamageState is EntityDamageState entityDamageState)
			{
				entityDamageState.CryoHeal(HealAmount);
			}
			foreach (Organ organ in occupant.Organs)
			{
				if (organ.DamageState.TotalRounded > 0)
				{
					organ.DamageState.Heal(HealAmount);
				}
			}
			if (CanRevive(occupant))
			{
				ReviveOccupant(occupant);
			}
		}
		catch (Exception ex)
		{
			if (!(ex is NullReferenceException))
			{
				ConsoleWindow.PrintError(ex).Forget();
			}
		}
	}

	private bool CanRevive(Human human)
	{
		if (human.State == EntityState.Dead && (bool)human.OrganBrain)
		{
			return human.OrganBrain.ClientId != 0;
		}
		return false;
	}

	private void ReviveOccupant(Human human)
	{
		float quantity = human.DamageState.MaxDamage * 0.25f;
		if (human.DamageState is EntityDamageState entityDamageState)
		{
			entityDamageState.CryoHeal(quantity);
		}
		foreach (Organ organ in human.Organs)
		{
			organ.DamageState.Heal(quantity);
		}
		human.DamageState.Damage(ChangeDamageType.Set, human.DamageState.MaxDamage, DamageUpdateType.Stun);
		human.State = EntityState.Unconscious;
	}

	public override void AssessError()
	{
		bool flag = !base.IsInputValid || !base.IsInput2Valid || !WillRevive || base.IsUnsafeAtmosphere;
		OutputNetwork = InputNetwork;
		if (GameManager.RunSimulation && HasErrorState && Error == 0 && flag)
		{
			if (Error != 1)
			{
				OnServer.Interact(base.InteractError, 1);
			}
		}
		else if (GameManager.RunSimulation && HasErrorState && Error == 1 && !flag && Error != 0)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			if (GameManager.RunSimulation && !IsCursor && Mask != null)
			{
				Mask.DestroyItem();
			}
			base.OnDestroy();
			base.BeingDestroyed = true;
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Temperature => true, 
			LogicType.Pressure => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Temperature => InternalTemperature.ToDouble(), 
			LogicType.Pressure => InternalPressure.ToDouble(), 
			_ => base.GetLogicValue(logicType), 
		};
	}
}
