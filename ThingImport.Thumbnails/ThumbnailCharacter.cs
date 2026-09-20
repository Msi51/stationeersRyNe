using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Util;
using CharacterCustomisation;
using UnityEngine;

namespace ThingImport.Thumbnails;

public sealed class ThumbnailCharacter
{
	private GameObject _staging;

	private PlayerCosmeticsBehaviour _avatar;

	private Transform _avatarTransform;

	private readonly List<CharacterKit> _kits = new List<CharacterKit>();

	public readonly List<string> BodyClothingOptions = new List<string>();

	public readonly List<string> ArmorOptions = new List<string>();

	private IReadOnlyList<Thing> _prefabs;

	private static readonly BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;

	private float _turntableDistance;

	private Quaternion _turntableFacing = Quaternion.identity;

	public bool Available => _avatar != null;

	public IReadOnlyList<CharacterKit> Kits => _kits;

	public bool TryCreate()
	{
		if (_avatar != null)
		{
			return true;
		}
		PlayerCosmeticsBehaviour playerCosmeticsBehaviour = UnityEngine.Object.FindObjectOfType<PlayerCosmeticsBehaviour>(includeInactive: true);
		if (playerCosmeticsBehaviour == null)
		{
			return false;
		}
		_staging = new GameObject("~ThumbnailStudioCharacter");
		_staging.SetActive(value: false);
		_staging.transform.position = new Vector3(0f, -500f, 0f);
		GameObject gameObject = UnityEngine.Object.Instantiate(playerCosmeticsBehaviour.gameObject, _staging.transform);
		_avatar = gameObject.GetComponent<PlayerCosmeticsBehaviour>();
		if (_avatar == null)
		{
			Destroy();
			return false;
		}
		_avatarTransform = gameObject.transform;
		StripNonCosmeticScripts(gameObject);
		ClearHelmetSlotChildren();
		SetPrivate("_loadCosmeticsOnStart", false);
		SetPrivate("_subToEvent", false);
		SetPrivate("_human", null);
		GatherKits(playerCosmeticsBehaviour);
		return true;
	}

	private static void StripNonCosmeticScripts(GameObject clone)
	{
		for (int i = 0; i < 8; i++)
		{
			MonoBehaviour[] componentsInChildren = clone.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
			bool flag = false;
			MonoBehaviour[] array = componentsInChildren;
			foreach (MonoBehaviour monoBehaviour in array)
			{
				if (!(monoBehaviour == null) && !(monoBehaviour is PlayerCosmeticsBehaviour) && !(monoBehaviour is HairBehaviour))
				{
					try
					{
						UnityEngine.Object.DestroyImmediate(monoBehaviour);
						flag = true;
					}
					catch
					{
					}
				}
			}
			if (!flag)
			{
				break;
			}
		}
	}

	private void ClearHelmetSlotChildren()
	{
		Transform transform = GetPrivate<Transform>("_helmetSlot");
		if (!(transform == null))
		{
			for (int num = transform.childCount - 1; num >= 0; num--)
			{
				UnityEngine.Object.DestroyImmediate(transform.GetChild(num).gameObject);
			}
		}
	}

	private void SetPrivate(string field, object value)
	{
		FieldInfo field2 = typeof(PlayerCosmeticsBehaviour).GetField(field, Instance);
		if (field2 != null && _avatar != null)
		{
			field2.SetValue(_avatar, value);
		}
	}

	private T GetPrivate<T>(string field) where T : class
	{
		return typeof(PlayerCosmeticsBehaviour).GetField(field, Instance)?.GetValue(_avatar) as T;
	}

	private void GatherKits(PlayerCosmeticsBehaviour source)
	{
		_kits.Clear();
		CharacterKit[] privateFrom = GetPrivateFrom<CharacterKit[]>(source, "_characterKits");
		if (privateFrom != null)
		{
			_kits.AddRange(privateFrom.Where((CharacterKit k) => k != null));
		}
		string[] array = new string[5] { "_fallbackKitHumanMale", "_fallbackKitHumanFemale", "_fallbackKitZrillianMale", "_fallbackKitZrillianFemale", "_fallbackKitRobot" };
		foreach (string field in array)
		{
			CharacterKit privateFrom2 = GetPrivateFrom<CharacterKit>(source, field);
			if (privateFrom2 != null && !_kits.Contains(privateFrom2))
			{
				_kits.Add(privateFrom2);
			}
		}
	}

