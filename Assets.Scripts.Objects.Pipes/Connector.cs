using Assets.Scripts.Atmospherics;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class Connector : Pipe, IPortablesConnector, IFastenedConnector, IReferencable, IEvaluable
{
	[SerializeField]
	private GameObject connectionObject;

	public PortableAtmospherics ConnectedPortableAtmospherics => TankSlot.Get<PortableAtmospherics>();

	public Slot TankSlot => Slots[0];

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(60f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	private void RefreshConnector()
	{
		bool active = (IsOpen = TankSlot.Contains<IVisuallyConnectable>());
		connectionObject.SetActive(active);
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		RefreshConnector();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		RefreshConnector();
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		RefreshConnector();
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (TankSlot.Contains<DynamicGasCanister>(out var occupant))
		{
			occupant.MainCollider.enabled = true;
		}
		return base.AttackWith(attack, doAction);
	}

	public override void SetSlotOccupantTransformData(DynamicThing newChild)
	{
		newChild.SetOnBaseOfSlot();
	}

	public override void OnAtmosphericTick()
	{
		PortableAtmospherics portableAtmospherics = TankSlot.Get<PortableAtmospherics>();
		if ((object)portableAtmospherics != null)
		{
			AtmosphereHelper.Mix(portableAtmospherics.InternalAtmosphere, base.PipeNetwork.Atmosphere, AtmosphereHelper.MatterState.All);
		}
	}
}
