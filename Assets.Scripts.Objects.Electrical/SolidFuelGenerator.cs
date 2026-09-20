using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Motherboards;
using Reagents;

namespace Assets.Scripts.Objects.Electrical;

public class SolidFuelGenerator : PowerGeneratorSlot
{
	private ReagentMixture _temporaryReagentMixture;

	public override bool CanBeginImport
	{
		get
		{
			if (base.CanBeginImport)
			{
				return OnOff;
			}
			return false;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (!base.GridController.CanContainAtmos(base.WorldGrid))
			{
				if (Error == 0 && Powered && GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 1);
				}
				return false;
			}
			if (Error == 1 && GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			return true;
		}
	}

	public override void OnAllPrefabsLoaded()
	{
		base.OnAllPrefabsLoaded();
		foreach (OreResource resource in Resources)
		{
			resource.Initialize();
		}
	}

	public override void Awake()
	{
		base.Awake();
		_temporaryReagentMixture = new ReagentMixture(this);
	}

	public override CanConstructInfo CanConstruct()
	{
		if (HasFrameBelow())
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
	}

	protected override void OnServerImportTick()
	{
		if (IsNextImportReady)
		{
			TryChuteImport();
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		int num = ((OnOff && IsOperable && PoweredTicks > 0) ? 1 : 0);
		if (Mode != num)
		{
			OnServer.Interact(base.InteractMode, num);
		}
		if (OnOff && IsOperable)
		{
			if (PoweredTicks > 0)
			{
				PoweredTicks--;
			}
			if ((bool)ImportingThing)
			{
				if (Importing == 0)
				{
					OnServer.Interact(base.InteractImport, 1);
				}
				while (PoweredTicks < 5 && !(ImportingThing == null) && IsValidOre())
				{
					SmeltResource();
				}
			}
			else if (Importing == 1)
			{
				OnServer.Interact(base.InteractImport, 0);
			}
		}
		else if (Importing == 1)
		{
			OnServer.Interact(base.InteractImport, 0);
		}
	}

	private void SmeltResource()
	{
		if (base.IsImportClosed)
		{
			Atmosphere localAtmosphere = base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
			PoweredTicks += TicksPerResource();
			ImportingThing?.Smelt(localAtmosphere, _temporaryReagentMixture);
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Mode => false, 
			LogicType.PowerGeneration => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.PowerGeneration)
		{
			return (PoweredTicks > 0 && OnOff) ? PowerGenerated : 0f;
		}
		return base.GetLogicValue(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Mode)
		{
			return false;
		}
		return base.CanLogicWrite(logicType);
	}
}
