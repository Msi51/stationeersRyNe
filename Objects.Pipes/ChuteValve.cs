using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace Objects.Pipes;

public class ChuteValve : Chute
{
	[SerializeField]
	private GameObject _lever;

	[SerializeField]
	private GameObject _shutter;

	private Connection _outputConnection;

	private static readonly Vector3 _openRotation = new Vector3(0f, 0f, 0f);

	private static readonly Vector3 _closedRotation = new Vector3(0f, 0f, 90f);

	private static readonly Vector3 _openShutterScale = new Vector3(1f, 0.015f, 1f);

	private static readonly Vector3 _closedShutterScale = new Vector3(1f, 1f, 1f);

	private static readonly float _openCloseTime = 0.4f;

	public override void Awake()
	{
		base.Awake();
		CacheOutputConnection();
	}

	public override void OnServerTick(float deltaTime)
	{
		if (IsOpen)
		{
			base.OnServerTick(deltaTime);
		}
	}

	public override void RebuildGridState()
	{
		base.RebuildGridState();
		_outputConnection.SetGrids();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Open)
		{
			RefreshNextNeighbour();
			LeanTween.cancel(_lever);
			LeanTween.cancel(_shutter);
			LeanTween.rotateLocal(_lever, IsOpen ? _openRotation : _closedRotation, _openCloseTime);
			LeanTween.scale(_shutter, IsOpen ? _openShutterScale : _closedShutterScale, _openCloseTime);
		}
	}

	private void RefreshNextNeighbour()
	{
		if (_outputConnection != null)
		{
			NextNeighbor = _outputConnection.GetChuteOrDevice();
		}
	}

	private void CacheOutputConnection()
	{
		foreach (Connection openEnd in OpenEnds)
		{
			if (openEnd.ConnectionRole == ConnectionRole.Output)
			{
				_outputConnection = openEnd;
				break;
			}
		}
	}
}
