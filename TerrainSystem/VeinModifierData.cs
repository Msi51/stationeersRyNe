using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace TerrainSystem;

public abstract class VeinModifierData
{
	private const string DEPTH_NAME = "Depth";

	public const string VALUE_NAME = "Value";

	[XmlAttribute("Value")]
	public float Value;

	[XmlElement("Depth")]
	public List<TreeDepthModifer> DepthModifiers = new List<TreeDepthModifer>();

	public virtual int GetChecksum()
	{
		int num = 0;
		num = (num ^ (int)Value) * 41;
		foreach (TreeDepthModifer depthModifier in DepthModifiers)
		{
			num = (num ^ depthModifier.GetChecksum()) * 41;
		}
		return num;
	}

	public static float CalculateModifiedValue(float baseValue, int depth, List<TreeDepthModifer> depthModifiers)
	{
		if (depthModifiers.Count == 0)
		{
			return baseValue;
		}
		float num = 1f;
		for (int i = 0; i < depthModifiers.Count; i++)
		{
			if (depth == depthModifiers[i].Value)
			{
				num = depthModifiers[i].Multiplier;
				break;
			}
			if (depth > depthModifiers[i].Value && i + 1 < depthModifiers.Count && depth < depthModifiers[i].Value)
			{
				float t = 1f;
				int num2 = depthModifiers[i + 1].Value - depthModifiers[i].Value;
				if (num2 > 0)
				{
					t = (float)(depth - depthModifiers[i].Value) / (float)num2;
				}
				num = Mathf.Lerp(depthModifiers[i].Multiplier, depthModifiers[i + 1].Multiplier, t);
				break;
			}
			if (depth > depthModifiers[i].Value && i + 1 >= depthModifiers.Count)
			{
				num = depthModifiers[i].Multiplier;
				break;
			}
		}
		return baseValue * num;
	}
}
