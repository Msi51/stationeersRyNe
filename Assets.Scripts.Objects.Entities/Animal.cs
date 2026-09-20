using System;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Entities;

public class Animal : Npc
{
	public enum AnimalStateEnum
	{
		Idle,
		Roam,
		Eat,
		Poop
	}

	[Space(30f)]
	[Header("Animal Info")]
	[ReadOnly]
	public AnimalStateEnum Animalstate;

	[ReadOnly]
	public bool PerformBaseState = true;

	public float LifeSpanInDays = -1f;

	private IAnimalFood _targetFood;

	private bool _canEat = true;

	protected float _eatTimer = 2f;

	private float MinTargetNutritionAmount => BaseNutritionStorage * 0.25f;

	public virtual Lungs LungsPrefab => Prefab.Organ.LungsHuman;

	public override Vector3 EntityForward => ThingTransform.forward;

	public override Quaternion EntityRotation => base.ActiveRigidbody.rotation;

	public override void Start()
	{
		base.Start();
		Animalstate = AnimalStateEnum.Roam;
	}

	public override void OnLifeCreated()
	{
		base.OnLifeCreated();
		Organ organ = OnServer.Create<Organ>(LungsPrefab, LungsSlot);
		MoleQuantity quantity = IdealGas.Quantity(Chemistry.OneAtmosphere, organ.InternalAtmosphere.Volume, Chemistry.Temperature.TwentyDegrees);
		MoleEnergy energy = IdealGas.Energy(Chemistry.Temperature.TwentyDegrees, Mole.SpecificHeat(Chemistry.GasType.Oxygen), quantity);
		AtmosphericEventInstance.CreateAdd(organ.InternalAtmosphere, new GasMixture(new Mole(Chemistry.GasType.Oxygen, quantity, energy)));
	}

	public override void HandleStateTimer()
	{
		base.HandleStateTimer();
		if (_eatTimer > 0f)
		{
			_eatTimer -= 1f;
		}
	}

	public override bool OnLifeTick()
	{
		if (!base.OnLifeTick())
		{
			return false;
		}
		float lifeSpanInDays = LifeSpanInDays;
		if (!(lifeSpanInDays > 0f))
		{
			if (lifeSpanInDays == 0f)
			{
				DamageState.Damage(ChangeDamageType.Increment, 10f, DamageUpdateType.All);
			}
		}
		else
		{
			LifeSpanInDays -= 1f / GameManager.TicksPerDay;
		}
		switch (base.State)
		{
		case EntityState.Alive:
			SetState();
			HandleStateTimer();
			if (OrganBrain == null || OrganBrain.DamageState.Stun > 100f)
			{
				base.State = EntityState.Unconscious;
			}
			break;
		case EntityState.Unconscious:
			if (OrganBrain != null && OrganBrain.DamageState.Stun < 50f)
			{
				base.State = EntityState.Alive;
			}
			break;
		}
		return true;
	}

	private void Eat()
	{
		if (GameManager.RunSimulation && _targetFood != null)
		{
			_eatTimer = 2f;
			OnServer.Interact(base.InteractOpen, 0);
			base.Nutrition += _targetFood.GetNutritionValue();
			_targetFood.OnUseItem(1f, this);
		}
	}

	public override void PhysicsUpdate()
	{
		base.PhysicsUpdate();
		if (GameManager.RunSimulation && base.State == EntityState.Alive)
		{
			PerformState();
		}
	}

	public override void FollowPath(RoomManager.PathfindingTask pathfindingTask)
	{
		base.FollowPath(pathfindingTask);
		if (pathfindingTask.State == RoomManager.JobState.FindFood)
		{
			_targetFood = pathfindingTask.FoundFood;
		}
	}

	public void SetState()
	{
		if (!(StateTimer > 1f))
		{
			if (base.Nutrition < 0.3f && Plant.AllEdibles.Count > 0)
			{
				Animalstate = AnimalStateEnum.Eat;
				RegisterFoodPathFindingTask();
				StateTimer = 8f;
			}
			else
			{
				Animalstate = AnimalStateEnum.Roam;
				RegisterPathFindingTask();
				StateTimer = 8f;
			}
		}
	}

