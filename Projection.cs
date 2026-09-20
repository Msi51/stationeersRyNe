using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public abstract class Projection : MonoBehaviour
{
	[SerializeField]
	private int priority;

	[SerializeField]
	protected TransparencyType transparencyType;

	[SerializeField]
	private float cutoff = 0.2f;

	[SerializeField]
	private MaskMethod maskMethod;

	[SerializeField]
	private bool[] masks = new bool[4];

	private Color MaskLayers;

	private PoolItem poolItem;

	private float scaleModifier = 1f;

	private float alphaModifier = 1f;

	private Visibility visibility;

	protected Transform forwardRenderer;

	private RenderTargetIdentifier[] deferredTargets;

	private RenderTargetIdentifier[] deferredHDRTargets;

	private bool replaceDeferred = true;

	private bool replaceForward = true;

	private bool updateMaterial = true;

	protected MaterialPropertyBlock materialProperties;

	public int Priority
	{
		get
		{
			return priority;
		}
		set
		{
			priority = value;
			Reprioritise();
		}
	}

	public TransparencyType TransparencyType
	{
		get
		{
			return transparencyType;
		}
		set
		{
			transparencyType = value;
			switch (transparencyType)
			{
			case TransparencyType.Cutout:
				cutoff = 0.2f;
				break;
			}
			ReplaceMaterial();
			UpdateMaterial();
		}
	}

	public float AlphaCutoff
	{
		get
		{
			return cutoff;
		}
		set
		{
			cutoff = Mathf.Clamp01(value);
			UpdateMaterial();
		}
	}

	public MaskMethod MaskMethod
	{
		get
		{
			return maskMethod;
		}
		set
		{
			maskMethod = value;
			UpdateMaterial();
		}
	}

	public bool MaskLayer1
	{
		get
		{
			return masks[0];
		}
		set
		{
			masks[0] = value;
			UpdateMaterial();
		}
	}

	public bool MaskLayer2
	{
		get
		{
			return masks[1];
		}
		set
		{
			masks[1] = value;
			UpdateMaterial();
		}
	}

	public bool MaskLayer3
	{
		get
		{
			return masks[2];
		}
		set
		{
			masks[2] = value;
			UpdateMaterial();
		}
	}

	public bool MaskLayer4
	{
		get
		{
			return masks[3];
		}
		set
		{
			masks[3] = value;
			UpdateMaterial();
		}
	}

	public PoolItem PoolItem
	{
		get
		{
			return poolItem;
		}
		set
		{
			poolItem = value;
		}
	}

	public float ScaleModifier
	{
		get
		{
			return scaleModifier;
		}
		set
		{
			scaleModifier = value;
		}
	}

	public float AlphaModifier
	{
		get
		{
			return alphaModifier;
		}
		set
		{
			alphaModifier = Mathf.Clamp01(value);
			UpdateMaterial();
		}
	}

	public bool Visible
	{
		get
		{
			if (this != null && DynamicDecals.RenderingPath == RenderingPath.Forward)
			{
				MeshRenderer component = GetComponent<MeshRenderer>();
				if (component != null)
				{
					return component.isVisible;
				}
				return true;
			}
			Visibility visibility = this.visibility;
			if (visibility != Visibility.NotVisible)
			{
				_ = 2;
				return true;
			}
			return false;
		}
	}

	public Matrix4x4 RenderMatrix
	{
		get
		{
			if (scaleModifier == 1f)
			{
				return base.transform.localToWorldMatrix;
			}
			return base.transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(scaleModifier, scaleModifier, scaleModifier));
		}
	}

	public abstract Material RenderMaterial { get; }

	public abstract int DeferredPass { get; }

	public abstract bool DeferredPrePass { get; }

	public bool[] DeferredBuffers { get; set; }

	public RenderTargetIdentifier[] DeferredTargets
	{
		get
		{
			if (deferredTargets == null || deferredTargets.Length < 1)
			{
				deferredTargets = (RenderTargetIdentifier[])DynamicDecals.PassesToTargets(DeferredBuffers, HDR: false).Clone();
			}
			return deferredTargets;
		}
		set
		{
			deferredTargets = value;
		}
	}

	public RenderTargetIdentifier[] DeferredHDRTargets
	{
		get
		{
			if (deferredHDRTargets == null || deferredHDRTargets.Length < 1)
			{
				deferredHDRTargets = (RenderTargetIdentifier[])DynamicDecals.PassesToTargets(DeferredBuffers, HDR: true).Clone();
			}
			return deferredHDRTargets;
		}
		set
		{
			deferredHDRTargets = value;
		}
	}

	public float timeID { get; private set; }

	public MaterialPropertyBlock MaterialProperties
	{
		get
		{
			if (updateMaterial)
			{
				UpdateMaterialProperties();
			}
			updateMaterial = false;
			return materialProperties;
		}
	}

	protected virtual bool RequiresRenderer => true;

	private void Start()
	{
		UpdateMaterialImmeditately();
		UpdateRenderer();
		Register();
	}

	private void OnEnable()
	{
		UpdateMaterialImmeditately();
		UpdateRenderer();
		Register();
	}

	private void OnDisable()
	{
		DestroyRenderer();
		Deregister();
	}

	private void Register()
	{
		timeID = Time.timeSinceLevelLoad;
		DynamicDecals.AddProjection(this);
	}

	private void Deregister()
	{
		DynamicDecals.RemoveProjection(this);
	}

	public void Reprioritise()
	{
		Reprioritise(DelayedSort: false);
	}

	public void Reprioritise(bool DelayedSort)
	{
		if (DelayedSort)
		{
			DynamicDecals.Sort();
			return;
		}
		Deregister();
		Register();
	}

	public void UpdateProjection()
	{
		visibility = Visibility.Unknown;
		if (!UpdateRenderer())
		{
			DestroyRenderer();
		}
	}

	public void ReplaceMaterial()
	{
		replaceDeferred = true;
		replaceForward = true;
	}

	public void UpdateMaterialImmeditately()
	{
		UpdateMaterialProperties();
		updateMaterial = false;
	}

	public void UpdateMaterial()
	{
		updateMaterial = true;
	}

	protected virtual void UpdateMaterialProperties()
	{
		if (materialProperties == null)
		{
			materialProperties = new MaterialPropertyBlock();
		}
		else
		{
			materialProperties.Clear();
		}
		UpdateTransparency();
		UpdateMasking();
		if (replaceDeferred)
		{
			UpdateDeferredRendering();
			replaceDeferred = false;
		}
	}

	private void UpdateTransparency()
	{
		materialProperties.SetFloat("_Cutoff", cutoff);
	}

	private void UpdateMasking()
	{
		switch (maskMethod)
		{
		case MaskMethod.DrawOnEverythingExcept:
			materialProperties.SetFloat("_MaskBase", 1f);
			if (masks.Length < 4)
			{
				masks = new bool[4];
			}
			MaskLayers.r = (masks[0] ? 0f : 0.5f);
			MaskLayers.g = (masks[1] ? 0f : 0.5f);
			MaskLayers.b = (masks[2] ? 0f : 0.5f);
			MaskLayers.a = (masks[3] ? 0f : 0.5f);
			materialProperties.SetVector("_MaskLayers", MaskLayers);
			break;
		case MaskMethod.OnlyDrawOn:
			materialProperties.SetFloat("_MaskBase", 0f);
			if (masks.Length < 4)
			{
				masks = new bool[4];
			}
			MaskLayers.r = (masks[0] ? 1f : 0.5f);
			MaskLayers.g = (masks[1] ? 1f : 0.5f);
			MaskLayers.b = (masks[2] ? 1f : 0.5f);
			MaskLayers.a = (masks[3] ? 1f : 0.5f);
			materialProperties.SetVector("_MaskLayers", MaskLayers);
			break;
		}
	}

	protected virtual void UpdateDeferredRendering()
	{
		DeferredTargets = (RenderTargetIdentifier[])DynamicDecals.PassesToTargets(DeferredBuffers, HDR: false).Clone();
		DeferredHDRTargets = (RenderTargetIdentifier[])DynamicDecals.PassesToTargets(DeferredBuffers, HDR: true).Clone();
	}

	protected virtual void UpdateForwardRendering(MeshRenderer Renderer)
	{
		Renderer.sharedMaterial.renderQueue = 2455 + Priority;
	}

	private bool UpdateRenderer()
	{
		if (DynamicDecals.RenderingPath == RenderingPath.Forward)
		{
			MeshRenderer meshRenderer = null;
			if (forwardRenderer == null)
			{
				foreach (Transform item in base.transform)
				{
					if (item.name.Equals("Forward Renderer"))
					{
						forwardRenderer = item;
					}
				}
				if (forwardRenderer == null)
				{
					forwardRenderer = new GameObject("Forward Renderer").transform;
					forwardRenderer.transform.SetParent(base.transform, worldPositionStays: false);
					forwardRenderer.gameObject.AddComponent<MeshFilter>().mesh = DynamicDecals.Cube;
					meshRenderer = forwardRenderer.gameObject.AddComponent<MeshRenderer>();
					meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
				}
			}
			if (meshRenderer == null)
			{
				meshRenderer = forwardRenderer.GetComponent<MeshRenderer>();
			}
			if (meshRenderer.sharedMaterials.Length == 0 || meshRenderer.sharedMaterial == null || replaceForward)
			{
				DestroyMaterials(meshRenderer);
				UpdateForwardRendering(meshRenderer);
				replaceForward = false;
			}
			meshRenderer.SetPropertyBlock(MaterialProperties);
			float num = Mathf.Clamp(scaleModifier, 1E-08f, 1E+09f);
			forwardRenderer.localScale = new Vector3(num, num, num);
			return true;
		}
		return false;
	}

	private void DestroyRenderer()
	{
		if (forwardRenderer != null)
		{
			DestroyMaterials(forwardRenderer.GetComponent<MeshRenderer>());
			if (Application.isPlaying)
			{
				Object.Destroy(forwardRenderer.gameObject);
			}
			else
			{
				Object.DestroyImmediate(forwardRenderer.gameObject, allowDestroyingAssets: true);
			}
		}
	}

	private void DestroyMaterials(MeshRenderer Renderer)
	{
		if (Renderer.sharedMaterials.Length == 0)
		{
			return;
		}
		for (int i = 0; i < Renderer.sharedMaterials.Length; i++)
		{
			if (Application.isPlaying)
			{
				Object.Destroy(Renderer.sharedMaterials[i]);
			}
			else
			{
				Object.DestroyImmediate(Renderer.sharedMaterials[i], allowDestroyingAssets: true);
			}
		}
	}

	public void SetVisibility(bool Visible)
	{
		if (!Visible && visibility == Visibility.Unknown)
		{
			visibility = Visibility.NotVisible;
		}
		if (Visible)
		{
			visibility = Visibility.Visible;
		}
	}

	public void Fade(FadeMethod Method, float InDuration, float Delay, float OutDuration)
	{
		if (poolItem != null)
		{
			poolItem.Fade(Method, InDuration, Delay, OutDuration);
			float num = 1f;
			num = ((!(InDuration > 0f)) ? 1f : 0f);
			if (Method == FadeMethod.Alpha || Method == FadeMethod.Both)
			{
				AlphaModifier = num;
			}
			if (Method == FadeMethod.Scale || Method == FadeMethod.Both)
			{
				ScaleModifier = num;
			}
			UpdateMaterialImmeditately();
		}
	}

	public void Culled(CullMethod Method, float Duration)
	{
		if (poolItem != null)
		{
			poolItem.Culled(Method, Duration);
		}
	}

	public void Return()
	{
		ProjectionPool.Return(this);
	}

	public void CopyBaseProperties(Projection Target)
	{
		Priority = Target.Priority;
		base.transform.localScale = Target.transform.localScale;
		TransparencyType = Target.TransparencyType;
		AlphaCutoff = Target.AlphaCutoff;
	}

	public void CopyMaskProperties(Projection Target)
	{
		MaskMethod = Target.MaskMethod;
		MaskLayer1 = Target.MaskLayer1;
		MaskLayer2 = Target.MaskLayer2;
		MaskLayer3 = Target.MaskLayer3;
		MaskLayer4 = Target.MaskLayer4;
	}
}
