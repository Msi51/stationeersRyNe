using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Util;
using CharacterCustomisation.Clothing;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using Util;

namespace CharacterCustomisation;

public sealed class PlayerCosmeticsBehaviour : MonoBehaviour
{
	public class FacialExpressionData : DataCollection
	{
		[XmlAttribute("Happy")]
		public float Happy;

		[XmlAttribute("Angry")]
		public float Angry;

		[XmlAttribute("Surprised")]
		public float Surprised;

		[XmlAttribute("Open")]
		public float Open;

		[XmlAttribute("None")]
		public float None;

		[XmlAttribute("Dead")]
		public float Dead;

		public override void Initialize(ModAbout mod)
		{
			DataCollection.Register(this, mod);
			FacialExpressions.TryAdd(Id, this);
		}
	}

	public static class FacialExpressions
	{
		public static List<string> ExpressionNames = new List<string>();

		public static Dictionary<int, FacialExpressionData> Expressions { get; } = new Dictionary<int, FacialExpressionData>();

		public static void TryAdd(string id, FacialExpressionData expression)
		{
			Expressions.TryAdd(Animator.StringToHash(id.ToLower()), expression);
			ExpressionNames.Add(id.ToLower());
		}

		public static bool TryGet(int id, out FacialExpressionData expression)
		{
			return Expressions.TryGetValue(id, out expression);
		}
	}

	private readonly struct RendererPair(Renderer renderer, int index = 0)
	{
		public readonly Renderer renderer = renderer;

		public readonly int index = index;
	}

	[SerializeField]
	private Human _human;

	public Action<PlayerCosmetics> OnLocalChanged;

	[Tooltip("Root bone for animations")]
	[SerializeField]
	private Transform _rootBone;

	[SerializeField]
	private Transform _helmetSlot;

	[SerializeField]
	private SkinnedMeshRenderer _headRenderer;

	[SerializeField]
	private SkinnedMeshRenderer _headRendererShadow;

	[SerializeField]
	private SkinnedMeshRenderer _bodyRenderer;

	[SerializeField]
	private SkinnedMeshRenderer _facialHairRenderer;

	[SerializeField]
	private SkinnedMeshRenderer[] _clothingRenderers;

	[SerializeField]
	[ReadOnly]
	private GameObject _eyes;

	[SerializeField]
	[ReadOnly]
	private HairBehaviour _hair;

	[SerializeField]
	[ReadOnly]
	private Transform[] _bones;

	[Header("Options")]
	[Tooltip("Starting layer int")]
	[SerializeField]
	[Range(0f, 31f)]
	private int _layerInt;

	[SerializeField]
	[ReadOnly]
	private string _layerName;

	[SerializeField]
	private HairMode _hairMode;

	[Tooltip("Will load the saved XML file on start")]
	[SerializeField]
	private bool _loadCosmeticsOnStart = true;

	[Tooltip("Subscribe to onCosmeticsChanged event. No needed for character customisation scene")]
	[SerializeField]
	private bool _subToEvent = true;

	[SerializeField]
	private bool _localHumanOnly;

	[Tooltip("Speed the blink animation will happen")]
	[SerializeField]
	private float _blinkSpeed = 7.5f;

	[Tooltip("Speed in which the expressions are changed")]
	[SerializeField]
	private float _expressionSpeed = 5f;

	[Tooltip("The time in seconds that to transition between expressions")]
	[SerializeField]
	private float expressionTransitionTime = 0.2f;

	[FormerlySerializedAs("_fallbackKit")]
	[Tooltip("The character kit to use when no kit has been saved yet")]
	[SerializeField]
	private CharacterKit _fallbackKitHumanMale;

	[SerializeField]
	private CharacterKit _fallbackKitHumanFemale;

	[SerializeField]
	private CharacterKit _fallbackKitZrillianMale;

	[SerializeField]
	private CharacterKit _fallbackKitZrillianFemale;

	[SerializeField]
	private CharacterKit _fallbackKitRobot;

	[SerializeField]
	private CharacterKit[] _characterKits;

	private readonly Queue<GameObject> _cacheObjects = new Queue<GameObject>();

	private readonly Tween[] _tweenEyes = new Tween[2];

	private Dictionary<string, Transform> _boneMap;

	private Material _skinMaterial;

	private SkinnedMeshRenderer _nakedBodySkin;

	private KitItem _currentClothing;

