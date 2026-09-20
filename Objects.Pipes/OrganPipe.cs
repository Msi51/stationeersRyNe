using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using UnityEngine;

namespace Objects.Pipes;

public class OrganPipe : Pipe
{
	private Atmosphere _environment;

	private static PressurekPa OrganPressureDelta = new PressurekPa(10.0);

	private static int LowestNote = 49;

	private static readonly PressurekPa _pressurePerTick = new PressurekPa(20.0);

	private string _tooltip;

	private static Dictionary<int, string> _noteNamesLookup;

	private static string _toolTipNoteName;

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (base.HasOpenGrid && base.PipeNetwork != null)
		{
			int num = Mathf.Clamp(base.PipeNetwork.StructureList.Count, 1, LowestNote);
			if (Mode != num)
			{
				OnServer.Interact(base.InteractMode, num);
			}
			_environment = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
			int num2 = ((base.PipeNetwork.Atmosphere.PressureGassesAndLiquids > _environment.PressureGassesAndLiquids + OrganPressureDelta) ? 1 : 0);
			if (Activate != num2)
			{
				OnServer.Interact(base.InteractActivate, num2);
			}
			AtmosphereHelper.MoveToEqualizeBidirectional(base.PipeNetwork.Atmosphere, _environment, _pressurePerTick, AtmosphereHelper.MatterState.Gas, MoleQuantity.MaxValue);
		}
	}

	public override CanConstructInfo CanConstruct()
	{
		SmallCell smallCell = base.GridController.GetSmallCell(OpenEnds[0].Transform.position);
		if (smallCell != null && smallCell.Device != null && smallCell.Device is DeviceAtmospherics)
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresPipeValve.DisplayString);
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		Tooltip.ToolTipStringBuilder.Clear();
		PassiveTooltip result = new PassiveTooltip(true);
		if (hitCollider == null || hitCollider.transform != ThingTransform)
		{
			return base.GetPassiveTooltip(hitCollider);
		}
		if (!base.SmallCell.Pipe)
		{
			return base.GetPassiveTooltip(hitCollider);
		}
		Tooltip.ToolTipStringBuilder.Append("Note: " + GetSoundName() + "\n");
		result.Title = DisplayName;
		result.Extended = Tooltip.ToolTipStringBuilder.ToString();
		return result;
	}

	private string GetSoundName()
	{
		if (_noteNamesLookup == null)
		{
			PopulateNoteNamesLookup();
		}
		int key = Mathf.Clamp(base.PipeNetwork.StructureList.Count, 1, LowestNote);
		_toolTipNoteName = string.Empty;
		_noteNamesLookup?.TryGetValue(key, out _toolTipNoteName);
		return _toolTipNoteName;
	}

	private void PopulateNoteNamesLookup()
	{
		_noteNamesLookup = new Dictionary<int, string>();
		foreach (GameAudioEvent audioEvent in AudioEvents)
		{
			foreach (SoundEffectCondition condition in audioEvent.Conditions)
			{
				if (condition.Type == InteractableType.Mode)
				{
					_noteNamesLookup.Add(condition.Value, audioEvent.Name);
				}
			}
		}
	}
}
