using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class UiComponentRenderer : GameBase
{
	public Image[] ImageComponents;

	public LayoutElement LayoutElement;

	public TextMeshProUGUI[] TextComponents;

	public UiComponentRenderer ParentRenderer;

	public List<UiComponentRenderer> ChildRenderers = new List<UiComponentRenderer>();

	[SerializeField]
	protected bool _shouldDisableRaycast;

	[Tooltip("Is this Component Visible. If parents are not visible the component will not be rendered")]
	public bool DEBUG_VISIBLE = true;

	[Tooltip("Is this Component and all its parents Visible")]
	public bool DEBUG_ACTIVE = true;

	private bool _isVisible = true;

	protected bool _isActive = true;

	public bool HasParent => ParentRenderer != null;

	public bool HasChildren => ChildRenderers.Count > 0;

	public override bool IsVisible
	{
		get
		{
			if (ParentRenderer == null)
			{
				return _isVisible;
			}
			if (ParentRenderer.IsVisible)
			{
				return _isVisible;
			}
			return false;
		}
	}

	public bool IsVisibleSelf => _isVisible;

	public void RefreshVisible(bool forceRefresh = false)
	{
		if (IsVisible != _isActive || forceRefresh)
		{
			SetActive(IsVisible);
		}
		foreach (UiComponentRenderer childRenderer in ChildRenderers)
		{
			childRenderer.RefreshVisible(forceRefresh);
		}
		DEBUG_ACTIVE = _isActive;
		DEBUG_VISIBLE = _isVisible;
	}

	public override void SetVisible(bool isVisble)
	{
		if (_isVisible != isVisble)
		{
			_isVisible = isVisble;
			RefreshVisible();
		}
	}

	public override void SetActive(bool active)
	{
		_isActive = active;
		if (LayoutElement != null)
		{
			LayoutElement.ignoreLayout = !active;
		}
		Image[] imageComponents = ImageComponents;
		foreach (Image image in imageComponents)
		{
			Color color = image.color;
			color.a = (active ? 1 : 0);
			image.color = color;
			if (_shouldDisableRaycast)
			{
				image.raycastTarget = active;
			}
		}
		TextMeshProUGUI[] textComponents = TextComponents;
		foreach (TextMeshProUGUI obj in textComponents)
		{
			Color color2 = obj.color;
			color2.a = (active ? 1 : 0);
			obj.color = color2;
		}
	}

	private bool RendererOwnsComponent(Image image)
	{
		if (ImageComponents.Contains(image))
		{
			return true;
		}
		foreach (UiComponentRenderer childRenderer in ChildRenderers)
		{
			if (childRenderer.RendererOwnsComponent(image))
			{
				return true;
			}
		}
		return false;
	}

	private bool RendererOwnsComponent(TextMeshProUGUI text)
	{
		if (TextComponents.Contains(text))
		{
			return true;
		}
		foreach (UiComponentRenderer childRenderer in ChildRenderers)
		{
			if (childRenderer.RendererOwnsComponent(text))
			{
				return true;
			}
		}
		return false;
	}
}
