using UnityEngine;

public abstract class BinaryTransformAnimComponent : BinaryAnimComponent
{
	[SerializeField]
	protected AnimKeyFrameCollection state0;

	[SerializeField]
	protected AnimKeyFrameCollection state1;

	protected override IKeyFrameCollection State0 => state0;

	protected override IKeyFrameCollection State1 => state1;
}
