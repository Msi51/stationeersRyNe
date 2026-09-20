using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using UnityEngine;

namespace ThingImport.Thumbnails;

public sealed class ThumbnailStudio
{
	public sealed class PuppetParameter
	{
		public string Name;

		public int Hash;

		public AnimatorControllerParameterType Type;

		public float Value;

		public float Default;
	}

	private sealed class DrawEntry
	{
		public Mesh Mesh;

		public bool OwnsMesh;

		public Matrix4x4 LocalMatrix;

		public Material[] BaseMaterials;

		public Material[] Materials;

		public bool[] Paintable;

		public Material MaskMaterial;
	}

	public sealed class ExportItem
	{
		public string PrefabName;

		public bool UseOverride;

		public Vector3 Euler;

		public float Zoom = 1f;

		public int ColorIndex = -1;

		public CharacterConfig Character;
	}

	public sealed class StudioLight
	{
		public string Name;

		public Light Light;

		public Vector3 Euler;

		public Vector3 DefaultEuler;

		public float DefaultIntensity;

		public bool DefaultEnabled = true;

		public void Apply()
		{
			Light.transform.localRotation = Quaternion.Euler(Euler);
		}

		public void Reset()
		{
			Euler = DefaultEuler;
			Light.intensity = DefaultIntensity;
			Light.enabled = DefaultEnabled;
			Apply();
		}
	}

	private struct StudioPose
	{
		public string PrefabName;

		public Vector3 Euler;

		public float Zoom;

		public int ColorIndex;

		public bool AutoFit;
	}

	public const int TextureSize = 256;

	private const int SuperSize = 1024;

	private const float SceneCameraFov = 20.8f;

	private static readonly Vector3 SceneCameraPosition = new Vector3(4.000001f, 4f, 1.2833396f);

	private static readonly Quaternion SceneCameraRotation = new Quaternion(0.22456284f, -0.74120647f, 0.29839897f, 0.55780166f);

	private static readonly Vector3 SceneKeyLightEuler = new Vector3(0.462f, -37.805f, 8.079f);

	private const float SceneKeyLightIntensity = 1f;

	public float Saturation = 1f;

	public const int GifMinWidth = 700;

	public const int GifMaxWidth = 1600;

	public const int GifSizeLimit = 5242880;

	public const int MinGifFrames = 4;

	public const int MaxGifFrames = 120;

	public const int MinExportSize = 16;

	public const int MaxExportSize = 8192;

	public const float MaxPaddingFraction = 0.45f;

	private const int MinCellDrawSize = 8;

	private const byte GifAlphaThreshold = 128;

	private const int ShadowBlurPasses = 3;

	private const float CharacterFrameFill = 0.9f;

	private const int TurntableProbeStep = 15;

	public Vector3 EulerAngles;

	public float ZoomMultiplier = 1f;

	public int ColorIndex = -1;

	public int BuildStateIndex = -1;

	public bool AutoFitFraming = true;

	private static readonly AnimationClip[] NoClips = new AnimationClip[0];

	public int AnimationClipIndex = -1;

	public float AnimationTime = 1f;

	public readonly List<PuppetParameter> PuppetParameters = new List<PuppetParameter>();

	private GameObject _puppetStaging;

	private GameObject _puppetInstance;

	private Animator _puppetAnimator;

	private static readonly int MaskColorId = Shader.PropertyToID("_MaskColor");

	private static readonly Vector3[] CornerSigns = new Vector3[8]
	{
		new Vector3(1f, 1f, 1f),
		new Vector3(1f, 1f, -1f),
		new Vector3(1f, -1f, 1f),
		new Vector3(1f, -1f, -1f),
		new Vector3(-1f, 1f, 1f),
		new Vector3(-1f, 1f, -1f),
		new Vector3(-1f, -1f, 1f),
		new Vector3(-1f, -1f, -1f)
	};

	private ThumbnailGeneratorRig _rig;

	private readonly List<DrawEntry> _entries = new List<DrawEntry>();

	private Bounds _bounds;

	private bool _dirty;

	private bool _hasMaskEntries;

	private bool _hasPaintableSlots;

	private RenderTexture _superTexture;

	private RenderTexture _halfTexture;

	private const int FitSize = 128;

	private RenderTexture _fitTexture;

	private Texture2D _fitReadback;

	private Vector3 _silhouetteOffset;

	private Matrix4x4 _lastRootMatrix = Matrix4x4.identity;

	private float _lastFinalDistance = 1f;

	private Vector3 _legacyObjectOffset;

	private static readonly List<Thing> NoPrefabs = new List<Thing>();

	private readonly HashSet<string> _savedPoseNames = new HashSet<string>();

	private readonly Dictionary<string, Sprite> _originalThumbnails = new Dictionary<string, Sprite>();

	private Vector3 _turntableEuler;

	private float _turntableDistance;

	public readonly List<StudioLight> Lights = new List<StudioLight>();

	private readonly Queue<string> _batchQueue = new Queue<string>();

	private const int MaskGrid = 48;

	public static ThumbnailStudio Instance { get; } = new ThumbnailStudio();

	public bool IsOpen { get; private set; }

	public Thing Current { get; private set; }

	public RenderTexture PreviewTexture { get; private set; }

	public ThumbnailLinkData Links { get; private set; }

	public ThumbnailRotationPresets Rotations { get; private set; }

	public ThumbnailItemStates ItemStates { get; private set; }

	public ThumbnailCharacter Character { get; private set; }

	public string Warning { get; private set; } = string.Empty;

	public bool CanColor
	{
		get
		{
			if (!_hasMaskEntries)
			{
				return _hasPaintableSlots;
			}
			return true;
		}
	}

	public float LastFillExtent { get; private set; }

	public bool LastFillClipped { get; private set; }

	public AnimationClip[] AnimationClips { get; private set; } = NoClips;

	public bool HasPuppet => _puppetAnimator != null;

	public Sprite OriginalThumbnail
	{
		get
		{
			if (Current == null)
			{
				return null;
			}
			if (!_originalThumbnails.TryGetValue(Current.PrefabName, out var value))
			{
				value = Current.Thumbnail;
				_originalThumbnails[Current.PrefabName] = value;
			}
			return value;
		}
	}

	public static IReadOnlyList<Thing> GalleryPrefabs
	{
		get
		{
			if (Prefab.AllPrefabs.Count > 0)
			{
				return Prefab.AllPrefabs;
			}
			WorldManager instance = WorldManager.Instance;
			if (!(instance != null))
			{
				return NoPrefabs;
			}
			return instance.SourcePrefabs;
		}
	}

	public ThumbnailLightSettings LightSettings { get; private set; }

	public int BatchRemaining => _batchQueue.Count;

	public int BatchTotal { get; private set; }

	public void SetAnimationPose(int clipIndex, float time)
	{
		AnimationClipIndex = clipIndex;
		AnimationTime = Mathf.Clamp01(time);
		PosePuppet();
		RebuildModel();
	}

	public void SetPuppetParameter(PuppetParameter parameter, float value)
	{
		if (parameter != null)
		{
			parameter.Value = value;
			PosePuppet();
			RebuildModel();
		}
	}

