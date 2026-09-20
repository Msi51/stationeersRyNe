using System;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Util;
using CharacterCustomisation.Clothing;
using DLC;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CharacterCustomisation;

public class CharacterCreationPanel : GameBase
{
	[Header("Character Creation Panel")]
	[SerializeField]
	private Button _confirmButton;

	[SerializeField]
	private Button _cancelButton;

	[SerializeField]
	private Button _randomButton;

	[SerializeField]
	private Button _dlcButton;

	[SerializeField]
	private TMP_InputField _usernameInputField;

	[Space]
	[SerializeField]
	private PlayerCosmeticsBehaviour _playerCosmeticsBehaviour;

	[Header("List Iterators")]
	[SerializeField]
	private UIListIterator _speciesIterator;

	[SerializeField]
	private UIListIterator _genderIterator;

	[SerializeField]
	private UIListIterator _faceIterator;

	[SerializeField]
	private UIListIterator _hairIterator;

	[SerializeField]
	private UIListIterator _hairColourIterator;

	[SerializeField]
	private UIListIterator _eyeColourIterator;

	[SerializeField]
	private UIListIterator _skinColourIterator;

	[SerializeField]
	private UIListIterator _facialHairIterator;

	[SerializeField]
	private UIListIterator _facialHairColourIterator;

	[FormerlySerializedAs("_kits")]
	[SerializeField]
	private KitSets[] _kitSets;

	[Header("Options")]
	[SerializeField]
	private Toggle _spaceSuitToggle;

	[SerializeField]
	private Toggle _jumpSuitToggle;

	[SerializeField]
	private Toggle _marineSuitToggle;

	[Space]
	[SerializeField]
	private Toggle _helmetToggle;

	[SerializeField]
	private Toggle _hatToggle;

	[SerializeField]
	private Toggle _marineHelmetToggle;

	[Space]
	[SerializeField]
	private TMP_Dropdown _expressionsDropdown;

	[SerializeField]
	private KitItem[] _suitClothes;

	[SerializeField]
	private ClothingItem[] _jumpsuitClothes;

	[SerializeField]
	private ClothingItem[] _marineClothes;

	[SerializeField]
	private ClothingItem[] _marineArmorClothes;

	[SerializeField]
	private GameObject[] _suitObjects;

	[SerializeField]
	private GameObject[] _helmetObjects;

	[SerializeField]
	private GameObject[] _hatObjects;

	[SerializeField]
	private GameObject[] _marineHelmetObjects;

	[Header("Camera")]
	public CameraFilterPack_Color_GrayScale GrayscaleEffect;

	private string _currentDlcLink;

	private PlayerCosmetics _playerCosmeticData;

	private SpeciesClass SpeciesClass => (SpeciesClass)(_speciesIterator.Index + 1);

	private Gender Gender => (Gender)_genderIterator.Index;

	public event Action OnConfirmed;

	public event Action OnCancelled;

	private void Start()
	{
		_confirmButton.onClick.AddListener(SaveAndExit);
		_cancelButton.onClick.AddListener(delegate
		{
			this.OnCancelled?.Invoke();
		});
		_randomButton.onClick.AddListener(RandomizeProps);
		_usernameInputField.text = NetworkManager.Cookie.Username;
		UIAudioManager.ToggleInventorySoundPause(boolState: true);
		_dlcButton.onClick.AddListener(delegate
		{
			NetworkManager.CurrentTransport?.OpenWebPageOverlay(_currentDlcLink);
		});
		_speciesIterator.onValueChanged.AddListener(delegate
		{
			ChangeSpecies();
		});
		_genderIterator.onValueChanged.AddListener(delegate
		{
			RefreshEverything();
		});
		_faceIterator.onValueChanged.AddListener(delegate
		{
			RefreshEverything();
		});
		_hairIterator.onValueChanged.AddListener(delegate
		{
			RefreshEverything();
		});
		_hairColourIterator.onValueChanged.AddListener(delegate
		{
			RefreshEverything();
		});
		_eyeColourIterator.onValueChanged.AddListener(delegate
		{
			RefreshEverything();
		});
		_skinColourIterator.onValueChanged.AddListener(delegate
		{
			RefreshEverything();
		});
		_facialHairIterator.onValueChanged.AddListener(delegate
		{
			RefreshEverything();
		});
		_facialHairColourIterator.onValueChanged.AddListener(delegate
		{
			RefreshEverything();
		});
		_spaceSuitToggle.onValueChanged.AddListener(ToggleSpaceSuit);
		_jumpSuitToggle.onValueChanged.AddListener(delegate
		{
			RefreshEverything();
		});
		_marineSuitToggle.onValueChanged.AddListener(delegate
		{
			RefreshEverything();
		});
		_helmetToggle.onValueChanged.AddListener(ToggleHelmet);
		_hatToggle.onValueChanged.AddListener(ToggleHat);
		_marineHelmetToggle.onValueChanged.AddListener(ToggleMarineHelmet);
		_expressionsDropdown.onValueChanged.AddListener(ChangeExpression);
		if ((bool)Human.LocalHuman)
		{
			_playerCosmeticData = Human.LocalHuman.CosmeticData;
			_speciesIterator.Index = (int)(Human.LocalHuman.SpeciesClass - 1);
			_speciesIterator.SetInteractivity(isInteractive: false);
		}
		else
		{
			int slot = (Singleton<GameManager>.Instance ? Singleton<GameManager>.Instance.CustomCosmeticsSlot : 0);
			_playerCosmeticData = PlayerCosmetics.Load(slot);
		}
		if (_playerCosmeticData != null)
		{
			SetUiToSavedData(_playerCosmeticData);
		}
		RefreshEverything();
	}

