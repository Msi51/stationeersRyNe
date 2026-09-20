using System;
using ch.sycoforge.Flares.Persistence;
using ch.sycoforge.Util.Attributes;
using UnityEngine;

namespace ch.sycoforge.Flares;

[Serializable]
public abstract class LensFlareBase : ScriptableObject
{
	private PropertyChangedHandler _OnPropertyChanged;

	internal const string Group_Mask = "Mask";

	internal const string Group_Texture = "Texture";

	internal const string Group_Transformation = "Transformation";

	internal const string Group_DynamicFalloff = "Dynamic Falloff";

	internal const string Group_Occlusion = "Occlusion";

	internal const string Dependency_EnableMask = "EnableMask";

	internal const string Dependency_LimitIntensity = "LimitIntensity";

	internal const int Indent = 20;

	public static Vector2 Vector2_one = Vector2.one;

	public string DisplayName = string.Empty;

	[SerializeField]
	internal Vector2 sampleOffset;

	[SerializeField]
	internal GradientMode gradientMode;

	[SerializeField]
	internal float sampleScale = 1f;

	internal Material materialClone;

	internal string shaderName = "Easy Flares/Simple";

	internal float haloEvolution = 1f;

	internal float haloNoiseAmp;

	internal float haloNoiseFreq;

	[SerializeField]
	internal float startScale = 1f;

	[SerializeField]
	internal float endScale = 10f;

	[SerializeField]
	internal bool useOcclusion;

	[SerializeField]
	internal bool use3DOcclusion = true;

	[SerializeField]
	internal Falloff scaleFalloff;

	[SerializeField]
	internal Falloff intensityFalloff;

	[SerializeField]
	internal float falloffRange = 1f;

	[SerializeField]
	internal float borderWidth = 1f;

	[SerializeField]
	internal AnimationCurve falloffHorizontal = AnimationCurve.Linear(-1f, 1f, 2f, 1f);

	[SerializeField]
	internal AnimationCurve falloffVertical = AnimationCurve.Linear(-1f, 1f, 2f, 1f);

	[SerializeField]
	internal AnimationCurve falloffOcclusion = AnimationCurve.Linear(0f, 1f, 1f, 0f);

	[SerializeField]
	internal float intensity = 1f;

	[SerializeField]
	internal bool limitIntensity = true;

	[SerializeField]
	internal float minIntensity = 0.01f;

	[SerializeField]
	internal float maxIntensity = 2f;

	[SerializeField]
	internal float maskSmooth = 15f;

	[SerializeField]
	internal float outerMaskRadius = 1f;

	[SerializeField]
	internal float innerMaskRadius = 0.2f;

	[SerializeField]
	internal bool invertMask;

	[SerializeField]
	internal bool enableMask = true;

	[SerializeField]
	internal bool rotateToCenter;

	[SerializeField]
	internal float rotationDamp = 1f;

	[SerializeField]
	internal float rotation;

	[SerializeField]
	internal float rotationSpeed;

	[SerializeField]
	internal Color tintColor = Color.white;

	[SerializeField]
	internal Texture2D elementTexture;

	[SerializeField]
	internal Texture2D lensColorTexture;

	[SerializeField]
	protected string flareName;

	[SerializeField]
	internal Vector2 offset = new Vector2(0f, 0f);

	[SerializeField]
	internal Vector2 distortion = new Vector2(1f, 1f);

	[SerializeField]
	internal Vector2 preScale = new Vector2(1f, 1f);

	[SerializeField]
	internal bool isVisible = true;

	[SerializeField]
	internal bool isSolo;

	[SerializeField]
	public FlareStyle style;

	internal MaterialPropertyBlock materialProperties;

	internal int _TintColor;

	internal int _LensColorTex;

	internal int _EnableMask;

	internal int _Mask;

	internal int _RotationValues;

	internal int _GradientMode;

	internal int _OffsetScale;

	internal int _SampleOffsetScale;

	internal int _MainTex;

	[SerializeField]
	protected int id;

	private float finalIntensity;

	public string Name
	{
		get
		{
			return flareName;
		}
		set
		{
			flareName = value;
		}
	}

	public int ID
	{
		get
		{
			return id;
		}
		set
		{
			id = value;
		}
	}

	public string ShaderName
	{
		get
		{
			return shaderName;
		}
		private set
		{
			shaderName = value;
		}
	}

	[Editable(Group = "Texture", Alias = "Element")]
	public virtual Texture2D ElementTexture
	{
		get
		{
			return elementTexture;
		}
		set
		{
			if (value == null)
			{
				elementTexture = Texture2D.whiteTexture;
			}
			else
			{
				elementTexture = value;
			}
		}
	}

