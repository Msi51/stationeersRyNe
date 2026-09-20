using System.Threading;
using Assets.Scripts.Atmospherics;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class ChemLight : StackableLight
{
	private UniTask _lightTask;

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (_lightTask.Status != UniTaskStatus.Pending && OnOff)
		{
			_lightTask = ChemlightOperation();
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.OnOff && OnOff && _lightTask.Status != UniTaskStatus.Pending)
		{
			SetCustomColor(emissive: true);
			_lightTask = ChemlightOperation();
		}
	}

	private async UniTask ChemlightOperation()
	{
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		FlareLight.gameObject.SetActive(value: true);
		if (CustomColor != null)
		{
			FlareLight.color = CustomColor.Light;
		}
		if (GameManager.RunSimulation)
		{
			AtmosphericsManager.Instance.Register(this);
		}
		await UniTask.NextFrame(cancelToken);
		if (cancelToken.IsCancellationRequested)
		{
			return;
		}
		Transform lightTransform = FlareLight.transform;
		while (base.ActualLifetime > 0f)
		{
			if ((object)ThingTransform != null)
			{
				lightTransform.position = base.ThingTransformPosition + ThingTransform.TransformDirection(Vector3.up) * UpOffset + Vector3.up * 0.1f;
			}
			base.ActualLifetime -= Time.deltaTime;
			await UniTask.NextFrame(cancelToken);
			if (cancelToken.IsCancellationRequested)
			{
				return;
			}
		}
		if (GameManager.RunSimulation)
		{
			OnServer.Destroy(this);
		}
	}
}
