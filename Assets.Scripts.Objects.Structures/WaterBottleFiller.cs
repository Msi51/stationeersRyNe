using System;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Structures;

public class WaterBottleFiller : Device, ISmartRotatable
{
	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public static readonly int WaterBottleFillHash = Animator.StringToHash("WaterBottleFill");

	private GameAudioEvent _waterBottleFill;

	private float _maxFillPerTick = 5.56f;

	public PipeNetwork ConnectedPipeNetwork
	{
		get
		{
			if (ConnectedPipeNetworks.Count <= 0)
			{
				return null;
			}
			return ConnectedPipeNetworks[0];
		}
	}

	private bool WaterTooHot
	{
		get
		{
			if (ConnectedPipeNetwork?.Atmosphere != null)
			{
				return ConnectedPipeNetwork?.Atmosphere.Temperature > Chemistry.Temperature.ZeroDegrees + new TemperatureKelvin(100.0);
			}
			return false;
		}
	}

	private bool WaterTooCold
	{
		get
		{
			if (ConnectedPipeNetwork?.Atmosphere != null)
			{
				return ConnectedPipeNetwork?.Atmosphere.Temperature < Chemistry.Temperature.ZeroDegrees;
			}
			return false;
		}
	}

	private bool WaterPolluted
	{
		get
		{
			if (ConnectedPipeNetwork?.Atmosphere != null)
			{
				if (!(ConnectedPipeNetwork.Atmosphere.GasMixture.TotalToxins > MoleQuantity.Zero))
				{
					return ConnectedPipeNetwork.Atmosphere.GasMixture.GetTotalMolesLiquids > ConnectedPipeNetwork.Atmosphere.GasMixture.Water.Quantity;
				}
				return true;
			}
			return false;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (ConnectedPipeNetworks.Count != 0)
			{
				PipeNetwork pipeNetwork = ConnectedPipeNetworks[0];
				if (pipeNetwork != null)
				{
					Atmosphere atmosphere = pipeNetwork.Atmosphere;
					if (atmosphere != null)
					{
						_ = atmosphere.GasMixture;
						if (0 == 0 && !(ConnectedPipeNetworks[0].Atmosphere.GasMixture.Water.Quantity < Chemistry.MINIMUM_VALID_TOTAL_MOLES))
						{
							Atmosphere atmosphere2 = ConnectedPipeNetworks[0].Atmosphere;
							if (atmosphere2.Temperature < Chemistry.Temperature.ZeroDegrees || atmosphere2.Temperature > Chemistry.Temperature.ZeroDegrees + new TemperatureKelvin(100.0))
							{
								if (Error != 2)
								{
									OnServer.Interact(base.InteractError, 2);
								}
								return false;
							}
							if (atmosphere2.GasMixture.TotalToxins > MoleQuantity.Zero || atmosphere2.GasMixture.GetTotalMolesLiquids > atmosphere2.GasMixture.Water.Quantity)
							{
								if (Error != 3)
								{
									OnServer.Interact(base.InteractError, 3);
								}
								return false;
							}
							if (Error != 0)
							{
								OnServer.Interact(base.InteractError, 0);
							}
							return base.IsOperable;
						}
					}
				}
			}
			if (Error != 1)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			return false;
		}
	}

	public override void Awake()
	{
		base.Awake();
		_waterBottleFill = GetAudioEvent(WaterBottleFillHash);
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (!OnOff || !Powered || Error != 0 || base.InteractActivate.State == 0)
		{
			return;
		}
		float num = 0f;
		float num2 = 0f;
		foreach (Slot slot in Slots)
		{
			if (slot.Contains<WaterBottle>(out var occupant))
			{
				num += occupant.Quantity;
				num2 += occupant.MaxQuantity;
			}
		}
		if (!(num2 <= 0f))
		{
			_waterBottleFill?.SetPitchMultiplier(Mathf.Lerp(0.75f, 2f, num / num2));
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		int num = 0;
		if (!OnOff || !Powered)
		{
			if (Activate != num)
			{
				OnServer.Interact(base.InteractActivate, num);
			}
			return;
		}
		if (IsOperable)
		{
			foreach (Slot slot in Slots)
			{
				if (!slot.Contains<WaterBottle>(out var occupant))
				{
					continue;
				}
				if (ConnectedPipeNetworks.Count == 0)
				{
					break;
				}
				Atmosphere atmosphere = ConnectedPipeNetworks[0]?.Atmosphere;
				if (atmosphere == null)
				{
					continue;
				}
				Atmosphere atmosphere2 = atmosphere;
				if (atmosphere2.TotalMolesLiquids > MoleQuantity.Zero && !(occupant == null) && !(occupant.GetMissingMoleCount() <= MoleQuantity.Zero))
				{
					float num2 = 55.555557f;
					float num3 = Mathf.Min((occupant.MaxQuantity - occupant.Quantity) * num2, atmosphere.GasMixture.Water.Quantity.ToFloat());
					MoleQuantity moleQuantity = new MoleQuantity(Math.Min(num3, _maxFillPerTick));
					MoleEnergy energy = atmosphere.GasMixture.Water.Energy * (moleQuantity / atmosphere.GasMixture.Water.Quantity).ToDouble();
					GasMixture gasMixture = new GasMixture(new Mole(Chemistry.GasType.Water, moleQuantity, energy));
					GasMixture gasMixture2 = atmosphere.Remove(gasMixture, AtmosphereHelper.MatterState.Liquid);
					occupant.AddLiquidToThing(gasMixture2.GetTotalMolesLiquids.ToFloat() / num2);
					if (num3 > 0f)
					{
						num = 1;
					}
				}
			}
		}
		if (Activate != num)
		{
			OnServer.Interact(base.InteractActivate, num);
		}
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (Error == 1)
		{
			extendedText.AppendLine(GameStrings.NoWaterAvailable.AsColor("red"));
		}
		if (WaterTooCold)
		{
			extendedText.AppendLine(GameStrings.DeviceWaterTooCold.AsColor("red"));
		}
		if (WaterTooHot)
		{
			extendedText.AppendLine(GameStrings.DeviceWaterTooHot.AsColor("red"));
		}
		if (WaterPolluted)
		{
			extendedText.AppendLine(GameStrings.DeviceWaterPolluted.AsColor("red"));
		}
		return extendedText;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		Tooltip.ToolTipStringBuilder.Clear();
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (string.IsNullOrEmpty(passiveTooltip.Title))
		{
			passiveTooltip.Title = DisplayName;
		}
		return passiveTooltip;
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}
}