	private KitItem _currentArmour;

	private CancellationTokenSource _humanEyeAnimationCancel;

	private int _blinkIndex = -1;

	private static readonly string BlinkString = EnumCollections.BlendShapeTypes.GetName(BlendShapeType.Blink);

	public SkinnedMeshRenderer BodyRenderer => _bodyRenderer;

	public SkinnedMeshRenderer ArmorRenderer => _clothingRenderers[1];

	private void OnValidate()
	{
		_layerName = LayerMask.LayerToName(_layerInt);
		SetHairMode(_hairMode);
	}

	private void Start()
	{
		if (_loadCosmeticsOnStart)
		{
			UpdateIdentity(Singleton<GameManager>.Instance.CustomCosmeticsSlot);
		}
		if (_human != null)
		{
			_human.OnEntityUnconciousEvent += OnHumanUnconscious;
			_human.OnEntityConsciousEvent += OnHumanConscious;
		}
		StartEyeBlinkAnimationLoop();
		EyeLookAnimationLoop().Forget();
	}

	private void StartEyeBlinkAnimationLoop()
	{
		_humanEyeAnimationCancel = new CancellationTokenSource();
		EyeBlinkAnimationLoop(_humanEyeAnimationCancel.Token).Forget();
	}

	public void SetFacialExpression(string expressionId)
	{
		Animator.StringToHash(expressionId.ToLower());
	}

	public void SetFacialExpression(FacialExpressionData expression)
	{
		SetExpression(BlendShapeType.Happy, tween: false, 0, expression.Happy);
		SetExpression(BlendShapeType.Angry, tween: false, 0, expression.Angry);
		SetExpression(BlendShapeType.Surprised, tween: false, 0, expression.Surprised);
		SetExpression(BlendShapeType.Open, tween: false, 0, expression.Open);
		SetExpression(BlendShapeType.None, tween: false, 0, expression.None);
		SetExpression(BlendShapeType.Dead, tween: false, 0, expression.Dead);
		BlendShapeNuancedMessage blendShapeNuancedMessage = new BlendShapeNuancedMessage
		{
			HumanNetId = _human.NetworkId,
			Happy = expression.Happy,
			Angry = expression.Angry,
			Surprised = expression.Surprised,
			Open = expression.Open,
			None = expression.None,
			Dead = expression.Dead,
			Duration = 0
		};
		if (NetworkManager.IsClient)
		{
			blendShapeNuancedMessage.SendToServer();
		}
		else if (NetworkManager.IsServer)
		{
			blendShapeNuancedMessage.SendToClients();
		}
	}

	public void OnHumanUnconscious()
	{
		try
		{
			_humanEyeAnimationCancel?.Cancel();
			_humanEyeAnimationCancel?.Dispose();
			if ((object)_headRenderer != null && _blinkIndex > -1)
			{
				SetBlendShape(_headRenderer, _blinkIndex, 100f);
			}
		}
		catch
		{
		}
	}

	public void OnHumanConscious()
	{
		try
		{
			_humanEyeAnimationCancel?.Cancel();
			_humanEyeAnimationCancel?.Dispose();
			if ((object)_headRenderer != null && _blinkIndex > -1)
			{
				SetBlendShape(_headRenderer, _blinkIndex, 100f);
			}
		}
		catch
		{
		}
		StartEyeBlinkAnimationLoop();
	}

	private void Awake()
	{
		if (_subToEvent)
		{
			PlayerCosmetics.onCosmeticsChanged += OnCosmeticsChanged;
		}
	}

	private void OnDestroy()
	{
		UnregisterEvents();
	}

	public void UnregisterEvents()
	{
		_subToEvent = false;
		PlayerCosmetics.onCosmeticsChanged -= OnCosmeticsChanged;
		StopAllCoroutines();
	}

	private void OnCosmeticsChanged(PlayerCosmetics playerCosmetics)
	{
		if (!_localHumanOnly || (_localHumanOnly && _human.IsLocalPlayer))
		{
			UpdateIdentity(playerCosmetics);
			OnLocalChanged?.Invoke(playerCosmetics);
		}
	}

	public void RefreshSetGameObjectLayer()
	{
		SetGameObjectLayer();
	}

