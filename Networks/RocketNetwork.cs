using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using Objects.Rockets;
using UnityEngine;

namespace Networks;

public class RocketNetwork : StructureNetwork, ISmallGridOwner
{
	public static readonly List<RocketNetwork> AllRocketNetworks = new List<RocketNetwork>();

	private readonly Dictionary<Grid3, RocketOccupiedCell> _smallGridsOccupied = new Dictionary<Grid3, RocketOccupiedCell>();

	private float _structureMass;

	private float _gasMasskg;

	public CrewModule CrewModule { get; private set; }

	public List<IRocketEngine> Engines { get; } = new List<IRocketEngine>(16);

	public List<Battery> Batteries { get; } = new List<Battery>(16);

	public List<IRocketMiner> RocketMiners { get; } = new List<IRocketMiner>(16);

	public List<RocketScanner> RocketScanners { get; } = new List<RocketScanner>(16);

	public List<RocketPayloadBay> RocketPayloadBays { get; } = new List<RocketPayloadBay>(16);

	public List<IRocketInternals> Internals { get; } = new List<IRocketInternals>(128);

	public HashSet<Atmosphere> RocketAtmospheres { get; } = new HashSet<Atmosphere>(16);

	public List<IUmbilical> RocketUmbilicals { get; } = new List<IUmbilical>(16);

	public float GasMass => _gasMasskg;

	public float DryMass => _structureMass;

	public float CargoMass => 0f;

	public static long[] AllNetworkIds => ReferencableNetworkHelper.GetValidNetworkIds(AllRocketNetworks);

	public override StructureNetworkType NetworkType => StructureNetworkType.Rocket;

	public RocketSize RocketSize { get; private set; }

	public EngineFuselage Anchor { get; private set; }

	public bool IsDeregistered { get; private set; }

	public Rocket Rocket { get; set; }

	public float CombinedMass()
	{
		return _structureMass + _gasMasskg + CargoMass;
	}

	public void CalculateGasMass()
	{
		double num = 0.0;
		foreach (Atmosphere rocketAtmosphere in RocketAtmospheres)
		{
			num += (double)rocketAtmosphere.GasMixture.TotalMassGassesAndLiquidsGrams();
		}
		_gasMasskg = (float)(num / 1000.0);
	}

	public RocketNetwork(long referenceId = 0L)
		: base(referenceId)
	{
		if (GameManager.RunSimulation && GameManager.GameState != GameState.Loading)
		{
			Rocket = new Rocket(this, 0L);
		}
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		AllRocketNetworks.Add(this);
	}

	protected override void OnDeregister()
	{
		base.OnDeregister();
		IsDeregistered = true;
		AllRocketNetworks.Remove(this);
		if (GameManager.RunSimulation)
		{
			Rocket?.DeRegister();
		}
	}

	protected override void OnClear()
	{
		Internals.Clear();
		Engines.Clear();
		Batteries.Clear();
		Rocket?.Clear();
		RocketMiners?.Clear();
		RocketScanners?.Clear();
		RocketPayloadBays?.Clear();
		RocketUmbilicals?.Clear();
	}

	public override bool Add(INetworkedStructure iNetworkedStructure)
	{
		INetworkedRocketPart part = iNetworkedStructure as INetworkedRocketPart;
		AddSmallCellOwnership(part);
		if (iNetworkedStructure is EngineFuselage anchor)
		{
			Anchor = anchor;
		}
		return base.Add(iNetworkedStructure);
	}

	public void AddSmallCellOwnership(INetworkedRocketPart part)
	{
		if (part == null)
		{
			return;
		}
		foreach (RocketInternalCellOffset internalCellOffset in part.InternalCellOffsets)
		{
			Grid3 internalCell = GetInternalCell(part, internalCellOffset.Offset);
			if (_smallGridsOccupied.TryGetValue(internalCell, out var value))
			{
				value.Overlapping = true;
				continue;
			}
			GridController.World.AddSmallCellOwner(internalCell, this);
			_smallGridsOccupied.Add(internalCell, new RocketOccupiedCell
			{
				CellType = internalCellOffset.CellType
			});
		}
	}

