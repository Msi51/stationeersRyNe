using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using Objects.Pipes;
using Reagents;
using Trading;
using UnityEngine;

namespace Objects.Electrical;

public class Fermenter : DeviceInputOutputImportCircuit, IResourceConsumer, IReferencable, IEvaluable
{
	private float _currentProgress;

	private static string[] _modeStrings;

	private double _volume = 100.0;

	private float _powerUsedDuringTick;

	private const float PROCESSING_POWER = 200f;

	public float CurrentProgress
	{
		get
		{
			return _currentProgress;
		}
		set
		{
			if (NetworkManager.IsServer && !RocketMath.Approximately(value, _currentProgress))
			{
				base.NetworkUpdateFlags |= 512;
			}
			_currentProgress = value;
		}
	}

	public override string[] ModeStrings => _modeStrings;

	public override bool PreventStateChange => true;

	public override bool HasReadableAtmosphere => false;

	public override bool HasValidConnections => base.IsOutputValid;

	protected override bool IsOperable
	{
		get
		{
			bool flag = (bool)base.ProgrammableChip && (CodeErrorState != 0 || base.ProgrammableChip.CompilationError);
			bool flag2 = (object)ImportingThing == null || CanProcess;
			bool flag3 = base.IsOutputValid && !flag && flag2;
			if (Error == 0 && !flag3)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (Error == 1 && flag3)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			return flag3;
		}
	}

	public override bool CanBeginImport
	{
		get
		{
			if (base.CanBeginImport && Powered && OnOff)
			{
				return ImportingThing is IFermentable;
			}
			return false;
		}
	}

	private bool IsProcessedReady
	{
		get
		{
			if (ImportingThing is IFermentable fermentable)
			{
				return CurrentProgress >= fermentable.SecondsToProcess;
			}
			return false;
		}
	}

	private bool IsProcessedFinished => (object)ImportingThing == null;

	private bool CanProcess => ImportingThing is IFermentable;

