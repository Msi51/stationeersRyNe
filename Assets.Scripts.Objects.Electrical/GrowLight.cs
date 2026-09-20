using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class GrowLight : WallLight, ILightActivated, IDensePoolable
{
	[SerializeField]
	private Material BulbMaterial;

	private ColorSwatch BulbColorSwatch;

	public BoxCollider LocalBoxCollider;

	private readonly List<IGrower> _plantersAffected = new List<IGrower>();

	protected override bool NeverCastShadows => true;

	public override void OnDestroy()
	{
		foreach (IGrower item in _plantersAffected)
		{
			item?.LinkedGrowLights.Remove(this);
		}
		base.OnDestroy();
	}

	public override void Awake()
	{
		base.Awake();
		if ((bool)BulbMaterial)
		{
			BulbColorSwatch = GameManager.GetColorSwatch(BulbMaterial);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		Collider[] array = Physics.OverlapBox(LocalBoxCollider.transform.position, LocalBoxCollider.size / 2f);
		foreach (Collider collider in array)
		{
			if (Thing.Find<IGrower>(collider) != null)
			{
				HandleEnterTrigger(collider);
			}
		}
	}

	public override void OnAnimationStop()
	{
		SetBulbMaterial(OnOff && Powered);
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		base.SetCustomColor(index, false);
		SetBulbMaterial(OnOff && Powered);
	}

	public override void SetCustomColor(bool emissive = false)
	{
		base.SetCustomColor(emissive: false);
		SetBulbMaterial(OnOff && Powered);
	}

	private void SetBulbMaterial(bool emissive)
	{
		if ((object)BulbColorSwatch.Normal == null)
		{
			return;
		}
		foreach (CustomColorMapping customMaterial in _customMaterials)
		{
			if (customMaterial.ThingRenderer.GetRendererGameObject().CompareTag("NotPaintable") || customMaterial.name != "Bulb")
			{
				continue;
			}
			if (emissive)
			{
				for (int i = 0; i < customMaterial.ThingRenderer.sharedMaterials.Length; i++)
				{
					customMaterial.MaterialIndex = i;
					customMaterial.SetEmissive(BulbColorSwatch.Emissive);
				}
			}
			else
			{
				for (int j = 0; j < customMaterial.ThingRenderer.sharedMaterials.Length; j++)
				{
					customMaterial.MaterialIndex = j;
					customMaterial.SetColor(BulbColorSwatch.Normal, BulbColorSwatch.Index);
				}
			}
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.GameState == GameState.Running && interactable.Action == InteractableType.Powered)
		{
			SetCustomColor(OnOff && Powered);
		}
	}

	public override void SetLightsCustomColor()
	{
	}

	public void HandleExitTrigger(Collider other)
	{
		Thing thing = Thing.Find(other);
		if (!(thing == null) && thing is IGrower grower)
		{
			_plantersAffected.Remove(grower);
			grower.LinkedGrowLights.Remove(this);
		}
	}

	public void HandleEnterTrigger(Collider other)
	{
		Thing thing = Thing.Find(other);
		if ((object)thing != null && thing is IGrower grower && !_plantersAffected.Contains(grower))
		{
			_plantersAffected.Add(grower);
			grower.LinkedGrowLights.Add(this);
		}
	}
}
