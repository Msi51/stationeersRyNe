using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Networks;
using Objects.Rockets;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class PortablesConnector : DeviceInputOutput, IPortablesConnector, IFastenedConnector, IReferencable, IEvaluable, IRocketInternals, IRocketComponent
{
	[Header("TankStorage")]
	public Pipe.ContentType ContentType = Pipe.ContentType.Gas;

	[SerializeField]
	private GameObject connectionObject;

	private Pipe.ContentType pipeContentType = Pipe.ContentType.All;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	public Pipe.ContentType PipeContentType => pipeContentType;

	public Slot TankSlot => Slots[0];

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(60f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	private void RefreshConnector()
	{
		bool active = (IsOpen = TankSlot.Contains<IVisuallyConnectable>());
		connectionObject.SetActive(active);
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		RefreshConnector();
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

	public override void SetSlotOccupantTransformData(DynamicThing newChild)
	{
		newChild.SetOnBaseOfSlot();
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (TankSlot.Contains<DynamicGasCanister>(out var occupant))
		{
			occupant.MainCollider.enabled = true;
		}
		return base.AttackWith(attack, doAction);
	}

	public override void OnAtmosphericTick()
	{
		PortableAtmospherics portableAtmospherics = TankSlot.Get<PortableAtmospherics>();
		if ((object)portableAtmospherics != null)
		{
			if (base.IsInputValid)
			{
				AtmosphereHelper.Mix(portableAtmospherics.InternalAtmosphere, InputNetwork.Atmosphere, AtmosphereHelper.MatterState.Gas);
			}
			if (base.IsInput2Valid)
			{
				AtmosphereHelper.Mix(portableAtmospherics.InternalAtmosphere, InputNetwork2.Atmosphere, AtmosphereHelper.MatterState.Liquid);
			}
		}
	}
}