	private void ChangeExpression(int index)
	{
		BlendShapeType blendShapeType = ((index != 0) ? ((BlendShapeType)index) : BlendShapeType.None);
		_playerCosmeticsBehaviour.SetExpression(blendShapeType, tween: true);
	}

	private void SetExpression()
	{
		int value = _expressionsDropdown.value;
		BlendShapeType blendShapeType = ((value != 0) ? ((BlendShapeType)value) : BlendShapeType.None);
		_playerCosmeticsBehaviour.SetExpression(blendShapeType, tween: false);
	}

	private void ToggleSpaceSuit(bool isOn)
	{
		GameObject[] suitObjects = _suitObjects;
		for (int i = 0; i < suitObjects.Length; i++)
		{
			suitObjects[i].SetActive(isOn);
		}
		if (!isOn)
		{
			_helmetToggle.isOn = false;
		}
		RefreshEverything();
	}

	private void ToggleHelmet(bool isOn)
	{
		if (isOn && !_spaceSuitToggle.isOn)
		{
			_spaceSuitToggle.isOn = true;
		}
		GameObject[] helmetObjects = _helmetObjects;
		for (int i = 0; i < helmetObjects.Length; i++)
		{
			helmetObjects[i].SetActive(isOn);
		}
		DetermineHairMode();
	}

	private void ToggleHat(bool isOn)
	{
		GameObject[] hatObjects = _hatObjects;
		for (int i = 0; i < hatObjects.Length; i++)
		{
			hatObjects[i].SetActive(isOn);
		}
		DetermineHairMode();
	}

	private void ToggleMarineHelmet(bool isOn)
	{
		GameObject[] marineHelmetObjects = _marineHelmetObjects;
		for (int i = 0; i < marineHelmetObjects.Length; i++)
		{
			marineHelmetObjects[i].SetActive(isOn);
		}
		DetermineHairMode();
	}

	private void DetermineHairMode()
	{
		HairMode hairMode = HairMode.Normal;
		if (_hatToggle.isOn)
		{
			hairMode = HairMode.Hat;
		}
		else if (_helmetToggle.isOn)
		{
			hairMode = HairMode.Helmet;
		}
		else if (_marineHelmetToggle.isOn)
		{
			hairMode = HairMode.None;
		}
		_playerCosmeticsBehaviour.SetHairMode(hairMode);
	}

	private void SetUiToSavedData(PlayerCosmetics savedKit)
	{
		int num = Array.FindIndex(_kitSets, (KitSets k) => k.SpeciesClass == savedKit.SpeciesClass);
		if (num == -1)
		{
			Debug.LogError("Saved species not found");
			return;
		}
		KitSets kitSets = _kitSets[num];
		int gender = (int)savedKit.Gender;
		if (gender < 0 || gender >= kitSets.Genders.Length)
		{
			Debug.LogError("Saved gender not found");
			return;
		}
		KitGender kitGender = kitSets.Genders[gender];
		int num2 = Array.FindIndex(kitGender.kits, (CharacterKit k) => k.Id == savedKit.KitId);
		if (num2 == -1)
		{
			Debug.LogError("Saved character not found");
			return;
		}
		CharacterKit obj = kitGender.kits[num2];
		int indexWithoutInvoke = obj.Hairs.IndexOf(savedKit.MetaData.Hair);
		int indexWithoutInvoke2 = obj.HairColours.IndexOf(savedKit.MetaData.HairColours[0]);
		int indexWithoutInvoke3 = obj.FacialHairs.IndexOf(savedKit.MetaData.FacialHair);
		int indexWithoutInvoke4 = obj.HairColours.IndexOf(savedKit.MetaData.HairColours[1]);
		int indexWithoutInvoke5 = obj.EyeColours.IndexOf(savedKit.MetaData.EyeColour);
		int indexWithoutInvoke6 = obj.SkinColours.IndexOf(savedKit.MetaData.SkinColour);
		_speciesIterator.SetIndexWithoutInvoke(num);
		_genderIterator.SetIndexWithoutInvoke(gender);
		_faceIterator.SetIndexWithoutInvoke(num2);
		_hairIterator.SetIndexWithoutInvoke(indexWithoutInvoke);
		_hairColourIterator.SetIndexWithoutInvoke(indexWithoutInvoke2);
		_facialHairIterator.SetIndexWithoutInvoke(indexWithoutInvoke3);
		_facialHairColourIterator.SetIndexWithoutInvoke(indexWithoutInvoke4);
		_eyeColourIterator.SetIndexWithoutInvoke(indexWithoutInvoke5);
		_skinColourIterator.SetIndexWithoutInvoke(indexWithoutInvoke6);
	}

