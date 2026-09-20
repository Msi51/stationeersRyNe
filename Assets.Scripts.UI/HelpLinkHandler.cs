using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class HelpLinkHandler : UserInterfaceBase, IPointerClickHandler, IEventSystemHandler, IPointerDownHandler, IPointerUpHandler
{
	public bool doesColorChangeOnHover = true;

	public Color hoverColor = new Color(0.23529412f, 0.47058824f, 1f);

	public TextMeshProUGUI Parent;

	public Canvas Canvas;

	private Camera _pCamera;

	public bool ForceOpen;

	private int _pCurrentLink = -1;

	private List<Color32[]> _pOriginalVertexColors = new List<Color32[]>();

	private bool _isBusy;

	public bool IsLinkHighlighted => _pCurrentLink != -1;

	protected virtual void Awake()
	{
	}

	public void Clear()
	{
		SetLinkToColor(_pCurrentLink, (int linkIdx, int vertIdx) => _pOriginalVertexColors[linkIdx][vertIdx]);
		_pOriginalVertexColors.Clear();
		_pCurrentLink = -1;
	}

	private void LateUpdate()
	{
		if (WorldManager.IsGamePaused)
		{
			return;
		}
		int num = ((TMP_TextUtilities.IsIntersectingRectTransform(Parent.rectTransform, Input.mousePosition, _pCamera) && !_isBusy) ? TMP_TextUtilities.FindIntersectingLink(Parent, Input.mousePosition, _pCamera) : (-1));
		if (_pCurrentLink != -1)
		{
			_ = _pCurrentLink;
		}
		if (num != -1 && num != _pCurrentLink)
		{
			_pCurrentLink = num;
			if (doesColorChangeOnHover)
			{
				_pOriginalVertexColors = SetLinkToColor(num);
			}
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		int num = TMP_TextUtilities.FindIntersectingLink(Parent, Input.mousePosition, _pCamera);
		if (num == -1)
		{
			return;
		}
		TMP_LinkInfo tMP_LinkInfo = Parent.textInfo.linkInfo[num];
		string linkID = tMP_LinkInfo.GetLinkID();
		if (linkID == "Clipboard")
		{
			if (!Stationpedia.Instance.BaseAnimator.GetBool("Copied"))
			{
				Stationpedia.Instance.BaseAnimator.SetBool("Copied", value: true);
			}
			GameManager.Clipboard = tMP_LinkInfo.GetLinkText();
		}
		else if (ForceOpen)
		{
			Stationpedia.OpenAt(linkID);
		}
		else
		{
			Stationpedia.Instance.SetPage(linkID);
		}
	}

	private IEnumerator LinkNextFrame(string linkId)
	{
		yield return Yielders.EndOfFrame;
		yield return Yielders.EndOfFrame;
		_isBusy = false;
	}

	private List<Color32[]> SetLinkToColor(int linkIndex, Func<int, int, Color32> colorForLinkAndVert)
	{
		TMP_LinkInfo tMP_LinkInfo = Parent.textInfo.linkInfo[linkIndex];
		List<Color32[]> list = new List<Color32[]>();
		for (int i = 0; i < tMP_LinkInfo.linkTextLength; i++)
		{
			int num = tMP_LinkInfo.linkTextfirstCharacterIndex + i;
			TMP_CharacterInfo tMP_CharacterInfo = Parent.textInfo.characterInfo[num];
			int materialReferenceIndex = tMP_CharacterInfo.materialReferenceIndex;
			int vertexIndex = tMP_CharacterInfo.vertexIndex;
			Color32[] colors = Parent.textInfo.meshInfo[materialReferenceIndex].colors32;
			list.Add(colors.ToArray());
			if (tMP_CharacterInfo.isVisible)
			{
				colors[vertexIndex] = colorForLinkAndVert(i, vertexIndex);
				colors[vertexIndex + 1] = colorForLinkAndVert(i, vertexIndex + 1);
				colors[vertexIndex + 2] = colorForLinkAndVert(i, vertexIndex + 2);
				colors[vertexIndex + 3] = colorForLinkAndVert(i, vertexIndex + 3);
			}
		}
		Parent.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
		return list;
	}

	private void Highlight(ref Color32[] colorArray, int index)
	{
		Color32 color = colorArray[index];
		color.a = byte.MaxValue;
		colorArray[index] = color;
	}

	private List<Color32[]> SetLinkToColor(int linkIndex)
	{
		TMP_LinkInfo tMP_LinkInfo = Parent.textInfo.linkInfo[linkIndex];
		List<Color32[]> list = new List<Color32[]>();
		for (int i = 0; i < tMP_LinkInfo.linkTextLength; i++)
		{
			int num = tMP_LinkInfo.linkTextfirstCharacterIndex + i;
			TMP_CharacterInfo tMP_CharacterInfo = Parent.textInfo.characterInfo[num];
			int materialReferenceIndex = tMP_CharacterInfo.materialReferenceIndex;
			int vertexIndex = tMP_CharacterInfo.vertexIndex;
			Color32[] colorArray = Parent.textInfo.meshInfo[materialReferenceIndex].colors32;
			list.Add(colorArray.ToArray());
			if (tMP_CharacterInfo.isVisible)
			{
				Highlight(ref colorArray, vertexIndex);
				Highlight(ref colorArray, vertexIndex + 1);
				Highlight(ref colorArray, vertexIndex + 2);
				Highlight(ref colorArray, vertexIndex + 3);
			}
		}
		Parent.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
		return list;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
	}

	public void OnPointerUp(PointerEventData eventData)
	{
	}
}
