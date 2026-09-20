using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class DynamicScrubber : PortableAtmosphericsPowered, IThermal
{
	[Header("Dynamic Scrubber")]
	[ReadOnly]
	public HashSet<GasFilter> GasFilters = new HashSet<GasFilter>();

	private Atmosphere _atmosphere;

	public static float MinimumRatioToFilterAll = 0.001f;

	public bool HasFilters => GasFilters.Count > 0;

	public bool ExceededPressure
	{
		get
		{
			if (base.InternalAtmosphere != null)
			{
				return base.InternalAtmosphere.PressureGassesAndLiquids > DynamicGasCanister.StandardPressure;
			}
			return false;
		}
	}

	public bool EmptyFilter => GasFilters.Find((GasFilter f) => f.Quantity <= 0f);

	public override void InitInternalAtmosphere()
	{
		base.InitInternalAtmosphere();
		base.InternalAtmosphere.NeverReset = true;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		GasFilter gasFilter = newChild as GasFilter;
		if (gasFilter != null)
		{
			GasFilters.Add(gasFilter);
			IsOperable();
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.AirContitioningAtmos);
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		Tooltip.ToolTipStringBuilder.Clear();
		if (hitCollider == InfoPanel)
		{
			PassiveTooltip passiveTooltip = new PassiveTooltip(true);
			passiveTooltip.Title = DisplayName;
			PassiveTooltip result = passiveTooltip;
			StringBuilder stringBuilder = new StringBuilder();
			if (ExceededPressure)
			{
				stringBuilder.AppendLine(GameStrings.ThingOverPressure.AsString(ToTooltip()));
				stringBuilder.AppendLine(GameStrings.ScrubberConnectToPipe);
			}
			if (IsOpen)
			{
				stringBuilder.AppendLine(GameStrings.ScrubberVentOpen.AsColor("red"));
			}
			if (!HasFilters)
			{
				stringBuilder.AppendLine(GameStrings.ScrubberNoFilters.AsColor("red"));
			}
			AtmosphericsManager.MakeGasTooltip(base.InternalAtmosphere, stringBuilder);
			result.Extended = stringBuilder.ToString();
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		GasFilter item = previousChild as GasFilter;
		if (GasFilters.Contains(item))
		{
			GasFilters.Remove(item);
			IsOperable();
		}
	}

	public override bool IsOperable()
	{
		if (!base.GridController.CanContainAtmos(base.WorldGrid) || (ExceededPressure && !IsOpen) || EmptyFilter)
		{
			if (Error == 0 && Powered && GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			return false;
		}
		if (Error == 1 && GameManager.RunSimulation)
		{
			OnServer.Interact(base.InteractError, 0);
		}
		return true;
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (!OnOff || !BatteryCell || BatteryCell.IsEmpty)
		{
			if (Powered)
			{
				OnServer.Interact(base.InteractPowered, 0);
			}
			return;
		}
		if (!Powered)
		{
			OnServer.Interact(base.InteractPowered, 1);
		}
		if (IsOperable())
		{
			BatteryCell.PowerStored -= UsedPower;
			if (IsOpen)
			{
				ReleaseToAtmos();
			}
			else
			{
				GetFromAtmos();
			}
		}
	}

	private void ReleaseToAtmos()
	{
		if (base.WorldAtmosphere == null)
		{
			base.WorldAtmosphere = base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
		}
		if (base.InternalAtmosphere.PressureGassesAndLiquids < new PressurekPa(100.0))
		{
			base.WorldAtmosphere.Add(base.InternalAtmosphere.GasMixture);
			base.InternalAtmosphere.GasMixture.Reset();
		}
		else
		{
			AtmosphereHelper.MoveVolume(base.InternalAtmosphere, base.WorldAtmosphere, base.InternalAtmosphere.Volume * 0.05000000074505806, AtmosphereHelper.MatterState.All, MoleQuantity.Zero);
		}
	}

	private void GetFromAtmos()
	{
		if (base.WorldAtmosphere != null)
		{
			base.WorldAtmosphere = base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
			if (base.WorldAtmosphere.IsAboveArmstrong())
			{
				base.WorldAtmosphere.GasMixture.AddEnergy(new MoleEnergy(UsedPower * PowerEfficiency));
			}
			GetFromCell(base.WorldGrid);
			for (int i = 0; i < base.WorldAtmosphere.OpenNeighbors.Count; i++)
			{
				GetFromCell(base.WorldAtmosphere.OpenNeighbors[i]);
			}
		}
	}

	private void GetFromCell(Grid3 neighbor)
	{
		Atmosphere atmosphere = base.GridController.AtmosphericsController.SampleGlobalAtmosphere(new WorldGrid(neighbor));
		if (atmosphere == null)
		{
			return;
		}
		MoleQuantity transferMoles = IdealGas.Quantity(base.PressurePerTick, base.InternalAtmosphere.Volume, atmosphere.Temperature);
		if (HasFilters)
		{
			foreach (GasFilter gasFilter in GasFilters)
			{
				GasMixture fromMix = atmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.All);
				if (!fromMix.IsValid)
				{
					break;
				}
				base.WorldAtmosphere = atmosphere;
				gasFilter.FilterGas(ref fromMix, ref base.InternalAtmosphere.GasMixture, base.WorldAtmosphere, MinimumRatioToFilterAll);
				base.WorldAtmosphere.Add(fromMix);
			}
			return;
		}
		GasMixture gasMixture = atmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.All);
		if (gasMixture.IsValid)
		{
			base.WorldAtmosphere = atmosphere;
			base.InternalAtmosphere.Add(gasMixture);
		}
	}
}
