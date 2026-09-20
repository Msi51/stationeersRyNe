using System.Text;
using Assets.Scripts.Objects.Items;
using Genetics;
using UnityEngine;

namespace Assets.Scripts.Genetics;

public class MultiPlantStat
{
	private Gene _lowGene;

	private Gene _highGene;

	private Plant _plant;

	private float _midPoint;

	private float _centerSize;

	private float _halfCenterSize;

	private float _lowSize;

	private float _highSize;

	private bool _clamp;

	public MultiPlantStat(Gene low, Gene high)
	{
		_lowGene = low;
		_highGene = high;
	}

	public void Initialize(MultiPlantStatData data, Plant plant)
	{
		_plant = plant;
		_midPoint = data.MidPoint;
		_lowSize = data.LowSize;
		_highSize = data.HighSize;
		_centerSize = data.CenterSize;
		_halfCenterSize = _centerSize * 0.5f;
		_clamp = data.Clamp;
	}

	private float GetScalar(float value)
	{
		float num = 0.4f;
		float b = 1f / num;
		if (!(value > 0f))
		{
			return Mathf.Lerp(1f, num, Mathf.Abs(value));
		}
		return Mathf.Lerp(1f, b, value);
	}

	private float GetGeneValue(Gene gene)
	{
		return _plant.Genes.GetValue(gene);
	}

	public float IdealMin()
	{
		return IdealMin(GetGeneValue(_lowGene));
	}

	public float IdealMin(float geneValue)
	{
		return _midPoint - _halfCenterSize * GetScalar(geneValue);
	}

	public float IdealMax()
	{
		return IdealMax(GetGeneValue(_highGene));
	}

	public float IdealMax(float geneValue)
	{
		return _midPoint + _halfCenterSize * GetScalar(geneValue);
	}

	public float Min()
	{
		return Min(GetGeneValue(_lowGene));
	}

	public float Min(float geneValue)
	{
		if (_clamp)
		{
			return Mathf.Max(0f, IdealMin(geneValue) - _lowSize * GetScalar(geneValue));
		}
		return IdealMin(geneValue) - _lowSize * GetScalar(geneValue);
	}

	public float Max()
	{
		return Max(GetGeneValue(_highGene));
	}

	public float Max(float geneValue)
	{
		return IdealMax(geneValue) + _highSize * GetScalar(geneValue);
	}

	public string DebugPrint()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Base, gene = 0");
		stringBuilder.Append(Min(0f));
		stringBuilder.Append(" ");
		stringBuilder.Append(IdealMin(0f));
		stringBuilder.Append(" ");
		stringBuilder.Append(IdealMax(0f));
		stringBuilder.Append(" ");
		stringBuilder.Append(Max(0f));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("High, gene = 1");
		stringBuilder.Append(Min(1f));
		stringBuilder.Append(" ");
		stringBuilder.Append(IdealMin(1f));
		stringBuilder.Append(" ");
		stringBuilder.Append(IdealMax(1f));
		stringBuilder.Append(" ");
		stringBuilder.Append(Max(1f));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Low, gene = -1");
		stringBuilder.Append(Min(-1f));
		stringBuilder.Append(" ");
		stringBuilder.Append(IdealMin(-1f));
		stringBuilder.Append(" ");
		stringBuilder.Append(IdealMax(-1f));
		stringBuilder.Append(" ");
		stringBuilder.Append(Max(-1f));
		stringBuilder.AppendLine();
		return stringBuilder.ToString();
	}
}
