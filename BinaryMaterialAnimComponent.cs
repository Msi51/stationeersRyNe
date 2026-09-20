using UnityEngine;

public abstract class BinaryMaterialAnimComponent : BinaryAnimComponent
{
	[SerializeField]
	protected MaterialKeyFrameCollection state0;

	[SerializeField]
	protected MaterialKeyFrameCollection state1;

	protected override IKeyFrameCollection State0 => state0;

	protected override IKeyFrameCollection State1 => state1;
}
