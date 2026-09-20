using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects.Entities;

public class Chicken : Animal
{
	[Header("Chicken")]
	public FertilizedEgg FertilizedEggPrefab;

	public DynamicThing EggPrefab;

	public Transform EggSpawn;

	public static List<Chicken> AllChickens = new List<Chicken>();

	public const uint MAX_NUMBER_OF_CHICKENS = 100u;

	private float _eggTimeDefault = GameManager.TicksPerDay;

	private float _layEggTimer = GameManager.TicksPerDay;

	public override float BaseNutritionStorage => 25f;

	public float NutritionNeededToLayEgg => BaseNutritionStorage * 0.75f;

	private float _eggTimeDefaultAfterNeedingFood => _eggTimeDefault * 0.2f;

	public override Lungs LungsPrefab => Prefab.Organ.LungsChicken;

	public override void Awake()
	{
		base.Awake();
		AllChickens.Add(this);
		RigRoot = base.transform.Find("Root");
		if (GameManager.RunSimulation)
		{
			_eggTimeDefault = _layEggTimer;
			_layEggTimer = Random.Range(_layEggTimer * 0.5f, _layEggTimer);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		AllChickens.Remove(this);
	}

	protected override void LifeNutrition()
	{
		if (!IsArtificial)
		{
			float num = BaseNutritionStorage / GameManager.TicksPerDay;
			base.Nutrition -= num;
			base.LifeNutrition();
		}
	}

	public override void HandleStateTimer()
	{
		base.HandleStateTimer();
		if (_layEggTimer > 0f)
		{
			_layEggTimer -= 1f;
		}
	}

	private void LayEgg()
	{
		if (GameManager.RunSimulation)
		{
			DynamicThing dynamicThing = OnServer.CreateOld(FertilizedEggPrefab, EggSpawn.position, Quaternion.identity, 0uL);
			dynamicThing.RigidBody.AddForce(-ThingTransform.forward);
			dynamicThing.RigidBody.AddTorque(Random.onUnitSphere);
			_layEggTimer = Random.Range(_eggTimeDefault * 0.75f, _eggTimeDefault * 1.25f);
			base.Nutrition -= NutritionNeededToLayEgg / 2f;
			OnServer.Interact(base.InteractOnOff, 0);
		}
	}

	public override void PerformState()
	{
		base.PerformState();
		if (_layEggTimer <= 0.1f)
		{
			if (base.Nutrition < NutritionNeededToLayEgg)
			{
				if (Plant.AllEdibles.Count >= 1)
				{
					Animalstate = AnimalStateEnum.Eat;
					RegisterFoodPathFindingTask();
					_layEggTimer = _eggTimeDefaultAfterNeedingFood;
					StateTimer = 8f;
				}
			}
			else
			{
				OnServer.Interact(base.InteractOnOff, 1);
				if (!RigidBody.isKinematic)
				{
					RigidBody.velocity = Vector3.zero;
				}
				PerformBaseState = false;
			}
		}
		else
		{
			PerformBaseState = true;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new ChickenSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		ChickenSaveData chickenSaveData = savedData as ChickenSaveData;
		_eggTimeDefault = ((!float.IsNaN(chickenSaveData.EggTimeDefault)) ? chickenSaveData.EggTimeDefault : 1200f);
		_layEggTimer = chickenSaveData.LayEggTimer;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		ChickenSaveData chickenSaveData = savedData as ChickenSaveData;
		if (GameManager.GameState != GameState.None)
		{
			chickenSaveData.EggTimeDefault = _eggTimeDefault;
			chickenSaveData.LayEggTimer = _layEggTimer;
		}
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DynamicThing sourceItem = attack.SourceItem;
		if (!sourceItem)
		{
			return null;
		}
		Labeller labeller = sourceItem as Labeller;
		if (!labeller)
		{
			return base.AttackWith(attack, doAction);
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = ActionStrings.Rename
		};
		if (!labeller.OnOff)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
		}
		if (!labeller.IsOperable)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
		}
		if (!doAction)
		{
			return delayedActionInstance;
		}
		labeller.Rename(this);
		return delayedActionInstance;
	}
}
