using Assets.Scripts;
using Assets.Scripts.Genetics;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Objects.Electrical;
using UnityEngine;

namespace Objects.Items;

public class PlantSampler : PowerTool, IGenetics
{
	[Header("Plant Sampler")]
	[SerializeField]
	private MeshRenderer _emptyMesh;

	[SerializeField]
	private MeshRenderer _fullMesh;

	public PlantSample PlantSample { get; private set; }

	public bool IsSampleFilled => PlantSample != null;

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is PlantSamplerSaveData plantSamplerSaveData && PlantSample != null)
		{
			plantSamplerSaveData.PlantSample = PlantSample;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		base.SerializeSave();
		ThingSaveData savedData = new PlantSamplerSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (saveData is PlantSamplerSaveData plantSamplerSaveData)
		{
			PlantSample = plantSamplerSaveData.PlantSample;
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		Invoke("RefreshAnimState", 1f);
	}

	public override void OnPrimaryUseStart()
	{
		base.OnPrimaryUseStart();
		Thing.Interact(base.InteractActivate, 1);
	}

	public override void OnPrimaryUseEnd()
	{
		base.OnPrimaryUseEnd();
		Thing.Interact(base.InteractActivate, 0);
	}

	public override bool TryInteractWithSlotOccupant(Interactable interactable, out DelayedActionInstance actionInstance, bool doAction = true)
	{
		actionInstance = null;
		if (interactable.Slot.Contains<Plant>(out var occupant) && HydroponicsUtils.PlantSamplerInHand(this, occupant, doAction, out var result))
		{
			actionInstance = result;
		}
		return actionInstance != null;
	}

	public void AddPlantSample(Plant plant)
	{
		PlantSample = new PlantSample(plant);
		OnServer.Interact(base.InteractMode, 1);
		RefreshAnimState();
	}

	public PlantSample RemovePlantSample()
	{
		PlantSample plantSample = PlantSample;
		PlantSample = null;
		OnServer.Interact(base.InteractMode, 0);
		RefreshAnimState();
		return plantSample;
	}

	public override bool CanCacheRenderer(Renderer selectedRenderer)
	{
		if (selectedRenderer == _emptyMesh || selectedRenderer == _fullMesh)
		{
			return false;
		}
		return true;
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		_emptyMesh.enabled = Powered && OnOff && Mode == 0;
		_fullMesh.enabled = Powered && OnOff && Mode == 1;
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		RefreshAnimState();
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.GeneticDevices);
	}
}