	private void SaveAndExit()
	{
		_playerCosmeticData?.Save(Singleton<GameManager>.Instance.CustomCosmeticsSlot);
		NetworkManager.Cookie.Username = _usernameInputField.text.Trim();
		this.OnConfirmed?.Invoke();
	}

	private void ChangeSpecies()
	{
		_genderIterator.SetIndexWithoutInvoke(0);
		_faceIterator.SetIndexWithoutInvoke(0);
		_hairIterator.SetIndexWithoutInvoke(0);
		_facialHairIterator.SetIndexWithoutInvoke(0);
		_eyeColourIterator.SetIndexWithoutInvoke(0);
		_hairColourIterator.SetIndexWithoutInvoke(0);
		_skinColourIterator.SetIndexWithoutInvoke(0);
		RefreshEverything();
	}

	private void SetOptionsToggleInteractivity()
	{
		_jumpSuitToggle.interactable = SpeciesClass != SpeciesClass.Robot;
		if (_jumpSuitToggle.isOn && !_jumpSuitToggle.interactable)
		{
			_jumpSuitToggle.isOn = false;
		}
		_marineSuitToggle.interactable = SpeciesClass != SpeciesClass.Robot;
		if (_marineSuitToggle.isOn && !_marineSuitToggle.interactable)
		{
			_marineSuitToggle.isOn = false;
		}
	}

	private void SetIteratorLengths(KitSets kitSets, CharacterKit characterKit)
	{
		_speciesIterator.Length = _kitSets.Length;
		_genderIterator.Length = kitSets.Genders.Length;
		_faceIterator.Length = kitSets.Genders.GetElement((int)Gender, kitSets.Genders[0]).kits.Length;
		_hairIterator.Length = ((characterKit.Hairs.Length != 0) ? characterKit.Hairs.Length : 0);
		_facialHairIterator.Length = ((characterKit.FacialHairs.Length != 0) ? characterKit.FacialHairs.Length : 0);
		_eyeColourIterator.Length = characterKit.EyeColours.Length;
		_hairColourIterator.Length = characterKit.HairColours.Length;
		_skinColourIterator.Length = characterKit.SkinColours.Length;
		_facialHairColourIterator.Length = characterKit.HairColours.Length;
	}

	private void RandomizeProps()
	{
		_genderIterator.RandomIndex();
		_faceIterator.RandomIndex();
		_hairIterator.RandomIndex();
		_hairColourIterator.RandomIndex();
		_eyeColourIterator.RandomIndex();
		_skinColourIterator.RandomIndex();
		_facialHairIterator.RandomIndex();
		RefreshEverything();
	}

	private void ApplyClothing()
	{
		KitItem kitItem = null;
		KitItem kitItem2 = null;
		if (_spaceSuitToggle.isOn)
		{
			kitItem = _suitClothes.GetElement(_genderIterator.Index);
		}
		else if (_jumpSuitToggle.isOn)
		{
			kitItem = _jumpsuitClothes.GetElement(_genderIterator.Index);
		}
		else if (_marineSuitToggle.isOn)
		{
			kitItem = _marineClothes.GetElement(_genderIterator.Index);
			kitItem2 = _marineArmorClothes.GetElement(_genderIterator.Index);
		}
		_playerCosmeticsBehaviour.ApplyClothing(kitItem);
		_playerCosmeticsBehaviour.ApplyArmor(kitItem2);
	}

