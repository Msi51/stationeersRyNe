using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Voxel;
using UnityEngine;

namespace TerrainSystem;

public class VeinGenerationData : DataCollection
{
	private const string TYPE_NAME = "Type";

	private const string WEIGHT_NAME = "Weight";

	private const string MAX_SIZE_NAME = "MaxSize";

	private const string MAX_DEPTH_NAME = "MaxDepth";

	private const string DIRECTION_NAME = "Direction";

	private const string BRANCH_ATTEMPTS_NAME = "BranchAttempts";

	private const string MOMENTUM_NAME = "Momentum";

	private const string THICKNESS_NAME = "Thickness";

	private const string MIN_DROP_QUANTITY_NAME = "MinDropQuantity";

	private const string MAX_DROP_QUANTITY_NAME = "MaxDropQuantity";

	private const string MINING_TIME_NAME = "MiningTime";

	private const string SEEK_SURFACE_NAME = "SeekSurface";

	private const string SEEK_DEPTH_NAME = "SeekDepth";

	[XmlAttribute("Type")]
	public MinableType Type;

	[XmlAttribute("Weight")]
	public float Weight = 10f;

	[XmlAttribute("MaxSize")]
	public int MaxSize = 254;

	[XmlAttribute("MaxDepth")]
	public int MaxDepth = 127;

	[XmlAttribute("MinDropQuantity")]
	public int MinDropQuantity = 1;

	[XmlAttribute("MaxDropQuantity")]
	public int MaxDropQuantity = 1;

	[XmlAttribute("MiningTime")]
	public float MiningTime = 1f;

	[XmlElement("Direction")]
	public VeinDirectionData Direction = new VeinDirectionData();

	[XmlElement("BranchAttempts")]
	public BranchAttemptsData BranchAttempts = new BranchAttemptsData();

	[XmlElement("Momentum")]
	public MomentumData Momentum = new MomentumData();

	[XmlElement("Thickness")]
	public ThicknessData Thickness = new ThicknessData();

	[XmlElement("SeekSurface")]
	public SeekSurfaceData SeekSurface;

	[XmlElement("SeekDepth")]
	public SeekDepthData SeekDepth;

	public override bool IsValid()
	{
		return Type != MinableType.None;
	}

	public int GetBranchAttempts(int depth)
	{
		return BranchAttempts.GetBranchAttempts(depth);
	}

	public override int GetChecksum()
	{
		return (((((((((((base.GetChecksum() ^ (int)Weight) * 41) ^ MaxSize) * 41) ^ MaxDepth) * 41) ^ Direction.GetChecksum()) * 41) ^ BranchAttempts.GetChecksum()) * 41) ^ Momentum.GetChecksum()) * 41;
	}

	public override void Initialize(ModAbout mod)
	{
		if (IsValid())
		{
			DataCollection.Register(this, mod);
		}
	}

	public float GetMinedQuantity()
	{
		return Mathf.Max(MinDropQuantity, (float)Random.Range(MinDropQuantity, MaxDropQuantity) * (float)DifficultySetting.Current.MiningYield);
	}
}
