using System;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.DeviceParts;

public class TwoStateButton : GameBase
{
	[Serializable]
	private class ButtonState
	{
		[SerializeField]
		private Material[] _materials;

		public void ApplyTo(MeshRenderer renderer)
		{
			Material[] sharedMaterials = renderer.sharedMaterials;
			for (int i = 0; i < _materials.Length; i++)
			{
				if ((bool)_materials[i])
				{
					sharedMaterials[i] = _materials[i];
				}
			}
			renderer.materials = sharedMaterials;
		}
	}

	[SerializeField]
	private Thing _parentThing;

	[SerializeField]
	private MeshRenderer _meshRenderer;

	[SerializeField]
	private ButtonState _on;

	[SerializeField]
	private ButtonState _off;

	private int _previous = -1;

	public void ChangeState(int onOff, bool skipAnimation)
	{
		if (onOff == 0)
		{
			_off.ApplyTo(_meshRenderer);
			if (!skipAnimation && onOff != _previous && _parentThing.IsAudible())
			{
				_parentThing.PlayPooledAudioSound(Defines.Sounds.SwitchOff, Transform.localPosition);
			}
		}
		else
		{
			_on.ApplyTo(_meshRenderer);
			if (!skipAnimation && onOff != _previous && _parentThing.IsAudible())
			{
				_parentThing.PlayPooledAudioSound(Defines.Sounds.SwitchOn, Transform.localPosition);
			}
		}
		_previous = onOff;
	}
}
