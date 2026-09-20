using System;
using System.Collections.Generic;
using Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects;

[Serializable]
public class ThingRenderer : IBatchable
{
	public Thing Parent;

	[FormerlySerializedAs("Renderer")]
	[SerializeField]
	private Renderer UnityRenderer;

	[SerializeField]
	private RocketRendererInstance _rocketRenderer;

	private DrawData _drawData;

	public ShadowCastingMode DefaultMode;

	public MeshFilter MeshFilter;

	public int BaseLayer;

	[SerializeField]
	private bool _enabled;

	[SerializeField]
	private bool _visible;

	private Dictionary<int, float> _floatProperties = new Dictionary<int, float>();

	private Dictionary<int, Vector4> _vectorProperties = new Dictionary<int, Vector4>();

	public Thing GetParent => Parent;

	public ShadowCastingMode ShadowCastingMode
	{
		get
		{
			if (Mode() != ThingRendererMode.DrawData)
			{
				return ShadowCastingMode.On;
			}
			return _drawData.shadowMode;
		}
	}

	public RocketRendererInstance RocketRenderer
	{
		get
		{
			return _rocketRenderer;
		}
		set
		{
			_rocketRenderer = value;
			if ((bool)RocketRenderer)
			{
				_rocketRenderer.Materials = UnityRenderer.sharedMaterials;
			}
			RenderStateChanged();
		}
	}

	public bool Enabled
	{
		get
		{
			return _enabled;
		}
		set
		{
			_enabled = value;
			RenderStateChanged();
		}
	}

	public bool Visible
	{
		get
		{
			return _visible;
		}
		set
		{
			_visible = value;
			RenderStateChanged();
		}
	}

	public Mesh SharedMesh
	{
		get
		{
			switch (Mode())
			{
			case ThingRendererMode.DrawData:
				return _drawData.mesh;
			case ThingRendererMode.MeshRenderer:
			case ThingRendererMode.RocketRenderer:
				return MeshFilter?.sharedMesh;
			default:
				return null;
			}
		}
		set
		{
			switch (Mode())
			{
			case ThingRendererMode.DrawData:
				_drawData.mesh = value;
				break;
			case ThingRendererMode.MeshRenderer:
			case ThingRendererMode.RocketRenderer:
				MeshFilter.sharedMesh = value;
				break;
			}
			RenderStateChanged();
		}
	}

	public Dictionary<int, float> FloatProperties => _floatProperties;

	public Material[] Materials
	{
		get
		{
			if (Mode() == ThingRendererMode.DrawData)
			{
				return (Material[])_drawData.materials.Clone();
			}
			if ((bool)UnityRenderer)
			{
				return UnityRenderer.sharedMaterials;
			}
			return null;
		}
		set
		{
			if (Mode() == ThingRendererMode.DrawData)
			{
				_ = Parent.ThingTransform.localToWorldMatrix;
				_drawData.materials = value;
				return;
			}
			UnityRenderer.materials = value;
			if ((bool)RocketRenderer)
			{
				RocketRenderer.Materials = value;
			}
		}
	}

	public Material[] sharedMaterials
	{
		get
		{
			if (Mode() == ThingRendererMode.DrawData)
			{
				return (Material[])_drawData.materials.Clone();
			}
			if (UnityRenderer == null || Parent == null || Parent.BeingDestroyed)
			{
				return null;
			}
			return UnityRenderer.sharedMaterials;
		}
		set
		{
			if (Mode() == ThingRendererMode.DrawData)
			{
				_drawData.materials = value;
				return;
			}
			UnityRenderer.sharedMaterials = value;
			if ((bool)RocketRenderer)
			{
				RocketRenderer.Materials = value;
			}
		}
	}

	public ThingRendererMode Mode()
	{
		return ThingRendererMode.MeshRenderer;
	}

	public Renderer GetRenderer()
	{
		return UnityRenderer;
	}

	public ThingRenderer(Thing parent, Renderer unityRenderer)
	{
		Parent = parent;
		UnityRenderer = unityRenderer;
		DefaultMode = UnityRenderer.shadowCastingMode;
		MeshFilter = UnityRenderer.GetComponent<MeshFilter>();
		BaseLayer = UnityRenderer.gameObject.layer;
		Enabled = UnityRenderer.enabled;
		Visible = true;
	}