	[Editable(Group = "Texture", Alias = "Lens Color")]
	public Texture2D LensColorTexture
	{
		get
		{
			return lensColorTexture;
		}
		set
		{
			if (value == null)
			{
				lensColorTexture = Texture2D.whiteTexture;
			}
			else
			{
				lensColorTexture = value;
			}
		}
	}

	[Editable(Group = "Texture")]
	public Vector2 SampleOffset
	{
		get
		{
			return sampleOffset;
		}
		set
		{
			sampleOffset = value;
		}
	}

	[Editable(Group = "Texture")]
	public GradientMode GradientMode
	{
		get
		{
			return gradientMode;
		}
		set
		{
			gradientMode = value;
		}
	}

	[Editable(Group = "Texture")]
	public float SampleScale
	{
		get
		{
			return sampleScale;
		}
		set
		{
			sampleScale = value;
		}
	}

	[Editable(Min = 0f, Max = 10f)]
	public float Intensity
	{
		get
		{
			return intensity;
		}
		set
		{
			intensity = value;
		}
	}

	[Editable]
	public bool LimitIntensity
	{
		get
		{
			return limitIntensity;
		}
		set
		{
			PropertyChanged(limitIntensity, value);
			limitIntensity = value;
		}
	}

	[Editable(Min = 0f, Max = 10f, VisibiltyDependsOn = "LimitIntensity")]
	public float MaxIntensity
	{
		get
		{
			return maxIntensity;
		}
		set
		{
			PropertyChanged(maxIntensity, value);
			maxIntensity = value;
		}
	}

	[Editable(Min = 0f, Max = 10f, VisibiltyDependsOn = "LimitIntensity")]
	public float MinIntensity
	{
		get
		{
			return minIntensity;
		}
		set
		{
			PropertyChanged(minIntensity, value);
			minIntensity = value;
		}
	}

	[Editable(Group = "Mask", Alias = "Enable")]
	public virtual bool EnableMask
	{
		get
		{
			return enableMask;
		}
		set
		{
			PropertyChanged(enableMask, value);
			enableMask = value;
		}
	}

	[Editable(Min = 0.01f, Max = 64f, Group = "Mask", VisibiltyDependsOn = "EnableMask", Alias = "Smooth")]
	public virtual float MaskSmooth
	{
		get
		{
			return maskSmooth;
		}
		set
		{
			PropertyChanged(maskSmooth, value);
			maskSmooth = value;
		}
	}

	[Editable(Min = 0f, Max = 0.5f, Group = "Mask", VisibiltyDependsOn = "EnableMask", Alias = "Outer Radius")]
	public virtual float OuterMaskRadius
	{
		get
		{
			return outerMaskRadius;
		}
		set
		{
			PropertyChanged(outerMaskRadius, value);
			outerMaskRadius = value;
			CheckMask(changeInner: false);
		}
	}

	[Editable(Min = 0f, Max = 0.5f, Group = "Mask", VisibiltyDependsOn = "EnableMask", Alias = "Inner Radius")]
	public virtual float InnerMaskRadius
	{
		get
		{
			return innerMaskRadius;
		}
		set
		{
			PropertyChanged(innerMaskRadius, value);
			innerMaskRadius = value;
			CheckMask(changeInner: true);
		}
	}

	[Editable(Group = "Mask", VisibiltyDependsOn = "EnableMask")]
	public virtual bool InvertMask
	{
		get
		{
			return invertMask;
		}
		set
		{
			PropertyChanged(invertMask, value);
			invertMask = value;
		}
	}

	[Editable(Group = "Transformation")]
	public Vector2 Offset
	{
		get
		{
			return offset;
		}
		set
		{
			offset = value;
		}
	}

	[Editable(Group = "Transformation")]
	public Vector2 Distortion
	{
		get
		{
			return distortion;
		}
		set
		{
			distortion = value;
		}
	}

	public Vector2 PreScale
	{
		get
		{
			return preScale;
		}
		set
		{
			preScale = value;
		}
	}

	[Editable(Group = "Transformation")]
	public virtual bool RotateToCenter
	{
		get
		{
			return rotateToCenter;
		}
		set
		{
			rotateToCenter = value;
		}
	}

	[Editable(Min = 0f, Max = 1f, Group = "Transformation")]
	public virtual float RotationDamp
	{
		get
		{
			return rotationDamp;
		}
		set
		{
			rotationDamp = value;
		}
	}

	[Editable(Min = 0f, Max = 360f, Group = "Transformation")]
	public virtual float Rotation
	{
		get
		{
			return rotation;
		}
		set
		{
			rotation = value;
		}
	}

	[Editable(Min = -3f, Max = 3f, Group = "Transformation")]
	public virtual float RotationSpeed
	{
		get
		{
			return rotationSpeed;
		}
		set
		{
			rotationSpeed = value;
		}
	}

	[Editable(Group = "Dynamic Falloff")]
	public Falloff ScaleFalloff
	{
		get
		{
			return scaleFalloff;
		}
		set
		{
			scaleFalloff = value;
		}
	}

