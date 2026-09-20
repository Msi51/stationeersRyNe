using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.UI;
using Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class GasFilter : Consumable
{
	private const int NORMAL_TICKS_BEFORE_DEGRADE = 144;

	private const int MEDIUM_TICKS_BEFORE_DEGRADE = 720;

	private const int LARGE_TICKS_BEFORE_DEGRADE = 2880;

	private const int SUPER_HEAVY_TICKS_BEFORE_DEGRADE = 11520;

	[Header("Gas Filter")]
	public Chemistry.GasType FilterType;

	private int _usedTicks;

	public GasFilterLife FilterLife;

	public bool IsLow => base.Quantity <= 0.05f;

	private ISuit ParentSuit => base.ParentSlot?.Parent as ISuit;

	public static bool IsUsable(DynamicThing dynamicThing)
	{
		GasFilter gasFilter = dynamicThing as GasFilter;
		if (gasFilter == null)
		{
			return false;
		}
		return !gasFilter.IsEmpty;
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.AirContitioningAtmos);
	}

	public void FilterGas(ref GasMixture fromMix, ref GasMixture toMix, Atmosphere atmosphere, float minRatio)
	{
		if (base.Quantity <= 0f)
		{
			HandleEmptyFilter(ParentSuit);
		}
		else if (AtmosphereHelper.FilterGas(FilterType, ref fromMix, ref toMix, atmosphere, minRatio) > MoleQuantity.Zero)
		{
			_usedTicks++;
			CheckUsedTicks();
		}
	}

	public void FilterGas(ref GasMixture fromMix, ref GasMixture toMix)
	{
		if (base.IsEmpty)
		{
			HandleEmptyFilter(ParentSuit);
		}
		else if (AtmosphereHelper.FilterGas(FilterType, ref fromMix, ref toMix))
		{
			_usedTicks++;
			CheckUsedTicks();
		}
	}

	private void HandleEmptyFilter(ISuit suit)
	{
		suit?.UpdateEmptyFilter();
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		HandleEmptyFilter(parent as ISuit);
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		HandleEmptyFilter(oldParent as ISuit);
	}

	private int GetTicksBeforeDegrade()
	{
		return FilterLife switch
		{
			GasFilterLife.Normal => 144, 
			GasFilterLife.Medium => 720, 
			GasFilterLife.Large => 2880, 
			GasFilterLife.SuperHeavy => 11520, 
			_ => 144, 
		};
	}

	private void CheckUsedTicks()
	{
		if (GameManager.RunSimulation && _usedTicks > GetTicksBeforeDegrade())
		{
			base.Quantity -= 1f;
			_usedTicks = 0;
		}
	}

	public override void Recycle()
	{
		if (GameManager.RunSimulation)
		{
			DestroyItem();
		}
	}
}
