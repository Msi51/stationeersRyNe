using Assets.Scripts.Objects;
using UnityEngine;

namespace Objects.Structures;

public class Frame : LargeStructure
{
	public override Material SelectColorSwatchMaterial(bool emissive)
	{
		if (!emissive)
		{
			return CustomColor.Normal;
		}
		return CustomColor.Emissive;
	}

	public override void Awake()
	{
		base.Awake();
		if (CustomColor != null)
		{
			SetCustomColor(CustomColor.Index);
		}
	}
}