	public override void RegisterPathFindingTask(bool force = false)
	{
		base.RegisterPathFindingTask(force);
		WorldGrid worldGrid;
		if (base.Room != null)
		{
			worldGrid = base.Room.Grids.Pick();
		}
		else
		{
			Span<Grid3> obj = stackalloc Grid3[32];
			int count = 0;
			GridController.PopulateGridNeighbours(obj, ref count, base.WorldGrid.Value, horizontalOnly: true);
			Span<Grid3> span = obj;
			Span<Grid3> span2 = span.Slice(0, count);
			worldGrid = new WorldGrid(span2.Pick());
		}
		GridController controller = GridController.GetController(base.Position);
		RoomManager.RegisterNewTask(this, controller.WorldToLocalGrid(base.Position + Vector3.up * 0.5f), worldGrid.Value, RoomManager.JobState.Random);
		LastPathChange = GameManager.FixedTime;
		StateTimer = 2f;
		if (!IsBusy)
		{
			IsBusy = true;
			StationaryTolerance = 0.5f;
			LastPathChange = GameManager.FixedTime;
		}
	}

	public void RegisterFoodPathFindingTask()
	{
		GridController controller = GridController.GetController(base.Position);
		RoomManager.RegisterNewSearchTask(this, controller.WorldToLocalGrid(base.Position + Vector3.up * 0.5f));
		LastPathChange = GameManager.FixedTime;
		StateTimer = 2f;
		if (!IsBusy)
		{
			IsBusy = true;
			StationaryTolerance = 0.5f;
			LastPathChange = GameManager.FixedTime;
		}
	}

	public virtual void PerformState()
	{
		if (!PerformBaseState)
		{
			return;
		}
		switch (Animalstate)
		{
		case AnimalStateEnum.Roam:
			if (PathList != null && base.Room != null && PathList.Count > 0)
			{
				NavigatePath();
			}
			if (base.Room == null)
			{
				NoGravityNavigation();
			}
			break;
		case AnimalStateEnum.Eat:
			if (PathList != null && PathList.Count > 0)
			{
				NavigatePath();
			}
			if (_targetFood != null && _targetFood.GridPosition == GridPosition && _eatTimer <= 0f && base.InteractOpen.State == 0)
			{
				OnServer.Interact(base.InteractOpen, 1);
			}
			break;
		case AnimalStateEnum.Poop:
			StateTimer = 0f;
			break;
		case AnimalStateEnum.Idle:
			break;
		}
	}

	public override void OnGravityEnabled()
	{
		base.OnGravityEnabled();
		RigidBody.drag = 0.2f;
	}

	public override void OnGravityDisabled()
	{
		base.OnGravityDisabled();
		RigidBody.drag = 0.2f;
	}

	protected override void OnStateChanged()
	{
		if (NetworkManager.IsServer)
		{
			base.NetworkUpdateFlags |= 1024;
		}
		switch (base.State)
		{
		case EntityState.Alive:
			if (_oldState == EntityState.Unconscious)
			{
				OnEntityConscious();
			}
			break;
		case EntityState.Dead:
			EntityDeath();
			break;
		case EntityState.Decay:
			OnEntityDecay();
			break;
		case EntityState.Unconscious:
			break;
		}
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (base.Nutrition < 0.2f)
		{
			extendedText.AppendLine(GameStrings.AnimalHungry.AsColor("yellow"));
		}
		else if (base.Nutrition < 0.05f)
		{
			extendedText.AppendLine(GameStrings.AnimalVeryHungry.AsColor("red"));
		}
		float lifeSpanInDays = LifeSpanInDays;
		if (lifeSpanInDays < 5f)
		{
			if (lifeSpanInDays != -1f)
			{
				extendedText.AppendLine(GameStrings.AnimalVeryOld.AsColor("red"));
			}
		}
		else if (lifeSpanInDays < 30f)
		{
			extendedText.AppendLine(GameStrings.AnimalOld.AsColor("yellow"));
		}
		return extendedText;
	}
}
