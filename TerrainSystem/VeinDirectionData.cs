using System.Xml.Serialization;
using UnityEngine;

namespace TerrainSystem;

public class VeinDirectionData : VeinModifierData
{
	private const string X_NAME = "X";

	private const string Y_NAME = "Y";

	private const string Z_NAME = "Z";

	[XmlAttribute("X")]
	public int X;

	[XmlAttribute("Y")]
	public int Y;

	[XmlAttribute("Z")]
	public int Z;

	public Vector3 Direction => new Vector3(X, Y, Z);

	public float GetBias(int depth)
	{
		return VeinModifierData.CalculateModifiedValue(Value, depth, DepthModifiers);
	}

	public override int GetChecksum()
	{
		return (((((0 ^ X) * 41) ^ Y) * 41) ^ Z) * 41;
	}
}
