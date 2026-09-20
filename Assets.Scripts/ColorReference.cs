using UnityEngine;

namespace Assets.Scripts;

public abstract class ColorReference : IChecksum
{
	public ColorReference()
	{
	}

	public ColorReference(Color color)
	{
	}

	public abstract Color ToColor();

	public static implicit operator Color(ColorReference reference)
	{
		return reference.ToColor();
	}

	public abstract int GetChecksum();
}
