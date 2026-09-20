using UnityEngine;

namespace Assets.Scripts.Util;

public class ManagerBase : MonoBehaviour, IManager
{
	public string ProfilerTag => base.gameObject.name;

	public virtual void ManagerStart()
	{
	}

	public virtual void ManagerAwake()
	{
	}

	public virtual void ManagerUpdate()
	{
	}

	public virtual void SlowUpdate()
	{
	}
}
