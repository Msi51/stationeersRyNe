using Assets.Scripts;
using Assets.Scripts.Localization2;
using TMPro;
using UnityEngine;

namespace UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class InitializeTextFromGameString : MonoBehaviour
{
	[SerializeField]
	private string _gameStringKey;

	[SerializeField]
	private TextMeshProUGUI _textMesh;

	[ReadOnly]
	[SerializeField]
	private int _keyHash;

	private bool _initialized;

	private void OnEnable()
	{
		if (!_initialized && Assets.Scripts.Localization2.GameString.TryGet(_keyHash, out var gameString))
		{
			_textMesh.text = gameString.DisplayString;
			_initialized = true;
		}
	}

	private void OnValidate()
	{
		_keyHash = Animator.StringToHash(_gameStringKey);
		_textMesh = GetComponent<TextMeshProUGUI>();
	}
}