	public ThingRenderer(Thing parent, DrawData drawData)
	{
		_drawData = drawData;
		Parent = parent;
		DefaultMode = drawData.shadowMode;
		BaseLayer = Parent.gameObject.layer;
		Enabled = Parent.enabled;
		Visible = true;
	}

	public bool IsCurrentRenderer(Renderer renderer)
	{
		return renderer == UnityRenderer;
	}

	public void OnParentDestroyed()
	{
	}

	public void Destroy()
	{
		UnityEngine.Object.Destroy(UnityRenderer);
		UnityEngine.Object.Destroy(RocketRenderer);
		UnityEngine.Object.Destroy(MeshFilter);
	}

	public bool RendererGameObjectActiveInHierarchy()
	{
		return Mode() switch
		{
			ThingRendererMode.DrawData => Parent.GameObject.activeInHierarchy, 
			ThingRendererMode.RocketRenderer => RocketRenderer.gameObject.activeInHierarchy, 
			ThingRendererMode.MeshRenderer => UnityRenderer.gameObject.activeInHierarchy, 
			_ => Parent.GameObject.activeInHierarchy, 
		};
	}

	public Material GetMaterial()
	{
		return _drawData.materials[0];
	}

	public int GetNumberOfMaterials()
	{
		return Mode() switch
		{
			ThingRendererMode.DrawData => _drawData.materials.Length, 
			ThingRendererMode.RocketRenderer => RocketRenderer.Materials.Length, 
			ThingRendererMode.MeshRenderer => UnityRenderer.materials.Length, 
			_ => 0, 
		};
	}

	public void SetShaderFloatProperty(int propertyID, float value)
	{
		_floatProperties[propertyID] = value;
	}

	public void SetShaderVectorProperty(int propertyID, Vector4 value)
	{
		_vectorProperties[propertyID] = value;
	}

	private void FlushShaderProperties()
	{
	}

	public Transform GetRendererTransform()
	{
		return Mode() switch
		{
			ThingRendererMode.DrawData => Parent.ThingTransform, 
			ThingRendererMode.RocketRenderer => RocketRenderer.Transform, 
			ThingRendererMode.MeshRenderer => UnityRenderer.transform, 
			_ => Parent.ThingTransform, 
		};
	}

	public GameObject GetRendererGameObject()
	{
		return Mode() switch
		{
			ThingRendererMode.DrawData => Parent.GameObject, 
			ThingRendererMode.RocketRenderer => RocketRenderer.gameObject, 
			ThingRendererMode.MeshRenderer => UnityRenderer.gameObject, 
			_ => Parent.GameObject, 
		};
	}

	public bool HasRenderer()
	{
		if (!UnityRenderer && !RocketRenderer)
		{
			return _drawData.IsValid();
		}
		return true;
	}

	public void SetShadowCastMode(ShadowCastingMode mode)
	{
		UnityRenderer.shadowCastingMode = mode;
	}

	public void SetColor(Color color)
	{
		Material[] array = (Material[])Materials.Clone();
		Material[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].color = color;
		}
		Materials = array;
	}

	public void SetLayer(int layerId)
	{
		if (!(UnityRenderer == null) && !(UnityRenderer.gameObject == null))
		{
			UnityRenderer.gameObject.layer = layerId;
		}
	}

	public void RenderStateChanged()
	{
		if (!(Parent == null) && HasRenderer())
		{
			bool enabled = !Parent.IsOccluded && _enabled && _visible;
			if ((object)UnityRenderer != null)
			{
				UnityRenderer.enabled = enabled;
			}
		}
	}

	public void OverrideShadowMode(ShadowCastingMode shadowMode)
	{
		if (DefaultMode == ShadowCastingMode.Off)
		{
			return;
		}
		ShadowCastingMode shadowCastingMode = UnityRenderer.shadowCastingMode;
		switch (shadowMode)
		{
		case ShadowCastingMode.On:
			if (shadowCastingMode != ShadowCastingMode.Off)
			{
				break;
			}
			goto IL_0024;
		case ShadowCastingMode.Off:
			{
				if (shadowCastingMode != ShadowCastingMode.On)
				{
					break;
				}
				goto IL_0024;
			}
			IL_0024:
			UnityRenderer.shadowCastingMode = shadowMode;
			break;
		}
	}
}
