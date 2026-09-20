using System;
using System.Collections.Generic;
using Rendering;
using UnityEngine;

namespace Assets.Scripts.Objects;

[Serializable]
public class BuildState
{
	[Header("Build State")]
	public Sprite Thumbnail;

	public bool DamagedBuildState;

	public bool BlockAir;

	public bool AlwaysBlockAir;

	public bool BlockLight;

	public bool BlockGravity;

	public BuildStateRenderMode RenderMode;

	public Renderer Visualizer;

	public List<Mesh> StateMeshes = new List<Mesh>();

	public ThingRenderer RendererInstance;

	public float TemperatureRadiationFactor;

	public ToolUse Tool;

	public BuildStateColliders ColliderMode;

	public List<Collider> Colliders = new List<Collider>();

	public List<GameObject> LinkedGameObjects = new List<GameObject>();

	public bool CanManufacture;

	public BuildStateManufacturingDat ManufactureDat;

	public GameObject InteractableParent;

	public List<Interactable> Interactables = new List<Interactable>();

	[SerializeField]
	[Tooltip("Contains mesh and material data for Batch Renderer. Set up on prefab to replace usage of MeshRenderer")]
	private DrawData initialDrawData;

	public DrawData InitialDrawData => initialDrawData;
}
