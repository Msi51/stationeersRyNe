using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects.Entities;

public class Chick : Animal
{
	[Header("Chick")]
	public float GrowTime = 20f;

	public Animal[] ChickenPrefabs;

	[ReadOnly]
	public float CurrentGrowthTime;

	public override Lungs LungsPrefab => Prefab.Organ.LungsChicken;

	public override void Awake()
	{
		base.Awake();
		RigRoot = base.transform.Find("Root");
	}

	public override void HandleStateTimer()
	{
		base.HandleStateTimer();
		if (CurrentGrowthTime < GrowTime)
		{
			CurrentGrowthTime += 1f;
		}
	}

	public override void PerformState()
	{
		base.PerformState();
		if (CurrentGrowthTime >= GrowTime)
		{
			PerformBaseState = false;
			OnServer.Interact(base.InteractOnOff, 1);
			if (!RigidBody.isKinematic)
			{
				RigidBody.velocity = Vector3.zero;
			}
		}
		else
		{
			PerformBaseState = true;
		}
	}

	private void Grow()
	{
		if (GameManager.RunSimulation)
		{
			OnServer.CreateOld(ChickenPrefabs[0], CenterPosition, Rotation, 0uL);
			OnServer.Destroy(this);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new ChickSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		ChickSaveData chickSaveData = savedData as ChickSaveData;
		CurrentGrowthTime = chickSaveData.CurrentGrowthTime;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		ChickSaveData chickSaveData = savedData as ChickSaveData;
		if (GameManager.GameState != GameState.None)
		{
			chickSaveData.CurrentGrowthTime = CurrentGrowthTime;
		}
	}
}