	private void RefreshEverything()
	{
		KitSets kitSets = _kitSets.FirstOrDefault((KitSets x) => x.SpeciesClass == SpeciesClass);
		if (!kitSets.IsValid())
		{
			kitSets = _kitSets[0];
		}
		KitGender element = kitSets.Genders.GetElement(_genderIterator.Index, kitSets.Genders[0]);
		CharacterKit element2 = element.kits.GetElement(_faceIterator.Index, element.kits[0]);
		if (!element2)
		{
			Debug.LogError("characterKit is null", this);
			return;
		}
		KitMetaData kitMetaData = new KitMetaData();
		kitMetaData.Body = element2.Bodies.GetElementId(0);
		kitMetaData.Head = element2.Heads.GetElementId(0);
		kitMetaData.Eyes = element2.Eyes.GetElementId(0);
		kitMetaData.Hair = element2.Hairs.GetElementId(_hairIterator.Index);
		kitMetaData.FacialHair = element2.FacialHairs.GetElementId(_facialHairIterator.Index);
		kitMetaData.SkinColour = element2.SkinColours.GetElementId(_skinColourIterator.Index);
		kitMetaData.EyeColour = element2.EyeColours.GetElementId(_eyeColourIterator.Index);
		kitMetaData.HairColours = new string[2]
		{
			element2.HairColours.GetElementId(_hairColourIterator.Index),
			element2.HairColours.GetElementId(_facialHairColourIterator.Index)
		};
		KitMetaData kitMetaData2 = kitMetaData;
		if (_playerCosmeticData == null)
		{
			_playerCosmeticData = new PlayerCosmetics();
		}
		_playerCosmeticData.SpeciesClass = SpeciesClass;
		_playerCosmeticData.Gender = Gender;
		_playerCosmeticData.KitId = element2.Id;
		_playerCosmeticData.MetaData = kitMetaData2;
		UpdateUIState(kitSets, element, element2);
		UpdatePlayerCosmetics(element2, kitMetaData2);
		SetExpression();
		ApplyClothing();
		DetermineHairMode();
	}

	private void UpdateUIState(KitSets kitSets, KitGender gender, CharacterKit characterKit)
	{
		SetIteratorLengths(kitSets, characterKit);
		_speciesIterator.gameObject.SetActive(value: true);
		_genderIterator.gameObject.SetActive(kitSets.Genders.Length > 1);
		_faceIterator.gameObject.SetActive(gender.kits.Length > 1);
		_hairIterator.gameObject.SetActive(characterKit.Hairs.Length > 1);
		_facialHairIterator.gameObject.SetActive(characterKit.FacialHairs.Length > 1);
		_eyeColourIterator.gameObject.SetActive(characterKit.EyeColours.Length > 1);
		_hairColourIterator.gameObject.SetActive(characterKit.HairColours.Length > 1);
		_skinColourIterator.gameObject.SetActive(characterKit.SkinColours.Length > 1);
		_facialHairColourIterator.gameObject.SetActive(characterKit.HairColours.Length > 1 && _facialHairIterator.isActiveAndEnabled);
		SetOptionsToggleInteractivity();
	}

	private void UpdatePlayerCosmetics(CharacterKit characterKit, KitMetaData meta)
	{
		_playerCosmeticsBehaviour.UpdateCosmetics(characterKit, meta);
		PreviewDLCMode(characterKit, meta);
	}

	private bool HasDLC(KitItem kitItem)
	{
		bool num = DLCManager.CheckAccess(kitItem);
		if (!num)
		{
			_currentDlcLink = DLCManager.GetStorePageLink(kitItem.DlcType);
		}
		return num;
	}

	private void PreviewDLCMode(CharacterKit characterKit, KitMetaData meta)
	{
		bool flag = HasDLC(characterKit.Heads.GetItem(meta.Head));
		bool flag2 = HasDLC(characterKit.Hairs.GetItem(meta.Hair));
		bool flag3 = HasDLC(characterKit.FacialHairs.GetItem(meta.FacialHair));
		bool flag4 = HasDLC(characterKit.HairColours.GetItem(meta.HairColours[0]));
		bool flag5 = HasDLC(characterKit.SkinColours.GetItem(meta.SkinColour));
		bool flag6 = HasDLC(characterKit.EyeColours.GetItem(meta.EyeColour));
		_speciesIterator.ShowWarningText(active: false);
		_genderIterator.ShowWarningText(active: false);
		_faceIterator.ShowWarningText(!flag);
		_hairIterator.ShowWarningText(!flag2);
		_hairColourIterator.ShowWarningText(!flag4);
		_eyeColourIterator.ShowWarningText(!flag6);
		_skinColourIterator.ShowWarningText(!flag5);
		_facialHairIterator.ShowWarningText(!flag3);
		bool flag7 = flag && flag2 && flag3 && flag4 && flag6 && flag5;
		_confirmButton.interactable = flag7;
		_dlcButton.gameObject.SetActive(!flag7);
		GrayscaleEffect.enabled = !flag7;
	}
}
