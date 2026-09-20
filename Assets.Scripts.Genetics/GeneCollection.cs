using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Genetics;

[Serializable]
public class GeneCollection : IRocketReaderWriter
{
	public static readonly Gene[] Genes = ((Gene[])Enum.GetValues(typeof(Gene))).Skip(1).ToArray();

	public string PlantCustomName;

	public string PlanterCustomName;

	public readonly Dictionary<Gene, GeneWrapper> Lookup = new Dictionary<Gene, GeneWrapper>();

	private static readonly System.Random Random = new System.Random();

	private float defaultGeneticDistroSize = 0.2f;

	private float defaultGeneticInstabilityOffset = 0.2f;

	public static float GetRange(float min, float max)
	{
		return (float)(Random.NextDouble() * (double)(max - min) + (double)min);
	}

	public static float GetRange(float delta)
	{
		delta = Mathf.Abs(delta);
		return GetRange(0f - delta, delta);
	}

	public GeneCollection()
	{
		Gene[] genes = Genes;
		foreach (Gene gene in genes)
		{
			if (gene != Gene.None)
			{
				Lookup[gene] = new GeneWrapper(gene, GetRange(0.05f), 0f);
			}
		}
	}

	public GeneCollection(GeneCollectionWrapper genes)
	{
		foreach (GeneWrapper geneWrapper in genes.GeneWrappers)
		{
			Lookup[geneWrapper.Gene] = geneWrapper;
		}
	}

	public static GeneCollection Copy(GeneCollection toCopy)
	{
		GeneCollection geneCollection = new GeneCollection();
		geneCollection.PlantCustomName = toCopy.PlantCustomName;
		foreach (Gene key in toCopy.Lookup.Keys)
		{
			geneCollection.SetGeneValue(key, toCopy.Lookup[key].Value);
			geneCollection.SetGeneStability(key, toCopy.Lookup[key].Stability);
		}
		return geneCollection;
	}

	public void LerpTowards(GeneCollection target, double t)
	{
		foreach (Gene key in Lookup.Keys)
		{
			SetGeneValue(key, (float)RocketMath.Lerp(GetValue(key), target.GetValue(key), t));
			SetGeneStability(key, (float)RocketMath.Lerp(GetGeneStability(key), target.GetGeneStability(key), t));
		}
	}

	public bool TryGetValue(Gene gene, out float value)
	{
		if (Lookup.TryGetValue(gene, out var value2))
		{
			value = value2.Value;
			return true;
		}
		ConsoleWindow.PrintError($"Can't find gene {gene} in gene collection");
		value = 0f;
		return false;
	}

	public float GetValue(Gene gene)
	{
		if (Lookup.TryGetValue(gene, out var value))
		{
			return value.Value;
		}
		ConsoleWindow.PrintError($"Can't find gene {gene} in gene collection - returning default value");
		return 0f;
	}

	public void SetGeneValue(Gene gene, float value)
	{
		if (!Lookup.ContainsKey(gene))
		{
			ConsoleWindow.PrintError($"Can't set value for gene {gene} - gene not in gene collection");
		}
		else
		{
			Lookup[gene].Value = value;
		}
	}

	public float GetGeneStability(Gene gene)
	{
		if (Lookup.TryGetValue(gene, out var value))
		{
			return value.Stability;
		}
		ConsoleWindow.PrintError($"Can't find gene {gene} in gene collection - returning default value");
		return 0f;
	}

	public void SetGeneStability(Gene gene, float stability)
	{
		if (!Lookup.ContainsKey(gene))
		{
			ConsoleWindow.PrintError($"Can't set value for gene {gene} - gene not in gene collection");
		}
		else
		{
			Lookup[gene].Stability = stability;
		}
	}

	public GeneCollection Copy()
	{
		return Copy(this);
	}

	public GeneCollection CreateSimilar(Plant parentPlant)
	{
		GeneCollection geneCollection = new GeneCollection();
		geneCollection.PlantCustomName = parentPlant.CustomName;
		geneCollection.PlanterCustomName = parentPlant.PlanterName;
		foreach (Gene key in Lookup.Keys)
		{
			geneCollection.SetGeneValue(key, Lookup[key].Value);
			float stability = Lookup[key].Stability;
			if (stability >= 0f)
			{
				geneCollection.SetGeneStability(key, Mathf.Clamp(stability - 0.1f, 0f, 1f));
			}
			else
			{
				geneCollection.SetGeneStability(key, Mathf.Clamp(stability + 0.2f, -1f, 0f));
			}
			float mutationBias = parentPlant.lifeRequirements.GetMutationBias(key);
			geneCollection.Mutate(key, mutationBias);
		}
		return geneCollection;
	}

	public void Reset()
	{
		foreach (GeneWrapper value in Lookup.Values)
		{
			value.Value = 0f;
			value.Stability = 0f;
		}
	}

	public void Mutate(Gene gene, float bias)
	{
		if (Lookup.TryGetValue(gene, out var value))
		{
			float value2 = MutateValue(value.Value, bias, value.Stability);
			Lookup[gene].Value = value2;
		}
		else
		{
			ConsoleWindow.PrintError($"Can't mutate gene {gene} - gene not found in gene collection");
		}
	}

	private float MutateValue(float value, float bias, float stability)
	{
		float num = 0f;
		if (!(stability >= 0f))
		{
			num = ((!(GetRange(0f, 1f) > 0.5f)) ? RocketMath.RandomGaussian(0f - defaultGeneticDistroSize + bias + defaultGeneticInstabilityOffset * (0f - stability), defaultGeneticDistroSize + bias + defaultGeneticInstabilityOffset * (0f - stability)) : RocketMath.RandomGaussian(0f - defaultGeneticDistroSize + bias + defaultGeneticInstabilityOffset * stability, defaultGeneticDistroSize + bias + defaultGeneticInstabilityOffset * stability));
		}
		else
		{
			float num2 = 1f - stability;
			num = RocketMath.RandomGaussian((0f - defaultGeneticDistroSize) * num2 + bias, defaultGeneticDistroSize * num2 + bias);
		}
		return Mathf.Clamp(value + num, -1f, 1f);
	}

	public void Read(RocketBinaryReader reader)
	{
		PlanterCustomName = reader.ReadString();
		PlantCustomName = reader.ReadString();
		Network.ReadIndex<byte>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			Gene key = (Gene)reader.ReadByte();
			if (!Lookup.ContainsKey(key))
			{
				Lookup[key] = new GeneWrapper();
			}
			Lookup[key].Read(reader);
		}
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteString(PlanterCustomName);
		writer.WriteString(PlantCustomName);
		Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
		foreach (KeyValuePair<Gene, GeneWrapper> item in Lookup)
		{
			writer.WriteByte((byte)item.Key);
			item.Value.Write(writer);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}
}
