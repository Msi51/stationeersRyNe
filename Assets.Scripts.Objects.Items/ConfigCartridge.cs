using System;
using System.Text;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class ConfigCartridge : Cartridge
{
	[SerializeField]
	private TextMeshProUGUI _displayTextMesh;

	private static string _notApplicableString = "N/A";

	private string _selectedText = string.Empty;

	private string _outputText = string.Empty;

	private Device _scannedDevice;

	private Device _lastScannedDevice;

	private bool _needTopScroll;

	public Device ScannedDevice
	{
		get
		{
			if (!RootParent || !RootParent.HasAuthority || !CursorManager.CursorThing)
			{
				return null;
			}
			return CursorManager.CursorThing as Device;
		}
	}

	public override void OnMainTick()
	{
		base.OnMainTick();
		ReadLogicText();
	}

	private void ReadLogicText()
	{
		_scannedDevice = ScannedDevice;
		lock (_outputText)
		{
			if (_scannedDevice != null)
			{
				if (_lastScannedDevice != _scannedDevice)
				{
					_needTopScroll = true;
				}
				_lastScannedDevice = _scannedDevice;
				_selectedText = _scannedDevice.DisplayName.ToUpper();
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("ReferenceId");
				stringBuilder.Append(" ... ");
				stringBuilder.AppendFormat("<color=#20B2AA>${0}</color>", _scannedDevice.ReferenceId.ToString("X"));
				for (int i = 0; i < EnumCollections.LogicTypes.Length; i++)
				{
					LogicType logicType = EnumCollections.LogicTypes.Values[i];
					if (_scannedDevice.CanLogicRead(logicType))
					{
						stringBuilder.Append("\n");
						stringBuilder.Append(EnumCollections.LogicTypes.Names[i]);
						stringBuilder.Append(" ... ");
						stringBuilder.Append(_scannedDevice.CanLogicWrite(logicType) ? "<color=grey>" : "<color=green>");
						stringBuilder.Append(Math.Round(_scannedDevice.GetLogicValue(logicType), 3, MidpointRounding.AwayFromZero));
						stringBuilder.Append("</color>");
					}
				}
				_outputText = stringBuilder.ToString();
			}
			else
			{
				_selectedText = _notApplicableString;
				_outputText = string.Empty;
			}
		}
	}

	public override void OnScreenUpdate()
	{
		base.OnScreenUpdate();
		if (_needTopScroll)
		{
			_needTopScroll = false;
			_scrollPanel.SetScrollPosition(0f);
		}
		SelectedTitle.text = _selectedText;
		_displayTextMesh.text = _outputText;
		_scrollPanel.SetContentHeight(_displayTextMesh.preferredHeight);
	}
}
