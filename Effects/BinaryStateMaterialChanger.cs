using Assets.Scripts.Objects;
using UnityEngine;

namespace Effects;

public class BinaryStateMaterialChanger : StateMaterialChanger
{
	[SerializeField]
	private Material[] _state0;

	[SerializeField]
	private Material[] _state1;

	[SerializeField]
	private InteractableType _interactableType;

	private Interactable _interactable;

	private bool _initialized;

	private int InteractableState
	{
		get
		{
			if (!_initialized)
			{
				return 0;
			}
			return _interactable.State;
		}
	}

	public override void RefreshState(bool skipAnimation)
	{
		Init();
		switch (InteractableState)
		{
		case 0:
			ChangeMaterials(_state0);
			break;
		case 1:
			ChangeMaterials(_state1);
			break;
		}
	}

	private void Init()
	{
		if (!_initialized && _interactable == null && !(Parent == null))
		{
			_interactable = Parent.GetInteractable(_interactableType);
			_initialized = true;
		}
	}

	private void ChangeMaterials(Material[] materials)
	{
		Material[] sharedMaterials = Renderer.sharedMaterials;
		for (int i = 0; i < materials.Length; i++)
		{
			if (!(materials[i] == null))
			{
				sharedMaterials[i] = materials[i];
			}
		}
		Renderer.materials = sharedMaterials;
	}
}
