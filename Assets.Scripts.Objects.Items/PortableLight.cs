using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Entities;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class PortableLight : PowerTool
{
	private bool _isExtended;

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None)
		{
			AtmosphericsManager.Instance.Register(this);
		}
	}

	public override void OnAncestryChangeAsChild()
	{
		base.OnAncestryChangeAsChild();
		HandleInventoryChange();
	}

	public override void OnParentLocalityChange()
	{
		base.OnParentLocalityChange();
		HandleInventoryChange();
	}

	private void HandleInventoryChange()
	{
		if (((base.ParentSlot?.Parent != null) ? base.ParentSlot.Parent.RootParent : null) is Human { IsLocalPlayer: not false })
		{
			{
				foreach (ThingLight light in Lights)
				{
					int cullingMask = light.Light.cullingMask;
					if (light.Light.shadows == LightShadows.None && cullingMask == (cullingMask | (1 << (int)Layers.Player)))
					{
						light.Light.enabled = true;
						continue;
					}
					int cullingMask2 = light.Light.cullingMask;
					cullingMask2 &= ~(1 << (int)Layers.Player);
					cullingMask2 &= ~(1 << (int)Layers.PlayerInvisible);
					light.Light.cullingMask = cullingMask2;
				}
				return;
			}
		}
		foreach (ThingLight light2 in Lights)
		{
			int cullingMask3 = light2.Light.cullingMask;
			if (light2.Light.shadows == LightShadows.None && cullingMask3 == (cullingMask3 | (1 << (int)Layers.Player)))
			{
				light2.Light.enabled = false;
			}
			else
			{
				light2.Light.cullingMask = light2.LayerMask;
			}
		}
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		HandleInventoryChange();
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		HandleInventoryChange();
	}

	public override void OnAnimationStart()
	{
		base.OnAnimationStop();
		if (!OnOff)
		{
			if ((bool)PaintableMaterial && CustomColor.Emissive == null)
			{
				CustomColor = GameManager.GetColorSwatch(PaintableMaterial);
			}
			SetCustomColor(OnOff && Powered);
			_isExtended = false;
		}
	}

	public override void OnAnimationStop()
	{
		base.OnAnimationStop();
		if (OnOff)
		{
			if ((bool)PaintableMaterial && CustomColor.Emissive == null)
			{
				CustomColor = GameManager.GetColorSwatch(PaintableMaterial);
			}
			SetCustomColor(OnOff && Powered);
			_isExtended = true;
		}
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		base.SetCustomColor(index, emissive);
		foreach (ThingLight light in Lights)
		{
			light.Light.color = CustomColor.Light;
		}
		SetCustomColor(OnOff && Powered);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Powered)
		{
			SetCustomColor(OnOff && Powered);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		AtmosphericsManager.Instance.Deregister(this);
	}
}
