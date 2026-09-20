using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Effects;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Electrical;

public class OccupantAtmospherics : SpawnPointAtmospherics
{
	private enum OccupantAtmosphericsMode : byte
	{
		Standby,
		Error,
		Inactive,
		Occupied,
		Dead
	}

	public readonly int AtmosUnpowered = Animator.StringToHash("unpowered");

	public readonly int AtmosNoError = Animator.StringToHash("noerror");

	public readonly int AtmosOccupied = Animator.StringToHash("occupied");

	public readonly int AtmosDead = Animator.StringToHash("dead");

	public readonly int AtmosError1 = Animator.StringToHash("error1");

	public readonly int AtmosError2 = Animator.StringToHash("error2");

	[FormerlySerializedAs("_materialChanger")]
	public MaterialChanger AtmosMaterialChanger;

	public LightChanger LightChanger;

	public Collider InfoPanel;

	private static EnumCollection<OccupantAtmosphericsMode, byte> _sleeperModes = new EnumCollection<OccupantAtmosphericsMode, byte>(toProper: false);

	private int _flashCount;

	private TemperatureKelvin _minimumTemperature = new TemperatureKelvin(263.15);

	private PressurekPa _minimumPressure = new PressurekPa(30.0);

	private bool _errorFlash;

	public Slot BedSlot => Slots[0];

	public override string[] ModeStrings => _sleeperModes.Names;

	public bool IsUnsafeAtmosphere
	{
		get
		{
			if (base.InternalAtmosphere == null)
			{
				return true;
			}
			if (base.InternalAtmosphere.PressureGassesAndLiquids < _minimumPressure)
			{
				return true;
			}
			if (base.InternalAtmosphere.Temperature > Chemistry.Temperature.FiftyDegrees)
			{
				return true;
			}
			if (base.InternalAtmosphere.Temperature < _minimumTemperature)
			{
				return true;
			}
			return false;
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		Tooltip.ToolTipStringBuilder.Clear();
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (passiveTooltip.Title.Equals(string.Empty))
		{
			passiveTooltip.Title = DisplayName;
		}
		if (InfoPanel != null && hitCollider == InfoPanel && Powered)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(GameStrings.SleeperState.AsColor("white"));
			AddErrorStateStrings(stringBuilder, indent: true);
			stringBuilder.AppendLine(GameStrings.SlotOccupant.AsColor("white"));
			AddOccupantString(stringBuilder, healthInfo: true, indent: true, alwaysShow: true);
			stringBuilder.AppendLine(GameStrings.BreathingAtmosphere.AsColor("white"));
			AtmosphericsManager.MakeGasTooltip(base.InternalAtmosphere, stringBuilder, indent: true);
			passiveTooltip.Extended = stringBuilder.ToString();
		}
		return passiveTooltip;
	}

	private void AddErrorStateStrings(StringBuilder sb, bool indent = false)
	{
		if (IsOperable)
		{
			if (indent)
			{
				sb.Append(StringManager.Indent);
			}
			sb.AppendLine(GameStrings.SleeperMetabolicSuspension.AsColor("green"));
		}
		if (IsOpen)
		{
			if (indent)
			{
				sb.Append(StringManager.Indent);
			}
			sb.AppendLine(GameStrings.SleeperDoorIsOpen.AsColor("red"));
		}
		if (InputNetwork == null)
		{
			if (indent)
			{
				sb.Append(StringManager.Indent);
			}
			sb.AppendLine(GameStrings.CryoNoAtmosphericInput.AsColor("red"));
		}
		if (IsUnsafeAtmosphere)
		{
			if (indent)
			{
				sb.Append(StringManager.Indent);
			}
			sb.AppendLine(GameStrings.CryoAtmosphereIsUnsafe.AsColor("red"));
		}
	}

	public override void Update100MS(float deltaTime)
	{
		base.Update100MS(deltaTime);
		if (GameManager.IsBatchMode || (byte)Mode != 1)
		{
			return;
		}
		_flashCount++;
		if (_flashCount >= 2)
		{
			_errorFlash = !_errorFlash;
			_flashCount = 0;
			if (_errorFlash)
			{
				AtmosMaterialChanger?.ChangeState(AtmosError2);
				LightChanger?.ChangeState(AtmosError2);
			}
			else
			{
				AtmosMaterialChanger?.ChangeState(AtmosError1);
				LightChanger?.ChangeState(AtmosError1);
			}
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		switch ((OccupantAtmosphericsMode)(byte)Mode)
		{
		case OccupantAtmosphericsMode.Standby:
			AtmosMaterialChanger?.ChangeState(AtmosNoError);
			LightChanger?.ChangeState(AtmosNoError);
			break;
		case OccupantAtmosphericsMode.Error:
			AtmosMaterialChanger?.ChangeState(AtmosError1);
			LightChanger?.ChangeState(AtmosError1);
			break;
		case OccupantAtmosphericsMode.Inactive:
			AtmosMaterialChanger?.ChangeState(AtmosUnpowered);
			LightChanger?.ChangeState(AtmosUnpowered);
			break;
		case OccupantAtmosphericsMode.Occupied:
			AtmosMaterialChanger?.ChangeState(AtmosOccupied);
			LightChanger?.ChangeState(AtmosOccupied);
			break;
		case OccupantAtmosphericsMode.Dead:
			AtmosMaterialChanger?.ChangeState(AtmosDead);
			LightChanger?.ChangeState(AtmosDead);
			break;
		}
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		Human occupant;
		OccupantAtmosphericsMode occupantAtmosphericsMode = ((!IsOpen && Powered && OnOff) ? ((IsUnsafeAtmosphere || Error > 0) ? OccupantAtmosphericsMode.Error : (BedSlot.Contains<Human>(out occupant) ? (occupant.IsDead ? OccupantAtmosphericsMode.Dead : OccupantAtmosphericsMode.Occupied) : OccupantAtmosphericsMode.Standby)) : OccupantAtmosphericsMode.Inactive);
		if ((int)occupantAtmosphericsMode != Mode)
		{
			OnServer.Interact(base.InteractMode, (int)occupantAtmosphericsMode);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		base.BeingDestroyed = true;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (base.IsBurning)
		{
			extendedText.AppendLine(GameStrings.CurrentlyOnFire.DisplayString);
		}
		return extendedText;
	}
}
