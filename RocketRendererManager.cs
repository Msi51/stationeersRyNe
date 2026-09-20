using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Util;
using Messages;
using UnityEngine;
using UnityEngine.Rendering;

public class RocketRendererManager : ManagerBase
{
	public List<InstancedIndirectDrawCall> AllManagedDrawCalls = new List<InstancedIndirectDrawCall>();

	public static RocketRendererManager instance;

	public Dictionary<RocketRendererInstance, List<InstancedIndirectDrawCall>> RenderInstanceDrawCallLookup = new Dictionary<RocketRendererInstance, List<InstancedIndirectDrawCall>>();

	public Shader StandardInstancedShader;

	public ComputeShader CullingComputeShader;

	public ComputeShader ExtractFrustumShader;

	public Material DefaultGreyMaterial;

	private List<RocketLightRenderer> AllRocketLightRenderers = new List<RocketLightRenderer>();

	private List<RocketRendererInstance> _allRendererInstances = new List<RocketRendererInstance>();

	[SerializeField]
	[ReadOnly]
	[Tooltip("Whether overriding Unity's renderers (MeshRenderer etc) with virtualized DrawMeshInstancedIndirect draws.")]
	private bool _instanceRender;

	public bool InstanceRender
	{
		get
		{
			return _instanceRender;
		}
		set
		{
			_instanceRender = value;
			SetInstancedRendererEnabled(value);
		}
	}

	public override void ManagerStart()
	{
		base.ManagerStart();
		instance = this;
		InstanceRender = false;
	}

	private void OnDestroy()
	{
		instance = null;
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		if (InstanceRender)
		{
			foreach (InstancedIndirectDrawCall allManagedDrawCall in AllManagedDrawCalls)
			{
				try
				{
					allManagedDrawCall.Draw(allManagedDrawCall.Bounds, ShadowCastingMode.On);
				}
				catch (Exception message)
				{
					Debug.LogError(message);
				}
			}
		}
		foreach (RocketLightRenderer allRocketLightRenderer in AllRocketLightRenderers)
		{
			if (allRocketLightRenderer.isActiveAndEnabled != InstanceRender)
			{
				allRocketLightRenderer.enabled = false;
			}
		}
	}

	private void SetInstancedRendererEnabled(bool enableInstancedRendering)
	{
		_ = RenderInstanceDrawCallLookup.Keys;
		GameManager.EventBus.Publish(new InstancedRendererSettingChangedMessage());
	}

	public void Register(RocketRendererInstance renderInstance)
	{
		for (int i = 0; i < renderInstance.Materials.Length; i++)
		{
			if (!(renderInstance.Materials[i] == null))
			{
				if (!RenderInstanceDrawCallLookup.ContainsKey(renderInstance))
				{
					RenderInstanceDrawCallLookup[renderInstance] = new List<InstancedIndirectDrawCall>(renderInstance.Materials.Length);
				}
				_ = RenderInstanceDrawCallLookup[renderInstance];
			}
		}
		_allRendererInstances.Add(renderInstance);
	}

	public void RegisterLight(RocketLightRenderer lightRenderer)
	{
		AllRocketLightRenderers.Add(lightRenderer);
	}

	public void Deregister(RocketRendererInstance renderInstance)
	{
		if (!RenderInstanceDrawCallLookup.TryGetValue(renderInstance, out var value))
		{
			return;
		}
		foreach (InstancedIndirectDrawCall item in value)
		{
			_ = item;
		}
		RenderInstanceDrawCallLookup.Remove(renderInstance);
	}

	public void DeregisterLight(RocketLightRenderer lightRenderer)
	{
		AllRocketLightRenderers.Remove(lightRenderer);
	}
}
