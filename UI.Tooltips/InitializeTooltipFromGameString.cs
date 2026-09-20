using Assets.Scripts;
using Assets.Scripts.Localization2;
using UnityEngine;

namespace UI.Tooltips;

public class InitializeTooltipFromGameString : MonoBehaviour
{
	[SerializeField]
	private string _gameStringKey;

	[SerializeField]
	private UITooltip _tooltip;

	[ReadOnly]
	[SerializeField]
	private int _keyHash;

	private bool _initialized;

	private void OnEnable()
	{
		if (!_initialized && Assets.Scripts.Localization2.GameString.TryGet(_keyHash, out var gameString))
		{
			_tooltip.TooltipText = gameString.DisplayString;
			_initialized = true;
		}
	}

	private void OnValidate()
	{
		_keyHash = Animator.StringToHash(_gameStringKey);
	}
}
