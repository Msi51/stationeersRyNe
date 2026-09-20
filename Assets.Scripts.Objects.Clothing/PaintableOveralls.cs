using UnityEngine;

namespace Assets.Scripts.Objects.Clothing;

public class PaintableOveralls : Uniform
{
	public int ColorMaterialIndex;

	protected override bool HasPaintableMaskMaterial => true;

	public override void SetWearableVisibility(bool clothingOn)
	{
		if (clothingOn)
		{
			SetWearableVisibleInternal(isArmor: false, ColorMaterialIndex);
		}
		ApplyMaskColor(force: true);
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		base.SetCustomColor(index, emissive);
		if (CustomColor != null)
		{
			HandlePaintableMaskMaterial();
			ApplyMaskColor(force: false);
		}
	}

	private void HandlePaintableMaskMaterial()
	{
		Material material = Renderers[0].GetRenderer().material;
		CustomColor.ApplyToSuitMaterial(material);
	}

	private void ApplyMaskColor(bool force)
	{
		if (CustomColor == null)
		{
			return;
		}
		foreach (SkinnedMeshRendererInstance skinnedMesh in SkinnedMeshes)
		{
			skinnedMesh.SetColorMaskOnMainMaterial = true;
			skinnedMesh.SetColor(CustomColor, force);
		}
	}
}