	public override bool Remove(INetworkedStructure iNetworkedStructure)
	{
		INetworkedRocketPart part = iNetworkedStructure as INetworkedRocketPart;
		RemoveSmallCellOwnerShip(part);
		return base.Remove(iNetworkedStructure);
	}

	public void RemoveSmallCellOwnerShip(INetworkedRocketPart part)
	{
		if (part == null)
		{
			return;
		}
		foreach (RocketInternalCellOffset internalCellOffset in part.InternalCellOffsets)
		{
			Grid3 internalCell = GetInternalCell(part, internalCellOffset.Offset);
			if (_smallGridsOccupied.TryGetValue(internalCell, out var value))
			{
				if (value.Overlapping)
				{
					_smallGridsOccupied[internalCell].Overlapping = false;
					continue;
				}
				GridController.World.RemoveSmallCellOwner(internalCell, this);
				_smallGridsOccupied.Remove(internalCell);
			}
		}
	}

	protected override ReferencableNetwork<INetworkedStructure> CreateNewNetwork()
	{
		return new RocketNetwork(0L);
	}

	protected override void OnRebuildNetworkCreated(ReferencableNetwork<INetworkedStructure> newNetwork)
	{
		if (newNetwork is RocketNetwork rocketNetwork && Rocket != null && !string.IsNullOrEmpty(Rocket.CustomName))
		{
			rocketNetwork.Rocket.CustomName = Rocket.CustomName;
			rocketNetwork.Rocket.AutomatedLanding = Rocket.AutomatedLanding;
			rocketNetwork.Rocket.ReEntryProfile = Rocket.ReEntryProfile;
			rocketNetwork.Rocket.AutomatedShutOff = Rocket.AutomatedShutOff;
			rocketNetwork.Rocket.RocketMode = Rocket.RocketMode;
		}
	}

	private Grid3 GetInternalCell(INetworkedStructure part, Vector3 offset)
	{
		return new Grid3(part.GetAsThing.Transform.position + offset * 0.5f);
	}

