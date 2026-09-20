using System.Collections;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Sound;
using UnityEngine;

public class UIAudioManager : ManagerBase
{
	public static UIAudioManager Instance;

	public static readonly int ActionFailHash = Animator.StringToHash("ActionFail");

	public static readonly int ObjectIntoHandHash = Animator.StringToHash("SFX_UI_ObjectIntoHand");

	public static readonly int AddToInventoryHash = Animator.StringToHash("SFX_UI_AddToInventory");

	public static readonly int HighlightInWorldCanInteractHash = Animator.StringToHash("HighlightInWorldCanInteract");

	public static readonly int HighlightInWorldCantInteractHash = Animator.StringToHash("HighlightInWorldCantInteract");

	public static readonly int HighlightInWorldItemHash = Animator.StringToHash("HighlightInWorldItem");

	public static readonly int NarrationPanelHash = Animator.StringToHash("NarrationPanel");

	public static readonly int NarrationPanelCloseHash = Animator.StringToHash("NarrationPanelClose");

	public static readonly int IntroPanelHash = Animator.StringToHash("IntroPanel");

	public static readonly int TaskCompleteHash = Animator.StringToHash("TaskComplete");

	public static readonly int StageCompleteHash = Animator.StringToHash("StageComplete");

	public static readonly int EnableHudLeftHash = Animator.StringToHash("EnableHudLeft");

	public static readonly int EnableHudRightHash = Animator.StringToHash("EnableHudRight");

	public static readonly int ThrowingHash = Animator.StringToHash("Throwing");

	public static readonly int ThrowSmallHash = Animator.StringToHash("ThrowSmall");

	public static readonly int ThrowMediumHash = Animator.StringToHash("ThrowMedium");

	public static readonly int ThrowBigHash = Animator.StringToHash("ThrowBig");

	public static readonly int ClickLightHash = Animator.StringToHash("ClickLight");

	public static readonly int ClickMediumHash = Animator.StringToHash("ClickMedium");

	public static readonly int ClickLargeHash = Animator.StringToHash("ClickLarge");

	public static readonly int HoverLightHash = Animator.StringToHash("HoverLight");

	public static readonly int HoverMediumHash = Animator.StringToHash("HoverMedium");

	public static readonly int HoverLargeHash = Animator.StringToHash("HoverLarge");

	public static readonly int SliderClickHash = Animator.StringToHash("SliderClick");

	public static readonly int ObjectPutHash = Animator.StringToHash("SFX_UI_ObjectPut");

	public static readonly int LocalPlayerSlotEnterSoundHash = Animator.StringToHash("SFX_UI_AddToInventory");

	public static readonly int InstallBatteryHash = Animator.StringToHash("SFX_UI_InstallBattery");

	public static readonly int InstallFilterHash = Animator.StringToHash("SFX_UI_InstallFilter");

	public static readonly int InstallCanisterHash = Animator.StringToHash("SFX_UI_InstallCanister");

	public static readonly int InstallLiquidCanisterHash = Animator.StringToHash("SFX_UI_InstallLiquidCanister");

	public static readonly int SlotEnterSoundHash = Animator.StringToHash("SFX_UI_ObjectPut");

	public static readonly int UiObjectIntoHandHash = Animator.StringToHash("SFX_UI_ObjectIntoHand");

	public static readonly int UiEquipHelmetHash = Animator.StringToHash("SFX_UI_Equip_1_Helmet");

	public static readonly int UiEquipSuitHash = Animator.StringToHash("SFX_UI_Equip_3_Suit");

	public static readonly int UiEquipBackHash = Animator.StringToHash("SFX_UI_Equip_4_Back");

	public static readonly int UiEquipUniformHash = Animator.StringToHash("SFX_UI_Equip_5_Uniform");

	public static readonly int UiEquipBeltHash = Animator.StringToHash("SFX_UI_Equip_6_Belt");

	public static readonly int UiEquipGlassesHash = Animator.StringToHash("SFX_UI_Equip_2_Glasses");

	public static readonly int PointOfInterestDiscovered = Animator.StringToHash("SFX_UI_PointOfInterestDiscovered");

	public static readonly int PointOfInterestSpace = Animator.StringToHash("SFX_UI_PointOfInterestSpace");

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		Instance = this;
	}

	public static void PlayMainMenuMusic(float fadeTime)
	{
		if (!GameManager.IsBatchMode && !Singleton<GameManager>.Instance.MenuMusic.isPlaying)
		{
			Singleton<GameManager>.Instance.MenuMusic.volume = 1f;
			Singleton<GameManager>.Instance.MenuMusic.Play();
		}
	}

	public static PooledAudioSource Play(int clipNameHash)
	{
		if (GameManager.IsBatchMode)
		{
			return null;
		}
		if (GameManager.GameState == GameState.None || (GameManager.GameState == GameState.Running && Singleton<AudioManager>.Instance != null))
		{
			if (!Singleton<AudioManager>.Instance)
			{
				return null;
			}
			return Singleton<AudioManager>.Instance.PlayAudioClipsData(clipNameHash, Vector3.zero);
		}
		return null;
	}

	public static IEnumerator PlayEnumerator(int clipNameHash)
	{
		if (!GameManager.IsBatchMode)
		{
			Play(clipNameHash);
		}
		yield break;
	}

	public static void ToggleInventorySoundPause(bool boolState)
	{
		if (!GameManager.IsBatchMode)
		{
			InventoryWindowManager._inventoryFinishedLoading = boolState;
		}
	}

	public PooledAudioSource PlayClipName(int clipNameHash)
	{
		if (!GameManager.IsBatchMode)
		{
			return Play(clipNameHash);
		}
		return null;
	}
}
