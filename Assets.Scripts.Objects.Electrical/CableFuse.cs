using System.Collections;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Electrical;

public class CableFuse : DeviceCableMounted
{
	public float PowerBreak = 4000f;

	public override void AddToNetwork()
	{
		base.AddToNetwork();
		if (base.CableNetwork != null)
		{
			lock (base.CableNetwork.FuseList)
			{
				base.CableNetwork.FuseList.Add(this);
			}
		}
	}

	public override void RemoveFromNetwork()
	{
		base.RemoveFromNetwork();
		if (RegisteredNetwork != null)
		{
			lock (RegisteredNetwork.FuseList)
			{
				RegisteredNetwork.FuseList.Remove(this);
			}
		}
	}

	private IEnumerator WaitThenBreak()
	{
		Break();
		yield break;
	}

	public void Break()
	{
		if (ThreadedManager.IsThread)
		{
			UnityMainThreadDispatcher.Instance().Enqueue(WaitThenBreak());
			return;
		}
		if (base.SmallCell != null && base.SmallCell.Cable != null)
		{
			base.SmallCell.Cable.Break();
		}
		OnServer.Destroy(this);
	}
}
