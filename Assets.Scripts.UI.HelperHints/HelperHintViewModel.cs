using System.Collections.Generic;
using Assets.Scripts.Serialization;
using Trading;

namespace Assets.Scripts.UI.HelperHints;

public class HelperHintViewModel
{
	private WorldObjectiveState _worldObjectiveState;

	private bool _dirty = true;

	private bool _wasActive;

	private bool _wasDisplayed;

	private bool _wasExpanded;

	private bool _wasCompleted;

	private bool _wasDismissed;

	public List<ConditionData> ConditionData => _worldObjectiveState.WorldObjective.Conditions;

	public List<ObjectiveConditionCollection> ObjectiveConditionCollections => _worldObjectiveState.WorldObjective.PrefabConditionCollections;

	public string Id => _worldObjectiveState.WorldObjective.Id;

	public string Title { get; private set; }

	public string Info { get; private set; }

	public List<LocalizedStringReference> Notices { get; private set; }

	public string ExpandId { get; private set; }

	public string DismissId { get; private set; }

	public bool Active => _worldObjectiveState.Triggered;

	public bool Displayed
	{
		get
		{
			if (Active)
			{
				return !Dismissed;
			}
			return false;
		}
	}

	public bool Expanded { get; private set; }

	public bool Completed => _worldObjectiveState.Completed;

	public bool Dismissed => _worldObjectiveState.Dismissed;

	public HelperHintViewModel(WorldObjectiveState worldObjectiveState)
	{
		_worldObjectiveState = worldObjectiveState;
		Title = _worldObjectiveState.WorldObjective.GetName();
		Info = _worldObjectiveState.WorldObjective.GetInfo();
		Notices = _worldObjectiveState.WorldObjective.GetNotices();
		Expanded = _worldObjectiveState.WorldObjective.ExpandOnTrigger || Settings.CurrentData.AutoExpandHelperHints;
		ExpandId = "expand:" + Id;
		DismissId = "dismiss:" + Id;
	}

	public bool IsDirty()
	{
		PlaySounds();
		CollapseOnComplete();
		CompleteOnDismissed();
		DismissOnComplete();
		bool active = Active;
		bool displayed = Displayed;
		bool expanded = Expanded;
		bool completed = Completed;
		bool dismissed = Dismissed;
		_dirty = _dirty || active != _wasActive || displayed != _wasDisplayed || expanded != _wasExpanded || completed != _wasCompleted || dismissed != _wasDismissed;
		_wasActive = active;
		_wasDisplayed = displayed;
		_wasExpanded = expanded;
		_wasCompleted = completed;
		_wasDismissed = dismissed;
		return _dirty;
	}

	private void CollapseOnComplete()
	{
		if (Completed && !_wasCompleted && Expanded)
		{
			Expanded = false;
		}
	}

	private void DismissOnComplete()
	{
		if (GameManager.IsNewTutorial && Displayed && Completed && !_wasCompleted)
		{
			Dismiss();
		}
	}

	private void PlaySounds()
	{
		if (!GameManager.IsBatchMode && GameManager.HelperHintsEnabled)
		{
			if (Completed && !_wasCompleted)
			{
				UIAudioManager.Play(UIAudioManager.StageCompleteHash);
			}
			if (Active && !_wasActive)
			{
				UIAudioManager.Play(UIAudioManager.TaskCompleteHash);
			}
		}
	}

	private void CompleteOnDismissed()
	{
		if (Dismissed && _wasDismissed && _worldObjectiveState.ShouldCompleteOnDismissed)
		{
			_worldObjectiveState.Completed = true;
		}
	}

	public void SetDirty(bool value)
	{
		_dirty = value;
	}

	public void ToggleExpanded()
	{
		Expanded = !Expanded;
		_dirty = true;
	}

	public void Dismiss()
	{
		_worldObjectiveState.SetDismissed(value: true);
		_dirty = true;
	}

	public void Restore()
	{
		_worldObjectiveState.SetDismissed(value: false);
		_dirty = true;
	}
}
