using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Reagents;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Pipes;

public class Centrifuge : DeviceImportExport
{
	public static OreRecipeComparable RecipeComparable = new OreRecipeComparable("Centrifuge");

	[SerializeField]
	protected Collider infoPanel;

	private float _rpm;

	private byte _processing;

	[SerializeField]
	[FormerlySerializedAs("OutputPrefab")]
	private Ore reagentMixPrefab;

	private static readonly int DrumSoundHash = Animator.StringToHash("spinning");

	private GameAudioEvent _drumAudio;

	[SerializeField]
	private Transform drum;

	private static readonly float RpmAdditionWhenPoweredOn = 0.5f;

	private static readonly float RpmLossWhenPoweredOff = 1.5f;

	private static readonly float MaxRpm = 100f;

	public const int MAX_REAGENTS = 400;

	private float _currentProgress;

	protected static float RPMToProgressStandard = 300f;

	public override WreckageSize WreckageSize => WreckageSize.Medium;

	public override int WreckageQuantity => 1;

	public override bool CanCompleteImport
	{
		get
		{
			if (base.CanCompleteImport)
			{
				return ReagentMixture.TotalReagents <= 0.0;
			}
			return false;
		}
	}

	public override bool CanBeginImport
	{
		get
		{
			if (OnOff && Powered && base.CanBeginImport)
			{
				return ImportingThing is ICentrifugable;
			}
			return false;
		}
	}

	public float Rpm
	{
		get
		{
			return _rpm;
		}
		set
		{
			value = Mathf.Max(0f, value);
			if (!RocketMath.Approximately(Rpm, value) && NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 16384;
			}
			_rpm = value;
		}
	}

	public byte Processing
	{
		get
		{
			return _processing;
		}
		set
		{
			if (value != Processing && NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 512;
			}
			_processing = value;
		}
	}

	public override bool HasReadableReagentMixture => true;

	protected override bool IsOperable
	{
		get
		{
			bool flag = base.IsOperable && !IsOpen && ReagentMixture.TotalReagents < 400.0;
			if (ImportingThing != null && !(ImportingThing is ICentrifugable))
			{
				flag = false;
			}
			if (Error == 0 && !flag)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (Error == 1 && flag)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			return flag;
		}
	}

	private bool IsProcessedReady
	{
		get
		{
			if (ImportingThing is ICentrifugable importingThing)
			{
				return _currentProgress >= ProgressRequired(importingThing);
			}
			return false;
		}
	}

	private bool IsProcessedFinished => (object)ImportingThing == null;

	private bool CanProcess => ImportingThing is ICentrifugable;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(40f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		Tooltip.ToolTipStringBuilder.Clear();
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (passiveTooltip.Title.Equals(string.Empty))
		{
			passiveTooltip.Title = DisplayName;
			if (Error == 1)
			{
				if (IsOpen && Rpm > 0f)
				{
					Tooltip.ToolTipStringBuilder.AppendLine(GameStrings.CentrifugeSpinningError.AsString(ToTooltip()));
				}
				else if (ReagentMixture.TotalReagents >= 400.0)
				{
					Tooltip.ToolTipStringBuilder.AppendLine(GameStrings.CentrifugeFull.AsString(ToTooltip()));
				}
			}
		}
		Tooltip.ToolTipStringBuilder.AppendLine(Rpm.ToStringPrefix("RPM", "yellow"));
		if (ImportingThing != null)
		{
			Tooltip.SetProcessingText(ImportingThing, (int)Processing);
		}
		if (hitCollider == infoPanel)
		{
			string value = ReagentMixture.ToString();
			Tooltip.ToolTipStringBuilder.Append(value);
			passiveTooltip.Title = Localization.GetInterface("Contents");
		}
		passiveTooltip.Extended = Tooltip.ToolTipStringBuilder.ToString();
		return passiveTooltip;
	}

	public static Ore CreateOutput(Ore orePrefab, int quantity, Slot exportSlot)
	{
		if (quantity <= 0 || (object)orePrefab == null)
		{
			return null;
		}
		Ore ore = Thing.Create<Ore>(orePrefab, exportSlot.Location);
		ore.ParentSlot = null;
		ore.SetQuantity(quantity);
		ore.QuantitySmelted = (((object)ore == null || ((ICentrifugable)ore).IsCentrifugeSmelt) ? quantity : 0);
		OnServer.MoveToSlot(ore, exportSlot);
		return ore;
	}

	public override void Awake()
	{
		base.Awake();
		ReagentMixture = new ReagentMixture(this);
		_drumAudio = GetAudioEvent(DrumSoundHash);
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (!IsOccluded && Rpm > 0.1f)
		{
			drum.Rotate(Vector3.forward, 360f * GameManager.DeltaTime * Rpm / 60f);
		}
	}

