using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Items;

public class MedicalAnalyser : Cartridge
{
	public DamageItem DamageItemPrefab;

	public GridLayoutGroup DamageGrid;

	public GameObject NoDamage;

	public GridLayoutGroup OrganGrid;

	public GameObject NoOrgans;

	private string _selectedText = string.Empty;

	private static string _damageString = "{0:F0}%";

	public Gradient OrganDamageGradient = new Gradient();

	private GameObject _damageGameObject;

	private GameObject _organGameObject;

	private bool _visibleTarget;

	private Organ[] OrganType;

	private GameObject[] OrganItemsGO;

	private DamageItem[] OrganItems;

	private GameObject[] DamageItemsGO;

	private DamageItem[] DamageItems;

	private Human _scannedHuman;

	private bool _isAnyDamage;

	private bool _isAnyOrgan;

	public Human ScannedHuman
	{
		get
		{
			if (!RootParent || !RootParent.HasAuthority)
			{
				return null;
			}
			return (CursorManager.CursorThing as Human) ?? (RootParent as Human);
		}
	}

	public void SetTarget(bool isVisible, bool force = false)
	{
		if (isVisible != _visibleTarget || force)
		{
			_damageGameObject.SetActive(isVisible);
			_organGameObject.SetActive(isVisible);
			_visibleTarget = isVisible;
		}
	}

	public override void Awake()
	{
		base.Awake();
		string[] array = new string[8] { "Brute", "Burn", "Oxygen", "Toxin", "Radiation", "Starvation", "Hydration", "Stun" };
		Color[] array2 = new Color[8]
		{
			Color.red,
			Color.red,
			Color.red,
			Color.yellow,
			Color.yellow,
			Color.red,
			Color.yellow,
			Color.blue
		};
		_damageGameObject = DamageGrid.gameObject;
		_organGameObject = OrganGrid.gameObject;
		DamageItems = new DamageItem[array.Length];
		DamageItemsGO = new GameObject[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			DamageItems[i] = Object.Instantiate(DamageItemPrefab, DamageGrid.transform);
			DamageItems[i].DamageTitle.text = array[i];
			DamageItems[i].DamagePercent.text = string.Format(_damageString, 0f);
			DamageItems[i].DamageSlider.value = 0f;
			DamageItems[i].DamageSliderFill.color = array2[i];
			DamageItemsGO[i] = DamageItems[i].gameObject;
		}
		OrganType = StatusUpdates.GetOrganTypes();
		OrganItems = new DamageItem[OrganType.Length];
		OrganItemsGO = new GameObject[OrganType.Length];
		for (int j = 0; j < OrganType.Length; j++)
		{
			OrganItems[j] = Object.Instantiate(DamageItemPrefab, OrganGrid.transform);
			OrganItems[j].DamageTitle.text = OrganType[j].GetType().Name;
			OrganItems[j].DamagePercent.text = string.Format(_damageString, 0f);
			OrganItems[j].DamageSlider.value = 0f;
			OrganItems[j].DamageSliderFill.color = Color.green;
			OrganItemsGO[j] = OrganItems[j].gameObject;
		}
		SetTarget(isVisible: false, force: true);
	}

	public override void OnPreScreenUpdate()
	{
		base.OnPreScreenUpdate();
		_scannedHuman = ScannedHuman;
		if (_scannedHuman != null)
		{
			_selectedText = _scannedHuman.DisplayName.ToUpper();
		}
		else
		{
			_selectedText = GameStrings.NotApplicableString.DisplayString;
		}
	}

	private void SetValue(int index, float value, ref GameObject[] gameObjects, ref DamageItem[] damageItems, ref bool isAny)
	{
		bool flag = value > 0f;
		if (gameObjects[index].activeSelf != flag)
		{
			gameObjects[index].SetActive(flag);
		}
		if (flag)
		{
			damageItems[index].DamagePercent.text = $"{value:F0}%";
			damageItems[index].DamageSlider.value = value / 100f;
			if (!isAny)
			{
				isAny = true;
			}
		}
	}

	public override void OnScreenUpdate()
	{
		base.OnScreenUpdate();
		SelectedTitle.text = _selectedText;
		SetTarget(_scannedHuman);
		if (!_scannedHuman)
		{
			return;
		}
		_isAnyDamage = false;
		SetValue(0, _scannedHuman.DamageState.Brute, ref DamageItemsGO, ref DamageItems, ref _isAnyDamage);
		SetValue(1, _scannedHuman.DamageState.Burn, ref DamageItemsGO, ref DamageItems, ref _isAnyDamage);
		SetValue(2, _scannedHuman.DamageState.Oxygen, ref DamageItemsGO, ref DamageItems, ref _isAnyDamage);
		SetValue(3, _scannedHuman.DamageState.Toxic, ref DamageItemsGO, ref DamageItems, ref _isAnyDamage);
		SetValue(4, _scannedHuman.DamageState.Radiation, ref DamageItemsGO, ref DamageItems, ref _isAnyDamage);
		SetValue(5, _scannedHuman.DamageState.Starvation, ref DamageItemsGO, ref DamageItems, ref _isAnyDamage);
		SetValue(6, _scannedHuman.DamageState.Hydration, ref DamageItemsGO, ref DamageItems, ref _isAnyDamage);
		SetValue(7, _scannedHuman.DamageState.Stun, ref DamageItemsGO, ref DamageItems, ref _isAnyDamage);
		NoDamage.SetActive(!_isAnyDamage);
		_isAnyOrgan = false;
		for (int i = 0; i < OrganType.Length; i++)
		{
			Organ organType = OrganType[i];
			DamageItem damageItem = OrganItems[i];
			GameObject gameObject = OrganItemsGO[i];
			Organ organ = _scannedHuman.Organs.Find((Organ o) => o.PrefabHash == organType.PrefabHash);
			if (gameObject.activeSelf != (bool)organ)
			{
				gameObject.SetActive(organ);
			}
			if ((bool)organ)
			{
				float totalRatio = organ.DamageState.TotalRatio;
				damageItem.DamagePercent.text = $"{(1f - totalRatio) * 100f:F0}%";
				damageItem.DamageSlider.value = 1f - organ.DamageState.TotalRatioClamped;
				damageItem.DamageSliderFill.color = OrganDamageGradient.Evaluate(totalRatio);
				if (!_isAnyOrgan)
				{
					_isAnyOrgan = true;
				}
			}
		}
		NoOrgans.SetActive(!_isAnyOrgan);
	}
}