	private static T GetPrivateFrom<T>(PlayerCosmeticsBehaviour source, string field) where T : class
	{
		return typeof(PlayerCosmeticsBehaviour).GetField(field, Instance)?.GetValue(source) as T;
	}

	public void RefreshClothingOptions(IReadOnlyList<Thing> prefabs)
	{
		_prefabs = prefabs;
		BodyClothingOptions.Clear();
		ArmorOptions.Clear();
		if (prefabs == null)
		{
			return;
		}
		foreach (Thing prefab in prefabs)
		{
			if (!(prefab == null))
			{
				if (prefab is IBodyArmor && prefab is Clothing clothing && (clothing._clothingMale != null || clothing._clothingFemale != null))
				{
					ArmorOptions.Add(prefab.PrefabName);
				}
				else if (prefab is IFullBody && GetSuitAsset(prefab) != null)
				{
					BodyClothingOptions.Add(prefab.PrefabName);
				}
				else if (prefab is Clothing clothing2 && !(prefab is IBodyArmor) && (clothing2._clothingMale != null || clothing2._clothingFemale != null))
				{
					BodyClothingOptions.Add(prefab.PrefabName);
				}
			}
		}
		BodyClothingOptions.Sort(StringComparer.OrdinalIgnoreCase);
		ArmorOptions.Sort(StringComparer.OrdinalIgnoreCase);
	}

	private static KitItem GetSuitAsset(object suit)
	{
		Type type = suit.GetType();
		while (type != null)
		{
			FieldInfo field = type.GetField("_suitAsset", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field != null)
			{
				return field.GetValue(suit) as KitItem;
			}
			type = type.BaseType;
		}
		return null;
	}

	public bool IsPaintable(string prefabName)
	{
		Thing thing = ResolvePrefab(prefabName);
		if (thing != null)
		{
			return thing.IsPaintable;
		}
		return false;
	}

	private Thing ResolvePrefab(string prefabName)
	{
		if (string.IsNullOrEmpty(prefabName) || _prefabs == null)
		{
			return null;
		}
		foreach (Thing prefab in _prefabs)
		{
			if (prefab != null && prefab.PrefabName == prefabName)
			{
				return prefab;
			}
		}
		return null;
	}

	public CharacterKit KitFor(CharacterConfig config)
	{
		if (_kits.Count != 0)
		{
			return _kits[Mathf.Clamp(config.KitIndex, 0, _kits.Count - 1)];
		}
		return null;
	}

	public bool IsCurrentFemale(CharacterConfig config)
	{
		return IsFemaleKit(KitFor(config));
	}

