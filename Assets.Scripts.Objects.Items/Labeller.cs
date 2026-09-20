using System.Globalization;
using System.Text.RegularExpressions;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Labeller : PowerTool
{
	private static readonly int LabelHash = Animator.StringToHash("Label");

	public static readonly int LabelConfirmHash = Animator.StringToHash("LabelConfirm");

	private static readonly int LabelCancelHash = Animator.StringToHash("LabelCancel");

	public void Rename(Thing thing)
	{
		if (base.ParentSlot.Parent.HasAuthority && InputWindow.ShowInputPanel(string.Format(InterfaceStrings.RenameThing, thing.SourcePrefab.DisplayName), thing.DisplayName, thing, this, 32))
		{
			InputWindow.OnSubmit += delegate(string input, string input2)
			{
				InputRenameFinished(input, input2, thing);
			};
			PlaySound(LabelHash);
			InputWindow.OnCancel += PlayCancelSound;
		}
	}

	public void PlayCancelSound()
	{
		PlaySound(LabelCancelHash);
		InputWindow.OnCancel -= PlayCancelSound;
	}

	public void Set(ISetable setable, LogicType logicType = LogicType.Setting)
	{
		if (base.ParentSlot.Parent.HasAuthority && InputWindow.ShowInputPanel(string.Format(InterfaceStrings.SetThingValue, setable.DisplayName), setable.GetLogicValue(logicType).ToStringExact(), setable, this, 32, TMP_InputField.ContentType.DecimalNumber))
		{
			InputWindow.OnSubmit += delegate(string input, string input2)
			{
				InputSetting(input, setable, logicType);
			};
			PlaySound(LabelHash);
			InputWindow.OnCancel += PlayCancelSound;
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		if ((bool)MaterialChanger && !BaseAnimator)
		{
			MaterialChanger.ChangeState((OnOff && Powered) ? Defines.Animator.On : Defines.Animator.Off);
		}
	}

	public void InputSetting(string value, ISetable settable, LogicType logicType = LogicType.Setting)
	{
		if (settable != null)
		{
			double.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var result);
			if (double.IsPositiveInfinity(result))
			{
				result = double.MaxValue;
			}
			if (NetworkManager.IsClient)
			{
				SetLogicFromClient setLogicFromClient = new SetLogicFromClient();
				setLogicFromClient.LogicId = settable.NetworkId;
				setLogicFromClient.LogicType = logicType;
				setLogicFromClient.Value = result;
				setLogicFromClient.SendToServer();
			}
			else if (settable.CanLogicWrite(logicType))
			{
				settable.SetLogicValue(logicType, result);
			}
			PlaySound(LabelConfirmHash);
		}
	}

	private void InputRenameFinished(string value, string value2, Thing thing)
	{
		if (thing is Sign)
		{
			value = value2;
		}
		if (string.IsNullOrEmpty(value))
		{
			value = thing.SourcePrefab.DisplayName;
		}
		if (!string.IsNullOrEmpty(value))
		{
			value = ((value.Length <= 200) ? value : value.Substring(0, 200));
		}
		if (!string.IsNullOrEmpty(value))
		{
			if (!(thing is Sign) && !(thing is Label))
			{
				value = Regex.Replace(value, "<.*?>", "");
			}
			if (GameManager.RunSimulation)
			{
				Thing.RenameThing(thing.ReferenceId, value);
			}
			else
			{
				NetworkClient.RenameThing(thing.ReferenceId, value);
			}
			PlaySound(LabelConfirmHash);
		}
	}
}
