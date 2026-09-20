using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class GasPipeVent : PassiveVent
{
	[SerializeField]
	private PipeValveAnimComponent _pipeValveAnimComponent;

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (_pipeValveAnimComponent != null)
		{
			_pipeValveAnimComponent.RefreshState(skipAnimation);
		}
	}

	public override void OnAtmosphericTick()
	{
		if (OnOff)
		{
			base.OnAtmosphericTick();
		}
	}
}
