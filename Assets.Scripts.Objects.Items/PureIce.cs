using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects.Pipes;
using Objects.Electrical;
using Reagents;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class PureIce : Ice
{
	public const int STACK_SIZE = 50;

	public const double DESIRED_MOLES_PER_ICE = 50.0;

	[SerializeField]
	private Chemistry.GasType gasType;

	public Chemistry.GasType GasType => gasType;

	public override void OnPrefabLoad()
	{
		base.OnPrefabLoad();
		AssignSpawnGasValues(this, gasType, new MoleQuantity(50.0));
	}

	public static void AssignSpawnGasValues(PureIce ice, Chemistry.GasType gasType, MoleQuantity quantity)
	{
		ice.SpawnContents.Clear();
		int num = Mathf.CeilToInt(quantity.ToFloat() / 50f);
		ice.SpawnContents = new List<SpawnGas>
		{
			new SpawnGas(gasType, (quantity / num).ToFloat())
		};
		ice.temperature = Mole.FreezingTemperature(gasType).ToFloat() + 0.1f;
		ice.meltTemperature = Mole.FreezingTemperature(gasType).ToFloat() + 1f;
		ice.SetQuantity(num);
	}

	protected override void HandleIceMelting()
	{
		if (!base.CanMelt)
		{
			return;
		}
		Atmosphere atmosphere = AtmosphericsController.World.SampleGlobalAtmosphere(base.WorldGrid);
		if (base.ParentSlot?.Parent is Furnace)
		{
			atmosphere = base.ParentSlot.Parent.InternalAtmosphere;
		}
		if (base.ParentSlot?.Parent is FridgePowered && base.ParentSlot.Parent.Powered && base.ParentSlot.Parent.OnOff)
		{
			base.IsMelting = false;
			return;
		}
		bool isMelting = atmosphere != null && atmosphere.IsActive() && atmosphere.WillMeltIce() && atmosphere.Temperature > base.MeltTemperature;
		base.IsMelting = isMelting;
		if (base.IsMelting)
		{
			Smelt(atmosphere, Ice._meltingReagentMix);
		}
	}

	public override void Smelt(Atmosphere localAtmosphere, ReagentMixture reagentMixture)
	{
		if (SpawnContents.Count > 0)
		{
			if (base.MeltTemperature > Chemistry.Temperature.TwentyDegrees && localAtmosphere.Temperature < base.MeltTemperature)
			{
				return;
			}
			GasMixture gasMixture = GasMixtureHelper.Create();
			foreach (SpawnGas spawnContent in SpawnContents)
			{
				if (spawnContent.Type != Chemistry.GasType.Undefined)
				{
					gasMixture.Add(new Mole(spawnContent.Type, spawnContent.GetQuantity(), spawnContent.GetEnergy()));
				}
			}
			gasMixture.TotalEnergy = IdealGas.Energy(gasMixture.HeatCapacity, base.Temperature);
			if (gasMixture.GetTotalMolesGassesAndLiquids < Chemistry.MINIMUM_QUANTITY_MOLES)
			{
				return;
			}
			if (localAtmosphere.IsGlobalAtmosphere)
			{
				AtmosphericEventInstance.CloneGlobalAddGasMix(base.WorldGrid, gasMixture);
			}
			else
			{
				AtmosphericEventInstance.CreateAdd(localAtmosphere, gasMixture);
			}
		}
		base.Quantity--;
	}

	protected override void OnSplitStack(Stackable newStack)
	{
		base.OnSplitStack(newStack);
		if (!(newStack is PureIce pureIce))
		{
			return;
		}
		pureIce.SpawnContents.Clear();
		foreach (SpawnGas spawnContent in SpawnContents)
		{
			pureIce.SpawnContents.Add(new SpawnGas(spawnContent));
		}
	}

	public override void Merge(IMergeable mergeable)
	{
		if (!(mergeable is Stackable stackable) || SpawnContents.Count == 0 || !(stackable is PureIce pureIce) || pureIce.SpawnContents.Count == 0)
		{
			return;
		}
		int num = Mathf.Min(stackable.Quantity + base.Quantity, MaxQuantity) - base.Quantity;
		int num2 = base.Quantity + num;
		float num3 = (float)base.Quantity / (float)num2;
		float num4 = (float)num / (float)num2;
		for (int i = 0; i < SpawnContents.Count; i++)
		{
			SpawnGas spawnGas = SpawnContents[i];
			bool flag = false;
			for (int j = 0; j < pureIce.SpawnContents.Count; j++)
			{
				if (pureIce.SpawnContents[j].Type == spawnGas.Type)
				{
					float num5 = SpawnContents[i].Quantity * num3;
					float num6 = pureIce.SpawnContents[j].Quantity * num4;
					SpawnContents[i].Quantity = num5 + num6;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				float num7 = SpawnContents[i].Quantity * num3;
				SpawnContents[i].Quantity = num7;
			}
		}
		for (int k = 0; k < pureIce.SpawnContents.Count; k++)
		{
			bool flag2 = false;
			for (int l = 0; l < SpawnContents.Count; l++)
			{
				if (pureIce.SpawnContents[k].Type == SpawnContents[l].Type)
				{
					flag2 = true;
				}
			}
			if (!flag2)
			{
				float num8 = pureIce.SpawnContents[k].Quantity * num4;
				SpawnContents.Add(new SpawnGas(pureIce.SpawnContents[k].Type, num8));
			}
		}
		base.Merge((IMergeable)pureIce);
	}

	public override void DoWeatherDamage(float damageMultiplier)
	{
		base.Quantity = 0;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (!(savedData is PureIceSaveData pureIceSaveData))
		{
			return;
		}
		pureIceSaveData.MeltTemperature = base.MeltTemperature.ToFloat();
		pureIceSaveData.Temperature = base.Temperature.ToFloat();
		foreach (SpawnGas spawnContent in SpawnContents)
		{
			if (spawnContent != null && spawnContent.IsValid)
			{
				pureIceSaveData.SpawnContentsDatas.Add(new SpawnContentsData
				{
					Gastype = spawnContent.Type,
					Quantity = spawnContent.Quantity
				});
			}
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new PureIceSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (!(saveData is PureIceSaveData pureIceSaveData))
		{
			return;
		}
		meltTemperature = pureIceSaveData.MeltTemperature;
		temperature = pureIceSaveData.Temperature;
		SpawnContents.Clear();
		foreach (SpawnContentsData spawnContentsData in pureIceSaveData.SpawnContentsDatas)
		{
			SpawnContents.Add(new SpawnGas(spawnContentsData.Gastype, spawnContentsData.Quantity));
		}
	}
}