	private static bool IsFemaleKit(CharacterKit kit)
	{
		if (kit == null)
		{
			return false;
		}
		if (kit.name.IndexOf("female", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		if (kit.name.IndexOf("male", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return false;
		}
		KitItem kitItem = ((kit.Bodies != null && kit.Bodies.Length != 0) ? kit.Bodies[0] : null);
		if (kitItem != null)
		{
			return kitItem.name.IndexOf("female", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return false;
	}

	public void Drive(CharacterConfig config)
	{
		if (!(_avatar == null))
		{
			CharacterKit characterKit = KitFor(config);
			if (!(characterKit == null))
			{
				ClearHelmetSlotChildren();
				_avatar.UpdateCosmetics(characterKit, BuildMeta(characterKit, config));
				bool female = IsFemaleKit(characterKit);
				ApplyLayer(ResolvePrefab(config.BodyClothing), config.BodyColor, isArmor: false, female);
				ApplyLayer(ResolvePrefab(config.ArmorClothing), config.ArmorColor, isArmor: true, female);
				_avatar.SetHairMode(config.HairMode);
				_avatar.SetExpression(config.Expression, tween: false, 0);
				_avatarTransform.gameObject.SetLayerRecursive(Layers.ThumbnailCreation);
			}
		}
	}

	private void ApplyLayer(Thing prefab, int colorIndex, bool isArmor, bool female)
	{
		KitItem kitItem = WearableItemFor(prefab, female);
		if (isArmor)
		{
			_avatar.ApplyArmor(kitItem);
		}
		else
		{
			_avatar.ApplyClothing(kitItem);
		}
		if (!(kitItem == null) && colorIndex >= 0 && !(prefab == null) && prefab.IsPaintable)
		{
			SkinnedMeshRenderer renderer = (isArmor ? _avatar.ArmorRenderer : _avatar.BodyRenderer);
			var (paintIndex, mask) = PaintSettingsFor(prefab);
			PaintRenderer(renderer, colorIndex, paintIndex, mask);
		}
	}

	private static KitItem WearableItemFor(Thing prefab, bool female)
	{
		if ((object)prefab != null)
		{
			if (!(prefab is IFullBody))
			{
				if (prefab is Clothing clothing)
				{
					object obj = (female ? clothing._clothingFemale : clothing._clothingMale);
					if (obj == null)
					{
						if (!female)
						{
							return clothing._clothingFemale;
						}
						obj = clothing._clothingMale;
					}
					return (KitItem)obj;
				}
				return null;
			}
			return GetSuitAsset(prefab);
		}
		return null;
	}

	private static (int index, bool mask) PaintSettingsFor(Thing prefab)
	{
		if (!(prefab is PaintableOveralls paintableOveralls))
		{
			if (!(prefab is Suit suit))
			{
				if (!(prefab is SuitBase suitBase))
				{
					if (prefab is BodyArmor bodyArmor)
					{
						return (index: bodyArmor.ColorMaterialIndex, mask: false);
					}
					return (index: 1, mask: false);
				}
				return (index: suitBase.ColorMaterialIndex, mask: true);
			}
			return (index: suit.ColorMaterialIndex, mask: true);
		}
		return (index: paintableOveralls.ColorMaterialIndex, mask: true);
	}

	private static void PaintRenderer(SkinnedMeshRenderer renderer, int swatchIndex, int paintIndex, bool mask)
	{
		if (renderer == null || swatchIndex < 0)
		{
			return;
		}
		Material[] sharedMaterials = renderer.sharedMaterials;
		int num = ((sharedMaterials != null) ? sharedMaterials.Length : 0);
		if (num != 0)
		{
			paintIndex = Mathf.Clamp(paintIndex, 0, num - 1);
			ColorSwatch colorSwatch = GameManager.GetColorSwatch(swatchIndex);
			if (colorSwatch != null)
			{
				SkinnedMeshRendererInstance skinnedMeshRendererInstance = new SkinnedMeshRendererInstance();
				skinnedMeshRendererInstance.Renderer = renderer;
				skinnedMeshRendererInstance.PaintableIndex = paintIndex;
				skinnedMeshRendererInstance.SetColorMaskOnMainMaterial = mask;
				skinnedMeshRendererInstance.SetColor(colorSwatch, force: true);
			}
		}
	}

	private static KitMetaData BuildMeta(CharacterKit kit, CharacterConfig config)
	{
		KitMetaData kitMetaData = new KitMetaData();
		kitMetaData.Body = kit.Bodies.GetElementId(0);
		kitMetaData.Head = kit.Heads.GetElementId(config.Head);
		kitMetaData.Eyes = kit.Eyes.GetElementId(config.Eyes);
		kitMetaData.Hair = kit.Hairs.GetElementId(config.Hair);
		kitMetaData.FacialHair = kit.FacialHairs.GetElementId(config.FacialHair);
		kitMetaData.SkinColour = kit.SkinColours.GetElementId(config.Skin);
		kitMetaData.EyeColour = kit.EyeColours.GetElementId(config.EyeColour);
		kitMetaData.HairColours = new string[2]
		{
			kit.HairColours.GetElementId(config.HairColour),
			kit.HairColours.GetElementId(config.FacialHairColour)
		};
		return kitMetaData;
	}

	public void Randomize(CharacterConfig config)
	{
		if (_kits.Count > 0)
		{
			config.KitIndex = UnityEngine.Random.Range(0, _kits.Count);
		}
		CharacterKit characterKit = KitFor(config);
		if (!(characterKit == null))
		{
			config.Head = RandomIndex(characterKit.Heads.Length);
			config.Eyes = RandomIndex(characterKit.Eyes.Length);
			config.Hair = RandomIndex(characterKit.Hairs.Length);
			config.HairColour = RandomIndex(characterKit.HairColours.Length);
			config.FacialHair = RandomIndex(characterKit.FacialHairs.Length);
			config.FacialHairColour = RandomIndex(characterKit.HairColours.Length);
			config.EyeColour = RandomIndex(characterKit.EyeColours.Length);
			config.Skin = RandomIndex(characterKit.SkinColours.Length);
		}
	}

	private static int RandomIndex(int count)
	{
		if (count <= 0)
		{
			return 0;
		}
		return UnityEngine.Random.Range(0, count);
	}

	public void Activate()
	{
		if (_staging != null)
		{
			_staging.SetActive(value: true);
		}
	}

	public void PositionForCapture(Vector3 cameraPos, Quaternion cameraRot, float verticalFovDeg, float fill, CharacterConfig config)
	{
		if (!(_avatar == null))
		{
			SkinnedMeshRenderer[] componentsInChildren = _avatarTransform.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].updateWhenOffscreen = true;
			}
			Vector3 vector = cameraRot * Vector3.forward;
			Vector3 vector2 = new Vector3(vector.x, 0f, vector.z);
			if (vector2.sqrMagnitude < 0.0001f)
			{
				vector2 = Vector3.forward;
			}
			Quaternion quaternion = Quaternion.LookRotation(-vector2.normalized, Vector3.up);
			_avatarTransform.rotation = quaternion * Quaternion.Euler(config.Euler);
			_avatarTransform.position = Vector3.zero;
			Bounds bounds = ComputeBounds();
			float num = Mathf.Max(bounds.size.x, bounds.size.y, 0.01f);
			float b = Mathf.Tan(verticalFovDeg * 0.5f * (MathF.PI / 180f));
			float num2 = Mathf.Clamp(fill, 0.3f, 0.99f);
			float num3 = num * 0.5f / (num2 * Mathf.Max(0.01f, b));
			num3 /= Mathf.Max(0.1f, config.Zoom);
			Vector3 vector3 = cameraPos + vector.normalized * num3;
			_avatarTransform.position += vector3 - bounds.center;
		}
	}

	private Bounds ComputeBounds()
	{
		bool flag = false;
		Bounds result = new Bounds(_avatarTransform.position, Vector3.zero);
		Renderer[] componentsInChildren = _avatarTransform.GetComponentsInChildren<Renderer>(includeInactive: false);
		foreach (Renderer renderer in componentsInChildren)
		{
			if (renderer.enabled && renderer.sharedMaterials.Length != 0)
			{
				if (!flag)
				{
					result = renderer.bounds;
					flag = true;
				}
				else
				{
					result.Encapsulate(renderer.bounds);
				}
			}
		}
		if (!flag)
		{
			return new Bounds(_avatarTransform.position, Vector3.one);
		}
		return result;
	}

	public void PrepareTurntable(Vector3 cameraPos, Quaternion cameraRot, float verticalFovDeg, float fill, CharacterConfig config)
	{
		if (!(_avatar == null))
		{
			SkinnedMeshRenderer[] componentsInChildren = _avatarTransform.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].updateWhenOffscreen = true;
			}
			Vector3 vector = cameraRot * Vector3.forward;
			Vector3 vector2 = new Vector3(vector.x, 0f, vector.z);
			if (vector2.sqrMagnitude < 0.0001f)
			{
				vector2 = Vector3.forward;
			}
			_turntableFacing = Quaternion.LookRotation(-vector2.normalized, Vector3.up);
			float b = Mathf.Tan(verticalFovDeg * 0.5f * (MathF.PI / 180f));
			float num = Mathf.Clamp(fill, 0.3f, 0.99f);
			float num2 = 0.25f;
			for (int j = 0; j < 360; j += 15)
			{
				_avatarTransform.rotation = _turntableFacing * Quaternion.Euler(config.Euler.x, config.Euler.y + (float)j, config.Euler.z);
				_avatarTransform.position = Vector3.zero;
				Bounds bounds = ComputeBounds();
				float num3 = Mathf.Max(bounds.size.x, bounds.size.y, 0.01f);
				num2 = Mathf.Max(num2, num3 * 0.5f / (num * Mathf.Max(0.01f, b)));
			}
			_turntableDistance = num2 / Mathf.Max(0.1f, config.Zoom);
		}
	}

	public void PositionTurntableFrame(Vector3 cameraPos, Quaternion cameraRot, CharacterConfig config, float frameYaw)
	{
		if (!(_avatar == null))
		{
			Vector3 normalized = (cameraRot * Vector3.forward).normalized;
			_avatarTransform.rotation = _turntableFacing * Quaternion.Euler(config.Euler.x, config.Euler.y + frameYaw, config.Euler.z);
			_avatarTransform.position = Vector3.zero;
			Bounds bounds = ComputeBounds();
			_avatarTransform.position += cameraPos + normalized * _turntableDistance - bounds.center;
		}
	}

	public void Deactivate()
	{
		if (_staging != null)
		{
			_staging.SetActive(value: false);
		}
	}

	public void Destroy()
	{
		if (_staging != null)
		{
			UnityEngine.Object.Destroy(_staging);
		}
		_staging = null;
		_avatar = null;
		_avatarTransform = null;
		_kits.Clear();
		BodyClothingOptions.Clear();
		ArmorOptions.Clear();
	}
}
