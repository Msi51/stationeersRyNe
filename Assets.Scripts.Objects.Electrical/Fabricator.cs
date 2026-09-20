using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using Reagents;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class Fabricator : FabricatorBase, IPrefabHash, IRequireReagent
{
	public static DynamicThingRecipeComparable RecipeComparable = new DynamicThingRecipeComparable("Fabricator");

	[Tooltip("The collider for contents display")]
	public HashSet<ManufacturingMotherboard> ControllingManufacturingMotherboards = new HashSet<ManufacturingMotherboard>();

	public List<FabricatorJob> JobReferences = new List<FabricatorJob>();

	private float _progress;

	private float _powerUsedDuringTick;

	[ByteArraySync]
	public float Progress
	{
		get
		{
			return _progress;
		}
		set
		{
			if (NetworkManager.IsServer && !RocketMath.Approximately(_progress, value, 0.1f))
			{
				base.NetworkUpdateFlags |= 512;
			}
			_progress = value;
			foreach (ManufacturingMotherboard controllingManufacturingMotherboard in ControllingManufacturingMotherboards)
			{
				controllingManufacturingMotherboard.RefreshProgress(this);
			}
		}
	}

	public bool IsReagentUser => true;

	public Recipe RequiredReagents => CurrentRecipe.GetMissingReagents(ReagentMixture);

	public Recipe CurrentRecipe
	{
		get
		{
			if (CurrentJob == null)
			{
				return default(Recipe);
			}
			return CurrentJob.Recipe;
		}
	}

	public int CurrentHash
	{
		get
		{
			if (CurrentJob == null || !CurrentJob.Prefab)
			{
				return 0;
			}
			return CurrentJob.Prefab.PrefabHash;
		}
		set
		{
		}
	}

	public bool HasIngredients
	{
		get
		{
			if (CurrentJob != null && CurrentJob.Prefab != null)
			{
				return ReagentMixture.Contains(CurrentJob.Recipe);
			}
			return false;
		}
	}

	public int GetPrefabHashFromReagentHash(int reagentHash)
	{
		return 0;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(Progress);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			Progress = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(Progress);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Progress = reader.ReadSingle();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new FabricatorSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (!(savedData is FabricatorSaveData fabricatorSaveData))
		{
			return;
		}
		if (fabricatorSaveData.CurrentJob != null)
		{
			CurrentJob = new FabricatorJob(fabricatorSaveData.CurrentJob, this);
			Progress = fabricatorSaveData.Progress;
		}
		foreach (FabricatorJob fabricatorJob in fabricatorSaveData.FabricatorJobs)
		{
			JobReferences.Add(new FabricatorJob(fabricatorJob, this));
		}
		StartCoroutine(DeserializeNextFrame());
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is FabricatorSaveData fabricatorSaveData)
		{
			fabricatorSaveData.FabricatorJobs = JobReferences;
			fabricatorSaveData.CurrentJob = CurrentJob;
			fabricatorSaveData.Progress = Progress;
		}
	}

	public IEnumerator DeserializeNextFrame()
	{
		while (GameManager.GameState != GameState.Running)
		{
			yield return Yielders.EndOfFrame;
		}
		yield return new WaitForSecondsRealtime(0.5f);
		foreach (ManufacturingMotherboard controllingManufacturingMotherboard in ControllingManufacturingMotherboards)
		{
			controllingManufacturingMotherboard.RefreshJobs(this);
			controllingManufacturingMotherboard.RefreshCurrentJob(this);
		}
	}

	public override void OnReagentUpdate()
	{
		base.OnReagentUpdate();
		foreach (ManufacturingMotherboard controllingManufacturingMotherboard in ControllingManufacturingMotherboards)
		{
			controllingManufacturingMotherboard.RefreshJobs(this);
			controllingManufacturingMotherboard.RefreshCurrentJob(this);
		}
	}

	public FabricatorJob CreateJob(bool addReference = true)
	{
		FabricatorJob fabricatorJob = new FabricatorJob(this);
		if (addReference)
		{
			JobReferences.Add(fabricatorJob);
		}
		return fabricatorJob;
	}

	private void OnEnterExportSlot()
	{
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.RecipeHash)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.RecipeHash)
		{
			return CurrentHash;
		}
		return base.GetLogicValue(logicType);
	}

	private DynamicThing CreateOutput(DynamicThing createdPrefab)
	{
		DynamicThing dynamicThing = Thing.Create<DynamicThing>(createdPrefab, ExportSlot.Location.position, ExportSlot.Location.rotation, 0L);
		dynamicThing.name = createdPrefab.name;
		dynamicThing.ParentSlot = null;
		OnServer.MoveToSlot(dynamicThing, ExportSlot);
		DynamicThing.ItemManufactured(dynamicThing, 1);
		return dynamicThing;
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (base.PowerCable == null || base.PowerCable.CableNetwork != cableNetwork)
		{
			return -1f;
		}
		if (!OnOff)
		{
			return 0f;
		}
		return UsedPower + _powerUsedDuringTick;
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.ReceivePower(cableNetwork, powerAdded);
		_powerUsedDuringTick = 0f;
	}

	protected override void OnServerExportTick()
	{
		if (!OnOff || !Powered)
		{
			return;
		}
		if (IsOpen)
		{
			if (IsNextExportReady && ReagentMixture.TotalReagents > 0.0)
			{
				Item item = DropReagent(ExportSlot);
				if (item != null)
				{
					OnServer.MoveToSlot(item, ExportSlot);
					OnServer.Interact(base.InteractExport, 1);
				}
			}
		}
		else
		{
			if (Activate == 0)
			{
				return;
			}
			if (CurrentJob == null && JobReferences.Count > 0)
			{
				if (JobReferences[0].Prefab == null)
				{
					return;
				}
				CurrentJob = JobReferences[0];
				if (NetworkManager.IsServer)
				{
					CurrentJob.CurrentJobMessage().SendToClients();
				}
				FabricatorJobDeleteMessage fabricatorJobDeleteMessage = CurrentJob.DeleteNetworkMessage();
				if (NetworkManager.IsClient)
				{
					fabricatorJobDeleteMessage.SendToServer();
				}
				fabricatorJobDeleteMessage.Process(-1L);
				{
					foreach (ManufacturingMotherboard controllingManufacturingMotherboard in ControllingManufacturingMotherboards)
					{
						controllingManufacturingMotherboard.RefreshActivate(this);
						controllingManufacturingMotherboard.RefreshCurrentJob(this);
					}
					return;
				}
			}
			if (CurrentJob == null)
			{
				return;
			}
			if (IsNextExportReady && Progress <= 0f && HasIngredients)
			{
				ExportingThing = CreateOutput(CurrentJob.Prefab);
				PrintingComponent.AddPrintingComponent(ExportingThing);
				ReagentMixture.Subtract(CurrentJob.Recipe);
			}
			else if (CanBeginExport)
			{
				_powerUsedDuringTick += CurrentJob.Recipe.Energy * 0.01f;
				Progress += 0.01f;
				if (Progress >= 1f)
				{
					OnServer.Interact(base.InteractExport, 1);
				}
			}
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action != InteractableType.Activate)
		{
			return;
		}
		foreach (ManufacturingMotherboard controllingManufacturingMotherboard in ControllingManufacturingMotherboards)
		{
			controllingManufacturingMotherboard.RefreshActivate(this);
		}
	}

	public override void OnExportClosingComplete()
	{
		base.OnExportClosingComplete();
		if (CurrentJob == null || !GameManager.RunSimulation || !(Progress >= 1f))
		{
			return;
		}
		Progress = 0f;
		CurrentJob.Quantity--;
		if (CurrentJob.Quantity <= 0)
		{
			CurrentJob.Prefab = null;
			CurrentJob.CurrentJobMessage().SendToClients();
			CurrentJob = null;
		}
		else
		{
			CurrentJob.CurrentJobMessage().SendToServer();
		}
		foreach (ManufacturingMotherboard controllingManufacturingMotherboard in ControllingManufacturingMotherboards)
		{
			controllingManufacturingMotherboard.RefreshCurrentJob(this);
		}
	}

	public override void SetSlotOccupantTransformData(DynamicThing newChild)
	{
		if (newChild.ParentSlot == ExportSlot)
		{
			newChild.SetOnBaseOfSlot();
		}
		else
		{
			base.SetSlotOccupantTransformData(newChild);
		}
	}

	public override bool HasFrameBelow(float upOffset = 0f)
	{
		return base.HasFrameBelow(0f - GridSize + upOffset);
	}
}
