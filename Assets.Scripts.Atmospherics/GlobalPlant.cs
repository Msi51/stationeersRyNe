using System.Collections.Generic;
using Assets.Scripts.Genetics;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Atmospherics;

public class GlobalPlant
{
	public Plant PlantPrefab;

	public GeneCollection GeneCollection;

	public int Count;

	public readonly Material Material;

	public readonly Mesh Mesh;

	public const float MaxSlope = 50f;

	public static readonly FloatRangeData Scale = new FloatRangeData
	{
		Min = 1f,
		Max = 1.5f
	};

	public static readonly Vector3 MaxRotationAngle = new Vector3(10f, 180f, 10f);

	private const double CHUNKS_PER_WORLD = 250000.0;

	private const double PLANT_SPREAD = 83333.33333333333;

	public double InstancesPerChunk => (double)Count / 83333.33333333333;

	public GlobalPlant(Plant plantPrefab)
	{
		PlantPrefab = plantPrefab;
		GeneCollection = new GeneCollection();
		GeneCollection.Reset();
		Count = 0;
		Material = Object.Instantiate(WorldManager.Instance.ClutterMaterial);
		List<PlantStage> growthStates = PlantPrefab.GrowthStates;
		MeshFilter component = growthStates[growthStates.Count - 2].Visualizer.gameObject.GetComponent<MeshFilter>();
		List<PlantStage> growthStates2 = PlantPrefab.GrowthStates;
		Texture mainTexture = growthStates2[growthStates2.Count - 2].Visualizer.material.mainTexture;
		Mesh = component?.sharedMesh;
		Material.SetTexture("_MainTex", mainTexture);
	}

	public void Write(RocketBinaryWriter writer)
	{
		GeneCollection.Write(writer);
		writer.WriteInt32(Count);
	}

	public void Read(RocketBinaryReader reader)
	{
		GeneCollection.Read(reader);
		Count = reader.ReadInt32();
	}
}
