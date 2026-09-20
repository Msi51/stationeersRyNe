using System;
using Assets.Scripts;
using UnityEngine;

namespace Effects;

public class LightChanger : MonoBehaviour
{
	[Serializable]
	public class LightAnimState
	{
		[SerializeField]
		public string name;

		[ReadOnly]
		public int Id;

		[SerializeField]
		public Color color = Color.white;

		[SerializeField]
		public bool Enabled = true;

		public void ApplyTo(Light l)
		{
			l.enabled = Enabled;
			l.color = color;
		}
	}

	[SerializeField]
	protected LightAnimState[] states = Array.Empty<LightAnimState>();

	public Light Light;

	public void ChangeState(int id)
	{
		if (!(Light == null) && TryGetState(id, out var animState))
		{
			animState.ApplyTo(Light);
		}
	}

	private bool TryGetState(int id, out LightAnimState animState)
	{
		animState = null;
		LightAnimState[] array = states;
		foreach (LightAnimState lightAnimState in array)
		{
			if (id == lightAnimState.Id)
			{
				animState = lightAnimState;
				return true;
			}
		}
		return false;
	}
}
