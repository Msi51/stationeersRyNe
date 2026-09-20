using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class FertilizedEgg : Egg
{
	private const float TOTAL_HATCH_TIME = 120f;

	[Header("Fertilized Egg")]
	public float HatchTime = 120f;

	public DynamicThing ChickPrefab;

	[SerializeField]
	public Material ViableMaterial;

	[SerializeField]
	public Material UnviableMaterial;

	[SerializeField]
	private MeshRenderer _meshRenderer;

	private bool _viable = true;

	private static readonly TemperatureKelvin MinHatchingTemp = TemperatureKelvin.FromCelsius(35f);

	private static readonly TemperatureKelvin MaxHatchingTemp = TemperatureKelvin.FromCelsius(40f);

	private static readonly TemperatureKelvin MinViableTemp = TemperatureKelvin.FromCelsius(10f);

	private static readonly TemperatureKelvin MaxViableTemp = TemperatureKelvin.FromCelsius(50f);

	private static readonly int COLOR = Shader.PropertyToID("_Color");

	private bool _hatching;

	private async UniTaskVoid Hatch()
	{
		if (GameManager.RunSimulation)
		{
			await UniTask.SwitchToMainThread();
			if ((long)Chicken.AllChickens.Count <= 100L)
			{
				OnServer.CreateOld(ChickPrefab, CenterPosition, Quaternion.identity, 0uL);
			}
			OnServer.CreateOld(DebrisEggTop, CenterPosition, Quaternion.identity, 0uL);
			OnServer.CreateOld(DebrisEggBottom, CenterPosition, Quaternion.identity, 0uL);
			Object.Destroy(base.gameObject);
		}
	}

	private void MakeUnviable()
	{
		_viable = false;
		SetMaterialUnviable().Forget();
		if (GameManager.RunSimulation)
		{
			base.NetworkUpdateFlags |= 4096;
		}
	}

	private async UniTaskVoid SetMaterialUnviable()
	{
		await UniTask.SwitchToMainThread();
		if (!base.IsBeingDestroyed)
		{
			_meshRenderer.material = UnviableMaterial;
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (!_viable || _hatching)
		{
			return;
		}
		if (base.WorldAtmosphere?.Temperature < MinViableTemp || base.WorldAtmosphere?.Temperature > MaxViableTemp)
		{
			if (base.ParentSlot == null)
			{
				MakeUnviable();
			}
		}
		else
		{
			if (!(base.WorldAtmosphere?.Temperature > MinHatchingTemp))
			{
				return;
			}
			TemperatureKelvin? temperatureKelvin = base.WorldAtmosphere?.Temperature;
			TemperatureKelvin maxHatchingTemp = MaxHatchingTemp;
			if (temperatureKelvin.HasValue && temperatureKelvin.GetValueOrDefault() < maxHatchingTemp && (base.ParentSlot == null || !base.ParentSlot.Occupant))
			{
				HatchTime -= 0.5f;
				if (GameManager.RunSimulation)
				{
					base.NetworkUpdateFlags |= 8192;
				}
				if (HatchTime <= 0f)
				{
					_hatching = true;
					Hatch().Forget();
				}
			}
		}
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (_viable)
		{
			bool flag = base.WorldAtmosphere?.Temperature > MaxHatchingTemp;
			bool flag2 = base.WorldAtmosphere?.Temperature < MinHatchingTemp;
			if (flag || flag2)
			{
				extendedText.AppendLine(GameStrings.ViableEggNotice.AsString(MinHatchingTemp.AsStringUnit(), MaxHatchingTemp.AsStringUnit()));
			}
			if (flag2)
			{
				extendedText.AppendLine(GameStrings.EggIsTooCold.AsColor("red"));
			}
			else if (flag)
			{
				extendedText.AppendLine(GameStrings.EggIsTooHot.AsColor("red"));
			}
			else
			{
				float value = 1f - HatchTime / 120f * 100f;
				extendedText.AppendLine(GameStrings.WillHatchInto.AsString(ChickPrefab.ToTooltip()));
				extendedText.AppendLine(GameStrings.FertilizedEggProcess.AsString(ToTooltip(), value.ToStringPercent("red")));
			}
		}
		else
		{
			extendedText.AppendLine(GameStrings.UnviableEggNotice.AsString(ToTooltip()));
		}
		return extendedText;
	}

	public override void Awake()
	{
		base.Awake();
		AtmosphericsManager.Instance.Register(this);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		AtmosphericsManager.Instance?.Deregister(this);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new FertilizedEggSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is FertilizedEggSaveData fertilizedEggSaveData)
		{
			HatchTime = fertilizedEggSaveData.EggHatchTime;
			if (!fertilizedEggSaveData.Viable)
			{
				MakeUnviable();
			}
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteBoolean(_viable);
		writer.WriteFloatHalf(HatchTime);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_viable = reader.ReadBoolean();
		HatchTime = reader.ReadFloatHalf();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			writer.WriteBoolean(_viable);
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			writer.WriteFloatHalf(HatchTime);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			_viable = reader.ReadBoolean();
			if (!_viable)
			{
				MakeUnviable();
			}
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			HatchTime = reader.ReadFloatHalf();
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		FertilizedEggSaveData fertilizedEggSaveData = savedData as FertilizedEggSaveData;
		if (GameManager.GameState != GameState.None)
		{
			fertilizedEggSaveData.EggHatchTime = HatchTime;
			fertilizedEggSaveData.Viable = _viable;
		}
	}
}