	public override void UpdateAudio(float deltaTime)
	{
		base.UpdateAudio(deltaTime);
		HandleDrumAudio();
	}

	private void HandleDrumAudio()
	{
		if (!IsOccluded)
		{
			float targetPitchMultiplier = Mathf.Clamp(Rpm / 60f, 0f, 3f);
			float num = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(Rpm / 10f));
			if (Rpm < 1f)
			{
				num = 0f;
			}
			bool flag = num > 0f;
			_drumAudio.UpdatePlayState(flag);
			if (flag)
			{
				_drumAudio.LerpVolumeAndPitch(num, targetPitchMultiplier, GameManager.DeltaTime);
			}
		}
	}

	public override void OnPowerTick()
	{
		if (!base.IsStructureCompleted)
		{
			Rpm = 0f;
		}
		else if (OnOff && Powered && IsOperable)
		{
			Rpm = Mathf.Min(Rpm + RpmAdditionWhenPoweredOn, MaxRpm);
			if (CanProcess)
			{
				ProgressProcessing();
			}
		}
		else
		{
			Rpm = Mathf.Max(Rpm - RpmLossWhenPoweredOff, 0f);
		}
	}

	public static float ProgressRequired(ICentrifugable importingThing)
	{
		return importingThing?.ProcessTime ?? 1f;
	}

	public static float RpmToProgressMultiplier(float rpm)
	{
		return rpm / RPMToProgressStandard;
	}

	private void ProgressProcessing()
	{
		_currentProgress += GameManager.GameTickSpeedSeconds * RpmToProgressMultiplier(Rpm);
		Processing = (byte)(Mathf.Clamp01(_currentProgress / ProgressRequired(ImportingThing as ICentrifugable)) * 100f);
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
				_currentProgress = 0f;
			}
			if (base.IsImportClosed && IsProcessedReady)
			{
				float currentProgress = _currentProgress - ProgressRequired(ImportingThing as ICentrifugable);
				CollectResource(ImportingThing as ICentrifugable);
				_currentProgress = currentProgress;
			}
			if (!OnOff)
			{
				OnServer.Interact(base.InteractImport, 0);
				_currentProgress = 0f;
			}
		}
	}

	protected override void OnServerExportTick()
	{
		if (!base.IsStructureCompleted)
		{
			return;
		}
		if (IsNextExportReady && IsOpen && _rpm == 0f && ReagentMixture.TotalReagents > 0.0)
		{
			Ore prefabForNextReagent = GetPrefabForNextReagent();
			ReagentMixture reagentMixture = new ReagentMixture();
			int quantity = ReagentMixture.AddNextReagent(reagentMixture, prefabForNextReagent.MaxQuantity);
			Ore ore = CreateOutput(prefabForNextReagent, quantity, ExportSlot);
			if ((object)ore != null)
			{
				ore.CreatedReagentMixture = reagentMixture;
				ExportingThing = ore;
			}
		}
		else
		{
			if (CanBeginExport)
			{
				OnServer.Interact(base.InteractExport, 1);
			}
			if (base.IsImportClosed && (object)ImportingThing == null)
			{
				OnServer.Interact(base.InteractImport, 0);
			}
		}
	}

	public Ore GetPrefabForNextReagent()
	{
		ReagentMixture nextMix = ReagentMixture.GetNextMix();
		RecipeComparable.AllRecipes.TryGetValue(new Recipe(nextMix, null), out var value);
		if ((object)value == null)
		{
			return reagentMixPrefab;
		}
		return value;
	}

	public void CollectResource(ICentrifugable input)
	{
		ReagentMixture.Add(input.CentrifugeProcessUnit());
		if (!(input is Stackable))
		{
			OnServer.Destroy(input as Thing);
		}
	}

	protected override void OnImportClosingComplete()
	{
		base.OnImportClosingComplete();
		if (GameManager.RunSimulation && !(ImportingThing is ICentrifugable))
		{
			OnServer.Interact(base.InteractImport, 0);
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteSingle(Rpm);
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteByte(Processing);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			Rpm = reader.ReadSingle();
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			Processing = reader.ReadByte();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(Rpm);
		writer.WriteByte(Processing);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Rpm = reader.ReadSingle();
		Processing = reader.ReadByte();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new CentrifugeSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is CentrifugeSaveData centrifugeSaveData)
		{
			centrifugeSaveData.Rpm = Rpm;
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is CentrifugeSaveData centrifugeSaveData)
		{
			Rpm = centrifugeSaveData.Rpm;
		}
	}
}
