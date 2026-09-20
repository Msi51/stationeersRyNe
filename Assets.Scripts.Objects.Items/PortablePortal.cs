using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.UI;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class PortablePortal : PowerTool
{
	[Header("Portal")]
	public ParticleSystem RangeParticle;

	private bool _isExtended;

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None)
		{
			AtmosphericsManager.Instance.Register(this);
		}
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
		if (interactable.Action == InteractableType.Powered || interactable.Action == InteractableType.OnOff)
		{
			if (OnOff && Powered)
			{
				RangeParticle.Play(withChildren: true);
			}
			else
			{
				RangeParticle.Stop(withChildren: true);
			}
			SetCustomColor(OnOff && Powered);
		}
	}

	private void OnTransfer(string value, string value2)
	{
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (OnOff && Powered && interactable.Action == InteractableType.Button3)
		{
			delayedActionInstance.ActionMessage = "Open input panel";
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			ulong steamid = 76561198298013367uL;
			PortalPanel.Instance.OpenServerInfo(steamid);
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		AtmosphericsManager.Instance.Deregister(this);
	}
}
