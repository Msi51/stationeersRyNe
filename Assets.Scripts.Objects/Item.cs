using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Items;
using Objects.RoboticArm;
using Reagents;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class Item : DynamicThing, IPerishable, IDensePoolable, ITradable, IEvaluable
{
	[Serializable]
	public class DecayMixture
	{
		public Chemistry.GasType Gas;

		public float DecayRateMultiplier;
	}

	public delegate void OnItemCreatedEvent(Item item);

	public static readonly Vector3 ChildOffsetFacing = new Vector3(135f, 100f, 90f);

	[ReadOnly]
	public ReplacementTool ReplacementTool;

	private static readonly Dictionary<int, Item> _itemReplacementBySubstitute = new Dictionary<int, Item>();

	private static readonly Dictionary<int, List<Item>> _replacementsByTarget = new Dictionary<int, List<Item>>();

	private const int MAX_DECAYING_ITEMS = 4096;

	public static readonly DensePool<IPerishable> AllDecayingItems = new DensePool<IPerishable>("AllDecayingItems", 4096);

	[Header("Item")]
	public float InventoryScale = 0.5f;

	[Tooltip("Local Transform position offset when in hands")]
	public Vector3 LocalOffSetInHand;

	[Tooltip("Local Transform rotation when in hands")]
	public Vector3 LocalRotationInHand;

	[Tooltip("Will Force Ik on Hands")]
	public bool PrecisionIk;

	public ReagentMixture CreatedReagentMixture;

	public bool AllowSelfUse;

	public bool AllowForwardCursor;

	public float TraderUniqueIdentifierRatio;

	public static readonly int EatingHash = Animator.StringToHash("Eating");

	public static readonly int EatingFinishedHash = Animator.StringToHash("EatingFinished");

	public static readonly int WaterBottleFillHash = Animator.StringToHash("WaterBottleFill");

	public static readonly int DrinkingHash = Animator.StringToHash("Drinking");

	public static readonly int DrinkingFinishedHash = Animator.StringToHash("DrinkingFinished");

	private static readonly int EquipSoundDefaultHash = Animator.StringToHash("EquipWelder");

	private static readonly int UnEquipSoundDefaultHash = Animator.StringToHash("UnEquipWelder");

	public static Item LastUsedItem = null;

	public const float RENDER_DISTANCE = 15f;

	public const float SHADOW_DISTANCE = 4f;

	[Header("Decaying")]
	public bool CanDecay;

	public float DecayRate = 0.00018f;

	private float _currentDecayRate = 1f;

	[ReadOnly]
	public bool IsDecayed;

	[ReadOnly]
	public float TemperatureDecayRateMultiplier = 1f;

	[ReadOnly]
	public float PressureDecayRateMultiplier = 1f;

	private static readonly AnimationCurve DefaultTemperatureDecayCurve = new AnimationCurve(new Keyframe(0f, 0.2f, -0.0040807934f, -0.0040807934f, 0f, 0.50074965f), new Keyframe(1f, 1.8f, 0.0016072573f, 0.0016072573f, 0.49901164f, 0f));

	public AnimationCurve TemperatureDecayRateMultiplierCurve = DefaultTemperatureDecayCurve;

	public AnimationCurve NutritionDecayRateCurve = new AnimationCurve(new Keyframe(0f, 0.1f, 0f, 0f, 0f, 0.333333f), new Keyframe(0.1f, 0.1f, 0.00993201f, 0.9197401f, 0.4321137f, 0.07177715f), new Keyframe(1f, 1f, 1f, 1f, 0.3f, 0f));

	private static readonly AnimationCurve DefaultPressureDecayCurve = new AnimationCurve(new Keyframe(0f, 5f, -12.372469f, -12.372469f, 1f / 3f, 0.027380947f), new Keyframe(1f, 1f, 0.008049617f, 0.008049617f, 0.6991061f, 1f / 3f));

	public AnimationCurve PressureDecayRateMultiplierCurve = DefaultPressureDecayCurve;

	public DecayMixture[] GasesDecayMultiplier = new DecayMixture[6]
	{
		new DecayMixture
		{
			DecayRateMultiplier = 1.5f,
			Gas = Chemistry.GasType.Oxygen
		},
		new DecayMixture
		{
			DecayRateMultiplier = 0.6f,
			Gas = Chemistry.GasType.Nitrogen
		},
		new DecayMixture
		{
			DecayRateMultiplier = 0.8f,
			Gas = Chemistry.GasType.CarbonDioxide
		},
		new DecayMixture
		{
			DecayRateMultiplier = 3f,
			Gas = Chemistry.GasType.Pollutant
		},
		new DecayMixture
		{
			DecayRateMultiplier = 1.5f,
			Gas = Chemistry.GasType.NitrousOxide
		},
		new DecayMixture
		{
			DecayRateMultiplier = 2f,
			Gas = Chemistry.GasType.Steam
		}
	};

	public DecayedFood DecayedFoodPrefab;

	private const float GLOBAL_DECAY_SCALE = 0.25f;

	private readonly float _maxTemperatureDecayKelvin = 500f;

	private readonly float _minTemperatureDecayKelvin;

	private readonly float _maxPressureDecayKpa = 101f;

	private readonly float _minPressureDecayKpa;

	private readonly DensePoolReference<IPerishable> _perishablePool = new DensePoolReference<IPerishable>(AllDecayingItems);

	public static readonly Comparison<Item> SmartSortItems = delegate(Item a, Item b)
	{
		int num = string.Compare(EnumCollections.SlotClasses.GetName(a.SlotType), EnumCollections.SlotClasses.GetName(b.SlotType), StringComparison.Ordinal);
		int num2 = string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
		int num3 = b.GetQuantity.CompareTo(a.GetQuantity);
		if (num != 0)
		{
			return num;
		}
		return (num2 != 0) ? num2 : num3;
	};

	public Item ReplacementOf
	{
		get
		{
			if (!ReplacementTool)
			{
				if (!_itemReplacementBySubstitute.TryGetValue(PrefabHash, out var value))
				{
					return null;
				}
				return value;
			}
			return ReplacementTool.PrefabToReplace;
		}
	}

	public ReagentMixture AddMixture => CreatedReagentMixture;

	public virtual float QuantityPerUse => 1f;

	public virtual int ConstructingSoundHash => 0;

	public virtual int FinishedConstructingSoundHash => 0;

	public virtual int FinishedDeconstructingSoundHash => 0;

	public override int EquipSoundHash => EquipSoundDefaultHash;

	public override int UnEquipSoundHash => UnEquipSoundDefaultHash;

	public int TimeToDecayFromNowInSeconds { get; private set; } = 1;

	[ByteArraySync]
	public float CurrentDecayRate
	{
		get
		{
			return _currentDecayRate;
		}
		set
		{
			if (!RocketMath.Approximately(value, CurrentDecayRate))
			{
				_currentDecayRate = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 2048;
				}
			}
		}
	}

	public virtual float GetQuantity => 1f;

	public float GetTradableQuantity => GetQuantity;

	public static event Event OnUseItemEvent;

	public static event OnItemCreatedEvent OnItemCreated;

	public static event Event OnItemDestroyed;

	public static IReadOnlyList<Item> GetReplacements(int prefabHash)
	{
		if (!_replacementsByTarget.TryGetValue(prefabHash, out var value))
		{
			return null;
		}
		return value;
	}

	public static void ClearReplacements()
	{
		_itemReplacementBySubstitute.Clear();
		_replacementsByTarget.Clear();
	}

	public static void RegisterItemReplacement(WorldManager.ItemReplacement replacement, ModAbout mod)
	{
		string text = mod?.Name ?? "Core";
		if (!(Prefab.Find(replacement.Substitute) is Item item))
		{
			ConsoleWindow.PrintError("'" + text + "' item replacement skipped: substitute '" + replacement.Substitute + "' is not a known item prefab");
			return;
		}
		if (!(Prefab.Find(replacement.Replaces) is Item item2))
		{
			ConsoleWindow.PrintError("'" + text + "' item replacement skipped: replaced item '" + replacement.Replaces + "' is not a known item prefab");
			return;
		}
		if (item.PrefabHash == item2.PrefabHash)
		{
			ConsoleWindow.PrintError("'" + text + "' item replacement skipped: '" + replacement.Substitute + "' cannot replace itself");
			return;
		}
		if ((bool)item.ReplacementTool)
		{
			ConsoleWindow.PrintError("'" + text + "' item replacement skipped: '" + replacement.Substitute + "' already has a ReplacementTool component, which takes precedence");
			return;
		}
		Item item3 = item2;
		int num = 0;
		while ((bool)item3 && num++ < 32)
		{
			if (item3.PrefabHash == item.PrefabHash)
			{
				ConsoleWindow.PrintError("'" + text + "' item replacement skipped: '" + replacement.Substitute + "' replacing '" + replacement.Replaces + "' would create a replacement loop");
				return;
			}
			item3 = item3.ReplacementOf;
		}
		if (_itemReplacementBySubstitute.TryGetValue(item.PrefabHash, out var value))
		{
			ConsoleWindow.PrintAction("'" + text + "' patching item replacement: '" + item.PrefabName + "' now replaces '" + item2.PrefabName + "' instead of '" + value.PrefabName + "'");
			if (_replacementsByTarget.TryGetValue(value.PrefabHash, out var value2))
			{
				value2.Remove(item);
			}
		}
		_itemReplacementBySubstitute[item.PrefabHash] = item2;
		AddReplacementForTooltip(item, item2);
	}

	private static void AddReplacementForTooltip(Item substitute, Item target)
	{
		if (!_replacementsByTarget.TryGetValue(target.PrefabHash, out var value))
		{
			_replacementsByTarget.Add(target.PrefabHash, value = new List<Item>());
		}
		if (!value.Contains(substitute))
		{
			value.Add(substitute);
		}
	}

	public override void OnAllPrefabsLoaded()
	{
		base.OnAllPrefabsLoaded();
		if ((bool)ReplacementOf)
		{
			AddReplacementForTooltip(this, ReplacementOf);
		}
	}

	public virtual ReagentMixture GetTotalReagentMixture()
	{
		return CreatedReagentMixture;
	}

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(15f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(4f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public virtual bool OnUseItem(float quantity, Thing useOnThing)
	{
		Human human = useOnThing as Human;
		if ((bool)human && this is INutrition food)
		{
			float eatAmount = Mathf.Min(human.GetNutritionStorage() - human.Nutrition, quantity);
			human.OnConsumeFood(eatAmount, food);
		}
		LastUsedItem = this;
		if (Item.OnUseItemEvent != null)
		{
			Item.OnUseItemEvent();
		}
		return true;
	}

	public virtual void SetHandPosition(bool leftHand)
	{
		Vector3 localRotationInHand = LocalRotationInHand;
		localRotationInHand.x = 0f - localRotationInHand.x;
		localRotationInHand.y = 0f - localRotationInHand.y;
		Vector3 localOffSetInHand = LocalOffSetInHand;
		localOffSetInHand.z *= -1f;
		base.ThingTransformLocalPosition = (leftHand ? LocalOffSetInHand : localOffSetInHand);
		base.ThingTransformLocalRotationEuler = (leftHand ? LocalRotationInHand : localRotationInHand);
	}

	public virtual float CalculateUniqueRatioIdentifier()
	{
		return TraderUniqueIdentifierRatio;
	}

	public override void Smelt(Atmosphere localAtmosphere, ReagentMixture reagentMixture)
	{
		if (localAtmosphere.IsGlobalAtmosphere)
		{
			AtmosphericEventInstance.CloneGlobalRemoveEnergy(base.WorldGrid, IdealGas.Energy(CreatedReagentMixture.HeatCapacity, base.FlashPointTemperature));
		}
		else
		{
			AtmosphericEventInstance.CreateRemoveEnergy(localAtmosphere, IdealGas.Energy(CreatedReagentMixture.HeatCapacity, base.FlashPointTemperature));
		}
		base.Smelt(localAtmosphere, reagentMixture);
	}

	public virtual void OnUsePrimary(Vector3 targetLocation, Quaternion targetRotation, ulong steamId, bool authoringMode)
	{
	}

	public virtual void OnPowerTick()
	{
	}

	public virtual bool CanItemDecay()
	{
		if (!IsDecayed)
		{
			return CanDecay;
		}
		return false;
	}

	public void OnDecayServer(float secondsDelay)
	{
		float num = DecayRate;
		if (base.WorldAtmosphere != null)
		{
			float time = NormalizeValues(base.WorldAtmosphere.Temperature.ToFloat(), _maxTemperatureDecayKelvin, _minTemperatureDecayKelvin);
			float time2 = NormalizeValues(base.WorldAtmosphere.PressureGassesAndLiquids.ToFloat(), _maxPressureDecayKpa, _minPressureDecayKpa);
			TemperatureDecayRateMultiplier = TemperatureDecayRateMultiplierCurve.Evaluate(time);
			PressureDecayRateMultiplier = PressureDecayRateMultiplierCurve.Evaluate(time2);
			DecayMixture[] gasesDecayMultiplier = GasesDecayMultiplier;
			foreach (DecayMixture decayMixture in gasesDecayMultiplier)
			{
				float num2 = Mathf.Clamp01(base.WorldAtmosphere.GetGasTypeRatio(decayMixture.Gas));
				if (!(num2 <= float.Epsilon))
				{
					float num3 = num * num2;
					num -= num3;
					num += num3 * decayMixture.DecayRateMultiplier;
				}
			}
		}
		else
		{
			TemperatureDecayRateMultiplier = 1f;
			PressureDecayRateMultiplier = PressureDecayRateMultiplierCurve.Evaluate(0f);
		}
		num *= TemperatureDecayRateMultiplier;
		num *= PressureDecayRateMultiplier;
		num *= 0.25f;
		num *= (float)DifficultySetting.Current.FoodDecayRate;
		num *= GetDecayScale();
		UpdateDecayTimes();
		if ((double)Math.Abs(CurrentDecayRate - num) > 1E-06)
		{
			CurrentDecayRate = num;
		}
		float value = num * 60f * secondsDelay;
		DamageState.Damage(ChangeDamageType.Increment, value, DamageUpdateType.Decay);
	}

	public void OnDecayClient(float secondsDelay)
	{
		UpdateDecayTimes();
	}

	public float GetDecay()
	{
		return DamageState.Decay;
	}

	public virtual float GetDecayScale()
	{
		return 1f;
	}

	private void UpdateDecayTimes()
	{
		TimeToDecayFromNowInSeconds = (int)((DamageState.MaxDamage - DamageState.Decay) / (CurrentDecayRate * 60f));
	}

	public override void InitializeDamageState()
	{
		DamageState = new OrganicDamageState(this);
	}

	public async UniTask SetDecayedAsync()
	{
		await UniTask.SwitchToMainThread();
		CreateDecayFood();
		IsDecayed = true;
	}

	private void CreateDecayFood(bool replaceSlot = true)
	{
		DecayedFood decayedFood = OnServer.Create<DecayedFood>(DecayedFoodPrefab, base.Position, Rotation);
		if (!(this is Plant { IsPlanted: not false }))
		{
			if (this is Stackable stackable)
			{
				decayedFood.SetQuantity(stackable.Quantity);
			}
			else
			{
				decayedFood.SetQuantity(1);
			}
			if (replaceSlot && base.ParentSlot != null)
			{
				Slot parentSlot = base.ParentSlot;
				OnServer.MoveToWorld(this);
				OnServer.MoveToSlot(decayedFood, parentSlot);
			}
		}
	}

	private static float NormalizeValues(float val, float max, float min)
	{
		return Mathf.Clamp01((val - min) / (max - min));
	}

	public virtual DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletedRatio = 1f)
	{
		return null;
	}

	public static void ClearEvents()
	{
		Item.OnItemCreated = null;
		Item.OnItemDestroyed = null;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (!(this is Stackable stackable))
		{
			if (this is Consumable)
			{
				extendedText.AppendLine(GameStrings.ItemInSlotValue.AsString(ToTooltip(), GetQuantityText()));
			}
		}
		else
		{
			extendedText.AppendLine(GameStrings.ItemInSlotStack.AsString(ToTooltip(), StringManager.Get(stackable.Quantity)));
		}
		if (CanDecay)
		{
			int timeToDecayFromNowInSeconds = TimeToDecayFromNowInSeconds;
			if (timeToDecayFromNowInSeconds < int.MaxValue && timeToDecayFromNowInSeconds > 0)
			{
				extendedText.AppendLine(GameStrings.ThingWillDecayIn.AsString(StringManager.GetFormattedTime(TimeToDecayFromNowInSeconds)));
			}
			if (base.ParentSlot?.Parent is FoodContainer { DecayScale: <1f } foodContainer)
			{
				string arg = (foodContainer.DecayScale * 100f).ToStringPercent("green");
				extendedText.AppendLine(GameStrings.FoodContainerDecayReduction.AsString(arg, foodContainer.ToTooltip()));
			}
		}
		if (base.ParentSlot != null)
		{
			if (this is IIngredientLabelled ingredientLabelled)
			{
				string value = CreatedReagentMixture.ToString(GetQuantity / ingredientLabelled.QuantityPerUse);
				if (!string.IsNullOrEmpty(value))
				{
					extendedText.Append(value);
				}
			}
			if (this is INutrition nutrition)
			{
				float nutritionalValue = nutrition.GetNutritionalValue();
				if (nutritionalValue > 0f)
				{
					extendedText.AppendLine(GameStrings.TradeItemNutrition.AsString(nutritionalValue.ToStringRounded("yellow")));
				}
			}
		}
		if (this is INutrition nutrition2 && nutrition2.GetNutritionalValue() > 0f)
		{
			Food.AppendFoodQualityText(extendedText, nutrition2);
		}
		return extendedText;
	}

	public override void Awake()
	{
		base.Awake();
		if (CanDecay)
		{
			AllDecayingItems.Add(this);
		}
		Item.OnItemCreated?.Invoke(this);
	}

	public override bool OnAddToPool(object densePool, int slot)
	{
		if (_perishablePool.CanAddToPool(densePool))
		{
			return _perishablePool.AddToPool(densePool, slot);
		}
		return base.OnAddToPool(densePool, slot);
	}

	public override void OnRemoveFromPool(object densePool)
	{
		base.OnRemoveFromPool(densePool);
		_perishablePool.OnRemovedFrom(densePool);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		base.RegisteredCollector?.DeRegisterItem(this);
		if (CanDecay)
		{
			AllDecayingItems.Remove(this);
		}
		Item.OnItemDestroyed?.Invoke();
	}

	public override void OnDamageDestroyed()
	{
		if (GameManager.RunSimulation)
		{
			DestroyItem();
		}
	}

	public virtual void DestroyItemAtZero()
	{
		DestroyItem();
	}

	public void DestroyItem()
	{
		if (ThreadedManager.IsThread)
		{
			DestroyFromThread().Forget();
		}
		else if (base.gameObject != null && DeleteOnDestroyed)
		{
			OnServer.Destroy(this);
		}
	}

	public override async UniTaskVoid DestroyFromThread()
	{
		if (GameManager.GameState == GameState.Running && DeleteOnDestroyed)
		{
			await UniTask.SwitchToMainThread();
			DestroyItem();
		}
	}

	public override object GetModXmlType()
	{
		return new ItemModData();
	}

	public override void DeserializModData(ThingModData modData)
	{
		base.DeserializModData(modData);
		if (modData is ItemModData itemModData)
		{
			if (!float.IsNaN(itemModData.InventoryScale))
			{
				InventoryScale = itemModData.InventoryScale;
			}
			if (itemModData.CreatedReagentMixture != null)
			{
				CreatedReagentMixture = itemModData.CreatedReagentMixture;
			}
		}
	}

	public string GetToolString(int quantity)
	{
		if (this is IQuantity)
		{
			return "<color=yellow>" + StringManager.Get(quantity) + "</color> x " + ToStationpediaLink();
		}
		return ToStationpediaLink();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			writer.WriteSingle(CurrentDecayRate);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			CurrentDecayRate = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(CurrentDecayRate);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		CurrentDecayRate = reader.ReadSingle();
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		base.RegisteredCollector?.DeRegisterItem(this);
		GameManager.SetInactive(this);
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		GameManager.SetActive(this);
	}

	public override void CheckForCollectorProximity()
	{
		ICollector collector = null;
		float num = float.MaxValue;
		foreach (ICollector allCollector in RoboticArmDockCollector.AllCollectors)
		{
			float num2 = Vector3.SqrMagnitude(allCollector.CollectionPosition - base.Position);
			if (num2 < num && (base.Room == allCollector.Room || (base.Room == null && allCollector.Room == null) || num2 < 1f))
			{
				num = num2;
				collector = allCollector;
			}
		}
		if (collector != base.RegisteredCollector || (collector != null && num > collector.RegistrationSquareDistance))
		{
			base.RegisteredCollector?.DeRegisterItem(this);
		}
		if (collector != null && num <= collector.RegistrationSquareDistance)
		{
			collector?.RegisterItem(this);
		}
		else
		{
			base.RegisteredCollector = null;
		}
	}

	public virtual bool TrySetCreatedReagentMixture(ReagentMixture reagentMixture)
	{
		if (CreatedReagentMixture == null)
		{
			return false;
		}
		CreatedReagentMixture.Set(reagentMixture);
		return true;
	}

	public float GetGasTypeRatio(Chemistry.GasType gasType)
	{
		if (base.InternalAtmosphere == null)
		{
			return 0f;
		}
		return base.InternalAtmosphere.GasMixture.GetGasTypeRatio(gasType);
	}

	public MoleQuantity GetGasMoles(Chemistry.GasType gasType)
	{
		if (base.InternalAtmosphere == null)
		{
			return MoleQuantity.Zero;
		}
		return base.InternalAtmosphere.GasMixture.GetGasMoles(gasType);
	}

	public TemperatureKelvin GetTemperatureK()
	{
		if (base.InternalAtmosphere == null)
		{
			return TemperatureKelvin.Zero;
		}
		return base.InternalAtmosphere.Temperature;
	}

	public MoleQuantity GetTotalMoles()
	{
		if (base.InternalAtmosphere == null)
		{
			return MoleQuantity.Zero;
		}
		return base.InternalAtmosphere.TotalMoles;
	}
}
