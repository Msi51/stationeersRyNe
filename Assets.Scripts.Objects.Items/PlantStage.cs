using System;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

[Serializable]
public class PlantStage
{
	[Tooltip("Which visualizer is enabled when this stage is displayed")]
	public Renderer Visualizer;

	[Tooltip("Length of time in seconds to advance")]
	public float Length = 5f;

	[Tooltip("When set to true the plant is Seeding")]
	public bool Seed;

	[Tooltip("When set to true the plant is harvestable")]
	public bool Mature;

	public bool Dead;
}
