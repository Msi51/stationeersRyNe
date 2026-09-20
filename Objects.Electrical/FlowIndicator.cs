using System;
using Assets.Scripts;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Objects.Electrical;

public class FlowIndicator : MonoBehaviour
{
	[Serializable]
	private class FlowIndicatorAnimState
	{
		public FlowIndicatorState state;

		public Material stateMaterial;
	}

	[Serializable]
	private class FlowStateCollection
	{
		[SerializeField]
		private FlowIndicatorAnimState[] flowAnimStates = Array.Empty<FlowIndicatorAnimState>();

		public Material GetStateMaterial(FlowIndicatorState state)
		{
			FlowIndicatorAnimState[] array = flowAnimStates;
			foreach (FlowIndicatorAnimState flowIndicatorAnimState in array)
			{
				if (state == flowIndicatorAnimState.state)
				{
					return flowIndicatorAnimState.stateMaterial;
				}
			}
			return null;
		}
	}

	private IFlowIndicator _parent;

	private FlowIndicatorState _currentStatus;

	[SerializeField]
	private FlowStateCollection flowStates;

	[SerializeField]
	private MeshRenderer stateRenderer;

	public void Init(IFlowIndicator parent)
	{
		_parent = parent;
		parent.FlowIndicator = this;
		RefreshState(force: true);
	}

	public void RefreshState(bool force = false)
	{
		if (_parent != null && (_parent.FlowIndicatorStatus != _currentStatus || force))
		{
			_currentStatus = _parent.FlowIndicatorStatus;
			Apply().Forget();
		}
	}

	private async UniTaskVoid Apply()
	{
		if (ThreadedManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
		}
		Material stateMaterial = flowStates.GetStateMaterial(_currentStatus);
		stateRenderer.material = stateMaterial;
	}
}
