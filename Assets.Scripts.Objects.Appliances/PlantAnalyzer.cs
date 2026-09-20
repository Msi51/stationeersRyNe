using System.Threading;
using Assets.Scripts.Genetics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using Assets.Scripts.UI.Genetics;
using Cysharp.Threading.Tasks;
using Effects;
using Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects.Appliances;

public class PlantAnalyzer : Appliance, IGenetics
{
	private CancellationTokenSource _analyseCancel;

	private PlantSample _completedSample;

	[Header("Plant Analyzer")]
	[SerializeField]
	private MaterialChanger _searchButtonMaterialChanger;

	[SerializeField]
	private MaterialChanger _infoScreenMaterialChanger;

	private readonly int PoweredState = Animator.StringToHash("powered");

	private readonly int UnPoweredState = Animator.StringToHash("unpowered");

	private PlantSampler SamplerTool => Slots[0].Occupant as PlantSampler;

	public override void BenchPowerStateChanged(bool receivingPower)
	{
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(this, InteractableType.Powered, receivingPower ? 1 : 0);
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		CheckErrorState();
		if (newChild is PlantSampler)
		{
			HandleActivate();
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		CheckErrorState();
		if (previousChild is PlantSampler)
		{
			CancelAnalyse();
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = base.InteractWith(interactable, interaction, doAction);
		switch (interactable.Action)
		{
		case InteractableType.OnOff:
			if (doAction)
			{
				HandleActivate();
			}
			break;
		case InteractableType.Button1:
			if (!OnOff)
			{
				break;
			}
			delayedActionInstance.ActionMessage = string.Empty;
			delayedActionInstance.AppendStateMessage(GetTooltipDescription());
			if (!doAction)
			{
				if (_completedSample != null)
				{
					return delayedActionInstance.Succeed();
				}
				return delayedActionInstance.Fail();
			}
			if (interaction.SourceThing == Human.LocalHuman)
			{
				ShowSampleInfo();
			}
			return delayedActionInstance.Succeed();
		}
		return delayedActionInstance;
	}

	private void HandleActivate()
	{
		int num = ((OnOff && Powered && (bool)SamplerTool && SamplerTool.IsSampleFilled) ? 1 : 0);
		if (Activate != num)
		{
			OnServer.Interact(base.InteractActivate, num);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		switch (interactable.Action)
		{
		case InteractableType.Activate:
			if (GameManager.RunSimulation && interactable.State == 1)
			{
				Analyse().Forget();
			}
			break;
		case InteractableType.OnOff:
		case InteractableType.Powered:
		{
			int id = ((OnOff && Powered) ? PoweredState : UnPoweredState);
			_infoScreenMaterialChanger.ChangeState(id);
			_searchButtonMaterialChanger.ChangeState(id);
			if (!OnOff || !Powered)
			{
				CancelAnalyse();
			}
			CheckErrorState();
			break;
		}
		}
	}

	private void CancelAnalyse()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		OnServer.Interact(base.InteractActivate, 0);
		if (_analyseCancel != null)
		{
			_analyseCancel.Cancel();
			_analyseCancel.Dispose();
			_analyseCancel = null;
			_completedSample = null;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 512;
			}
		}
	}

	private async UniTaskVoid Analyse()
	{
		_analyseCancel = new CancellationTokenSource();
		_completedSample = null;
		bool num = await UniTask.Delay(Mathf.RoundToInt(11667f), ignoreTimeScale: false, PlayerLoopTiming.Update, _analyseCancel.Token).SuppressCancellationThrow();
		_analyseCancel?.Dispose();
		_analyseCancel = null;
		if (!num)
		{
			FinishedAnalysing();
		}
	}

	private void ShowSampleInfo()
	{
		if (_completedSample != null)
		{
			PanelPlantGenetics.Instance.Show(_completedSample);
		}
	}

	private void FinishedAnalysing()
	{
		_completedSample = SamplerTool.RemovePlantSample();
		OnServer.Interact(base.InteractActivate, 0);
		if (NetworkManager.IsServer)
		{
			base.NetworkUpdateFlags |= 512;
		}
	}

	private Assets.Scripts.Localization2.GameString GetTooltipDescription()
	{
		if (_completedSample != null)
		{
			return GameStrings.PlantViewSampleInfo;
		}
		return GameStrings.PlantSamplerNoSample;
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteBoolean(_completedSample != null);
		_completedSample?.Write(writer);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		if (reader.ReadBoolean())
		{
			if (_completedSample == null)
			{
				_completedSample = new PlantSample();
			}
			_completedSample.Read(reader);
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteBoolean(_completedSample != null);
			_completedSample?.Write(writer);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType) && reader.ReadBoolean())
		{
			if (_completedSample == null)
			{
				_completedSample = new PlantSample();
			}
			_completedSample.Read(reader);
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.GeneticDevices);
	}

	private void CheckErrorState()
	{
		if (GameManager.RunSimulation)
		{
			DynamicThing occupant = Slots[0].Occupant;
			int num = (((object)occupant != null && !(occupant is PlantSampler) && Powered && OnOff) ? 1 : 0);
			if (Error != num)
			{
				OnServer.Interact(base.InteractError, num);
			}
		}
	}
}
