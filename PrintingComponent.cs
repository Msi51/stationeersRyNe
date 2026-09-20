using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using UnityEngine;
using UnityEngine.Rendering;

public class PrintingComponent : MonoBehaviour
{
	public Fabricator ParentFabricator;

	public Thing SwappingThing;

	public float Progress;

	public static Material PrintingMaterial;

	private float _lastProgress;

	private Dictionary<ThingRenderer, Material[]> _origionalMaterials;

	private bool _materialsSwapped;

	private void Update()
	{
		UpdatePrintingVisuals();
	}

	public void UpdatePrintingVisuals()
	{
		if (SwappingThing == null)
		{
			return;
		}
		float value = Mathf.Lerp(ParentFabricator.Progress, _lastProgress, 0.5f);
		Renderer[] componentsInChildren = SwappingThing.GetComponentsInChildren<Renderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Material[] materials = componentsInChildren[i].materials;
			foreach (Material obj in materials)
			{
				obj.SetFloat("_ConstructionLevel", value);
				obj.SetFloat("_ObjectHeight", SwappingThing.Bounds.size.y);
				obj.SetVector("_ObjectPosition", SwappingThing.CenterPosition);
			}
		}
		_lastProgress = ParentFabricator.Progress;
		if (ParentFabricator.Progress >= 1f)
		{
			RemovePrintingComponent(SwappingThing);
		}
	}

	private void SwapMaterial()
	{
		if (_materialsSwapped)
		{
			return;
		}
		_origionalMaterials = new Dictionary<ThingRenderer, Material[]>(SwappingThing.Renderers.Count);
		foreach (ThingRenderer renderer in SwappingThing.Renderers)
		{
			renderer.SetShadowCastMode(ShadowCastingMode.Off);
			if (SwappingThing.BaseAnimator != null)
			{
				SwappingThing.BaseAnimator.enabled = false;
			}
			_origionalMaterials.Add(renderer, renderer.Materials);
			Material[] array = new Material[renderer.Materials.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = PrintingMaterial;
			}
			renderer.Materials = array;
		}
		_materialsSwapped = true;
	}

	private void RestoreMaterials()
	{
		if (!_materialsSwapped)
		{
			return;
		}
		for (int i = 0; i < SwappingThing.Renderers.Count; i++)
		{
			ThingRenderer thingRenderer = SwappingThing.Renderers[i];
			thingRenderer.SetShadowCastMode(ShadowCastingMode.On);
			if (SwappingThing.BaseAnimator != null)
			{
				SwappingThing.BaseAnimator.enabled = true;
			}
			_origionalMaterials.TryGetValue(thingRenderer, out var value);
			if (value != null)
			{
				thingRenderer.Materials = value;
			}
		}
		_materialsSwapped = false;
	}

	public void Initialise()
	{
		SwappingThing = GetComponent<Thing>();
		SwapMaterial();
	}

	public static void AddPrintingComponent(Thing obj)
	{
		if (GameManager.RunSimulation)
		{
			OnServer.AddPrintingComponent(obj);
		}
	}

	public static void AttachPrintingComponent(Thing obj)
	{
		if (PrintingMaterial == null)
		{
			PrintingMaterial = Resources.Load<Material>("Objects/Models/Materials/3dPrinterMat");
		}
		PrintingComponent printingComponent = obj.gameObject.AddComponent<PrintingComponent>();
		printingComponent.ParentFabricator = obj.GetComponentInParent<Fabricator>();
		printingComponent.Initialise();
	}

	public static void RemovePrintingComponent(Thing obj)
	{
		PrintingComponent component = obj.GetComponent<PrintingComponent>();
		if (!(component == null))
		{
			component.RestoreMaterials();
			Object.Destroy(component);
		}
	}
}
