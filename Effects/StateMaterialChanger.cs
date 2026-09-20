using Assets.Scripts.Objects;
using UnityEngine;
using UnityEngine.Serialization;

namespace Effects;

public class StateMaterialChanger : MonoBehaviour, IAnimComponent
{
	[FormerlySerializedAs("_parent")]
	[SerializeField]
	public Thing Parent;

	[FormerlySerializedAs("_renderer")]
	[FormerlySerializedAs("_meshRenderer")]
	[SerializeField]
	public MeshRenderer Renderer;

	public virtual void RefreshState(bool skipAnimation)
	{
	}
}