	public void SetGameObjectsAsDefault()
	{
		int layer = LayerMask.NameToLayer("Default");
		_bodyRenderer.gameObject.layer = layer;
		_headRenderer.gameObject.layer = layer;
		_headRendererShadow.gameObject.layer = layer;
		_facialHairRenderer.gameObject.layer = layer;
		SkinnedMeshRenderer[] clothingRenderers = _clothingRenderers;
		for (int i = 0; i < clothingRenderers.Length; i++)
		{
			clothingRenderers[i].gameObject.layer = layer;
		}
		foreach (GameObject cacheObject in _cacheObjects)
		{
			cacheObject.SetLayerRecursive(layer);
		}
	}

	private void SetGameObjectLayer(int layerInt = 0)
	{
		if (_layerInt == (int)Layers.CharacterCreation)
		{
			return;
		}
		if (!_human || !_human.IsLocalPlayer)
		{
			SetGameObjectsAsDefault();
			return;
		}
		_layerInt = layerInt;
		_bodyRenderer.gameObject.layer = Layers.Player;
		_headRenderer.gameObject.layer = Layers.PlayerInvisible;
		_headRendererShadow.gameObject.layer = Layers.Player;
		_facialHairRenderer.gameObject.layer = Layers.PlayerInvisible;
		SkinnedMeshRenderer[] clothingRenderers = _clothingRenderers;
		for (int i = 0; i < clothingRenderers.Length; i++)
		{
			clothingRenderers[i].gameObject.layer = Layers.Player;
		}
		foreach (GameObject cacheObject in _cacheObjects)
		{
			cacheObject.SetLayerRecursive(Layers.PlayerInvisible);
		}
	}

	public void SetHairMode(HairMode mode)
	{
		_hairMode = mode;
		if ((bool)_hair)
		{
			_hair.Set(mode);
		}
	}

	public PlayerCosmetics UpdateIdentity(int slot)
	{
		PlayerCosmetics playerCosmetics = PlayerCosmetics.Load(slot);
		UpdateIdentity(playerCosmetics);
		return playerCosmetics;
	}

	public void UpdateIdentity(PlayerCosmetics savedKit)
	{
		if (!GameManager.IsBatchMode)
		{
			CharacterKit characterKit = savedKit?.FindIn(_characterKits);
			if ((object)characterKit == null)
			{
				characterKit = savedKit?.SpeciesClass switch
				{
					SpeciesClass.Human => (savedKit.Gender == Gender.Male) ? _fallbackKitHumanMale : _fallbackKitHumanFemale, 
					SpeciesClass.Zrilian => (savedKit.Gender == Gender.Male) ? _fallbackKitZrillianMale : _fallbackKitZrillianFemale, 
					SpeciesClass.Robot => _fallbackKitRobot, 
					_ => _fallbackKitHumanMale, 
				};
			}
			if ((bool)characterKit)
			{
				UpdateCosmetics(characterKit, savedKit?.MetaData ?? KitMetaData.DefaultKit);
			}
		}
	}

	public void UpdateCosmetics(CharacterKit kit, KitMetaData meta)
	{
		if (!meta.IsValid())
		{
			meta = KitMetaData.DefaultKit;
		}
		Clear();
		_nakedBodySkin = kit.Bodies.GetItem(meta.Body)?.SkinnedMeshRenderer;
		CopyRenderer(_nakedBodySkin, _bodyRenderer);
		CopyRenderer(kit.Heads.GetItem(meta.Head)?.SkinnedMeshRenderer, _headRenderer);
		CopyRenderer(kit.Heads.GetItem(meta.Head)?.SkinnedMeshRenderer, _headRendererShadow);
		CopyRenderer(kit.FacialHairs.GetItem(meta.FacialHair, allowNullReturn: true)?.SkinnedMeshRenderer, _facialHairRenderer);
		_eyes = ApplyObj<GameObject>(kit.Eyes.GetItem(meta.Eyes), _helmetSlot);
		_hair = ApplyObj<HairBehaviour>(kit.Hairs.GetItem(meta.Hair, allowNullReturn: true), _helmetSlot);
		GameObject go = (_hair ? _hair.gameObject : null);
		_skinMaterial = kit.SkinColours.GetItem(meta.SkinColour)?.Item as Material;
		ApplyMaterial(_skinMaterial, new RendererPair(_headRenderer), new RendererPair(_bodyRenderer));
		ApplyMaterial(kit.EyeColours.GetItem(meta.EyeColour)?.Item as Material, GetRenderPairsFromGo(_eyes, 0));
		ApplyMaterial(kit.HairColours.GetItem(meta.HairColours[0])?.Item as Material, GetRenderPairsFromGo(go, 0, new RendererPair(_headRenderer, 1)));
		ApplyMaterial(kit.HairColours.GetItem(meta.HairColours[1])?.Item as Material, new RendererPair(_facialHairRenderer));
		SetHairMode(_hairMode);
		if ((bool)_currentClothing)
		{
			ApplyClothing(_currentClothing);
		}
		if ((bool)_currentArmour)
		{
			ApplyArmor(_currentArmour);
		}
		SetGameObjectLayer();
		if (_human?.SuitSlot?.Occupant is ISuit suit)
		{
			suit.RefreshSkinnedMeshCustomColor();
		}
	}