	[Editable(Group = "Dynamic Falloff")]
	public Falloff IntensityFalloff
	{
		get
		{
			return intensityFalloff;
		}
		set
		{
			intensityFalloff = value;
		}
	}

	[Editable(Alias = "Horizontal Falloff", Min = 0f, Max = 4f, MinX = -1f, MaxX = 2f, Group = "Dynamic Falloff")]
	public AnimationCurve FalloffHorizontal
	{
		get
		{
			return falloffHorizontal;
		}
		set
		{
			falloffHorizontal = value;
		}
	}

	[Editable(Alias = "Vertical Falloff", Min = 0f, Max = 4f, MinX = -1f, MaxX = 2f, Group = "Dynamic Falloff")]
	public AnimationCurve FalloffVertical
	{
		get
		{
			return falloffVertical;
		}
		set
		{
			falloffVertical = value;
		}
	}

	[Editable(Alias = "Occlusion Falloff", Min = 0f, Max = 4f, MinX = 0f, MaxX = 1f, Group = "Dynamic Falloff")]
	public AnimationCurve FalloffOcclusion
	{
		get
		{
			return falloffOcclusion;
		}
		set
		{
			falloffOcclusion = value;
		}
	}

	[Editable(Group = "Dynamic Falloff")]
	public float StartScale
	{
		get
		{
			return startScale;
		}
		set
		{
			startScale = value;
		}
	}

	[Editable(Group = "Dynamic Falloff")]
	public float EndScale
	{
		get
		{
			return endScale;
		}
		set
		{
			endScale = value;
		}
	}

	[Editable(Group = "Occlusion")]
	public bool UseOcclusion
	{
		get
		{
			return useOcclusion;
		}
		set
		{
			useOcclusion = value;
		}
	}

	[Editable(Alias = "3D Occlusion", Group = "Occlusion")]
	public virtual bool Use3DOcclusion
	{
		get
		{
			return use3DOcclusion;
		}
		set
		{
			use3DOcclusion = value;
		}
	}

	[Editable]
	public Color TintColor
	{
		get
		{
			return tintColor;
		}
		set
		{
			PropertyChanged(tintColor, value);
			tintColor = value;
		}
	}

	public bool IsVisible
	{
		get
		{
			return isVisible;
		}
		set
		{
			PropertyChanged(isVisible, value);
			isVisible = value;
		}
	}

	public bool IsSolo
	{
		get
		{
			return isSolo;
		}
		set
		{
			if (isSolo != value)
			{
				if (value)
				{
					style.SoloFlares++;
				}
				else
				{
					style.SoloFlares--;
				}
			}
			PropertyChanged(isSolo, value);
			isSolo = value;
		}
	}

	protected float OverallIntensity { get; set; }

	internal bool ShouldBeRendered => !Mathf.Approximately(tintColor.a, 0f);

	public event PropertyChangedHandler OnPropertyChanged
	{
		add
		{
			_OnPropertyChanged = (PropertyChangedHandler)Delegate.Combine(_OnPropertyChanged, value);
		}
		remove
		{
			_OnPropertyChanged = (PropertyChangedHandler)Delegate.Remove(_OnPropertyChanged, value);
		}
	}

