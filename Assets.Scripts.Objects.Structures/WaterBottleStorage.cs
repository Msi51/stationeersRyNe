using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Structures;

public class WaterBottleStorage : SmallDevice, ISmartRotatable
{
	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public static readonly int WaterBottleFillHash = Animator.StringToHash("WaterBottleFill");

	private GameAudioEvent _waterBottleFill;

	public List<WaterBottle> ConnectedWaterBottles = new List<WaterBottle>();

	private float _maxFillPerTick = 5.56f;

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
						if (0 == 0 && !(ConnectedPipeNetworks[0].Atmosphere.TotalMoles < Chemistry.MINIMUM_VALID_TOTAL_MOLES))
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
							if (atmosphere2.GasMixture.TotalToxins > MoleQuantity.Zero || atmosphere2.GasMixture.LiquidNitrousOxide.Quantity > MoleQuantity.Zero)
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
		if (base.InteractActivate.State == 0 || ConnectedWaterBottles.Count == 0)
		{
			return;
		}
		float num = 0f;
		float num2 = 0f;
		foreach (WaterBottle connectedWaterBottle in ConnectedWaterBottles)
		{
			if (!(connectedWaterBottle == null))
			{
				num += connectedWaterBottle.Quantity;
				num2 += connectedWaterBottle.MaxQuantity;
			}
		}
		if (!(num2 <= 0f))
		{
			_waterBottleFill?.SetPitchMultiplier(Mathf.Lerp(0.75f, 2f, num / num2));
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (newChild is WaterBottle item)
		{
			ConnectedWaterBottles.Add(item);
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (previousChild is WaterBottle item)
		{
			ConnectedWaterBottles.Remove(item);
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		int num = 0;
		if (IsOperable && ConnectedWaterBottles.Count > 0)
		{
			foreach (WaterBottle connectedWaterBottle in ConnectedWaterBottles)
			{
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
				if (atmosphere2.TotalMolesLiquids > MoleQuantity.Zero && !(connectedWaterBottle == null) && !(connectedWaterBottle.GetMissingMoleCount() <= MoleQuantity.Zero))
				{
					double num2 = 55.55555555555556;
					double num3 = Math.Min((double)(connectedWaterBottle.MaxQuantity - connectedWaterBottle.Quantity) * num2, atmosphere.GasMixture.Water.Quantity.ToFloat());
					MoleQuantity moleQuantity = new MoleQuantity(Math.Min(num3, _maxFillPerTick));
					MoleEnergy energy = atmosphere.GasMixture.Water.Energy * (moleQuantity / atmosphere.GasMixture.Water.Quantity).ToFloat();
					GasMixture gasMixture = new GasMixture(new Mole(Chemistry.GasType.Water, moleQuantity, energy));
					connectedWaterBottle.AddLiquidToThing((atmosphere.Remove(gasMixture, AtmosphereHelper.MatterState.Liquid).GetTotalMolesLiquids / num2).ToFloat());
					if (num3 > 0.0)
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
			extendedText.AppendLine(GameStrings.NoWaterAvailable.DisplayString);
		}
		else if (Error == 2)
		{
			extendedText.AppendLine(GameStrings.WaterWrongTemp.DisplayString);
		}
		else if (Error == 3)
		{
			extendedText.AppendLine(GameStrings.ToxicLiquidsInPipe.DisplayString);
		}
		return extendedText;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		Tooltip.ToolTipStringBuilder.Clear();
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (passiveTooltip.Title.Equals(string.Empty))
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
