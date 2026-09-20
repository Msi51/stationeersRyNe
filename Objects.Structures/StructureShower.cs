using System;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using CharacterCustomisation;
using DefaultNamespace;
using UnityEngine;
using Util;

namespace Objects.Structures;

public class StructureShower : WaterDevice, ISanitation
{
	private enum CleanResult
	{
		Invalid,
		Allowed,
		NotInPosition,
		CannotWearingSuit,
		CannotWearingUniform
	}

	public static readonly MoleQuantity MolesUsedPerTick = new MoleQuantity(5.0);

	public const float HYGIENE_PER_TICK = 0.05f;

	[SerializeField]
	private Transform particleEmitTransform;

	[SerializeField]
	[ReadOnly]
	private Vector3 cachedParticleEmitTransformPosition;

	[SerializeField]
	private ValveLeverAnimationComponent _pipeValveAnimComponent;

	private CancellationTokenWrapper _particleCancellationToken = new CancellationTokenWrapper();

	protected override bool IsOperable
	{
		get
		{
			if (base.IsOperable)
			{
				return HasEnoughWater(MolesUsedPerTick);
			}
			return false;
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.Sanitation);
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		cachedParticleEmitTransformPosition = particleEmitTransform.position;
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (_pipeValveAnimComponent != null)
		{
			_pipeValveAnimComponent.RefreshState(skipAnimation: true);
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (_pipeValveAnimComponent != null)
		{
			_pipeValveAnimComponent.RefreshState(skipAnimation);
		}
	}

	private CleanResult CanClean(Human human)
	{
		if ((object)human == null)
		{
			return CleanResult.Invalid;
		}
		if (human.GridPosition != LocalGrid)
		{
			return CleanResult.NotInPosition;
		}
		if (!human.SuitSlot.IsEmpty())
		{
			return CleanResult.CannotWearingSuit;
		}
		if (!human.UniformSlot.IsEmpty() && human.SpeciesClass != SpeciesClass.Robot)
		{
			return CleanResult.CannotWearingUniform;
		}
		return CleanResult.Allowed;
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (!IsOpen || !IsOperable)
		{
			return;
		}
		bool flag = false;
		foreach (Human allHuman in Human.AllHumans)
		{
			if (CanClean(allHuman) == CleanResult.Allowed)
			{
				flag = true;
				allHuman.Hygiene = Mathf.Min(allHuman.Hygiene + 0.05f, 1.5f);
			}
		}
		GasMixture gasMixture = InputNetwork.Atmosphere.Remove(MolesUsedPerTick, AtmosphereHelper.MatterState.Liquid);
		if (flag)
		{
			Mole mole = gasMixture.Remove(Chemistry.GasType.Water, MoleQuantity.MaxValue);
			gasMixture.Add(new Mole(Chemistry.GasType.PollutedWater, mole.Quantity, mole.Energy));
		}
		OutputNetwork.Atmosphere.Add(gasMixture);
		AtmosphereHelper.MoveToEqualize(InputNetwork.Atmosphere, OutputNetwork.Atmosphere, base.PressurePerTick, AtmosphereHelper.MatterState.Gas);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (interactable.Action == InteractableType.Open)
		{
			return HandleOpen(result, interaction, doAction);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.RunSimulation && interactable.Action == InteractableType.Open)
		{
			bool flag = IsOpen && IsOperable;
			if (flag && Activate == 0)
			{
				OnServer.Interact(base.InteractActivate, 1);
			}
			else if (!flag && Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
		}
		if (interactable.Action == InteractableType.Activate)
		{
			if (interactable.State == 1)
			{
				_particleCancellationToken.CancelAndInitialize();
				AtmosphericsManager.Instance.ShowerParticleSystem.EmitShowerParticles(cachedParticleEmitTransformPosition, 1, _particleCancellationToken.Token).Forget();
			}
			else
			{
				_particleCancellationToken.Cancel();
			}
		}
	}

	protected virtual DelayedActionInstance HandleOpen(DelayedActionInstance result, Interaction interaction, bool doAction)
	{
		MoleQuantity molesUsedPerTick = MolesUsedPerTick;
		if (!base.IsInputValid || !base.IsOutputValid)
		{
			if (!base.IsInputValid)
			{
				result.AppendStateMessage(GameStrings.DeviceNetworkInvalidSpecified.AsString("input").AsColor("red"));
			}
			if (!base.IsOutputValid)
			{
				result.AppendStateMessage(GameStrings.DeviceNetworkInvalidSpecified.AsString("output").AsColor("red"));
			}
		}
		else
		{
			if (!HasEnoughWater(molesUsedPerTick))
			{
				result.AppendStateMessage(GameStrings.DeviceNotEnoughWater.AsColor("red"));
			}
			if (base.WaterTooCold)
			{
				result.AppendStateMessage(GameStrings.DeviceWaterTooCold.AsColor("red"));
			}
			if (base.WaterTooHot)
			{
				result.AppendStateMessage(GameStrings.DeviceWaterTooHot.AsColor("red"));
			}
			if (base.WaterPolluted)
			{
				result.AppendStateMessage(GameStrings.DeviceWaterPolluted.AsColor("red"));
			}
			if (base.OutputFull)
			{
				result.AppendStateMessage(GameStrings.DeviceOutputFull.AsColor("red"));
			}
		}
		if (!base.IsMinimumWorldPressure)
		{
			result.AppendStateMessage(GameStrings.DeviceMinimumPressure.AsString(WaterDevice.MinimumWorldPressure.ToFloat().ToStringPrefix("Pa", "yellow")).AsColor("red"));
		}
		if (interaction.SourceThing is Human human)
		{
			StringBuilder stringBuilder = new StringBuilder();
			switch (CanClean(human))
			{
			case CleanResult.Allowed:
				stringBuilder.AppendLine(GameStrings.ShowerWillClean.AsString(human.ToTooltip()));
				break;
			case CleanResult.NotInPosition:
				stringBuilder.AppendLine(GameStrings.DeviceNotInPosition.AsString(human.ToTooltip()).AsColor("red"));
				break;
			case CleanResult.CannotWearingSuit:
				stringBuilder.AppendLine(GameStrings.DeviceCannotWhenWearing.AsString(human.Suit.ToTooltip()).AsColor("red"));
				break;
			case CleanResult.CannotWearingUniform:
				stringBuilder.AppendLine(GameStrings.DeviceCannotWhenWearing.AsString(human.Uniform.ToTooltip()).AsColor("red"));
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
			result.ExtendedMessage = stringBuilder.ToString();
		}
		if (!doAction)
		{
			return result.Succeed();
		}
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(base.InteractOpen, (!IsOpen) ? 1 : 0);
		}
		return result.Succeed();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		_particleCancellationToken.Cancel();
	}
}
