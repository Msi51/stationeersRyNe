using System;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Motherboards;

public class HashDisplay : Circuitboard
{
	[Header("Hash Display UI Config")]
	public TMP_Text DisplayTitle;

	public Image DisplayImage;

	private static readonly string[] HashTypeStrings = EnumCollections.HashTypes;

	private IPrefabHash _viewedDevice;

	private bool _waitingForFlagChange;

	public override string[] ModeStrings => HashTypeStrings;

	public IPrefabHash ViewedDevice
	{
		get
		{
			return _viewedDevice;
		}
		set
		{
			_viewedDevice = value;
			OnFlagChanged();
		}
	}

	public override bool IsOperable
	{
		get
		{
			if (!base.IsOperable)
			{
				return base.Flag != 0;
			}
			return true;
		}
	}

	public override bool CanDeviceLink(Device device)
	{
		if (ViewedDevice == null)
		{
			return device is IPrefabHash;
		}
		return false;
	}

	public override void Awake()
	{
		base.Awake();
		SetFlag(base.Flag);
	}

	public override void OnDeviceListChanged()
	{
		base.OnDeviceListChanged();
		ViewedDevice = null;
		foreach (Device linkedDevice in base.LinkedDevices)
		{
			if (IsDeviceConnected(linkedDevice) && linkedDevice is IPrefabHash viewedDevice)
			{
				ViewedDevice = viewedDevice;
			}
		}
	}

	private bool ShouldDraw()
	{
		if (ParentComputer != null && ParentComputer.AsThing().OnOff)
		{
			return ParentComputer.AsThing().Powered;
		}
		return false;
	}

	public override void UpdateEachFrame()
	{
		if (!WorldManager.IsGamePaused)
		{
			base.UpdateEachFrame();
			if (ShouldDraw() && ViewedDevice != null && base.Flag != ViewedDevice.CurrentHash)
			{
				base.Flag = ViewedDevice.CurrentHash;
			}
		}
	}

	public override void OnFlagChanged()
	{
		base.OnFlagChanged();
		if (!GameManager.IsBatchMode)
		{
			if (GameManager.IsThread)
			{
				WaitForFlagChange().Forget();
				return;
			}
			switch ((HashType)(byte)Mode)
			{
			case HashType.Prefab:
				SetUsingPrefab();
				break;
			case HashType.GasLiquid:
				SetUsingGas();
				break;
			default:
			{
				string text = EnumCollections.HashTypes.GetName((HashType)Mode);
				throw new NotImplementedException("HashType '" + text + "' not implemented");
			}
			}
		}
		ParentComputer?.CheckStatus();
	}

	private void SetUsingGas()
	{
		Chemistry.GasType flag = (Chemistry.GasType)base.Flag;
		if (Stationpedia.TryGetGasThumbnail(flag, out var thumbnail))
		{
			DisplayTitle.text = Localization.GetName(flag);
			DisplayImage.sprite = thumbnail;
			if (!DisplayImage.enabled)
			{
				DisplayImage.enabled = true;
			}
		}
		else
		{
			DisplayTitle.text = string.Empty;
			if (DisplayImage.enabled)
			{
				DisplayImage.enabled = false;
			}
		}
	}

	private void SetUsingPrefab()
	{
		DynamicThing dynamicThing = null;
		if (base.Flag != 0)
		{
			dynamicThing = Prefab.Find<DynamicThing>(base.Flag);
		}
		if (dynamicThing != null)
		{
			DisplayTitle.text = dynamicThing.DisplayName;
			DisplayImage.sprite = dynamicThing.Thumbnail;
			if (!DisplayImage.enabled)
			{
				DisplayImage.enabled = true;
			}
		}
		else
		{
			DisplayTitle.text = string.Empty;
			if (DisplayImage.enabled)
			{
				DisplayImage.enabled = false;
			}
		}
	}

	private async UniTaskVoid WaitForFlagChange()
	{
		if (!_waitingForFlagChange)
		{
			_waitingForFlagChange = true;
			await UniTask.SwitchToMainThread();
			OnFlagChanged();
			_waitingForFlagChange = false;
		}
	}
}
