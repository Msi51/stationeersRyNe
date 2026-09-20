using System.Collections;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Effects;
using UnityEngine;

public class GasCanisterWithDisplay : GasCanister
{
	public enum CanisterPressureState
	{
		Empty,
		Low,
		Medium,
		High,
		Full,
		Critical
	}

	private CanisterPressureState _pressureState;

	[SerializeField]
	private GameObject[] displayStates = new GameObject[4];

	[SerializeField]
	private MaterialChanger displayCritical;

	private Interactable _modeInteractable;

	private bool _pressureUpdate;

	private bool _destroyed;

	public override void Awake()
	{
		base.Awake();
		_modeInteractable = Interactables.Find((Interactable i) => i.Action == InteractableType.Mode);
		RefreshQuantityDisplay();
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (_destroyed)
		{
			return;
		}
		float num = (base.InternalAtmosphere.PressureGassesAndLiquids / base.MaxPressure).ToFloat();
		_pressureState = CanisterPressureState.Empty;
		if (num >= 0.999f)
		{
			_pressureState = CanisterPressureState.Critical;
		}
		else if (num >= 0.9f)
		{
			_pressureState = CanisterPressureState.Full;
		}
		else if (num >= 0.8f)
		{
			_pressureState = CanisterPressureState.High;
		}
		else if (num >= 0.5f)
		{
			_pressureState = CanisterPressureState.Medium;
		}
		else if (num >= 0.1f)
		{
			_pressureState = CanisterPressureState.Low;
		}
		if (!_pressureUpdate && GameManager.RunSimulation && _pressureState != (CanisterPressureState)Mode)
		{
			_pressureUpdate = true;
			if (ThreadedManager.IsThread)
			{
				UnityMainThreadDispatcher.Instance().Enqueue(WaitThenUpdatePressureDisplay());
			}
			else
			{
				StartCoroutine(WaitThenUpdatePressureDisplay());
			}
		}
	}

	public IEnumerator WaitThenUpdatePressureDisplay()
	{
		yield return new WaitForSecondsRealtime(0.5f);
		Mode = (int)_pressureState;
		OnServer.Interact(_modeInteractable, Mode);
		_pressureUpdate = false;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		_destroyed = true;
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Mode)
		{
			RefreshQuantityDisplay();
		}
	}

	private void RefreshQuantityDisplay()
	{
		GameObject[] array = displayStates;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
		switch ((CanisterPressureState)Mode)
		{
		case CanisterPressureState.Low:
			displayStates[0].SetActive(value: true);
			break;
		case CanisterPressureState.Medium:
			displayStates[1].SetActive(value: true);
			break;
		case CanisterPressureState.High:
			displayStates[2].SetActive(value: true);
			break;
		case CanisterPressureState.Full:
			displayStates[3].SetActive(value: true);
			displayCritical.ChangeState(Defines.Animator.Normal);
			break;
		case CanisterPressureState.Critical:
			displayStates[3].SetActive(value: true);
			displayCritical.ChangeState(Defines.Animator.Critical);
			break;
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		RefreshQuantityDisplay();
	}
}
