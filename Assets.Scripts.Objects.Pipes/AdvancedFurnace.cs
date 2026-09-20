using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Effects;
using Reagents;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class AdvancedFurnace : FurnaceBase, ISetable, ILogicable, IReferencable, IEvaluable, ISetable2, IProducesAllIngots, IResourceConsumer
{
	public static IQuantityRecipeComparable RecipeComparable = new IQuantityRecipeComparable("AdvancedFurnace");

	[SerializeField]
	private MaterialChanger indicatorIn;

	[SerializeField]
	private MaterialChanger indicatorOut;

	private float _outputSetting2 = 5f;

	public float MinSetting2;

	public float MaxSetting2 = 10f;

	public SettingWheel SettingWheel2;

	[ByteArraySync]
	public float OutputSetting2
	{
		get
		{
			return _outputSetting2;
		}
		set
		{
			float num = Mathf.Clamp(value, MinSetting2, MaxSetting2);
			if (!RocketMath.Approximately(num, OutputSetting2))
			{
				_outputSetting2 = num;
				OnOutputSettingChanged2();
			}
		}
	}

	public double Setting
	{
		get
		{
			return base.OutputSetting;
		}
		set
		{
			value = base.OutputSetting;
		}
	}

	public double Setting2
	{
		get
		{
			return OutputSetting2;
		}
		set
		{
			value = OutputSetting2;
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
	}

	private async UniTaskVoid RefreshIndicators()
	{
		if (!GameManager.IsBatchMode)
		{
			await UniTask.SwitchToMainThread();
			if (GameManager.GameState == GameState.Running && !base.BeingDestroyed)
			{
				indicatorOut.ChangeState((Powered && base.OutputSetting > 0f) ? Defines.Animator.OnPowered : Defines.Animator.Off);
				indicatorIn.ChangeState((Powered && OutputSetting2 > 0f) ? Defines.Animator.OnPowered : Defines.Animator.Off);
			}
		}
	}

	public override CanConstructInfo CanConstruct()
	{
		Vector3 worldPosition = base.ThingTransformPosition - ThingTransform.up * 0.75f;
		Structure structure = base.GridController.Get<Structure>(worldPosition, StructureElement.Center);
		if (!structure || ((bool)structure && !structure.AllowMounting))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
		}
		return base.CanConstruct();
	}

	public override float GetSmelterScale()
	{
		return RecipeComparable.GetOutputScale(CurrentRecipe);
	}

	public override IQuantity GetSmelterResult()
	{
		if (ReagentMixture.TotalReagents <= 0.0)
		{
			return null;
		}
		RatioMix = ReagentMixture.GetRatioMixture();
		CurrentRecipe = RecipeComparable.GetCleanRecipe(new Recipe(RatioMix, base.InternalAtmosphere));
		RecipeComparable.Recipes.TryGetValue(CurrentRecipe, out var value);
		return value;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteSingle(OutputSetting2);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			OutputSetting2 = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(OutputSetting2);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		OutputSetting2 = reader.ReadSingle();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new AdvancedFurnaceSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is AdvancedFurnaceSaveData advancedFurnaceSaveData)
		{
			OutputSetting2 = advancedFurnaceSaveData.OutputSetting2;
		}
		if ((bool)SettingWheel2)
		{
			SettingWheel2.OnDeserialize();
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is AdvancedFurnaceSaveData advancedFurnaceSaveData)
		{
			advancedFurnaceSaveData.OutputSetting2 = OutputSetting2;
		}
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType - 91 <= LogicType.Power)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		switch (logicType)
		{
		case LogicType.SettingInput:
			OutputSetting2 = (float)value;
			break;
		case LogicType.SettingOutput:
			base.OutputSetting = (float)value;
			break;
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType - 91 <= LogicType.Power)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.SettingInput => OutputSetting2, 
			LogicType.SettingOutput => base.OutputSetting, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override void HandleGasInput()
	{
		if (OnOff && Powered && Error <= 0)
		{
			if (InputNetwork != null)
			{
				AtmosphereHelper.MoveVolume(InputNetwork.Atmosphere, base.InternalAtmosphere, new VolumeLitres(OutputSetting2), AtmosphereHelper.MatterState.All, MoleQuantity.Zero);
			}
			if (OutputNetwork != null)
			{
				AtmosphereHelper.MoveVolume(base.InternalAtmosphere, OutputNetwork.Atmosphere, new VolumeLitres(base.OutputSetting), AtmosphereHelper.MatterState.Gas, MoleQuantity.Zero);
			}
			if (OutputNetwork2 != null)
			{
				AtmosphereHelper.MoveLiquidVolume(base.InternalAtmosphere, OutputNetwork2.Atmosphere, new VolumeLitres(base.OutputSetting));
			}
		}
	}

	public override void OnOutputSettingChanged()
	{
		base.OnOutputSettingChanged();
		RefreshIndicators().Forget();
	}

	public void OnOutputSettingChanged2()
	{
		base.OnOutputSettingChanged();
		if (NetworkManager.IsServer)
		{
			base.NetworkUpdateFlags |= 256;
		}
		CheckWheel2();
		RefreshIndicators().Forget();
	}

	public override void Awake()
	{
		base.Awake();
		if (!(SettingWheel2 == null))
		{
			SettingWheel2.Awake();
		}
	}

	private void CheckWheel2()
	{
		if (!(SettingWheel2 == null))
		{
			SettingWheel2.CheckWheel().Forget();
		}
	}

	public override void InitializeDevice()
	{
		base.InitializeDevice();
		if ((bool)SettingWheel2)
		{
			SettingWheel2.SetLastAngle();
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if ((bool)SettingWheel2)
		{
			SettingWheel2.SetRotation();
		}
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (base.PowerCable == null || base.PowerCable.CableNetwork != cableNetwork)
		{
			return -1f;
		}
		float num = (OnOff ? UsedPower : 0f);
		num += (float)Setting / MaxSetting * UsedPower;
		num += (float)Setting2 / MaxSetting2 * UsedPower;
		if (!OnOff)
		{
			return 0f;
		}
		return num;
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Powered)
		{
			RefreshIndicators().Forget();
		}
	}

	private DelayedActionInstance HandleButtonSetting(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action != InteractableType.Button1 && interactable.Action != InteractableType.Button2 && interactable.Action != InteractableType.Button3 && interactable.Action != InteractableType.Button4)
		{
			return null;
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		Labeller labeller = interaction.SourceSlot.Occupant as Labeller;
		if ((bool)labeller)
		{
			delayedActionInstance.ActionMessage = ActionStrings.Set;
			delayedActionInstance.AppendStateMessage(GameStrings.DeviceManualInputWindow);
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
				return delayedActionInstance.Succeed();
			}
			InteractableType action = interactable.Action;
			if (action == InteractableType.Button1 || action == InteractableType.Button2)
			{
				labeller.Set(this, LogicType.SettingOutput);
			}
			else
			{
				action = interactable.Action;
				if (action == InteractableType.Button3 || action == InteractableType.Button4)
				{
					labeller.Set(this, LogicType.SettingInput);
				}
			}
			return delayedActionInstance.Succeed();
		}
		return null;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = HandleButtonSetting(interactable, interaction, doAction);
		if (delayedActionInstance != null)
		{
			return delayedActionInstance;
		}
		DelayedActionInstance delayedActionInstance2 = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		switch (interactable.Action)
		{
		case InteractableType.Button1:
			delayedActionInstance2.ActionMessage = GameStrings.GlobalIncrease.AsString();
			delayedActionInstance2.AppendStateMessage(GameStrings.OutputLitres, StringManager.Get((int)base.OutputSetting));
			delayedActionInstance2.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
			delayedActionInstance2.AppendStateMessage(GameStrings.UseLabelerToSet);
			if (!doAction)
			{
				return delayedActionInstance2.Succeed();
			}
			SettingWheel.PlayWheelSound(this, SettingWheel.Wheel, increaseSetting: true, interaction.AltKey, base.OutputSetting, MinSetting, MaxSetting, WheelSettingIncrement, WheelAltSettingIncrement);
			if (GameManager.RunSimulation)
			{
				base.OutputSetting += (interaction.AltKey ? WheelAltSettingIncrement : WheelSettingIncrement);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button2:
			delayedActionInstance2.ActionMessage = GameStrings.GlobalDecrease.AsString();
			delayedActionInstance2.AppendStateMessage(GameStrings.OutputLitres, StringManager.Get((int)base.OutputSetting));
			delayedActionInstance2.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
			delayedActionInstance2.AppendStateMessage(GameStrings.UseLabelerToSet);
			if (!doAction)
			{
				return delayedActionInstance2.Succeed();
			}
			SettingWheel.PlayWheelSound(this, SettingWheel.Wheel, increaseSetting: false, interaction.AltKey, base.OutputSetting, MinSetting, MaxSetting, WheelSettingIncrement, WheelAltSettingIncrement);
			if (GameManager.RunSimulation)
			{
				base.OutputSetting -= (interaction.AltKey ? WheelAltSettingIncrement : WheelSettingIncrement);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button3:
			delayedActionInstance2.ActionMessage = GameStrings.GlobalIncrease.AsString();
			delayedActionInstance2.AppendStateMessage(GameStrings.InputLitres, StringManager.Get((int)OutputSetting2));
			delayedActionInstance2.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
			delayedActionInstance2.AppendStateMessage(GameStrings.UseLabelerToSet);
			if (!doAction)
			{
				return delayedActionInstance2.Succeed();
			}
			SettingWheel.PlayWheelSound(this, SettingWheel2.Wheel, increaseSetting: true, interaction.AltKey, OutputSetting2, MinSetting2, MaxSetting2, WheelSettingIncrement, WheelAltSettingIncrement);
			if (GameManager.RunSimulation)
			{
				OutputSetting2 += (interaction.AltKey ? WheelAltSettingIncrement : WheelSettingIncrement);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button4:
			delayedActionInstance2.ActionMessage = GameStrings.GlobalDecrease.AsString();
			delayedActionInstance2.AppendStateMessage(GameStrings.InputLitres, StringManager.Get((int)OutputSetting2));
			delayedActionInstance2.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
			delayedActionInstance2.AppendStateMessage(GameStrings.UseLabelerToSet);
			if (!doAction)
			{
				return delayedActionInstance2.Succeed();
			}
			SettingWheel.PlayWheelSound(this, SettingWheel2.Wheel, increaseSetting: false, interaction.AltKey, OutputSetting2, MinSetting2, MaxSetting2, WheelSettingIncrement, WheelAltSettingIncrement);
			if (GameManager.RunSimulation)
			{
				OutputSetting2 -= (interaction.AltKey ? WheelAltSettingIncrement : WheelSettingIncrement);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}
}
