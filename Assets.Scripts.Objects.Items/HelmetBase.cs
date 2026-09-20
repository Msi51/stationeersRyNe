using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Clothing;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class HelmetBase : CharacterItem, IWearable, IReferencable, IEvaluable
{
	private static string _lightString = "Light {0}";

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.OnOff)
		{
			return string.Format(_lightString, OnOff ? ActionStrings.Off : ActionStrings.On);
		}
		return base.GetContextualName(interactable);
	}

	private void SetGameObjectLayer()
	{
		LayerMask layerMask = (((bool)RootParentHuman && RootParentHuman.IsLocalPlayer) ? Layers.PlayerInvisible : Layers.Default);
		foreach (ThingRenderer renderer in Renderers)
		{
			renderer.BaseLayer = layerMask;
			renderer.SetLayer(layerMask);
		}
	}

	public override void OnParentLocalityChange()
	{
		base.OnParentLocalityChange();
		SetGameObjectLayer();
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		if (OnOff && GameManager.RunSimulation && parent is Structure)
		{
			OnServer.Interact(base.InteractOnOff, 0);
		}
		SetGameObjectLayer();
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		SetGameObjectLayer();
	}

	public void SetWearableVisibility(bool clothingOn)
	{
	}

	public void RefreshVisibility()
	{
	}
}