	public void DestroyAllInternalsAndStructures()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		base.BeingDestroyed = true;
		for (int num = Internals.Count - 1; num >= 0; num--)
		{
			if (Internals[num] is Thing thing)
			{
				KillOccupants(thing);
			}
		}
		for (int num2 = base.StructureList.Count - 1; num2 >= 0; num2--)
		{
			if (base.StructureList[num2] is Thing thing2)
			{
				KillOccupants(thing2);
			}
		}
		for (int num3 = Internals.Count - 1; num3 >= 0; num3--)
		{
			if (Internals[num3] is Thing thing3)
			{
				OnServer.Destroy(thing3);
			}
			else
			{
				Internals.RemoveAt(num3);
			}
		}
		for (int num4 = base.StructureList.Count - 1; num4 >= 0; num4--)
		{
			if (base.StructureList[num4] is Thing thing4)
			{
				OnServer.Destroy(thing4);
			}
			else
			{
				Internals.RemoveAt(num4);
			}
		}
	}

	private static void KillOccupants(Thing thing)
	{
		foreach (Slot slot in thing.Slots)
		{
			if (slot.Occupant is Entity entity)
			{
				entity.MoveToWorld();
				entity.DamageState?.Damage(ChangeDamageType.Increment, 999f, DamageUpdateType.Brute);
			}
		}
	}

	private void RegenerateCollections()
	{
		Engines?.Clear();
		Batteries?.Clear();
		RocketScanners?.Clear();
		RocketMiners?.Clear();
		RocketPayloadBays?.Clear();
		RocketUmbilicals?.Clear();
		EnsureAnchor();
		foreach (Grid3 key in _smallGridsOccupied.Keys)
		{
			SmallCell smallCell = GridController.World.GetSmallCell(key);
			if (smallCell != null)
			{
				TryAdopt(smallCell.Cable);
				TryAdopt(smallCell.Chute);
				TryAdopt(smallCell.Pipe);
				TryAdopt(smallCell.Device as IRocketInternals);
			}
		}
		RocketAtmospheres.Clear();
		for (int num = Internals.Count - 1; num >= 0; num--)
		{
			IRocketInternals rocketInternals = Internals[num];
			if ((rocketInternals == null || rocketInternals is Thing { IsBeingDestroyed: not false }) ? true : false)
			{
				Internals.RemoveAt(num);
			}
			else
			{
				if (!(rocketInternals is IRocketEngine item))
				{
					if (!(rocketInternals is Battery item2))
					{
						if (!(rocketInternals is IRocketMiner item3))
						{
							if (!(rocketInternals is RocketScanner item4))
							{
								if (!(rocketInternals is RocketPayloadBay item5))
								{
									if (rocketInternals is IUmbilical item6)
									{
										RocketUmbilicals.Add(item6);
									}
								}
								else
								{
									RocketPayloadBays.Add(item5);
								}
							}
							else
							{
								RocketScanners.Add(item4);
							}
						}
						else
						{
							RocketMiners.Add(item3);
						}
					}
					else
					{
						Batteries.Add(item2);
					}
				}
				else
				{
					Engines.Add(item);
				}
				if (rocketInternals is Thing { InternalAtmosphere: not null } thing2)
				{
					RocketAtmospheres.Add(thing2.InternalAtmosphere);
				}
				if (rocketInternals is Pipe { PipeNetwork: { Atmosphere: not null } } pipe)
				{
					RocketAtmospheres.Add(pipe.PipeNetwork.Atmosphere);
				}
			}
		}
		RecalculateStructureMass();
	}

	private void EnsureAnchor()
	{
		if (Anchor != null && !Anchor.IsBeingDestroyed && Anchor.RocketNetwork == this)
		{
			return;
		}
		Anchor = null;
		for (int i = 0; i < base.StructureList.Count; i++)
		{
			if (base.StructureList[i] is EngineFuselage anchor)
			{
				Anchor = anchor;
				break;
			}
		}
	}

	private void TryAdopt(IRocketInternals rocketInternals)
	{
		if (rocketInternals != null && !Internals.Contains(rocketInternals))
		{
			RocketNetwork rocketNetwork = rocketInternals.RocketNetwork;
			if (rocketNetwork == null || rocketNetwork == this || rocketNetwork.IsDeregistered)
			{
				bool repoint = rocketNetwork != null && rocketNetwork != this;
				AddInternal(rocketInternals);
				SetRocketData(rocketInternals, repoint);
			}
		}
	}

	private void SetRocketData(IRocketInternals rocketInternals, bool repoint)
	{
		if (!(Anchor == null) && rocketInternals is Structure structure)
		{
			if (structure.RocketData != null && !repoint)
			{
				structure.RocketData.Network = this;
			}
			else
			{
				structure.RocketData = new RocketData(this, structure.ThingTransformPosition - Anchor.ThingTransformPosition);
			}
		}
	}

	public void RecalculateStructureMass()
	{
		float num = 0f;
		for (int num2 = Internals.Count - 1; num2 >= 0; num2--)
		{
			if (Internals[num2] is IRocketMassContributor rocketMassContributor)
			{
				num += rocketMassContributor.MassContribution;
			}
		}
		for (int num3 = base.StructureList.Count - 1; num3 >= 0; num3--)
		{
			if (base.StructureList[num3] != null && base.StructureList[num3] is IRocketMassContributor rocketMassContributor2)
			{
				num += rocketMassContributor2.MassContribution;
			}
			if (base.StructureList[num3] != null && base.StructureList[num3] is EngineFuselage engineFuselage)
			{
				RocketSize = engineFuselage.RocketSize;
			}
		}
		_structureMass = num;
	}

	private void AddInternal(IRocketInternals rocketInternals)
	{
		rocketInternals.RocketNetwork = this;
		Internals.Add(rocketInternals);
	}

	public void RemoveInternal(IRocketInternals rocketInternals)
	{
		if (rocketInternals?.RocketNetwork == this)
		{
			rocketInternals.RocketNetwork = null;
		}
		Internals.Remove(rocketInternals);
	}

	public void AdoptFromLoad(IRocketInternals rocketInternals)
	{
		if (rocketInternals != null && !Internals.Contains(rocketInternals))
		{
			AddInternal(rocketInternals);
		}
	}

	public void AttachAll()
	{
		lock (base.StructureList)
		{
			for (int num = base.StructureList.Count - 1; num >= 0; num--)
			{
				INetworkedStructure networkedStructure = base.StructureList[num];
				if (networkedStructure != null)
				{
					if (networkedStructure is Structure structure)
					{
						structure.AttachToGrid();
					}
					AddSmallCellOwnership(networkedStructure as INetworkedRocketPart);
				}
			}
			for (int num2 = Internals.Count - 1; num2 >= 0; num2--)
			{
				if (Internals[num2] is Structure structure2)
				{
					structure2.AttachToGrid();
				}
			}
		}
	}

	public void DetatchAll()
	{
		lock (base.StructureList)
		{
			for (int num = base.StructureList.Count - 1; num >= 0; num--)
			{
				INetworkedStructure networkedStructure = base.StructureList[num];
				if (networkedStructure != null)
				{
					Structure structure = networkedStructure as Structure;
					if (structure != null)
					{
						structure.DetatchFromGrid();
					}
					RemoveSmallCellOwnerShip((INetworkedRocketPart)networkedStructure);
				}
			}
			for (int num2 = Internals.Count - 1; num2 >= 0; num2--)
			{
				IRocketInternals rocketInternals = Internals[num2];
				if (rocketInternals != null)
				{
					Structure structure2 = rocketInternals as Structure;
					if (structure2 != null)
					{
						structure2.DetatchFromGrid();
					}
				}
			}
		}
	}

	public void RebuildAllGridState()
	{
		foreach (INetworkedStructure structure3 in base.StructureList)
		{
			Structure structure = structure3 as Structure;
			if (structure != null)
			{
				structure.RebuildGridState();
			}
		}
		foreach (IRocketInternals @internal in Internals)
		{
			Structure structure2 = @internal as Structure;
			if (structure2 != null)
			{
				structure2.RebuildGridState();
			}
		}
	}

	protected override void OnNetworkChanged()
	{
		base.OnNetworkChanged();
		if (GameManager.GameState == GameState.Running)
		{
			RefreshRocket();
		}
	}

	public void RefreshRocket()
	{
		Rocket rocket = Rocket;
		if (rocket != null && rocket.BeingDestroyed)
		{
			return;
		}
		if (Rocket == null && GameManager.RunSimulation)
		{
			Rocket = new Rocket(this, 0L);
		}
		CrewModule = null;
		foreach (INetworkedStructure structure in base.StructureList)
		{
			if (structure is CrewModule crewModule)
			{
				CrewModule = crewModule;
			}
		}
		RegenerateCollections();
		FindLaunchPad();
		if (Rocket != null)
		{
			Rocket.MapIconDirty = true;
		}
	}

	protected override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (Rocket == null && GameManager.RunSimulation)
		{
			Rocket = new Rocket(this, 0L);
		}
		RefreshRocket();
		WarnIfDecoupled();
	}

	private void WarnIfDecoupled()
	{
		if (GameManager.RunSimulation && !base.BeingDestroyed && Internals.Count <= 0 && base.StructureList.Count != 0)
		{
			RocketRelinkPlan rocketRelinkPlan = RocketRelinker.Compute(this);
			if (rocketRelinkPlan.IsReady)
			{
				ConsoleWindow.PrintError($"Rocket '{Rocket?.DisplayName}' #{StringManager.Get(base.ReferenceId)} appears decoupled: {rocketRelinkPlan.MoveSet.Count} stranded structure(s) found ({rocketRelinkPlan.AnchorsMatched}/{rocketRelinkPlan.AnchorCount} anchors matched). Run 'rocket relink {StringManager.Get(base.ReferenceId)}' to recover.", suppressStacktrace: true);
			}
		}
	}

	public void FindLaunchPad()
	{
		if (!GameManager.RunSimulation || Rocket == null || base.StructureList.Count == 0 || Rocket.CurrentNode != null)
		{
			return;
		}
		Structure structure = base.StructureList[0] as Structure;
		WorldGrid worldGrid = new WorldGrid(structure.ThingTransformPosition);
		for (int i = 1; i < 10; i++)
		{
			Grid3 worldGrid2 = worldGrid.Value + Grid3.Down * i;
			if (GridController.World.Get<Structure>(worldGrid2, StructureElement.Center) is ISpaceMapNodeOwner spaceMapNodeOwner)
			{
				Rocket.SetCurrentNode(spaceMapNodeOwner.SpaceMapNode);
				Rocket.RocketState = RocketState.OnLaunchMount;
				Rocket.SetRocketSizeToLaunchMount(spaceMapNodeOwner);
				ConsoleWindow.Print(Rocket.DisplayName + " Snapped to " + spaceMapNodeOwner.DisplayName);
				break;
			}
		}
	}

	public override void OnImguiDraw()
	{
		if (base.StructureList.Count == 0 || InventoryManager.ParentHuman == null)
		{
			return;
		}
		foreach (INetworkedStructure structure in base.StructureList)
		{
			if (structure is INetworkedRocketPart networkedRocketPart)
			{
				networkedRocketPart.OnImGuiDraw();
			}
		}
		foreach (KeyValuePair<Grid3, RocketOccupiedCell> item in _smallGridsOccupied)
		{
			SmallCell smallCell = GridController.World.GetSmallCell(item.Key);
			if (smallCell == null)
			{
				continue;
			}
			if (smallCell.Cable != null)
			{
				ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.LightRedTransparent;
			}
			else if (smallCell.Chute != null)
			{
				ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.DarkGrey;
			}
			else if (smallCell.Pipe != null)
			{
				ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.BlueTransparent;
			}
			else if (smallCell.Device != null)
			{
				ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.GreenTransparent;
				if (smallCell.Device is IRocketEngine)
				{
					ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.Yellow;
				}
			}
			else
			{
				ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.WhiteTransparent;
			}
			if (item.Value.CellType != RocketInternalCellType.None)
			{
				ImGuiExtensions.Rendering.DrawCube(item.Key.ToVector3(), RocketGrid.SmallGridSquare);
			}
		}
	}

	public bool IsCollision(SmallGrid gridObject, Grid3 grid)
	{
		if (!_smallGridsOccupied.TryGetValue(grid, out var value))
		{
			return false;
		}
		if (!(gridObject is IRocketInternals rocketInternals))
		{
			return true;
		}
		if (rocketInternals.InternalCellType == RocketInternalCellType.None)
		{
			return true;
		}
		return (value.CellType & rocketInternals.InternalCellType) != rocketInternals.InternalCellType;
	}

	public void OnGridPlaced(SmallGrid sg)
	{
		if (GameManager.GameState == GameState.Running)
		{
			RefreshRocket();
		}
	}

	public void OnGridUpdated(SmallGrid sg)
	{
		RefreshRocket();
	}

	public void OnGridRemoved(SmallGrid sg)
	{
		RefreshRocket();
	}

	public void UpdateStructurePositions()
	{
		bool flag = Anchor != null;
		Vector3 vector = (flag ? Anchor.ThingTransformPosition : Vector3.zero);
		for (int num = Internals.Count - 1; num >= 0; num--)
		{
			IRocketInternals rocketInternals = Internals[num];
			if ((rocketInternals != null && !(rocketInternals is Thing { IsBeingDestroyed: not false })) || 1 == 0)
			{
				if (flag && rocketInternals is Structure { RocketData: { } rocketData })
				{
					rocketInternals.Transform.position = vector + rocketData.Offset;
				}
				rocketInternals.Position = rocketInternals.ThingTransformPosition;
				rocketInternals.WorldGrid = new WorldGrid(rocketInternals.Position);
			}
		}
		for (int num2 = base.StructureList.Count - 1; num2 >= 0; num2--)
		{
			INetworkedStructure networkedStructure = base.StructureList[num2];
			if ((networkedStructure != null && !(networkedStructure is Thing { IsBeingDestroyed: not false })) || 1 == 0)
			{
				networkedStructure.GetAsThing.Position = networkedStructure.GetAsThing.ThingTransformPosition;
				networkedStructure.GetAsThing.WorldGrid = new WorldGrid(networkedStructure.GetAsThing.Position);
			}
		}
		RocketState rocketState = Rocket.RocketState;
		if (rocketState != RocketState.OnLaunchMount && rocketState != RocketState.InSpace)
		{
			return;
		}
		foreach (IRocketInternals @internal in Internals)
		{
			if (@internal is IWorkingAtmosphere workingAtmosphere)
			{
				workingAtmosphere.CacheWorkingAtmosphere();
			}
		}
	}
}
