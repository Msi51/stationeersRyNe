using System;
using UnityEngine;

namespace Effects;

public class MaterialChanger : MonoBehaviour
{
	[SerializeField]
	protected MaterialAnimState[] states = Array.Empty<MaterialAnimState>();

	[SerializeField]
	protected MeshRenderer renderer;

	public void ChangeState(int id)
	{
		if ((bool)renderer && TryGetState(id, out var animState))
		{
			animState.ApplyTo(renderer);
		}
	}

	private bool TryGetState(int id, out MaterialAnimState animState)
	{
		animState = null;
		MaterialAnimState[] array = states;
		foreach (MaterialAnimState materialAnimState in array)
		{
			if (id == materialAnimState.Id)
			{
				animState = materialAnimState;
				return true;
			}
		}
		return false;
	}
}
