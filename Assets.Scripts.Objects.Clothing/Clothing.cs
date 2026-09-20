using System.Collections.Generic;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using CharacterCustomisation.Clothing;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Clothing;

public abstract class Clothing : CharacterItem, IWearable, IReferencable, IEvaluable
{
	[Header("Clothing")]
	[SerializeField]
	public ClothingItem _clothingMale;

	[SerializeField]
	public ClothingItem _clothingFemale;

	public List<SkinnedMeshRendererInstance> SkinnedMeshes = new List<SkinnedMeshRendererInstance>();

	public override bool MoveToSlot(Slot destinationSlot, Thing originThing, bool forced = false)
	{
		if (base.IsChild)
		{
			_ = base.ParentSlot.Type == SlotType;
		}
		else
			_ = 0;
		base.MoveToSlot(destinationSlot, originThing, forced);
		SetWearableVisibility(SlotType == destinationSlot.Type);
		return true;
	}

	public override bool MoveToWorld(Vector3 worldPosition, Quaternion worldRotation, Vector3 velocity, Vector3 angularVelocity, float force = 0f)
	{
		SetWearableVisibility(clothingOn: false);
		return base.MoveToWorld(worldPosition, worldRotation, velocity, angularVelocity, force);
	}

	public void RefreshUniform()
	{
		SetWearableVisibility(clothingOn: false);
		SetWearableVisibility(clothingOn: true);
	}

	public override void OnParentLocalityChange()
	{
		base.OnParentLocalityChange();
		if (base.ParentSlot.Parent is Human human && base.ParentSlot.Type == SlotType)
		{
			human.SetWearable().Forget();
		}
	}

	public virtual void SetWearableVisibility(bool clothingOn)
	{
	}

	public virtual void RefreshVisibility()
	{
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.PersonalClothing);
	}

	public virtual int GetCurrencySlot()
	{
		return 0;
	}

	protected void SetWearableVisibleInternal(bool isArmor, int colorMaterialIndex = 1)
	{
		if (!base.IsChild || !(base.ParentSlot.Parent is Human human))
		{
			return;
		}
		SkinnedMeshes.Clear();
		if (human.ShowUniform)
		{
			ClothingItem kitItem = (human.IsMale ? _clothingMale : _clothingFemale);
			if (isArmor)
			{
				human.CosmeticsBehaviour.ApplyArmor(kitItem);
			}
			else
			{
				human.CosmeticsBehaviour.ApplyClothing(kitItem);
			}
		}
		SkinnedMeshRenderer skinnedMeshRenderer = (isArmor ? human.CosmeticsBehaviour.ArmorRenderer : human.CosmeticsBehaviour.BodyRenderer);
		SkinnedMeshRendererInstance item = new SkinnedMeshRendererInstance
		{
			Parent = this,
			Renderer = skinnedMeshRenderer,
			PaintableIndex = colorMaterialIndex
		};
		if (human.IsLocalPlayer)
		{
			skinnedMeshRenderer.gameObject.layer = Layers.PlayerImmune;
		}
		SkinnedMeshes.Add(item);
	}
}
