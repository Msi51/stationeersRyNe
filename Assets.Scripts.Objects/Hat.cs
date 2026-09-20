using Assets.Scripts.Objects.Entities;

namespace Assets.Scripts.Objects;

public class Hat : CharacterItem
{
	public Human ParentHuman => base.ParentSlot?.Occupant as Human;

	public virtual void EnterInventory()
	{
		if (ParentHuman != null && ParentHuman.IsLocalPlayer)
		{
			for (int i = 0; i < Renderers.Count; i++)
			{
				Renderers[i].BaseLayer = Layers.PlayerInvisible;
			}
		}
	}

	public virtual void ExitInventory(Human oldParent)
	{
		if (oldParent != null && oldParent.IsLocalPlayer)
		{
			for (int i = 0; i < Renderers.Count; i++)
			{
				Renderers[i].BaseLayer = Human.LayerDefault;
				Renderers[i].SetLayer(Human.LayerDefault);
			}
		}
	}

	public override void OnParentLocalityChange()
	{
		base.OnParentLocalityChange();
		EnterInventory();
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		EnterInventory();
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		ExitInventory(oldParent as Human);
	}
}
