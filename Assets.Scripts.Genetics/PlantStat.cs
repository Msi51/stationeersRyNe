using Assets.Scripts.Objects.Items;
using Genetics;
using UnityEngine;

namespace Assets.Scripts.Genetics;

public class PlantStat
{
	private Plant _plant;

	private Gene _gene;

	private bool _invert;

	public float Base { get; private set; }

	public float Min { get; private set; }

	public float Max { get; private set; }

	public static implicit operator float(PlantStat stat)
	{
		return stat.Get();
	}

	public PlantStat(Gene gene)
	{
		_gene = gene;
	}

	public void Initialize(PlantStatData data, Plant plant)
	{
		Base = data.Base;
		Min = data.Base * 0.75f;
		Max = data.Base * 1.25f;
		_invert = data.Invert;
		_plant = plant;
	}

	private float Get()
	{
		float value = _plant.Genes.GetValue(_gene);
		if (_invert)
		{
			if (!(value > 0f))
			{
				return Mathf.Lerp(Base, Max, Mathf.Abs(value));
			}
			return Mathf.Lerp(Base, Min, value);
		}
		if (!(value > 0f))
		{
			return Mathf.Lerp(Base, Min, Mathf.Abs(value));
		}
		return Mathf.Lerp(Base, Max, value);
	}
}
