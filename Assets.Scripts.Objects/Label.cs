using System.Collections;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class Label : DevicePipeMounted
{
	private GameObject _cameraObject;

	private Material _material;

	private RenderTexture _textTexture;

	public MeshRenderer MeshRenderer;

	public int materialLabelIndex;

	public int textTextureWidth = 512;

	public int textTextureHeight = 128;

	private static readonly WaitForEndOfFrame WaitFrame = new WaitForEndOfFrame();

	private static bool HasDrawn = false;

	public virtual string DefaultString => "Pipe Label";

	public override void Awake()
	{
		base.Awake();
		_textTexture = new RenderTexture(textTextureWidth, textTextureHeight, 0);
		_cameraObject = CursorManager.Instance.TextCamera.gameObject;
		if (MeshRenderer != null)
		{
			_material = MeshRenderer.materials[materialLabelIndex];
			_material.mainTexture = _textTexture;
			ChangeText(DefaultString);
		}
	}

	public override void OnGridPlaced(SmallGrid newOccupant)
	{
		base.OnGridPlaced(newOccupant);
		Pipe pipe = GridController.GetController(CenterPosition)?.GetSmallCell(CenterPosition)?.Pipe;
		if (pipe != null)
		{
			base.transform.localScale = new Vector3(pipe.LabelSizeOffset, pipe.LabelSizeOffset, pipe.LabelSizeOffset);
		}
	}

	public override bool IsContentMatch(Pipe.ContentType inContentType)
	{
		return true;
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

	public void ChangeText(string labelText)
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
}