	private void TryCreatePuppet(Thing prefab)
	{
		Animator componentInChildren = prefab.GetComponentInChildren<Animator>(includeInactive: true);
		if (componentInChildren == null || componentInChildren.runtimeAnimatorController == null || prefab is Structure)
		{
			return;
		}
		_puppetStaging = new GameObject("~ThumbnailStudioPuppet");
		_puppetStaging.SetActive(value: false);
		_puppetStaging.transform.position = new Vector3(0f, -600f, 0f);
		_puppetInstance = UnityEngine.Object.Instantiate(prefab.gameObject, _puppetStaging.transform);
		for (int i = 0; i < 6; i++)
		{
			MonoBehaviour[] componentsInChildren = _puppetInstance.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
			if (componentsInChildren.Length == 0)
			{
				break;
			}
			MonoBehaviour[] array = componentsInChildren;
			foreach (MonoBehaviour monoBehaviour in array)
			{
				if (monoBehaviour != null)
				{
					UnityEngine.Object.DestroyImmediate(monoBehaviour);
				}
			}
		}
		if (_puppetInstance.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).Length != 0)
		{
			Debug.LogWarning("Thumbnail studio: couldn't strip all scripts from " + prefab.PrefabName + "; state posing unavailable for this item.");
			DestroyPuppet();
			return;
		}
		_puppetAnimator = _puppetInstance.GetComponentInChildren<Animator>(includeInactive: true);
		if (_puppetAnimator == null || _puppetAnimator.runtimeAnimatorController == null)
		{
			DestroyPuppet();
			return;
		}
		Transform[] componentsInChildren2 = _puppetInstance.GetComponentsInChildren<Transform>(includeInactive: true);
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			componentsInChildren2[j].gameObject.layer = Layers.ThumbnailCreation;
		}
		PuppetParameters.Clear();
		_puppetStaging.SetActive(value: true);
		AnimatorControllerParameter[] parameters = _puppetAnimator.parameters;
		foreach (AnimatorControllerParameter animatorControllerParameter in parameters)
		{
			if (animatorControllerParameter.type != AnimatorControllerParameterType.Trigger)
			{
				float num = animatorControllerParameter.type switch
				{
					AnimatorControllerParameterType.Float => animatorControllerParameter.defaultFloat, 
					AnimatorControllerParameterType.Int => animatorControllerParameter.defaultInt, 
					_ => animatorControllerParameter.defaultBool ? 1f : 0f, 
				};
				PuppetParameters.Add(new PuppetParameter
				{
					Name = animatorControllerParameter.name,
					Hash = animatorControllerParameter.nameHash,
					Type = animatorControllerParameter.type,
					Value = num,
					Default = num
				});
			}
		}
		_puppetStaging.SetActive(value: false);
	}

	private void DestroyPuppet()
	{
		if (_puppetStaging != null)
		{
			UnityEngine.Object.Destroy(_puppetStaging);
		}
		_puppetStaging = null;
		_puppetInstance = null;
		_puppetAnimator = null;
		PuppetParameters.Clear();
	}

	private void PosePuppet()
	{
		if (_puppetAnimator == null)
		{
			return;
		}
		_puppetStaging.SetActive(value: true);
		foreach (PuppetParameter puppetParameter in PuppetParameters)
		{
			switch (puppetParameter.Type)
			{
			case AnimatorControllerParameterType.Float:
				_puppetAnimator.SetFloat(puppetParameter.Hash, puppetParameter.Value);
				break;
			case AnimatorControllerParameterType.Int:
				_puppetAnimator.SetInteger(puppetParameter.Hash, Mathf.RoundToInt(puppetParameter.Value));
				break;
			case AnimatorControllerParameterType.Bool:
				_puppetAnimator.SetBool(puppetParameter.Hash, puppetParameter.Value > 0.5f);
				break;
			}
		}
		for (int i = 0; i < 24; i++)
		{
			_puppetAnimator.Update(0.25f);
		}
		AnimationClip animationClip = ((AnimationClipIndex >= 0 && AnimationClipIndex < AnimationClips.Length) ? AnimationClips[AnimationClipIndex] : null);
		if (animationClip != null)
		{
			animationClip.SampleAnimation(_puppetInstance, Mathf.Clamp01(AnimationTime) * animationClip.length);
		}
		_puppetStaging.SetActive(value: false);
	}

	private string SerializePuppetStates()
	{
		if (PuppetParameters.Count == 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (PuppetParameter puppetParameter in PuppetParameters)
		{
			if (!Mathf.Approximately(puppetParameter.Value, puppetParameter.Default))
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(';');
				}
				stringBuilder.Append(puppetParameter.Name).Append('=').Append(puppetParameter.Value.ToString(CultureInfo.InvariantCulture));
			}
		}
		if (stringBuilder.Length <= 0)
		{
			return null;
		}
		return stringBuilder.ToString();
	}

	private void RestorePuppetStates(string states)
	{
		if (string.IsNullOrEmpty(states) || PuppetParameters.Count == 0)
		{
			return;
		}
		string[] array = states.Split(';');
		foreach (string text in array)
		{
			string[] split = text.Split('=');
			if (split.Length == 2 && float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				PuppetParameter puppetParameter = PuppetParameters.Find((PuppetParameter p) => p.Name == split[0]);
				if (puppetParameter != null)
				{
					puppetParameter.Value = result;
				}
			}
		}
	}

	public bool Open()
	{
		if (IsOpen)
		{
			return true;
		}
		if (ThumbnailGenerator.Instance == null)
		{
			ConsoleWindow.PrintError("Thumbnail studio: no ThumbnailGenerator found in the scene.");
			return false;
		}
		_rig = ThumbnailGenerator.Instance.CreateRig();
		_rig.SetRigActive(active: false);
		_rig.Camera.fieldOfView = 20.8f;
		ConfigureStudioLighting();
		AddSceneSsao();
		_superTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32)
		{
			filterMode = FilterMode.Bilinear,
			antiAliasing = 4
		};
		_halfTexture = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32)
		{
			filterMode = FilterMode.Bilinear
		};
		PreviewTexture = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32)
		{
			filterMode = FilterMode.Trilinear,
			useMipMap = true,
			autoGenerateMips = true
		};
		_fitTexture = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32);
		_fitReadback = new Texture2D(128, 128, TextureFormat.ARGB32, mipChain: false);
		Links = ThumbnailLinkData.Load();
		Rotations = ThumbnailRotationPresets.Load();
		ItemStates = ThumbnailItemStates.Load();
		_savedPoseNames.Clear();
		foreach (ThumbnailItemState item in ItemStates.Items)
		{
			if (item != null && !string.IsNullOrEmpty(item.Name))
			{
				_savedPoseNames.Add(item.Name);
			}
		}
		ThumbnailOverrides.Apply(GalleryPrefabs);
		Character = new ThumbnailCharacter();
		Character.TryCreate();
		Character.RefreshClothingOptions(GalleryPrefabs);
		IsOpen = true;
		return true;
	}

	private void ConfigureStudioLighting()
	{
		Lights.Clear();
		Light[] componentsInChildren = _rig.GetComponentsInChildren<Light>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Light light = componentsInChildren[i];
			if (i > 0)
			{
				UnityEngine.Object.Destroy(light.gameObject);
				continue;
			}
			light.color = Color.white;
			light.intensity = 1f;
			light.transform.localRotation = Quaternion.Euler(SceneKeyLightEuler);
			Lights.Add(new StudioLight
			{
				Name = "Key",
				Light = light,
				Euler = SceneKeyLightEuler,
				DefaultEuler = SceneKeyLightEuler,
				DefaultIntensity = 1f,
				DefaultEnabled = true
			});
		}
		LightSettings = ThumbnailLightSettings.Load();
		Saturation = LightSettings.Saturation;
		foreach (StudioLight light2 in Lights)
		{
			ThumbnailLightSetting thumbnailLightSetting = LightSettings.Find(light2.Name);
			if (thumbnailLightSetting != null)
			{
				light2.Light.enabled = thumbnailLightSetting.Enabled;
				light2.Light.intensity = thumbnailLightSetting.Intensity;
				light2.Euler = new Vector3(thumbnailLightSetting.Pitch, thumbnailLightSetting.Yaw, thumbnailLightSetting.Roll);
				light2.Apply();
			}
		}
	}

	private void AddSceneSsao()
	{
		try
		{
			Camera camera = _rig.Camera;
			camera.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.DepthNormals;
			SSAOPro sSAOPro = camera.gameObject.AddComponent<SSAOPro>();
			sSAOPro.UseHighPrecisionDepthMap = false;
			sSAOPro.Samples = SSAOPro.SampleCount.Ultra;
			sSAOPro.Downsampling = 1;
			sSAOPro.Radius = 0.029f;
			sSAOPro.Intensity = 4.7f;
			sSAOPro.Distance = 1.03f;
			sSAOPro.Bias = 0.193f;
			sSAOPro.LumContribution = 0.072f;
			sSAOPro.OcclusionColor = Color.black;
			sSAOPro.CutoffDistance = 150f;
			sSAOPro.CutoffFalloff = 50f;
			sSAOPro.Blur = SSAOPro.BlurMode.HighQualityBilateral;
			sSAOPro.BlurDownsampling = false;
			sSAOPro.BlurPasses = 2;
			sSAOPro.BlurBilateralThreshold = 20f;
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	public void SaveLightSettings()
	{
		if (LightSettings == null)
		{
			return;
		}
		LightSettings.Saturation = Saturation;
		foreach (StudioLight light in Lights)
		{
			if (!(light?.Light == null))
			{
				LightSettings.AddOrUpdate(light.Name, light.Light.enabled, light.Light.intensity, light.Euler.x, light.Euler.y, light.Euler.z);
			}
		}
		LightSettings.Save();
	}

	public void ResetLights()
	{
		foreach (StudioLight light in Lights)
		{
			if (!(light?.Light == null))
			{
				light.Reset();
			}
		}
		Saturation = 1f;
		SaveLightSettings();
		MarkDirty();
	}

	public void Close()
	{
		if (IsOpen)
		{
			SaveLightSettings();
			Lights.Clear();
			ClearModel();
			DestroyPuppet();
			Character?.Destroy();
			Character = null;
			Current = null;
			if (_rig != null)
			{
				UnityEngine.Object.Destroy(_rig.gameObject);
			}
			_rig = null;
			ReleaseTexture(ref _superTexture);
			ReleaseTexture(ref _halfTexture);
			ReleaseTexture(ref _fitTexture);
			if (_fitReadback != null)
			{
				UnityEngine.Object.Destroy(_fitReadback);
				_fitReadback = null;
			}
			RenderTexture texture = PreviewTexture;
			ReleaseTexture(ref texture);
			PreviewTexture = null;
			IsOpen = false;
		}
	}

	private static void ReleaseTexture(ref RenderTexture texture)
	{
		if (!(texture == null))
		{
			texture.Release();
			UnityEngine.Object.Destroy(texture);
			texture = null;
		}
	}

	public void Select(Thing prefab, bool keepCurrentRotation = false)
	{
		if (prefab == null || prefab == Current)
		{
			return;
		}
		Current = prefab;
		if (!_originalThumbnails.ContainsKey(prefab.PrefabName))
		{
			_originalThumbnails[prefab.PrefabName] = prefab.Thumbnail;
		}
		ColorIndex = -1;
		BuildStateIndex = ((prefab is Structure { BuildStates: not null } structure && structure.BuildStates.Count > 0) ? (structure.BuildStates.Count - 1) : (-1));
		_legacyObjectOffset = prefab.ThumbnailOffset;
		Animator componentInChildren = prefab.GetComponentInChildren<Animator>(includeInactive: true);
		AnimationClips = ((componentInChildren != null && componentInChildren.runtimeAnimatorController != null) ? componentInChildren.runtimeAnimatorController.animationClips : NoClips);
		AnimationClipIndex = -1;
		AnimationTime = 1f;
		ThumbnailItemState thumbnailItemState = ItemStates?.Find(prefab.PrefabName);
		if (thumbnailItemState != null && !string.IsNullOrEmpty(thumbnailItemState.Clip))
		{
			for (int i = 0; i < AnimationClips.Length; i++)
			{
				if (!(AnimationClips[i] == null) && !(AnimationClips[i].name != thumbnailItemState.Clip))
				{
					AnimationClipIndex = i;
					AnimationTime = thumbnailItemState.ClipTime;
					break;
				}
			}
		}
		DestroyPuppet();
		TryCreatePuppet(prefab);
		if (thumbnailItemState != null)
		{
			RestorePuppetStates(thumbnailItemState.States);
		}
		PosePuppet();
		if (!keepCurrentRotation)
		{
			ThumbnailItemState thumbnailItemState2 = ItemStates?.Find(prefab.PrefabName);
			if (thumbnailItemState2 != null)
			{
				EulerAngles = thumbnailItemState2.Euler;
				ZoomMultiplier = thumbnailItemState2.Zoom;
			}
			else
			{
				EulerAngles = EulerFromStored(prefab.ThumbnailRotation);
				ZoomMultiplier = 1f;
			}
		}
		RebuildModel();
	}

	public string LoadPrefabPose()
	{
		if (Current == null)
		{
			return "Nothing selected.";
		}
		EulerAngles = EulerFromStored(Current.ThumbnailRotation);
		_legacyObjectOffset = Current.ThumbnailOffset;
		ZoomMultiplier = 1f;
		MarkDirty();
		return "Loaded " + Current.PrefabName + "'s baked thumbnail rotation/offset.";
	}

	private static Vector3 EulerFromStored(Quaternion stored)
	{
		if (!(stored.x * stored.x + stored.y * stored.y + stored.z * stored.z + stored.w * stored.w < 0.0001f))
		{
			return NormalizeEuler(stored.eulerAngles);
		}
		return Vector3.zero;
	}

	public void MarkDirty()
	{
		_dirty = true;
	}

	public void RotateLocal(Vector3 localAxis, float degrees)
	{
		EulerAngles = NormalizeEuler((Quaternion.Euler(EulerAngles) * Quaternion.AngleAxis(degrees, localAxis)).eulerAngles);
		MarkDirty();
	}

	private static Vector3 NormalizeEuler(Vector3 euler)
	{
		return new Vector3(Mathf.Repeat(euler.x + 180f, 360f) - 180f, Mathf.Repeat(euler.y + 180f, 360f) - 180f, Mathf.Repeat(euler.z + 180f, 360f) - 180f);
	}

	public void SetBuildState(int index)
	{
		if (BuildStateIndex != index)
		{
			BuildStateIndex = index;
			RebuildModel();
		}
	}

	public void SetColor(int index)
	{
		ColorIndex = index;
		ApplyColor(index);
		_dirty = true;
	}

	public void RenderIfDirty()
	{
		if (_dirty)
		{
			Render(PreviewTexture);
		}
	}

	private void RebuildModel()
	{
		ClearModel();
		Warning = string.Empty;
		if (Current == null)
		{
			return;
		}
		Transform transform = ((_puppetInstance != null) ? _puppetInstance.transform : Current.transform);
		Dictionary<Renderer, bool> rendererShow = new Dictionary<Renderer, bool>();
		Dictionary<GameObject, bool> goActive = new Dictionary<GameObject, bool>();
		if (Current is Structure { BuildStates: not null } structure && BuildStateIndex >= 0)
		{
			ComputeBuildStateVisibility(structure, BuildStateIndex, rendererShow, goActive);
		}
		AnimationClip animationClip = ((_puppetInstance == null && AnimationClipIndex >= 0 && AnimationClipIndex < AnimationClips.Length) ? AnimationClips[AnimationClipIndex] : null);
		List<(Transform, Vector3, Quaternion, Vector3)> list = null;
		if (animationClip != null)
		{
			Transform[] componentsInChildren = Current.GetComponentsInChildren<Transform>(includeInactive: true);
			list = new List<(Transform, Vector3, Quaternion, Vector3)>(componentsInChildren.Length);
			Transform[] array = componentsInChildren;
			foreach (Transform transform2 in array)
			{
				list.Add((transform2, transform2.localPosition, transform2.localRotation, transform2.localScale));
			}
			animationClip.SampleAnimation(Current.gameObject, Mathf.Clamp01(AnimationTime) * animationClip.length);
		}
		try
		{
			Renderer[] componentsInChildren2 = transform.GetComponentsInChildren<Renderer>(includeInactive: true);
			foreach (Renderer renderer in componentsInChildren2)
			{
				if (!ShouldDraw(renderer, transform, rendererShow, goActive))
				{
					continue;
				}
				Mesh mesh = null;
				bool ownsMesh = false;
				MeshFilter component;
				if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
				{
					if (skinnedMeshRenderer.sharedMesh == null)
					{
						continue;
					}
					mesh = new Mesh();
					skinnedMeshRenderer.BakeMesh(mesh);
					ownsMesh = true;
				}
				else if (renderer.TryGetComponent<MeshFilter>(out component))
				{
					mesh = component.sharedMesh;
				}
				if (mesh == null)
				{
					continue;
				}
				Material[] sharedMaterials = renderer.sharedMaterials;
				if (sharedMaterials == null || sharedMaterials.Length == 0)
				{
					continue;
				}
				DrawEntry drawEntry = new DrawEntry
				{
					Mesh = mesh,
					OwnsMesh = ownsMesh,
					LocalMatrix = transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix,
					BaseMaterials = (Material[])sharedMaterials.Clone(),
					Materials = (Material[])sharedMaterials.Clone(),
					Paintable = new bool[sharedMaterials.Length]
				};
				if (!renderer.gameObject.CompareTag("NotPaintable"))
				{
					if (Thing.ThingHasThumbnailVariations(Current) && sharedMaterials[0] != null && sharedMaterials[0].HasProperty(MaskColorId))
					{
						drawEntry.MaskMaterial = new Material(sharedMaterials[0]);
						drawEntry.Materials[0] = drawEntry.MaskMaterial;
						_hasMaskEntries = true;
					}
					if (Current.PaintableMaterial != null)
					{
						for (int j = 0; j < sharedMaterials.Length; j++)
						{
							if (!(sharedMaterials[j] != Current.PaintableMaterial))
							{
								drawEntry.Paintable[j] = true;
								_hasPaintableSlots = true;
							}
						}
					}
				}
				_entries.Add(drawEntry);
			}
		}
		finally
		{
			if (list != null)
			{
				foreach (var (transform3, localPosition, localRotation, localScale) in list)
				{
					if (!(transform3 == null))
					{
						transform3.localPosition = localPosition;
						transform3.localRotation = localRotation;
						transform3.localScale = localScale;
					}
				}
			}
		}
		if (_entries.Count == 0)
		{
			Warning = "No renderable meshes found for this prefab (it may rely on batched DrawData rendering).";
		}
		ComputeBounds();
		ApplyColor(ColorIndex);
		_dirty = true;
	}

	private static bool ShouldDraw(Renderer renderer, Transform root, Dictionary<Renderer, bool> rendererShow, Dictionary<GameObject, bool> goActive)
	{
		if (rendererShow.TryGetValue(renderer, out var value))
		{
			if (!value)
			{
				return false;
			}
		}
		else if (!renderer.enabled)
		{
			return false;
		}
		Transform transform = renderer.transform;
		while (transform != null)
		{
			GameObject gameObject = transform.gameObject;
			if (!(goActive.TryGetValue(gameObject, out var value2) ? value2 : gameObject.activeSelf))
			{
				return false;
			}
			if (transform == root)
			{
				break;
			}
			transform = transform.parent;
		}
		return true;
	}

	private static void ComputeBuildStateVisibility(Structure structure, int stateIndex, Dictionary<Renderer, bool> rendererShow, Dictionary<GameObject, bool> goActive)
	{
		List<BuildState> buildStates = structure.BuildStates;
		for (int i = 0; i < buildStates.Count; i++)
		{
			BuildState buildState = buildStates[i];
			if (buildState == null)
			{
				continue;
			}
			bool flag = i == stateIndex;
			if (buildState.LinkedGameObjects != null)
			{
				foreach (GameObject linkedGameObject in buildState.LinkedGameObjects)
				{
					if (linkedGameObject != null)
					{
						goActive[linkedGameObject] = flag;
					}
				}
			}
			bool value = buildState.RenderMode switch
			{
				BuildStateRenderMode.OnMineAndPreviousStates => i <= stateIndex, 
				BuildStateRenderMode.OnMyState => flag, 
				_ => false, 
			};
			if (buildState.Visualizer != null)
			{
				rendererShow[buildState.Visualizer] = value;
			}
			else if (buildState.RendererInstance != null && buildState.RendererInstance.HasRenderer())
			{
				GameObject rendererGameObject = buildState.RendererInstance.GetRendererGameObject();
				if (rendererGameObject != null && rendererGameObject.TryGetComponent<Renderer>(out var component))
				{
					rendererShow[component] = value;
				}
			}
		}
	}

	private void ApplyColor(int index)
	{
		ColorSwatch colorSwatch = ((index >= 0) ? GameManager.GetColorSwatch(index) : null);
		foreach (DrawEntry entry in _entries)
		{
			if (entry.MaskMaterial != null)
			{
				entry.MaskMaterial.CopyPropertiesFromMaterial(entry.BaseMaterials[0]);
				colorSwatch?.ApplyToSuitMaterial(entry.MaskMaterial);
			}
			for (int i = 0; i < entry.Materials.Length; i++)
			{
				if (entry.Paintable[i])
				{
					entry.Materials[i] = ((colorSwatch?.Normal != null) ? colorSwatch.Normal : entry.BaseMaterials[i]);
				}
			}
		}
	}

	private void ComputeBounds()
	{
		bool flag = false;
		Bounds bounds = default(Bounds);
		foreach (DrawEntry entry in _entries)
		{
			Bounds bounds2 = entry.Mesh.bounds;
			Vector3[] cornerSigns = CornerSigns;
			foreach (Vector3 b in cornerSigns)
			{
				Vector3 point = bounds2.center + Vector3.Scale(bounds2.extents, b);
				Vector3 vector = entry.LocalMatrix.MultiplyPoint3x4(point);
				if (!flag)
				{
					bounds = new Bounds(vector, Vector3.zero);
					flag = true;
				}
				else
				{
					bounds.Encapsulate(vector);
				}
			}
		}
		_bounds = (flag ? bounds : new Bounds(Vector3.zero, Vector3.one));
	}

	private float ComputeFitDistance(Quaternion modelRotation)
	{
		Quaternion quaternion = Quaternion.Inverse(SceneCameraRotation);
		float num = Mathf.Tan(_rig.Camera.fieldOfView * 0.5f * (MathF.PI / 180f));
		float num2 = 0.25f;
		Vector3[] cornerSigns = CornerSigns;
		foreach (Vector3 b in cornerSigns)
		{
			Vector3 vector = Vector3.Scale(_bounds.extents, b);
			Vector3 vector2 = quaternion * (modelRotation * vector);
			num2 = Mathf.Max(num2, Mathf.Abs(vector2.x) / num - vector2.z, Mathf.Abs(vector2.y) / num - vector2.z);
		}
		return num2 * 1.06f;
	}

	private void ClearModel()
	{
		foreach (DrawEntry entry in _entries)
		{
			if (entry.OwnsMesh && entry.Mesh != null)
			{
				UnityEngine.Object.Destroy(entry.Mesh);
			}
			if (entry.MaskMaterial != null)
			{
				UnityEngine.Object.Destroy(entry.MaskMaterial);
			}
		}
		_entries.Clear();
		_hasMaskEntries = false;
		_hasPaintableSlots = false;
	}

	private void Render(RenderTexture target)
	{
		_dirty = false;
		if (!(_rig == null) && !(Current == null) && !(target == null) && _entries.Count != 0)
		{
			Quaternion rotation = Quaternion.Euler(EulerAngles);
			if (AutoFitFraming)
			{
				ComputeAutoFitFraming(rotation);
			}
			else
			{
				ComputeLegacyFraming(rotation);
			}
			_rig.SetRigActive(active: true);
			_rig.SetZoom(_lastFinalDistance);
			SubmitDrawMeshes(_lastRootMatrix);
			_rig.RenderTo(_fitTexture);
			LastFillExtent = MeasureSilhouette(out var _, out var touchesEdge);
			LastFillClipped = touchesEdge;
			_rig.SetRigActive(active: false);
			RenderFinal(target, new Color(0f, 0f, 0f, 0f));
		}
	}

	private void ComputeLegacyFraming(Quaternion rotation)
	{
		_rig.CameraRigTransform.SetPositionAndRotation(SceneCameraPosition / Mathf.Max(0.1f, ZoomMultiplier), SceneCameraRotation);
		_lastFinalDistance = 0f;
		_lastRootMatrix = Matrix4x4.TRS(_legacyObjectOffset, rotation, Vector3.one);
	}

	private void ComputeAutoFitFraming(Quaternion rotation)
	{
		_rig.CameraRigTransform.SetPositionAndRotation(SceneCameraPosition, SceneCameraRotation);
		_lastFinalDistance = 0f;
		Vector3 forward = SceneCameraRotation * Vector3.forward;
		_silhouetteOffset = Vector3.zero;
		float num = ComputeFitDistance(rotation);
		float num2 = Mathf.Tan(_rig.Camera.fieldOfView * 0.5f * (MathF.PI / 180f));
		_rig.SetRigActive(active: true);
		_rig.SetZoom(0f);
		for (int i = 0; i < 3; i++)
		{
			SubmitDrawMeshes(RootAt(num));
			_rig.RenderTo(_fitTexture);
			Vector2 centreError;
			bool touchesEdge;
			float num3 = MeasureSilhouette(out centreError, out touchesEdge);
			if (num3 <= 0.01f)
			{
				break;
			}
			float num4 = num * num2;
			_silhouetteOffset -= SceneCameraRotation * Vector3.right * (centreError.x * num4) + SceneCameraRotation * Vector3.up * (centreError.y * num4);
			float num5 = num3 / 0.94f;
			num *= num5;
			if (Mathf.Abs(1f - num5) < 0.02f && centreError.magnitude < 0.02f)
			{
				break;
			}
		}
		_rig.SetRigActive(active: false);
		float num6 = Mathf.Max(0.1f, ZoomMultiplier);
		_silhouetteOffset /= num6;
		_lastRootMatrix = RootAt(num / num6);
		Matrix4x4 RootAt(float d)
		{
			return Matrix4x4.TRS(SceneCameraPosition + forward * d + _silhouetteOffset - rotation * _bounds.center, rotation, Vector3.one);
		}
	}

	private void RenderFinal(RenderTexture target, Color background)
	{
		RenderModelTo(_superTexture, _halfTexture, target, background);
	}

	private void RenderModelTo(RenderTexture super, RenderTexture mid, RenderTexture target, Color background, bool drawList = true)
	{
		Camera camera = _rig.Camera;
		Color backgroundColor = camera.backgroundColor;
		camera.backgroundColor = background;
		_rig.SetRigActive(active: true);
		_rig.SetZoom(_lastFinalDistance);
		if (drawList)
		{
			SubmitDrawMeshes(_lastRootMatrix);
		}
		_rig.RenderTo(super);
		_rig.SetRigActive(active: false);
		camera.backgroundColor = backgroundColor;
		if (mid != null)
		{
			Graphics.Blit(super, mid);
			Graphics.Blit(mid, target);
		}
		else
		{
			Graphics.Blit(super, target);
		}
		RenderTexture.active = null;
	}

	private void SubmitDrawMeshes(Matrix4x4 rootMatrix)
	{
		Camera camera = _rig.Camera;
		int layer = Layers.ThumbnailCreation;
		foreach (DrawEntry entry in _entries)
		{
			Matrix4x4 matrix = rootMatrix * entry.LocalMatrix;
			int b = entry.Mesh.subMeshCount - 1;
			for (int i = 0; i < entry.Materials.Length; i++)
			{
				if (!(entry.Materials[i] == null))
				{
					Graphics.DrawMesh(entry.Mesh, matrix, entry.Materials[i], layer, camera, Mathf.Min(i, b));
				}
			}
		}
	}

	private float MeasureSilhouette(out Vector2 centreError, out bool touchesEdge)
	{
		centreError = Vector2.zero;
		touchesEdge = false;
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = _fitTexture;
		_fitReadback.ReadPixels(new Rect(0f, 0f, 128f, 128f), 0, 0);
		RenderTexture.active = active;
		Color32[] pixels = _fitReadback.GetPixels32();
		int num = int.MaxValue;
		int num2 = int.MaxValue;
		int num3 = -1;
		int num4 = -1;
		for (int i = 0; i < pixels.Length; i++)
		{
			if (pixels[i].a >= 8)
			{
				int num5 = i % 128;
				int num6 = i / 128;
				if (num5 < num)
				{
					num = num5;
				}
				if (num5 > num3)
				{
					num3 = num5;
				}
				if (num6 < num2)
				{
					num2 = num6;
				}
				if (num6 > num4)
				{
					num4 = num6;
				}
			}
		}
		if (num3 < 0)
		{
			return 0f;
		}
		touchesEdge = num <= 0 || num2 <= 0 || num3 >= 127 || num4 >= 127;
		float num7 = 64f;
		centreError = new Vector2(((float)(num + num3 + 1) * 0.5f - num7) / num7, ((float)(num2 + num4 + 1) * 0.5f - num7) / num7);
		return Mathf.Clamp01((float)Mathf.Max(num3 - num + 1, num4 - num2 + 1) * 0.5f / num7);
	}

	private Texture2D CaptureMatted()
	{
		Render(PreviewTexture);
		Texture2D black = ReadTexture(PreviewTexture);
		RenderFinal(PreviewTexture, new Color(1f, 1f, 1f, 0f));
		Texture2D white = ReadTexture(PreviewTexture);
		Texture2D result = MatteToTexture(black, white, Saturation);
		RenderFinal(PreviewTexture, new Color(0f, 0f, 0f, 0f));
		return result;
	}

	private static Texture2D MatteToTexture(Texture2D black, Texture2D white, float saturation)
	{
		Color32[] pixels = black.GetPixels32();
		Color32[] pixels2 = white.GetPixels32();
		for (int i = 0; i < pixels.Length; i++)
		{
			Color32 color = pixels[i];
			Color32 color2 = pixels2[i];
			float value = (float)(color2.r - color.r + (color2.g - color.g) + (color2.b - color.b)) / 765f;
			float a = 1f - Mathf.Clamp01(value);
			a = Mathf.Max(a, (float)(int)color.a / 255f);
			if (a <= 0.004f)
			{
				pixels[i] = new Color32(0, 0, 0, 0);
				continue;
			}
			float num = Mathf.Min(255f, (float)(int)color.r / a);
			float num2 = Mathf.Min(255f, (float)(int)color.g / a);
			float num3 = Mathf.Min(255f, (float)(int)color.b / a);
			if (Mathf.Abs(saturation - 1f) > 0.01f)
			{
				float num4 = num * 0.299f + num2 * 0.587f + num3 * 0.114f;
				num = Mathf.Clamp(num4 + (num - num4) * saturation, 0f, 255f);
				num2 = Mathf.Clamp(num4 + (num2 - num4) * saturation, 0f, 255f);
				num3 = Mathf.Clamp(num4 + (num3 - num4) * saturation, 0f, 255f);
			}
			pixels[i] = new Color32((byte)num, (byte)num2, (byte)num3, (byte)(a * 255f));
		}
		black.SetPixels32(pixels);
		black.Apply(updateMipmaps: true);
		UnityEngine.Object.Destroy(white);
		return black;
	}

	public Texture2D CaptureMattedAt(int outSize)
	{
		if (_rig == null || Current == null || _entries.Count == 0)
		{
			return null;
		}
		Quaternion rotation = Quaternion.Euler(EulerAngles);
		if (AutoFitFraming)
		{
			ComputeAutoFitFraming(rotation);
		}
		else
		{
			ComputeLegacyFraming(rotation);
		}
		return CaptureMattedCore(outSize, drawList: true);
	}

	public Texture2D CaptureRigMatted(int outSize)
	{
		if (_rig == null)
		{
			return null;
		}
		return CaptureMattedCore(outSize, drawList: false);
	}

	private Texture2D CaptureMattedCore(int outSize, bool drawList)
	{
		outSize = Mathf.Clamp(outSize, 32, 4096);
		int num = ((outSize <= 1024) ? 4 : ((outSize > 2048) ? 1 : 2));
		while (outSize * num > 4096 && num > 1)
		{
			num /= 2;
		}
		RenderTexture texture = new RenderTexture(outSize * num, outSize * num, 24, RenderTextureFormat.ARGB32)
		{
			antiAliasing = Mathf.Clamp(num * 2, 1, 4),
			filterMode = FilterMode.Bilinear
		};
		RenderTexture texture2 = ((num < 4) ? null : new RenderTexture(outSize * 2, outSize * 2, 0, RenderTextureFormat.ARGB32)
		{
			filterMode = FilterMode.Bilinear
		});
		RenderTexture texture3 = new RenderTexture(outSize, outSize, 0, RenderTextureFormat.ARGB32)
		{
			filterMode = FilterMode.Bilinear
		};
		try
		{
			RenderModelTo(texture, texture2, texture3, new Color(0f, 0f, 0f, 0f), drawList);
			Texture2D black = ReadTexture(texture3);
			RenderModelTo(texture, texture2, texture3, new Color(1f, 1f, 1f, 0f), drawList);
			Texture2D white = ReadTexture(texture3);
			return MatteToTexture(black, white, Saturation);
		}
		finally
		{
			ReleaseTexture(ref texture);
			ReleaseTexture(ref texture2);
			ReleaseTexture(ref texture3);
		}
	}

	public Texture2D CaptureCharacterTile(CharacterConfig config, int outSize)
	{
		if (_rig == null || Character == null || !Character.Available || config == null)
		{
			return null;
		}
		Character.Activate();
		Character.Drive(config);
		_rig.CameraRigTransform.SetPositionAndRotation(SceneCameraPosition, SceneCameraRotation);
		_lastFinalDistance = 0f;
		Character.PositionForCapture(SceneCameraPosition, SceneCameraRotation, 20.8f, 0.9f, config);
		Texture2D result = CaptureRigMatted(outSize);
		Character.Deactivate();
		return result;
	}

	public void RenderCharacterPreview(CharacterConfig config)
	{
		Texture2D texture2D = CaptureCharacterTile(config, 256);
		if (!(texture2D == null))
		{
			Graphics.Blit(texture2D, PreviewTexture);
			UnityEngine.Object.Destroy(texture2D);
			LastFillExtent = 1f;
			LastFillClipped = false;
		}
	}

	private void PrepareObjectTurntable()
	{
		_turntableEuler = EulerAngles;
		float num = 0.25f;
		for (int i = 0; i < 360; i += 15)
		{
			Quaternion modelRotation = Quaternion.Euler(_turntableEuler.x, _turntableEuler.y + (float)i, _turntableEuler.z);
			num = Mathf.Max(num, ComputeFitDistance(modelRotation));
		}
		_turntableDistance = num * 1.02f / Mathf.Max(0.1f, ZoomMultiplier);
	}

	private Texture2D CaptureObjectTurntableTile(float frameYaw, int outSize)
	{
		Quaternion quaternion = Quaternion.Euler(_turntableEuler.x, _turntableEuler.y + frameYaw, _turntableEuler.z);
		_rig.CameraRigTransform.SetPositionAndRotation(SceneCameraPosition, SceneCameraRotation);
		_lastFinalDistance = 0f;
		Vector3 vector = SceneCameraRotation * Vector3.forward;
		_lastRootMatrix = Matrix4x4.TRS(SceneCameraPosition + vector * _turntableDistance - quaternion * _bounds.center, quaternion, Vector3.one);
		return CaptureMattedCore(outSize, drawList: true);
	}

	private static Texture2D ReadTexture(RenderTexture source)
	{
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = source;
		Texture2D texture2D = new Texture2D(source.width, source.height, TextureFormat.ARGB32, mipChain: true);
		texture2D.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
		texture2D.Apply(updateMipmaps: true);
		RenderTexture.active = active;
		return texture2D;
	}

	private static Sprite MakeSprite(Texture2D texture, string name)
	{
		Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
		sprite.name = name;
		texture.name = name;
		return sprite;
	}

	public string Save()
	{
		if (Current == null)
		{
			return "Nothing selected.";
		}
		if (_entries.Count == 0)
		{
			return "Nothing to render for this prefab.";
		}
		ThumbnailLinkGroup thumbnailLinkGroup = Links?.GroupFor(Current.PrefabName);
		if (thumbnailLinkGroup != null && thumbnailLinkGroup.Leader != Current.PrefabName)
		{
			return Current.PrefabName + " is linked to " + thumbnailLinkGroup.Leader + " — save the leader instead.";
		}
		string prefabName = Current.PrefabName;
		int num = 0;
		Sprite[] array = null;
		if (!(Current is Structure) && Thing.ThingHasThumbnailVariations(Current))
		{
			List<ColorSwatch> customColors = Singleton<GameManager>.Instance.CustomColors;
			array = new Sprite[customColors.Count];
			for (int i = 0; i < customColors.Count; i++)
			{
				ColorSwatch colorSwatch = customColors[i];
				if (colorSwatch != null && (!(colorSwatch.Normal == null) || _hasMaskEntries))
				{
					ApplyColor(i);
					Texture2D texture2D = CaptureMatted();
					string text = ThumbnailPaths.ColorVariantName(colorSwatch, i);
					File.WriteAllBytes(ThumbnailPaths.Variant(prefabName, text), texture2D.EncodeToPNG());
					array[i] = MakeSprite(texture2D, prefabName + "_" + text);
					num++;
				}
			}
			ApplyColor(ColorIndex);
		}
		Texture2D texture2D2 = CaptureMatted();
		File.WriteAllBytes(ThumbnailPaths.Base(prefabName), texture2D2.EncodeToPNG());
		Sprite sprite = MakeSprite(texture2D2, prefabName);
		num++;
		Current.Thumbnail = sprite;
		if (array != null)
		{
			Current.Thumbnails = array;
		}
		if (Current is Structure { BuildStates: not null } structure && structure.BuildStates.Count > 1)
		{
			int buildStateIndex = BuildStateIndex;
			for (int j = 0; j < structure.BuildStates.Count; j++)
			{
				if (structure.BuildStates[j] != null)
				{
					SetBuildState(j);
					Texture2D texture2D3 = CaptureMatted();
					File.WriteAllBytes(ThumbnailPaths.BuildState(prefabName, j), texture2D3.EncodeToPNG());
					structure.BuildStates[j].Thumbnail = MakeSprite(texture2D3, $"{prefabName}_BuildState{j}");
					num++;
				}
			}
			SetBuildState(buildStateIndex);
		}
		int num2 = PropagateToLinks(prefabName, sprite, array);
		_dirty = true;
		string clip = ((AnimationClipIndex >= 0 && AnimationClipIndex < AnimationClips.Length && AnimationClips[AnimationClipIndex] != null) ? AnimationClips[AnimationClipIndex].name : null);
		ItemStates.AddOrUpdate(prefabName, EulerAngles, ZoomMultiplier, clip, AnimationTime, SerializePuppetStates());
		ItemStates.Save();
		_savedPoseNames.Add(prefabName);
		SaveLightSettings();
		string text2 = $"Saved {num} image(s) for {prefabName} to {ThumbnailPaths.Folder}";
		if (num2 > 0)
		{
			text2 += $", shared with {num2} linked item(s)";
		}
		ConsoleWindow.PrintAction(text2);
		return text2;
	}

	private int PropagateToLinks(string name, Sprite baseSprite, Sprite[] variants)
	{
		ThumbnailLinkGroup thumbnailLinkGroup = Links?.GroupFor(name);
		if (thumbnailLinkGroup == null)
		{
			return 0;
		}
		List<ColorSwatch> customColors = Singleton<GameManager>.Instance.CustomColors;
		int num = 0;
		foreach (string member in thumbnailLinkGroup.Members)
		{
			if (member == name)
			{
				continue;
			}
			Thing thing = FindPrefab(member);
			if (thing == null)
			{
				continue;
			}
			thing.Thumbnail = baseSprite;
			if (variants != null)
			{
				thing.Thumbnails = variants;
			}
			TryCopy(ThumbnailPaths.Base(name), ThumbnailPaths.Base(member));
			if (variants != null)
			{
				for (int i = 0; i < variants.Length && i < customColors.Count; i++)
				{
					if (!(variants[i] == null))
					{
						string colorName = ThumbnailPaths.ColorVariantName(customColors[i], i);
						TryCopy(ThumbnailPaths.Variant(name, colorName), ThumbnailPaths.Variant(member, colorName));
					}
				}
			}
			num++;
		}
		return num;
	}

	private static Thing FindPrefab(string prefabName)
	{
		Thing thing = Prefab.Find(prefabName);
		if (thing != null)
		{
			return thing;
		}
		IReadOnlyList<Thing> galleryPrefabs = GalleryPrefabs;
		for (int i = 0; i < galleryPrefabs.Count; i++)
		{
			if (galleryPrefabs[i] != null && galleryPrefabs[i].PrefabName == prefabName)
			{
				return galleryPrefabs[i];
			}
		}
		return null;
	}

	private static void TryCopy(string from, string to)
	{
		try
		{
			if (File.Exists(from))
			{
				File.Copy(from, to, overwrite: true);
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	public string LinkSelected(IEnumerable<string> members)
	{
		if (Current == null)
		{
			return "Select a leader item first.";
		}
		ThumbnailLinkGroup thumbnailLinkGroup = Links.Link(Current.PrefabName, members);
		Links.Save();
		return $"Linked {thumbnailLinkGroup.Members.Count} item(s) with '{thumbnailLinkGroup.Leader}' as leader.";
	}

	public string UnlinkCurrent()
	{
		if (Current == null)
		{
			return "Nothing selected.";
		}
		Links.Unlink(Current.PrefabName);
		Links.Save();
		return "Unlinked " + Current.PrefabName + ".";
	}

	public ThumbnailLinkGroup GetLinkGroup(string prefabName)
	{
		return Links?.GroupFor(prefabName);
	}

	public bool SelectByName(string prefabName, bool keepCurrentRotation = false)
	{
		Thing thing = FindPrefab(prefabName);
		if (thing == null)
		{
			return false;
		}
		Select(thing, keepCurrentRotation);
		return true;
	}

	public string StartBatchRethumbnail()
	{
		_batchQueue.Clear();
		foreach (ThumbnailItemState item in ItemStates.Items)
		{
			if (item != null && !string.IsNullOrEmpty(item.Name))
			{
				ThumbnailLinkGroup thumbnailLinkGroup = Links?.GroupFor(item.Name);
				if ((thumbnailLinkGroup == null || !(thumbnailLinkGroup.Leader != item.Name)) && !(FindPrefab(item.Name) == null))
				{
					_batchQueue.Enqueue(item.Name);
				}
			}
		}
		BatchTotal = _batchQueue.Count;
		if (BatchTotal != 0)
		{
			return $"Re-rendering {BatchTotal} saved item(s)…";
		}
		return "No previously saved items to re-render.";
	}

	public void CancelBatch()
	{
		_batchQueue.Clear();
	}

	public string ProcessBatchStep()
	{
		if (_batchQueue.Count == 0)
		{
			return null;
		}
		string text = _batchQueue.Dequeue();
		Thing thing = FindPrefab(text);
		if (thing == null)
		{
			return "Skipped " + text + " (prefab not found).";
		}
		Select(thing);
		return Save();
	}

	public string AutoAlignToOriginal(bool fromCurrentOnly = false)
	{
		if (Current == null || _entries.Count == 0)
		{
			return "Nothing selected.";
		}
		Sprite originalThumbnail = OriginalThumbnail;
		if (originalThumbnail == null || originalThumbnail.texture == null)
		{
			return "No existing thumbnail to align to.";
		}
		bool[,] target = BuildMaskFromSprite(originalThumbnail);
		if (target == null)
		{
			return "Couldn't read a silhouette from the existing thumbnail.";
		}
		bool[,] targetFlipped = FlipMaskVertically(target);
		int renderBudget = 900;
		Dictionary<Vector3Int, float> scoreCache = new Dictionary<Vector3Int, float>();
		List<(Vector3, float)> list = new List<(Vector3, float)>();
		Vector3 vector = NormalizeEuler(EulerAngles);
		float[] array;
		if (fromCurrentOnly)
		{
			list.Add((vector, Score(vector)));
			array = new float[4] { 15f, 7.5f, 3f, 1.5f };
		}
		else
		{
			for (float num = -90f; num <= 90f; num += 15f)
			{
				for (float num2 = -180f; num2 < 180f; num2 += 15f)
				{
					Vector3 vector2 = new Vector3(num, num2, 0f);
					float num3 = Score(vector2);
					bool flag = false;
					for (int i = 0; i < list.Count; i++)
					{
						if (!(Quaternion.Angle(Quaternion.Euler(vector2), Quaternion.Euler(list[i].Item1)) > 25f))
						{
							if (num3 > list[i].Item2)
							{
								list[i] = (vector2, num3);
							}
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						list.Add((vector2, num3));
					}
				}
			}
			list.Sort(((Vector3 euler, float score) a, (Vector3 euler, float score) b) => b.score.CompareTo(a.score));
			if (list.Count > 4)
			{
				list.RemoveRange(4, list.Count - 4);
			}
			list.Add((vector, Score(vector)));
			array = new float[3] { 7.5f, 3f, 1.5f };
		}
		Vector3 euler = EulerAngles;
		float num4 = -1f;
		foreach (var item in list)
		{
			Vector3 vector3 = item.Item1;
			float num5 = item.Item2;
			float[] array2 = array;
			foreach (float num7 in array2)
			{
				for (int num8 = 0; num8 < 8; num8++)
				{
					bool flag2 = false;
					for (int num9 = 0; num9 < 3; num9++)
					{
						for (int num10 = -1; num10 <= 1; num10 += 2)
						{
							Vector3 vector4 = vector3;
							vector4[num9] += (float)num10 * num7;
							float num11 = Score(vector4);
							if (!(num11 <= num5))
							{
								vector3 = vector4;
								num5 = num11;
								flag2 = true;
							}
						}
					}
					if (!flag2)
					{
						break;
					}
				}
			}
			if (num5 > num4)
			{
				num4 = num5;
				euler = vector3;
			}
		}
		EulerAngles = NormalizeEuler(euler);
		MarkDirty();
		return $"Auto-aligned to the existing thumbnail ({num4:P0} silhouette match).";
		float Score(Vector3 euler2)
		{
			Vector3Int key = new Vector3Int(Mathf.RoundToInt(euler2.x * 2f), Mathf.RoundToInt(euler2.y * 2f), Mathf.RoundToInt(euler2.z * 2f));
			if (scoreCache.TryGetValue(key, out var value))
			{
				return value;
			}
			if (renderBudget-- <= 0)
			{
				return -1f;
			}
			bool[,] a = RenderMask(euler2);
			float num12 = Mathf.Max(MaskIoU(a, target), MaskIoU(a, targetFlipped));
			scoreCache[key] = num12;
			return num12;
		}
	}

	private bool[,] RenderMask(Vector3 euler)
	{
		Quaternion quaternion = Quaternion.Euler(euler);
		_rig.CameraRigTransform.SetPositionAndRotation(SceneCameraPosition, SceneCameraRotation);
		_rig.SetRigActive(active: true);
		_rig.SetZoom(0f);
		Vector3 pos = SceneCameraPosition + SceneCameraRotation * Vector3.forward * ComputeFitDistance(quaternion) - quaternion * _bounds.center;
		SubmitDrawMeshes(Matrix4x4.TRS(pos, quaternion, Vector3.one));
		_rig.RenderTo(_fitTexture);
		_rig.SetRigActive(active: false);
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = _fitTexture;
		_fitReadback.ReadPixels(new Rect(0f, 0f, 128f, 128f), 0, 0);
		RenderTexture.active = active;
		return ReadNormalizedMask();
	}

	private bool[,] BuildMaskFromSprite(Sprite sprite)
	{
		RenderTexture temporary = RenderTexture.GetTemporary(128, 128, 0, RenderTextureFormat.ARGB32);
		RenderTexture active = RenderTexture.active;
		Graphics.Blit(sprite.texture, temporary);
		RenderTexture.active = temporary;
		_fitReadback.ReadPixels(new Rect(0f, 0f, 128f, 128f), 0, 0);
		RenderTexture.active = active;
		RenderTexture.ReleaseTemporary(temporary);
		return ReadNormalizedMask();
	}

	private bool[,] ReadNormalizedMask()
	{
		Color32[] pixels = _fitReadback.GetPixels32();
		int num = int.MaxValue;
		int num2 = int.MaxValue;
		int num3 = -1;
		int num4 = -1;
		for (int i = 0; i < pixels.Length; i++)
		{
			if (pixels[i].a >= 8)
			{
				int num5 = i % 128;
				int num6 = i / 128;
				if (num5 < num)
				{
					num = num5;
				}
				if (num5 > num3)
				{
					num3 = num5;
				}
				if (num6 < num2)
				{
					num2 = num6;
				}
				if (num6 > num4)
				{
					num4 = num6;
				}
			}
		}
		if (num3 < 0)
		{
			return null;
		}
		bool[,] array = new bool[48, 48];
		int num7 = num3 - num + 1;
		int num8 = num4 - num2 + 1;
		for (int j = 0; j < 48; j++)
		{
			int num9 = num2 + (int)(((float)j + 0.5f) * (float)num8 / 48f);
			for (int k = 0; k < 48; k++)
			{
				int num10 = num + (int)(((float)k + 0.5f) * (float)num7 / 48f);
				array[k, j] = pixels[num9 * 128 + num10].a >= 8;
			}
		}
		return array;
	}

	private static bool[,] FlipMaskVertically(bool[,] mask)
	{
		bool[,] array = new bool[48, 48];
		for (int i = 0; i < 48; i++)
		{
			for (int j = 0; j < 48; j++)
			{
				array[j, 47 - i] = mask[j, i];
			}
		}
		return array;
	}

	private static float MaskIoU(bool[,] a, bool[,] b)
	{
		if (a == null || b == null)
		{
			return 0f;
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < 48; i++)
		{
			for (int j = 0; j < 48; j++)
			{
				bool num3 = a[j, i];
				bool flag = b[j, i];
				if (num3 && flag)
				{
					num++;
				}
				if (num3 || flag)
				{
					num2++;
				}
			}
		}
		if (num2 != 0)
		{
			return (float)num / (float)num2;
		}
		return 0f;
	}

	public bool HasSavedPose(string prefabName)
	{
		return _savedPoseNames.Contains(prefabName);
	}

	public bool ApplySavedPose(string prefabName)
	{
		ThumbnailItemState thumbnailItemState = ItemStates?.Find(prefabName);
		if (thumbnailItemState == null)
		{
			return false;
		}
		EulerAngles = thumbnailItemState.Euler;
		ZoomMultiplier = thumbnailItemState.Zoom;
		MarkDirty();
		return true;
	}

	public void SaveRotationPreset(string name)
	{
		if (!string.IsNullOrWhiteSpace(name))
		{
			Rotations.AddOrUpdate(name.Trim(), EulerAngles, ZoomMultiplier);
			Rotations.Save();
		}
	}

	public void ApplyRotationPreset(ThumbnailRotationPreset preset)
	{
		if (preset != null)
		{
			EulerAngles = preset.Euler;
			ZoomMultiplier = preset.Zoom;
			MarkDirty();
		}
	}

	public void RemoveRotationPreset(string name)
	{
		Rotations.Remove(name);
		Rotations.Save();
	}

	private StudioPose CapturePose()
	{
		return new StudioPose
		{
			PrefabName = Current?.PrefabName,
			Euler = EulerAngles,
			Zoom = ZoomMultiplier,
			ColorIndex = ColorIndex,
			AutoFit = AutoFitFraming
		};
	}

	private void RestorePose(StudioPose pose)
	{
		AutoFitFraming = pose.AutoFit;
		if (pose.PrefabName != null && SelectByName(pose.PrefabName))
		{
			EulerAngles = pose.Euler;
			ZoomMultiplier = pose.Zoom;
			SetColor(pose.ColorIndex);
		}
		MarkDirty();
	}

	private bool SelectForExport(ExportItem item)
	{
		if (string.IsNullOrEmpty(item.PrefabName))
		{
			return false;
		}
		Thing thing = FindPrefab(item.PrefabName);
		if (thing == null)
		{
			return false;
		}
		Select(thing);
		if (!item.UseOverride)
		{
			return true;
		}
		EulerAngles = item.Euler;
		ZoomMultiplier = item.Zoom;
		SetColor(item.ColorIndex);
		return true;
	}

	public string ExportSheet(IReadOnlyList<ExportItem> items, int width, int height, int rows, float paddingFraction, bool dropShadow, float shadowOpacity)
	{
		if (!IsOpen || _rig == null)
		{
			return "Thumbnail studio isn't open.";
		}
		if (items == null || items.Count == 0)
		{
			return "No objects selected for export.";
		}
		width = Mathf.Clamp(width, 16, 8192);
		height = Mathf.Clamp(height, 16, 8192);
		rows = Mathf.Clamp(rows, 1, items.Count);
		int num = Mathf.CeilToInt((float)items.Count / (float)rows);
		paddingFraction = Mathf.Clamp(paddingFraction, 0f, 0.45f);
		int num2 = Mathf.Min(width, height);
		int num3 = (dropShadow ? Mathf.Clamp(Mathf.RoundToInt((float)num2 * 0.012f), 2, 48) : 0);
		int num4 = (dropShadow ? Mathf.Clamp(Mathf.RoundToInt((float)num2 * 0.007f), 1, 28) : 0);
		int num5 = (dropShadow ? Mathf.Clamp(num3 + num4 + 3, 0, num2 / 4) : 0);
		int num6 = (width - 2 * num5) / num;
		int num7 = (height - 2 * num5) / rows;
		int num8 = Mathf.RoundToInt((float)Mathf.Min(num6, num7) * (1f - paddingFraction));
		if (num8 < 8)
		{
			return "Cells are too small - increase the image size, or reduce the column count / padding.";
		}
		int outSize = Mathf.Clamp(num8, 256, 1024);
		StudioPose pose = CapturePose();
		AutoFitFraming = true;
		Color32[] array = new Color32[width * height];
		int num9 = 0;
		try
		{
			for (int i = 0; i < items.Count; i++)
			{
				ExportItem exportItem = items[i];
				if (exportItem == null)
				{
					continue;
				}
				Texture2D texture2D;
				if (exportItem.Character != null)
				{
					texture2D = CaptureCharacterTile(exportItem.Character, outSize);
				}
				else
				{
					if (!SelectForExport(exportItem))
					{
						continue;
					}
					texture2D = CaptureMattedAt(outSize);
				}
				if (!(texture2D == null))
				{
					int num10 = i % num;
					int num11 = i / num;
					CompositeTile(array, width, height, texture2D, num5 + num10 * num6 + (num6 - num8) / 2, num5 + num11 * num7 + (num7 - num8) / 2, num8);
					UnityEngine.Object.Destroy(texture2D);
					num9++;
				}
			}
			if (num9 == 0)
			{
				return "Nothing could be rendered for the selected objects.";
			}
			Color32[] pixels = (dropShadow ? ComposeWithShadow(array, width, height, Mathf.Clamp01(shadowOpacity), num3, num4) : array);
			Texture2D texture2D2 = new Texture2D(width, height, TextureFormat.ARGB32, mipChain: false);
			texture2D2.SetPixels32(pixels);
			texture2D2.Apply(updateMipmaps: false);
			string text = Path.Combine(ThumbnailPaths.ExportFolder, $"render_{width}x{height}_{num9}items_{DateTime.Now:yyyyMMdd_HHmmss}.png");
			File.WriteAllBytes(text, texture2D2.EncodeToPNG());
			UnityEngine.Object.Destroy(texture2D2);
			ConsoleWindow.PrintAction($"Exported {num9} object(s) to {text}");
			return $"Exported {num9} object(s) ({width}x{height}) to {text}";
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			return "Export failed: " + ex.Message;
		}
		finally
		{
			RestorePose(pose);
		}
	}

	public string ExportTurntableGif(IReadOnlyList<ExportItem> items, int width, int height, int rows, float paddingFraction, int gifWidth, float rotationSeconds, int frameCount, bool dither)
	{
		if (!IsOpen || _rig == null)
		{
			return "Thumbnail studio isn't open.";
		}
		if (items == null || items.Count == 0)
		{
			return "No objects selected for export.";
		}
		width = Mathf.Clamp(width, 16, 8192);
		height = Mathf.Clamp(height, 16, 8192);
		gifWidth = Mathf.Clamp(gifWidth, 700, 1600);
		int num = Mathf.Max(16, Mathf.RoundToInt((float)gifWidth * (float)height / (float)width));
		int num2 = Mathf.Clamp(frameCount, 4, 120);
		int num3 = Mathf.Clamp(Mathf.RoundToInt((float)Mathf.Clamp(Mathf.RoundToInt(rotationSeconds * 100f), 50, 3000) / (float)num2), 2, 250);
		rows = Mathf.Clamp(rows, 1, items.Count);
		int num4 = Mathf.CeilToInt((float)items.Count / (float)rows);
		paddingFraction = Mathf.Clamp(paddingFraction, 0f, 0.45f);
		int num5 = gifWidth / num4;
		int num6 = num / rows;
		int num7 = Mathf.RoundToInt((float)Mathf.Min(num5, num6) * (1f - paddingFraction));
		if (num7 < 8)
		{
			return "Cells too small for a GIF - increase the width or reduce columns/padding.";
		}
		int num8 = Mathf.Clamp(num7, 128, 512);
		StudioPose pose = CapturePose();
		AutoFitFraming = true;
		Color32[][][] array = new Color32[items.Count][][];
		int num9 = 0;
		try
		{
			for (int i = 0; i < items.Count; i++)
			{
				ExportItem exportItem = items[i];
				array[i] = new Color32[num2][];
				if (exportItem == null)
				{
					continue;
				}
				if (exportItem.Character != null)
				{
					if (Character != null && Character.Available)
					{
						Character.Activate();
						Character.Drive(exportItem.Character);
						Character.PrepareTurntable(SceneCameraPosition, SceneCameraRotation, 20.8f, 0.9f, exportItem.Character);
						for (int j = 0; j < num2; j++)
						{
							_rig.CameraRigTransform.SetPositionAndRotation(SceneCameraPosition, SceneCameraRotation);
							_lastFinalDistance = 0f;
							Character.PositionTurntableFrame(SceneCameraPosition, SceneCameraRotation, exportItem.Character, (float)j * 360f / (float)num2);
							array[i][j] = ReadTilePixels(CaptureRigMatted(num8));
						}
						Character.Deactivate();
						num9++;
					}
				}
				else if (SelectForExport(exportItem) && _entries.Count != 0)
				{
					PrepareObjectTurntable();
					for (int k = 0; k < num2; k++)
					{
						array[i][k] = ReadTilePixels(CaptureObjectTurntableTile((float)k * 360f / (float)num2, num8));
					}
					num9++;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			return "GIF export failed: " + ex.Message;
		}
		finally
		{
			RestorePose(pose);
		}
		if (num9 == 0)
		{
			return "Nothing could be rendered for the selected objects.";
		}
		List<Color32[]> list = new List<Color32[]>(num2);
		for (int l = 0; l < num2; l++)
		{
			Color32[] array2 = new Color32[gifWidth * num];
			for (int m = 0; m < items.Count; m++)
			{
				Color32[][] obj = array[m];
				Color32[] array3 = ((obj != null) ? obj[l] : null);
				if (array3 != null)
				{
					int num10 = m % num4;
					int num11 = m / num4;
					CompositeTilePixels(array2, gifWidth, num, array3, num8, num10 * num5 + (num5 - num7) / 2, num11 * num6 + (num6 - num7) / 2, num7);
				}
			}
			list.Add(array2);
		}
		byte[] array4 = ThumbnailGif.Encode(list, gifWidth, num, num3, 128, dither);
		if (array4 == null)
		{
			return "Couldn't produce the GIF.";
		}
		try
		{
			string text = Path.Combine(ThumbnailPaths.ExportFolder, $"turntable_{gifWidth}x{num}_{num2}f_{DateTime.Now:yyyyMMdd_HHmmss}.gif");
			File.WriteAllBytes(text, array4);
			int num12 = array4.Length / 1024;
			string text2 = (100f / (float)num3).ToString("0.#");
			ConsoleWindow.PrintAction($"Exported turntable GIF ({num2}f, {num12} KB) to {text}");
			string text3 = $"Exported GIF: {num2} frames @ {text2} fps, {gifWidth}x{num}, " + $"{num12} KB  {text}";
			if (array4.Length > 5242880)
			{
				text3 += $"  - over 5 MB ({num12} KB), reduce width, frames, or rotation time.";
			}
			return text3;
		}
		catch (Exception ex2)
		{
			Debug.LogException(ex2);
			return "GIF write failed: " + ex2.Message;
		}
	}

	private static Color32[] ReadTilePixels(Texture2D tile)
	{
		if (tile == null)
		{
			return null;
		}
		Color32[] pixels = tile.GetPixels32();
		UnityEngine.Object.Destroy(tile);
		return pixels;
	}

	private static void CompositeTile(Color32[] canvas, int canvasWidth, int canvasHeight, Texture2D tile, int originX, int originYTop, int drawSize)
	{
		CompositeTilePixels(canvas, canvasWidth, canvasHeight, tile.GetPixels32(), tile.width, originX, originYTop, drawSize);
	}

	private static void CompositeTilePixels(Color32[] canvas, int canvasWidth, int canvasHeight, Color32[] source, int sourceSize, int originX, int originYTop, int drawSize)
	{
		for (int i = 0; i < drawSize; i++)
		{
			int num = originYTop + i;
			if (num < 0 || num >= canvasHeight)
			{
				continue;
			}
			int num2 = (canvasHeight - 1 - num) * canvasWidth;
			float y = ((float)i + 0.5f) / (float)drawSize * (float)sourceSize - 0.5f;
			for (int j = 0; j < drawSize; j++)
			{
				int num3 = originX + j;
				if (num3 >= 0 && num3 < canvasWidth)
				{
					float x = ((float)j + 0.5f) / (float)drawSize * (float)sourceSize - 0.5f;
					Color32 color = SampleBilinear(source, sourceSize, x, y);
					if (color.a != 0)
					{
						canvas[num2 + num3] = color;
					}
				}
			}
		}
	}

	private static Color32 SampleBilinear(Color32[] pixels, int size, float x, float y)
	{
		int num = Mathf.Clamp(Mathf.FloorToInt(x), 0, size - 1);
		int num2 = Mathf.Clamp(Mathf.FloorToInt(y), 0, size - 1);
		int num3 = Mathf.Min(num + 1, size - 1);
		int row = Mathf.Min(num2 + 1, size - 1);
		float fx = Mathf.Clamp01(x - (float)num);
		float fy = Mathf.Clamp01(y - (float)num2);
		Color32 color = pixels[Row(num2) + num];
		Color32 color2 = pixels[Row(num2) + num3];
		Color32 color3 = pixels[Row(row) + num];
		Color32 color4 = pixels[Row(row) + num3];
		return new Color32((byte)Blend((int)color.r, (int)color2.r, (int)color3.r, (int)color4.r), (byte)Blend((int)color.g, (int)color2.g, (int)color3.g, (int)color4.g), (byte)Blend((int)color.b, (int)color2.b, (int)color3.b, (int)color4.b), (byte)Blend((int)color.a, (int)color2.a, (int)color3.a, (int)color4.a));
		float Blend(float a, float b, float c, float d)
		{
			return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
		}
		int Row(int num4)
		{
			return (size - 1 - num4) * size;
		}
	}

	private static Color32[] ComposeWithShadow(Color32[] objects, int width, int height, float opacity, int blurRadius, int offset)
	{
		int num = width * height;
		float[] array = new float[num];
		for (int i = 0; i < height; i++)
		{
			int num2 = i + offset;
			if (num2 < 0 || num2 >= height)
			{
				continue;
			}
			int num3 = i * width;
			int num4 = num2 * width;
			for (int j = 0; j < width; j++)
			{
				int num5 = j - offset;
				if (num5 >= 0 && num5 < width)
				{
					array[num3 + j] = (float)(int)objects[num4 + num5].a / 255f;
				}
			}
		}
		BoxBlur(array, width, height, blurRadius, 3);
		Color32[] array2 = new Color32[num];
		for (int k = 0; k < num; k++)
		{
			Color32 color = objects[k];
			float num6 = (float)(int)color.a / 255f;
			float num7 = array[k] * opacity;
			float num8 = num6 + num7 * (1f - num6);
			if (num8 <= 0.0001f)
			{
				array2[k] = new Color32(0, 0, 0, 0);
				continue;
			}
			float num9 = num6 / num8;
			array2[k] = new Color32((byte)((float)(int)color.r * num9), (byte)((float)(int)color.g * num9), (byte)((float)(int)color.b * num9), (byte)Mathf.Clamp(num8 * 255f, 0f, 255f));
		}
		return array2;
	}

	private static void BoxBlur(float[] buffer, int width, int height, int radius, int passes)
	{
		if (radius < 1)
		{
			return;
		}
		float[] array = new float[buffer.Length];
		int num = radius * 2 + 1;
		for (int i = 0; i < passes; i++)
		{
			for (int j = 0; j < height; j++)
			{
				int num2 = j * width;
				float num3 = 0f;
				for (int k = -radius; k <= radius; k++)
				{
					num3 += buffer[num2 + Mathf.Clamp(k, 0, width - 1)];
				}
				for (int l = 0; l < width; l++)
				{
					array[num2 + l] = num3 / (float)num;
					num3 += buffer[num2 + Mathf.Clamp(l + radius + 1, 0, width - 1)] - buffer[num2 + Mathf.Clamp(l - radius, 0, width - 1)];
				}
			}
			for (int m = 0; m < width; m++)
			{
				float num4 = 0f;
				for (int n = -radius; n <= radius; n++)
				{
					num4 += array[Mathf.Clamp(n, 0, height - 1) * width + m];
				}
				for (int num5 = 0; num5 < height; num5++)
				{
					buffer[num5 * width + m] = num4 / (float)num;
					num4 += array[Mathf.Clamp(num5 + radius + 1, 0, height - 1) * width + m] - array[Mathf.Clamp(num5 - radius, 0, height - 1) * width + m];
				}
			}
		}
	}
}
