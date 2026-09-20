using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Util;

namespace Assets.Scripts.UI;

public class TwoStepButton : UserInterfaceBase
{
	public Action OnConfirm;

	[Header("Two step button")]
	[SerializeField]
	private GameObject _mainText;

	[SerializeField]
	private GameObject _cancelText;

	[Space(15f)]
	[SerializeField]
	private BasicButton _mainButton;

	[SerializeField]
	private BasicButton _confirmButton;

	private bool _isConfirming;

	private readonly CancellationTokenWrapper _cancellation = new CancellationTokenWrapper();

	private async UniTaskVoid ReturnToDefault(CancellationToken cancellationToken)
	{
		await UniTask.Delay(5000, ignoreTimeScale: false, PlayerLoopTiming.Update, cancellationToken);
		SetToDefaultState();
	}

	private void MainClick()
	{
		if (!_isConfirming)
		{
			_cancellation.CancelAndInitialize();
			ReturnToDefault(_cancellation.Token).Forget();
			SetToConfirmingState();
		}
		else
		{
			_cancellation.Cancel();
			SetToDefaultState();
		}
	}

	private void SetToConfirmingState()
	{
		_isConfirming = true;
		_mainText.SetActive(value: false);
		_cancelText.SetActive(value: true);
		_confirmButton.SetActive(active: true);
	}

	private void SetToDefaultState()
	{
		_isConfirming = false;
		_mainText.SetActive(value: true);
		_cancelText.SetActive(value: false);
		_confirmButton.SetActive(active: false);
	}

	private void ConfirmClick()
	{
		_cancellation.Cancel();
		SetToDefaultState();
		OnConfirm?.Invoke();
	}

	public override void OnEnable()
	{
		base.OnEnable();
		SetToDefaultState();
	}

	public override void OnDisable()
	{
		base.OnDisable();
		_cancellation.Cancel();
	}

	private void Awake()
	{
		BasicButton mainButton = _mainButton;
		mainButton.OnClick = (Action)Delegate.Combine(mainButton.OnClick, new Action(MainClick));
		BasicButton confirmButton = _confirmButton;
		confirmButton.OnClick = (Action)Delegate.Combine(confirmButton.OnClick, new Action(ConfirmClick));
	}

	private void OnDestroy()
	{
		BasicButton mainButton = _mainButton;
		mainButton.OnClick = (Action)Delegate.Remove(mainButton.OnClick, new Action(MainClick));
		BasicButton confirmButton = _confirmButton;
		confirmButton.OnClick = (Action)Delegate.Remove(confirmButton.OnClick, new Action(ConfirmClick));
	}
}