	public bool IsEventHandlerRegistered(Delegate prospectiveHandler)
	{
		if (_OnPropertyChanged != null)
		{
			Delegate[] invocationList = _OnPropertyChanged.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				if (invocationList[i] == prospectiveHandler)
				{
					return true;
				}
			}
		}
		return false;
	}

	public virtual void OnEnable()
	{
		if (id == 0)
		{
			id = UnityEngine.Random.Range(0, int.MaxValue);
		}
		_TintColor = Shader.PropertyToID("_TintColor");
		_LensColorTex = Shader.PropertyToID("_LensColorTex");
		_EnableMask = Shader.PropertyToID("_EnableMask");
		_Mask = Shader.PropertyToID("_Mask");
		_RotationValues = Shader.PropertyToID("_RotationValues");
		_GradientMode = Shader.PropertyToID("_GradientMode");
		_OffsetScale = Shader.PropertyToID("_OffsetScale");
		_SampleOffsetScale = Shader.PropertyToID("_SampleOffsetScale");
		_MainTex = Shader.PropertyToID("_MainTex");
	}

	internal bool ShouldRender(EasyFlares parent, Camera camera)
	{
		FlareState flareState = parent.GetFlareState(camera);
		if (!flareState.IsInFront || !ShouldBeRendered)
		{
			if (!UseOcclusion)
			{
				return flareState.IsInFront;
			}
			return false;
		}
		return true;
	}

	internal virtual void Initialize(FlareStyle style)
	{
		if (elementTexture == null)
		{
			elementTexture = Texture2D.whiteTexture;
		}
		if (lensColorTexture == null)
		{
			lensColorTexture = Texture2D.whiteTexture;
		}
		if (!(style == null))
		{
			materialProperties = new MaterialPropertyBlock();
		}
	}

	internal virtual void UpdateFlare(EasyFlares parent, Camera camera)
	{
		if (style == null)
		{
			style = parent.Style;
		}
		FlareState flareState = parent.GetFlareState(camera);
		if (rotateToCenter)
		{
			Vector2 to = new Vector2(0.5f, 0.5f) - flareState.ViewportPosition;
			float num = Vector2.Angle(Vector2.right, to);
			num = ((to.y < 0f) ? (0f - num) : num);
			rotation = num * RotationDamp;
		}
		float num2 = (useOcclusion ? parent.OcclusionFactor : 1f);
		num2 = falloffOcclusion.Evaluate(1f - num2);
		OverallIntensity = GetTriggeredIntensity(parent, flareState) * Intensity * GetIntensityFalloff(parent, flareState) * num2;
		if (limitIntensity)
		{
			float b = ((useOcclusion && flareState.IsOccluded) ? parent.OcclusionFactor : minIntensity);
			finalIntensity = Mathf.Max(Mathf.Min(OverallIntensity, maxIntensity), b);
		}
		else
		{
			finalIntensity = OverallIntensity;
		}
		if (parent.SmoothFade)
		{
			tintColor.a = Mathf.Lerp(tintColor.a, finalIntensity, parent.DampingFactor);
		}
		else
		{
			tintColor.a = finalIntensity;
		}
	}

	internal float GetTriggeredIntensity(EasyFlares parent, FlareState state)
	{
		float x = state.ViewportPosition.x;
		float y = state.ViewportPosition.y;
		float num = falloffHorizontal.Evaluate(x);
		float num2 = falloffVertical.Evaluate(y);
		return num * num2;
	}

	internal float GetFalloffFactor(EasyFlares parent, Falloff falloff, FlareState state)
	{
		float num = Mathf.Abs(endScale - startScale);
		float result = 1f;
		switch (falloff)
		{
		case Falloff.Linear:
			result = num / state.Distance;
			break;
		case Falloff.Quadratic:
			result = num / (state.Distance * state.Distance);
			break;
		}
		return result;
	}

	internal Vector2 GetScaleFalloff(EasyFlares parent, FlareState state)
	{
		float num = GetFalloffFactor(parent, scaleFalloff, state) * GetTriggeredIntensity(parent, state) * (useOcclusion ? parent.OcclusionFactor : 1f);
		return new Vector2(num * preScale.x, num * preScale.y);
	}

	internal float GetIntensityFalloff(EasyFlares parent, FlareState state)
	{
		return GetFalloffFactor(parent, intensityFalloff, state);
	}

	internal virtual void Draw(EasyFlares parent, Camera camera)
	{
		if (!(ElementTexture == null) && ShouldRender(parent, camera))
		{
			FlareState flareState = parent.GetFlareState(camera);
			Vector2 screenPosition = flareState.ScreenPosition;
			Graphics.DrawTexture(GetRectAt(screenPosition, new Vector2(0.5f, 0.5f), GetScaleFalloff(parent, flareState)), elementTexture, materialClone);
		}
	}

	public virtual void OnCreated(FlareStyle style)
	{
		Initialize(style);
	}

	private void CheckMask(bool changeInner)
	{
		float num = 0.01f;
		if (innerMaskRadius >= outerMaskRadius)
		{
			if (changeInner)
			{
				outerMaskRadius = innerMaskRadius + num;
			}
			else
			{
				innerMaskRadius = outerMaskRadius - num;
			}
		}
	}

	internal Rect GetRectAt(Vector2 screenPos)
	{
		return GetRectAt(screenPos, new Vector2(0.5f, 0.5f), Vector2.one);
	}

	internal Rect GetRectAt(Vector2 screenPos, Vector2 anchor, Vector2 scale)
	{
		float num = (float)Screen.width * scale.x;
		float num2 = (float)Screen.height * scale.y;
		float num3 = num * anchor.x;
		float num4 = num2 * anchor.y;
		return new Rect(screenPos.x - num3, screenPos.y - num4, num, num2);
	}

	internal void ThrowOnPropertyChanged()
	{
		if (_OnPropertyChanged != null)
		{
			try
			{
				_OnPropertyChanged(this);
			}
			catch (Exception ex)
			{
				Debug.Log("ThrowOnPropertyChanged: " + ex);
			}
		}
	}

	internal void PropertyChanged(object oldValue, object newValue)
	{
		if (!oldValue.Equals(newValue) && Application.isEditor)
		{
			ThrowOnPropertyChanged();
		}
	}

	public override string ToString()
	{
		return DisplayName;
	}
}
