using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Objects.Rockets.Scanning;
using TMPro;
using UnityEngine;

namespace Objects.Rockets.UI;

public class TransferActionProgressDisplay : ActionProgressDisplay
{
	[SerializeField]
	private TMP_Dropdown _TargetSelect;

	public override void Apply(ProgressDisplayData data)
	{
		base.Apply(data);
		SetDropdownValues(data.IRocketActionProgressable);
		SelectCurrentTarget(((IRocketTransferActionProgressable)data.IRocketActionProgressable).CurrentTarget);
	}

	private void SelectCurrentTarget(IRocketActionProgressableTarget currentTarget)
	{
		int valueWithoutNotify = 0;
		if (currentTarget != null)
		{
			for (int i = 0; i < _TargetSelect.options.Count; i++)
			{
				if (_TargetSelect.options[i] is RocketActionTargetDropdownOption rocketActionTargetDropdownOption && rocketActionTargetDropdownOption.Target == currentTarget)
				{
					valueWithoutNotify = i;
					break;
				}
			}
		}
		_TargetSelect.SetValueWithoutNotify(valueWithoutNotify);
	}

	private void SetDropdownValues(IRocketActionProgressable actionProgressable)
	{
		List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>();
		List<IRocketActionProgressableTarget> validTargets = ((IRocketTransferActionProgressable)actionProgressable).GetValidTargets();
		list.Add(new RocketActionTargetDropdownOption(null, GameStrings.None));
		if (validTargets != null)
		{
			foreach (IRocketActionProgressableTarget item in validTargets)
			{
				string text = item.DisplayName + " : " + item.RocketNetwork.Rocket.DisplayName;
				list.Add(new RocketActionTargetDropdownOption(item, text));
			}
		}
		_TargetSelect.options = list;
		RefreshSelectedTarget();
	}

	private void RefreshSelectedTarget()
	{
		IRocketActionProgressableTarget rocketActionProgressableTarget = ((IRocketTransferActionProgressable)_data.IRocketActionProgressable)?.CurrentTarget;
		if (rocketActionProgressableTarget == null)
		{
			_TargetSelect.SetValueWithoutNotify(0);
		}
		else
		{
			SelectCurrentTarget(rocketActionProgressableTarget);
		}
	}

	private RocketActionTargetDropdownOption GetSelected()
	{
		if (_TargetSelect.options != null && _TargetSelect.value < _TargetSelect.options.Count)
		{
			return _TargetSelect.options[_TargetSelect.value] as RocketActionTargetDropdownOption;
		}
		return null;
	}

	private void TargetSelected(int index)
	{
		RocketActionTargetDropdownOption selected = GetSelected();
		if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new SetTransferTargetMessage
			{
				SourceId = _data.IRocketActionProgressable.ReferenceId,
				TargetId = (selected?.Target?.ReferenceId).GetValueOrDefault()
			});
		}
		else
		{
			((IRocketTransferActionProgressable)_data.IRocketActionProgressable).SetTarget(selected.Target);
		}
	}

	private void Awake()
	{
		_TargetSelect.onValueChanged.AddListener(TargetSelected);
	}

	private void OnDestroy()
	{
		_TargetSelect.onValueChanged.RemoveListener(TargetSelected);
	}
}
