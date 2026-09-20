using Assets.Scripts.Objects.Entities;
using CharacterCustomisation.Clothing;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Clothing;

public class BodyArmor : Clothing, IBodyArmor, IReferencable, IEvaluable
{
	public int ColorMaterialIndex = 1;

	public override void RefreshVisibility()
	{
		base.RefreshVisibility();
		Human human = base.ParentSlot.Parent as Human;
		if (!human || !base.IsChild)
		{
			return;
		}
		foreach (SkinnedMeshRendererInstance skinnedMesh in SkinnedMeshes)
		{
			if (human.IsLocalPlayer)
			{
				skinnedMesh.Parent.GameObject.layer = Layers.PlayerImmune;
				skinnedMesh.Renderer.updateWhenOffscreen = true;
			}
			else
			{
				skinnedMesh.Parent.GameObject.layer = Layers.Default;
				skinnedMesh.Renderer.updateWhenOffscreen = false;
			}
		}
	}

	public override void SetWearableVisibility(bool clothingOn)
	{
		if (!base.IsChild)
		{
			return;
		}
		if (!(base.ParentSlot.Parent is Human human) || base.ParentSlot.Type != SlotType || !clothingOn)
		{
			SkinnedMeshes.Clear();
			return;
		}
		if (human.ToolbeltSlot.Occupant is ToolBelt toolBelt)
		{
			toolBelt.CheckSuitSlot();
		}
		SkinnedMeshes.Clear();
		ClothingItem kitItem = (human.IsMale ? _clothingMale : _clothingFemale);
		human.CosmeticsBehaviour.ApplyArmor(kitItem);
		if (human.UniformSlot.Occupant is IWearable wearable)
		{
			wearable.SetWearableVisibility(clothingOn: true);
		}
		SkinnedMeshRenderer armorRenderer = human.CosmeticsBehaviour.ArmorRenderer;
		SkinnedMeshRendererInstance skinnedMeshRendererInstance = new SkinnedMeshRendererInstance
		{
			Parent = this,
			Renderer = armorRenderer,
			PaintableIndex = ColorMaterialIndex
		};
		if (human.IsLocalPlayer)
		{
			armorRenderer.gameObject.layer = Layers.PlayerImmune;
			skinnedMeshRendererInstance.Renderer.updateWhenOffscreen = true;
		}
		else
		{
			armorRenderer.gameObject.layer = Layers.Default;
		}
		SkinnedMeshes.Add(skinnedMeshRendererInstance);
		foreach (SkinnedMeshRendererInstance skinnedMesh in SkinnedMeshes)
		{
			skinnedMesh.SetColor(CustomColor);
		}
	}

	public override bool MoveToSlot(Slot destinationSlot, Thing originThing, bool forced = false)
	{
		Human human = destinationSlot.Parent as Human;
		base.MoveToSlot(destinationSlot, originThing, forced);
		SetWearableVisibility(destinationSlot == human?.SuitSlot);
		if (human != null && human.ToolbeltSlot.Occupant is ToolBelt toolBelt)
		{
			toolBelt.CheckSuitSlot();
		}
		return true;
	}

	public override bool MoveToWorld(Vector3 worldPosition, Quaternion worldRotation, Vector3 velocity, Vector3 angularVelocity, float force = 0f)
	{
		if (base.ParentSlot?.Parent != null)
		{
			Human human = base.ParentSlot.Parent as Human;
			if (human != null && human.ToolbeltSlot.Occupant is ToolBelt toolBelt)
			{
				toolBelt.SwapMesh(suitEquip: false);
			}
		}
		SetWearableVisibility(clothingOn: false);
		return base.MoveToWorld(worldPosition, worldRotation, velocity, angularVelocity, force);
	}
}
