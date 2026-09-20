using System.Collections.Generic;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class TriggerPlate : LogicInputBase, IDoorControl
{
	public Collider TriggerCollider;

	public HashSet<DynamicThing> TriggerThings = new HashSet<DynamicThing>();

	private static readonly int PressurePlateOnHash = Animator.StringToHash("PressurePlateOn");

	private new static readonly Vector3 SoundOffset = new Vector3(0f, 0f, 0f);

	public List<Motherboard> LinkedMotherboards = new List<Motherboard>();

	public override double Setting
	{
		get
		{
			return base.Setting;
		}
		set
		{
			base.Setting = value;
			if (Setting >= 1.0)
			{
				PlayPooledAudioSound(PressurePlateOnHash, Vector3.zero);
			}
		}
	}

	public virtual bool IsTriggered => Setting > 0.0;

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return Setting;
		}
		return base.GetLogicValue(logicType);
	}

	public void HandleEnterTrigger(Collider other)
	{
		if (GameManager.RunSimulation)
		{
			DynamicThing triggerThing = GetTriggerThing(other);
			if ((bool)triggerThing && !TriggerThings.Contains(triggerThing))
			{
				triggerThing.OnParentChanged += CheckState;
				TriggerThings.Add(triggerThing);
				CheckState();
			}
		}
	}

	public void HandleExitTrigger(Collider other)
	{
		if (GameManager.RunSimulation)
		{
			DynamicThing triggerThing = GetTriggerThing(other);
			if ((bool)triggerThing)
			{
				triggerThing.OnParentChanged -= CheckState;
				TriggerThings.Remove(triggerThing);
				CheckState();
			}
		}
	}

	private DynamicThing GetTriggerThing(Collider other)
	{
		if (!GameManager.RunSimulation)
		{
			return null;
		}
		if (other.isTrigger)
		{
			return null;
		}
		if (Thing._colliderLookup.TryGetValue(other, out var value))
		{
			return value as DynamicThing;
		}
		return null;
	}

	private void CheckState()
	{
		TriggerThings.RemoveWhere((DynamicThing thing) => thing == null || thing.ParentSlot != null);
		Setting = TriggerThings.Count;
		SetMotherboards(IsTriggered);
	}

	public void SetMotherboards(bool isTriggered)
	{
		foreach (Motherboard linkedMotherboard in LinkedMotherboards)
		{
			if (linkedMotherboard is Circuitboard circuitboard && circuitboard.ParentComputer.AsDevice().Powered)
			{
				circuitboard.RemoteToggle(isTriggered);
			}
		}
	}

	public override void OnLinkWithBoard(Motherboard motherboard)
	{
		base.OnLinkWithBoard(motherboard);
		LinkedMotherboards.Add(motherboard);
	}

	public override void OnUnlinkWithBoard(Motherboard motherboard)
	{
		base.OnUnlinkWithBoard(motherboard);
		LinkedMotherboards.Remove(motherboard);
	}
}
