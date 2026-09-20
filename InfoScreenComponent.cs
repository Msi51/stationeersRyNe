using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Effects;
using UnityEngine;

public class InfoScreenComponent : GameBase
{
	[SerializeField]
	private MaterialChanger _materialChanger;

	[SerializeField]
	private Collider infoTrigger;

	public Collider InfoTrigger => infoTrigger;

	public void RefreshState(Device parent)
	{
		_materialChanger.ChangeState(parent.Powered ? Defines.Animator.OnPowered : Defines.Animator.NotPowered);
	}
}