	public override void OnPrefabLoad()
	{
		base.OnPrefabLoad();
		_modeStrings = new string[2]
		{
			ActionStrings.Idle,
			ActionStrings.Active
		};
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, new VolumeLitres(_volume), 0L);
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (IsOperable)
		{
			OutputNetwork.Atmosphere.Add(base.InternalAtmosphere.GasMixture);
			base.InternalAtmosphere.GasMixture.Reset();
		}
	}

	public override void AssessError()
	{
		bool flag = !base.IsOutputValid;
		bool flag2 = (object)ImportingThing != null && !(ImportingThing is IFermentable);
		bool flag3 = (object)base.ProgrammableChip != null && (CodeErrorState != 0 || base.ProgrammableChip.CompilationError);
		if (GameManager.RunSimulation && HasErrorState && Error == 0 && (flag || flag3 || flag2))
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if ((GameManager.RunSimulation && HasErrorState && Error == 1 && !flag && !flag3 && !flag2) || !OnOff || !Powered)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(CurrentProgress);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			CurrentProgress = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(CurrentProgress);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		CurrentProgress = reader.ReadSingle();
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (!OnOff || cableNetwork != base.PowerCableNetwork || base.PowerCableNetwork == null || !base.IsStructureCompleted)
		{
			return 0f;
		}
		if (!IsOperable)
		{
			return UsedPower;
		}
		return UsedPower + _powerUsedDuringTick;
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.ReceivePower(cableNetwork, powerAdded);
		_powerUsedDuringTick = 0f;
	}

	public override void OnPowerTick()
	{
		if (base.IsStructureCompleted && Error == 0)
		{
			CableNetwork powerCableNetwork = base.PowerCableNetwork;
			if (powerCableNetwork != null)
			{
				float usedPower = GetUsedPower(powerCableNetwork);
				UsePower(powerCableNetwork, usedPower);
			}
		}
	}

	private static float ProgressRequired(IFermentable importingThing)
	{
		return importingThing?.SecondsToProcess ?? 1f;
	}

	protected override void OnServerImportTick()
	{
		if (base.IsStructureCompleted)
		{
			if (IsNextImportReady)
			{
				TryChuteImport();
			}
			if (CanBeginImport)
			{
				OnServer.Interact(base.InteractImport, 1);
			}
			if ((object)ImportingThing == null)
			{
				CurrentProgress = 0f;
			}
			if (base.IsImportClosed && IsProcessedReady && Error == 0 && OnOff && Powered)
			{
				float currentProgress = CurrentProgress - ProgressRequired(ImportingThing as IFermentable);
				CollectResource(ImportingThing as IFermentable);
				CurrentProgress = currentProgress;
			}
			if (!OnOff)
			{
				OnServer.Interact(base.InteractImport, 0);
				CurrentProgress = 0f;
			}
		}
	}

	public void CollectResource(IFermentable input)
	{
		FermentAlcohol(input);
		Stackable stackable = ImportingThing as Stackable;
		if ((!stackable || stackable.OnUseItem(1f, stackable)) && !(input is Stackable))
		{
			OnServer.Destroy(input as Thing);
		}
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (base.IsStructureCompleted && IsOperable && OnOff && Mode == 1 && CanProcess)
		{
			CurrentProgress += deltaTime;
			_powerUsedDuringTick = 200f;
			if (Activate == 0)
			{
				OnServer.Interact(base.InteractActivate, 1);
			}
		}
		else
		{
			_powerUsedDuringTick = 0f;
			if (Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		if ((bool)_infoScreen && hitCollider == _infoScreen.InfoTrigger && OnOff && Powered)
		{
			result.Title = Localization.GetInterface("Contents");
			if (Error == 1)
			{
				result.Extended = ActionStrings.Error;
				return result;
			}
			int value = 0;
			if ((bool)ImportingThing)
			{
				value = ((!(ImportingThing is Stackable stackable)) ? 1 : stackable.Quantity);
			}
			float value2 = Math.Clamp(CurrentProgress / ProgressRequired(ImportingThing as IFermentable), 0f, 1f) * 100f;
			StringBuilder stringBuilder = new StringBuilder(Localization.GetName(base.InteractMode));
			stringBuilder.Append(' ');
			stringBuilder.AppendLine(ModeStrings[Mode].AsColor("green"));
			if (Mode == 1)
			{
				if ((bool)ImportingThing && ImportingThing is IFermentable fermentable)
				{
					stringBuilder.AppendLine(GameStrings.FermenterItemsLeftToFerment.AsString(ImportingThing.DisplayName, StringManager.Get(value2), StringManager.Get(value)) ?? "");
					SpawnGas[] spawnGasList = fermentable.SpawnGasList;
					foreach (SpawnGas arg in spawnGasList)
					{
						stringBuilder.AppendLine(GameStrings.FermenterWillProduce.AsString($"{arg}") ?? "");
					}
				}
				else
				{
					stringBuilder.AppendLine(GameStrings.FermenterInputIsEmpty.AsString() ?? "");
				}
			}
			result.State = stringBuilder.ToString();
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	private void FermentAlcohol(IFermentable fermentable)
	{
		GasMixture gasMixture = GasMixtureHelper.Create();
		SpawnGas[] spawnGasList = fermentable.SpawnGasList;
		foreach (SpawnGas spawnGas in spawnGasList)
		{
			gasMixture.Add(new Mole(spawnGas.Type, spawnGas.GetQuantity(), spawnGas.GetEnergy()));
		}
		AtmosphericEventInstance.CreateAdd(base.InternalAtmosphere, gasMixture);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.CompletionRatio => true, 
			LogicType.Activate => false, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Activate)
		{
			return false;
		}
		return base.CanLogicWrite(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.CompletionRatio)
		{
			return (ImportingThing is IFermentable fermentable) ? (CurrentProgress / fermentable.SecondsToProcess) : 0f;
		}
		return base.GetLogicValue(logicType);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new FermenterSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is FermenterSaveData fermenterSaveData)
		{
			CurrentProgress = fermenterSaveData.CurrentProgress;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is FermenterSaveData fermenterSaveData)
		{
			fermenterSaveData.CurrentProgress = CurrentProgress;
		}
	}

	public List<Item> GetResourcesUsed()
	{
		List<Item> list = new List<Item>();
		foreach (Thing allPrefab in Prefab.AllPrefabs)
		{
			if (allPrefab is Item item && item is IFermentable)
			{
				list.Add(item);
			}
		}
		return list;
	}

	bool IResourceConsumer.CanProcess(Recipe recipe)
	{
		return false;
	}

	bool IResourceConsumer.CanProcess(Reagent reagentType)
	{
		return false;
	}
}
