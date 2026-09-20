using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Trading;

namespace Networks;

public abstract class ReferencableNetwork : IReferencable, IEvaluable
{
	public virtual string DisplayName { get; }

	public ushort NetworkUpdateFlags { get; set; }

	public long ReferenceId { get; set; }

	public bool BeingDestroyed { get; set; }

	protected ReferencableNetwork(long referenceId = 0L)
	{
		ReferencableNetworkHelper.AssignReference(this, referenceId);
	}

	public virtual void PrintDebugInfo(bool verbose = false)
	{
	}

	public virtual void OnAssignedReference()
	{
		ReferencableNetworkHelper.AllNetworks.Add(this);
	}

	protected virtual void OnDeregister()
	{
		ReferencableNetworkHelper.AllNetworks.Remove(this);
	}

	public abstract bool RemoveMember(INetworkMember member);

	public abstract bool IsNetworkValid();

	public abstract void RebuildNetworkClient(INetworkMember member, ReferencableNetwork oldNetwork);

	protected void RegisterRebuildServer(long rebuildingMemberId, long newNetworkId, long oldNetworkId)
	{
		if (NetworkManager.IsServer && NetworkServer.HasClients())
		{
			RebuildReferencableNetworkEvent.NewEvents.Add(new RebuildReferencableNetworkEvent(rebuildingMemberId, newNetworkId, oldNetworkId));
		}
	}
}
public abstract class ReferencableNetwork<TMember>(long referenceId = 0L) : ReferencableNetwork(referenceId) where TMember : class, INetworkMember
{
	public readonly List<TMember> Members = new List<TMember>();

	public virtual bool IsAwaitingEvent => false;

	public virtual bool Add(TMember member)
	{
		lock (Members)
		{
			if (member.Network == this || Members.Contains(member))
			{
				return false;
			}
		}
		if (member.Network != null && !(member.Network is ReferencableNetwork<TMember>))
		{
			ConsoleWindow.PrintError("Can't add '" + member.DisplayName + "' to '" + DisplayName + "' as it already belongs to a different network family: '" + member.Network.DisplayName + "'");
			return false;
		}
		member.Network?.RemoveMember(member);
		member.Network = this;
		lock (Members)
		{
			Members.Add(member);
		}
		OnMemberAdded(member);
		OnNetworkChanged();
		return true;
	}

	public virtual bool Remove(TMember member)
	{
		if (member.Network == null || member.Network != this)
		{
			return false;
		}
		member.Network = null;
		lock (Members)
		{
			Members.Remove(member);
		}
		OnMemberRemoved(member);
		RefreshNetwork();
		return true;
	}

	public sealed override bool RemoveMember(INetworkMember member)
	{
		if (member is TMember member2)
		{
			return Remove(member2);
		}
		return false;
	}

	public void RefreshNetwork()
	{
		lock (Members)
		{
			for (int num = Members.Count - 1; num >= 0; num--)
			{
				if (Members[num] == null)
				{
					Members.RemoveAt(num);
				}
			}
		}
		OnNetworkChanged();
		if (!IsNetworkValid() && GameManager.GameState != GameState.None && !IsAwaitingEvent)
		{
			Referencable.Deregister(this);
			OnDeregister();
		}
	}

	public override bool IsNetworkValid()
	{
		List<TMember> members = Members;
		if (members != null)
		{
			return members.Count > 0;
		}
		return false;
	}

	protected virtual void OnNetworkChanged()
	{
	}

	protected virtual void OnMemberAdded(TMember member)
	{
	}

	protected virtual void OnMemberRemoved(TMember member)
	{
	}

	protected abstract IEnumerable<TMember> GetNeighbours(TMember member);

	protected abstract ReferencableNetwork<TMember> CreateNewNetwork();

	public virtual void RebuildNetworkServer(TMember member)
	{
		if (GameManager.GameState != GameState.None && !base.BeingDestroyed)
		{
			ReferencableNetwork<TMember> referencableNetwork = CreateNewNetwork();
			OnRebuildNetworkCreated(referencableNetwork);
			referencableNetwork.Add(member);
			RegisterRebuildServer(member.ReferenceId, referencableNetwork.ReferenceId, base.ReferenceId);
			referencableNetwork.RebuildNetworkCore(member, this);
		}
	}

	protected virtual void OnRebuildNetworkCreated(ReferencableNetwork<TMember> newNetwork)
	{
	}

	public sealed override void RebuildNetworkClient(INetworkMember member, ReferencableNetwork oldNetwork)
	{
		if (!(member is TMember val))
		{
			ConsoleWindow.PrintError("Can't rebuild '" + DisplayName + "' from '" + member?.DisplayName + "' as it is not a member of this network family");
		}
		else
		{
			Add(val);
			RebuildNetworkCore(val, oldNetwork);
		}
	}

	protected void RebuildNetworkCore(TMember origin, ReferencableNetwork oldNetwork = null)
	{
		Queue<TMember> queue = new Queue<TMember>(GetNeighbours(origin));
		HashSet<TMember> hashSet = new HashSet<TMember>();
		while (queue.Count > 0)
		{
			TMember val = queue.Dequeue();
			if (hashSet.Contains(val) || queue.Contains(val) || val.IsBeingDestroyed)
			{
				continue;
			}
			hashSet.Add(val);
			foreach (TMember neighbour in GetNeighbours(val))
			{
				if (!hashSet.Contains(neighbour))
				{
					queue.Enqueue(neighbour);
				}
			}
			OnRebuildVisit(val, oldNetwork);
			Add(val);
		}
	}

	protected virtual void OnRebuildVisit(TMember current, ReferencableNetwork oldNetwork)
	{
	}

	protected bool MergeMembersFrom(ReferencableNetwork<TMember> oldNetwork)
	{
		if (oldNetwork == null || oldNetwork == this)
		{
			return false;
		}
		for (int num = oldNetwork.Members.Count - 1; num >= 0; num--)
		{
			TMember val = oldNetwork.Members[num];
			if (val != null)
			{
				Add(val);
			}
		}
		if (oldNetwork.Members.Count > 0)
		{
			ConsoleWindow.PrintError("Old network still has members after a merge!");
		}
		oldNetwork.RefreshNetwork();
		return true;
	}
}