	public void ApplyClothing(KitItem kitItem)
	{
		_currentClothing = kitItem;
		ApplyClothingOrArmor(kitItem, BodyRenderer, _nakedBodySkin);
	}

	public void ApplyArmor(KitItem kitItem)
	{
		_currentArmour = kitItem;
		ApplyClothingOrArmor(kitItem, ArmorRenderer);
	}

	private void ApplyClothingOrArmor(KitItem kitItem, SkinnedMeshRenderer renderer, SkinnedMeshRenderer fallbackSkin = null)
	{
		SkinnedMeshRenderer skinnedMeshRenderer = (kitItem ? kitItem.SkinnedMeshRenderer : fallbackSkin);
		CopyRenderer(skinnedMeshRenderer, renderer);
		if (skinnedMeshRenderer == fallbackSkin)
		{
			ApplyMaterial(_skinMaterial, new RendererPair(renderer));
		}
		else if (kitItem is ClothingItem { SkinMaterialIndex: >-1 } clothingItem)
		{
			ApplyMaterial(_skinMaterial, new RendererPair(renderer, clothingItem.SkinMaterialIndex));
		}
	}

	private T ApplyObj<T>(KitItem kitItem, Transform parent) where T : UnityEngine.Object
	{
		if (!kitItem)
		{
			return null;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(kitItem.GetPrefabGameObject(), parent);
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.transform.localRotation = Quaternion.Euler(Vector3.zero);
		gameObject.SetLayerRecursive(_layerInt);
		_cacheObjects.Enqueue(gameObject);
		if (!(gameObject is T result))
		{
			return gameObject.GetComponent<T>();
		}
		return result;
	}

	private void CopyRenderer(SkinnedMeshRenderer origin, SkinnedMeshRenderer target)
	{
		if (target == null)
		{
			return;
		}
		target.sharedMesh = (origin ? origin.sharedMesh : null);
		target.sharedMaterials = (origin ? origin.sharedMaterials : Array.Empty<Material>());
		List<Transform> list = new List<Transform>();
		Transform[] obj = (origin ? origin.bones : Array.Empty<Transform>());
		if (_boneMap == null)
		{
			_boneMap = _bones.ToDictionary((Transform b) => b.name);
		}
		Transform[] array = obj;
		foreach (Transform transform in array)
		{
			if (_boneMap.TryGetValue(transform.name, out var value))
			{
				list.Add(value);
			}
		}
		if (target != null)
		{
			target.bones = list.ToArray();
		}
	}

	private static void ApplyMaterial(Material mat, params RendererPair[] renderPairs)
	{
		if (!mat)
		{
			return;
		}
		for (int i = 0; i < renderPairs.Length; i++)
		{
			RendererPair rendererPair = renderPairs[i];
			if ((bool)rendererPair.renderer)
			{
				Material[] sharedMaterials = rendererPair.renderer.sharedMaterials;
				if (rendererPair.index < sharedMaterials.Length)
				{
					sharedMaterials[rendererPair.index] = mat;
				}
				rendererPair.renderer.sharedMaterials = sharedMaterials;
			}
		}
	}

	private void Clear()
	{
		Tween[] tweenEyes = _tweenEyes;
		for (int i = 0; i < tweenEyes.Length; i++)
		{
			tweenEyes[i]?.Kill();
		}
		while (_cacheObjects.Count > 0)
		{
			_cacheObjects.Dequeue().DestroyGameObject(this);
		}
	}

	private static RendererPair[] GetRenderPairsFromGo(GameObject go, int index = 0, params RendererPair[] additions)
	{
		return (go ? go.GetComponentsInChildren<Renderer>(includeInactive: true) : Array.Empty<Renderer>()).Select((Renderer x) => new RendererPair(x, index)).Union(additions).ToArray();
	}

	private async UniTaskVoid EyeLookAnimationLoop()
	{
		Vector3 angle = default(Vector3);
		bool isNeutral = true;
		while ((bool)_human && _human.State == EntityState.Alive)
		{
			if (!_eyes)
			{
				await UniTask.Yield();
				continue;
			}
			if (_eyes.transform.childCount != 2)
			{
				break;
			}
			Transform child = _eyes.transform.GetChild(0);
			Transform child2 = _eyes.transform.GetChild(1);
			angle.x = (isNeutral ? 0f : UnityEngine.Random.Range(-20f, 20f));
			angle.y = (isNeutral ? 0f : UnityEngine.Random.Range(-30f, 30f));
			_tweenEyes[0] = child.DOLocalRotate(angle, 0.1f);
			_tweenEyes[1] = child2.DOLocalRotate(angle, 0.1f);
			await UniTask.Delay(Mathf.RoundToInt((isNeutral ? UnityEngine.Random.Range(2f, 5f) : UnityEngine.Random.Range(0.5f, 1f)) * 1000f));
			isNeutral = !isNeutral;
		}
	}

	private async UniTaskVoid EyeBlinkAnimationLoop(CancellationToken cancellationToken)
	{
		_blinkIndex = GetBlendShapeIndex(_headRenderer, BlinkString);
		while ((bool)_human && _human.State == EntityState.Alive)
		{
			SetBlendShape(_headRenderer, _blinkIndex, 0f);
			if (!_headRenderer)
			{
				await UniTask.NextFrame(cancellationToken);
				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}
				continue;
			}
			if (cancellationToken.IsCancellationRequested)
			{
				SetBlendShape(_headRenderer, _blinkIndex, 100f);
				return;
			}
			if (_blinkIndex == -1)
			{
				break;
			}
			if (_human.ParentSlot?.Parent is Bed)
			{
				SetBlendShape(_headRenderer, _blinkIndex, 100f);
				while (_human.ParentSlot?.Parent is Bed)
				{
					await UniTask.NextFrame(cancellationToken);
				}
				continue;
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}
			await UniTask.Delay(UnityEngine.Random.Range(5000, 12000), ignoreTimeScale: false, PlayerLoopTiming.Update, cancellationToken);
			float t = 0f;
			if (cancellationToken.IsCancellationRequested)
			{
				SetBlendShape(_headRenderer, _blinkIndex, 100f);
				return;
			}
			while (t < 1f)
			{
				SetBlendShape(_headRenderer, _blinkIndex, t * 100f);
				t += Time.deltaTime * _blinkSpeed;
				await UniTask.NextFrame(cancellationToken);
				if (cancellationToken.IsCancellationRequested)
				{
					SetBlendShape(_headRenderer, _blinkIndex, 100f);
					return;
				}
			}
			if (cancellationToken.IsCancellationRequested)
			{
				SetBlendShape(_headRenderer, _blinkIndex, 100f);
				return;
			}
			await UniTask.Delay(100, ignoreTimeScale: false, PlayerLoopTiming.Update, cancellationToken);
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}
			while (t > 0f)
			{
				SetBlendShape(_headRenderer, _blinkIndex, t * 100f);
				t -= Time.deltaTime * _blinkSpeed;
				await UniTask.NextFrame(cancellationToken);
				if (cancellationToken.IsCancellationRequested)
				{
					SetBlendShape(_headRenderer, _blinkIndex, 100f);
					return;
				}
			}
			if (cancellationToken.IsCancellationRequested)
			{
				SetBlendShape(_headRenderer, _blinkIndex, 100f);
				return;
			}
		}
		SetBlendShape(_headRenderer, _blinkIndex, 100f);
	}

	public void SetExpression(BlendShapeType blendShapeType, bool tween)
	{
		if (tween && Application.isPlaying && base.gameObject.activeInHierarchy)
		{
			SetExpressionTween(blendShapeType).Forget();
		}
		else
		{
			SetExpressionImmediate(blendShapeType);
		}
	}

	public void SetExpression(BlendShapeType blendShapeType, bool tween, int duration = 5000)
	{
		if (tween && Application.isPlaying && base.gameObject.activeInHierarchy)
		{
			SetExpressionTween(blendShapeType, duration).Forget();
		}
		else
		{
			SetExpressionImmediate(blendShapeType, duration);
		}
	}

	public void SetExpression(BlendShapeType blendShapeType, bool tween, int duration, float intensity)
	{
		if (tween && Application.isPlaying && base.gameObject.activeInHierarchy)
		{
			SetExpressionTween(blendShapeType, duration, intensity).Forget();
		}
		else
		{
			SetExpressionImmediate(blendShapeType, duration, intensity).Forget();
		}
	}

	private async UniTaskVoid SetExpressionImmediate(BlendShapeType blendShapeType, int duration = 5000)
	{
		string text = EnumCollections.BlendShapeTypes.GetName(blendShapeType);
		int blendShapeIndex = GetBlendShapeIndex(_headRenderer, text);
		int blendShapeIndex2 = GetBlendShapeIndex(_facialHairRenderer, text);
		string[] blendExceptions = new string[2] { BlinkString, text };
		RevertAllBlendShapes(_headRenderer, 0f, blendExceptions);
		RevertAllBlendShapes(_facialHairRenderer, 0f, blendExceptions);
		SetBlendShape(_headRenderer, blendShapeIndex, 100f);
		SetBlendShape(_facialHairRenderer, blendShapeIndex2, 100f);
		if (duration != 0)
		{
			await UniTask.Delay(duration);
			RevertAllBlendShapes(_headRenderer, 0f, blendExceptions);
			RevertAllBlendShapes(_facialHairRenderer, 0f, blendExceptions);
		}
	}

	private async UniTaskVoid SetExpressionImmediate(BlendShapeType blendShapeType, int duration, float intensity)
	{
		intensity = Mathf.Clamp(intensity, 0f, 1f);
		string blendShapeName = EnumCollections.BlendShapeTypes.GetName(blendShapeType);
		int headIndex = GetBlendShapeIndex(_headRenderer, blendShapeName);
		int facialIndex = GetBlendShapeIndex(_facialHairRenderer, blendShapeName);
		SetBlendShape(_headRenderer, headIndex, 100f * intensity);
		SetBlendShape(_facialHairRenderer, facialIndex, 100f * intensity);
		if (duration != 0)
		{
			await UniTask.Delay(duration);
			SetBlendShape(_headRenderer, headIndex, 0f);
			SetBlendShape(_facialHairRenderer, facialIndex, 0f);
		}
	}

	private async UniTaskVoid SetExpressionTween(BlendShapeType blendShapeType, int duration = 5000)
	{
		if (blendShapeType == BlendShapeType.Blink)
		{
			Debug.LogWarning("Blink blend shape not supported. Blinking is handled internally", this);
			return;
		}
		float t = 0f;
		string text = EnumCollections.BlendShapeTypes.GetName(blendShapeType);
		int headIndex = GetBlendShapeIndex(_headRenderer, text);
		int facialIndex = GetBlendShapeIndex(_facialHairRenderer, text);
		string[] blendExceptions = new string[2] { BlinkString, text };
		while (t < 1f)
		{
			RevertAllBlendShapes(_headRenderer, (1f - t) * 100f, blendExceptions);
			RevertAllBlendShapes(_facialHairRenderer, (1f - t) * 100f, blendExceptions);
			if (blendShapeType > BlendShapeType.None)
			{
				SetBlendShape(_headRenderer, headIndex, t * 100f);
				SetBlendShape(_facialHairRenderer, facialIndex, t * 100f);
			}
			t += Time.deltaTime * _expressionSpeed;
			await UniTask.NextFrame();
		}
		SetExpressionImmediate(blendShapeType, duration);
		if (duration == 0)
		{
			return;
		}
		await UniTask.Delay(duration);
		t = 0f;
		while (t < 1f)
		{
			RevertAllBlendShapes(_headRenderer, (1f - t) * 100f, blendExceptions);
			RevertAllBlendShapes(_facialHairRenderer, (1f - t) * 100f, blendExceptions);
			if (blendShapeType > BlendShapeType.None)
			{
				SetBlendShape(_headRenderer, headIndex, 100f - t * 100f);
				SetBlendShape(_facialHairRenderer, facialIndex, 100f - t * 100f);
			}
			t += Time.deltaTime * _expressionSpeed;
			await UniTask.NextFrame();
		}
	}

	private async UniTaskVoid SetExpressionTween(BlendShapeType blendShapeType, int duration, float intensity)
	{
		intensity = Mathf.Clamp(intensity, 0f, 1f);
		if (blendShapeType == BlendShapeType.Blink)
		{
			Debug.LogWarning("Blink blend shape not supported. Blinking is handled internally", this);
			return;
		}
		string blendShapeName = EnumCollections.BlendShapeTypes.GetName(blendShapeType);
		int headIndex = GetBlendShapeIndex(_headRenderer, blendShapeName);
		int facialIndex = GetBlendShapeIndex(_facialHairRenderer, blendShapeName);
		float normalizedBlendshapeWeight = _headRenderer.GetBlendShapeWeight(headIndex) / 100f;
		float initialValue = normalizedBlendshapeWeight;
		float targetValue = intensity;
		float time = 0f;
		while (time < expressionTransitionTime)
		{
			time += Time.deltaTime;
			if (blendShapeType > BlendShapeType.None)
			{
				float value = normalizedBlendshapeWeight * 100f;
				SetBlendShape(_headRenderer, headIndex, value);
				SetBlendShape(_facialHairRenderer, facialIndex, value);
			}
			float t = Mathf.Clamp01(time / expressionTransitionTime);
			normalizedBlendshapeWeight = Mathf.Lerp(initialValue, targetValue, t);
			await UniTask.NextFrame();
		}
		if (duration == 0)
		{
			return;
		}
		await UniTask.Delay(duration);
		time = 0f;
		while (time < expressionTransitionTime)
		{
			time += Time.deltaTime;
			if (blendShapeType > BlendShapeType.None)
			{
				float value2 = normalizedBlendshapeWeight * 100f;
				SetBlendShape(_headRenderer, headIndex, value2);
				SetBlendShape(_facialHairRenderer, facialIndex, value2);
			}
			float t2 = Mathf.Clamp01(time / expressionTransitionTime);
			normalizedBlendshapeWeight = Mathf.Lerp(targetValue, 0f, t2);
			await UniTask.NextFrame();
		}
		SetBlendShape(_headRenderer, headIndex, 0f);
		SetBlendShape(_facialHairRenderer, facialIndex, 0f);
	}

	private static void RevertAllBlendShapes(SkinnedMeshRenderer skinnedMeshRenderer, float value, params string[] exceptions)
	{
		if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null)
		{
			return;
		}
		for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
		{
			string blendShapeName = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i);
			if (!exceptions.Any((string x) => blendShapeName.Contains(x.ToLower())) && skinnedMeshRenderer.GetBlendShapeWeight(i) > value)
			{
				SetBlendShape(skinnedMeshRenderer, i, value);
			}
		}
	}

	private static void SetBlendShape(SkinnedMeshRenderer skinnedMeshRenderer, int blendIndex, float value)
	{
		try
		{
			if (!(skinnedMeshRenderer == null) && !(skinnedMeshRenderer.sharedMesh == null) && blendIndex >= 0 && blendIndex < skinnedMeshRenderer.sharedMesh.blendShapeCount)
			{
				skinnedMeshRenderer.SetBlendShapeWeight(blendIndex, value);
			}
		}
		catch (Exception)
		{
		}
	}

	private static int GetBlendShapeIndex(SkinnedMeshRenderer skinnedMeshRenderer, string blendShapeName)
	{
		if (skinnedMeshRenderer?.sharedMesh == null)
		{
			return -1;
		}
		for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
		{
			if (skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i).ToLower().Contains(blendShapeName.ToLower()))
			{
				return i;
			}
		}
		return -1;
	}

	[ContextMenu("Get Bones")]
	private void GetBones()
	{
		_bones = _rootBone.GetComponentsInChildren<Transform>(includeInactive: true);
	}

	public void ResetExpressions()
	{
		SetExpression(BlendShapeType.None, tween: false, 0, 0f);
		SetExpression(BlendShapeType.Blink, tween: false, 0, 0f);
		SetExpression(BlendShapeType.Happy, tween: false, 0, 0f);
		SetExpression(BlendShapeType.Dead, tween: false, 0, 0f);
		SetExpression(BlendShapeType.Angry, tween: false, 0, 0f);
		SetExpression(BlendShapeType.Open, tween: false, 0, 0f);
		SetExpression(BlendShapeType.Surprised, tween: false, 0, 0f);
	}
}
