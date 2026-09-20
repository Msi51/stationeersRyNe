using System.Collections;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class Sign : SmallDevice, ISmartRotatable
{
	public int textTextureWidth = 512;

	public int textTextureHeight = 128;

	public MeshRenderer MeshRenderer;

	public int materialLabelIndex;

	private GameObject _cameraObject;

	private Material _material;

	private RenderTexture _textTexture;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	private static readonly WaitForEndOfFrame WaitFrame = new WaitForEndOfFrame();

	private static bool HasDrawn = false;

	private string _defaultString => string.Empty;

	public override void Awake()
	{
		base.Awake();
		_textTexture = new RenderTexture(textTextureWidth, textTextureHeight, 0);
		_cameraObject = CursorManager.Instance.TextCamera.gameObject;
		if (MeshRenderer != null)
		{
			_material = MeshRenderer.materials[materialLabelIndex];
			_material.mainTexture = _textTexture;
			ChangeText(_defaultString);
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		ChangeText(DisplayName);
	}

	public override void OnFinishedThingSync()
	{
		base.OnFinishedThingSync();
		ChangeText(DisplayName);
	}

	public override void OnRenamed()
	{
		base.OnRenamed();
		ChangeText(DisplayName);
	}

	private void ChangeText(string labelText)
	{
		RedrawTextTexture(labelText);
	}

	private void RedrawTextTexture(string labelText)
	{
		if (!GameManager.IsBatchMode)
		{
			StartCoroutine(Draw(labelText));
		}
	}

	private void SetupCamera()
	{
		if (!GameManager.IsBatchMode)
		{
			CursorManager.Instance.TextCamera.targetTexture = _textTexture;
		}
	}

	private IEnumerator Draw(string labelText)
	{
		if (!GameManager.IsBatchMode)
		{
			do
			{
				yield return WaitFrame;
			}
			while (HasDrawn);
			HasDrawn = true;
			_cameraObject.SetActive(value: true);
			SetupCamera();
			CursorManager.Instance.LabelTextField.text = labelText;
			yield return WaitFrame;
			CursorManager.Instance.TextCamera.Render();
			_cameraObject.SetActive(value: false);
			HasDrawn = false;
		}
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}
}
