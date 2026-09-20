using System.Threading;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Effects;
using UnityEngine;
using Util;

namespace Assets.Scripts.Objects.Electrical;

public abstract class AnimComponentManufactory : SimpleFabricatorBase
{
	[SerializeField]
	private Transform fan1;

	[SerializeField]
	private Transform fan2;

	[SerializeField]
	private MaterialChanger light;

	[SerializeField]
	private MaterialChanger screenMaterialChanger;

	[SerializeField]
	private GameObject screen;

	[SerializeField]
	private GenericAssignableAnimComponent doorsAnimComponent;

	[SerializeField]
	private float fanSpeed = 1f;

	private const int STARTUP_DELAY_MS = 2000;

	private CancellationTokenWrapper _startUpCancellationToken = new CancellationTokenWrapper();

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		doorsAnimComponent.RefreshState(skipAnimation);
		if (light != null)
		{
			light.ChangeState((OnOff && Powered && Activate == 1) ? Defines.Animator.On : Defines.Animator.Off);
		}
		if ((!OnOff || !Powered) && screen != null)
		{
			screen.SetActive(value: false);
			_startUpCancellationToken.Cancel();
		}
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (!GameManager.IsBatchMode && !IsOccluded && base.IsStructureCompleted && OnOff && Powered)
		{
			fan1.Rotate(Vector3.forward, 360f * GameManager.DeltaTime * fanSpeed);
			fan2.Rotate(Vector3.forward, 360f * GameManager.DeltaTime * fanSpeed);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Activate && Activate == 1 && OnOff && Powered)
		{
			OnBeginSmelt();
		}
		InteractableType action = interactable.Action;
		if (action == InteractableType.OnOff || action == InteractableType.Powered)
		{
			if (OnOff && Powered && !_startUpCancellationToken.Initialized)
			{
				_startUpCancellationToken.Initialize();
				StartUp(_startUpCancellationToken.Token).Forget();
			}
			else if (!OnOff || !Powered)
			{
				_startUpCancellationToken.Cancel();
				SetScreenInteraction(enable: false);
			}
		}
	}

	private void SetScreenInteraction(bool enable)
	{
		if (screen != null)
		{
			screen.SetActive(enable);
		}
		if (base.InteractActivate?.Collider != null)
		{
			base.InteractActivate.Collider.enabled = enable;
		}
		if (base.InteractButton1?.Collider != null)
		{
			base.InteractButton1.Collider.enabled = enable;
		}
		if (base.InteractButton2?.Collider != null)
		{
			base.InteractButton2.Collider.enabled = enable;
		}
		if (base.InteractButton3?.Collider != null)
		{
			base.InteractButton3.Collider.enabled = enable;
		}
		if (IconMaterial != null)
		{
			IconMaterial.transform.gameObject.SetActive(enable);
		}
	}

	private async UniTaskVoid StartUp(CancellationToken token)
	{
		if (IconMaterial != null)
		{
			IconMaterial.transform.gameObject.SetActive(value: false);
		}
		if (screen != null)
		{
			screen.SetActive(value: true);
		}
		if (screenMaterialChanger != null)
		{
			screenMaterialChanger.ChangeState(Defines.Animator.StartUp);
		}
		await UniTask.Delay(2000, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		if (screenMaterialChanger != null)
		{
			screenMaterialChanger.ChangeState(Defines.Animator.On);
		}
		SetScreenInteraction(enable: true);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		_startUpCancellationToken.Cancel();
	}
}
