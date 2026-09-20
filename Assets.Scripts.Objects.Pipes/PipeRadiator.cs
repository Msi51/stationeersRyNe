using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Objects.Rockets;

namespace Assets.Scripts.Objects.Pipes;

public class PipeRadiator : DevicePipeMounted, IThermal, IWorkingAtmosphere
{
	private CrewModule _crewModule;

	private Atmosphere _worldAtmosphere;

	public override Atmosphere ThermalAtmosphere => base.NetworkAtmosphere;

	public override float ConvectionFactor => 1f;

	public override float RadiationFactor => 0.75f;

	public Atmosphere GetWorkingAtmosphere()
	{
		if (_crewModule != null)
		{
			return _crewModule.InternalAtmosphere;
		}
		return base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
	}

	public void CacheWorkingAtmosphere()
	{
		_crewModule = (Cell.IsInCrewModule(base.WorldGrid, out var crewModule) ? crewModule : null);
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		LocalGrid = base.GridController.WorldToLocalGrid(base.ThingTransformPosition + ThingTransform.up * 0.5f);
		_crewModule = (Cell.IsInCrewModule(base.WorldGrid, out var crewModule) ? crewModule : null);
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (base.NetworkAtmosphere != null && base.NetworkAtmosphere.IsValid() && base.HasOpenGrid)
		{
			MoleEnergy moleEnergy = MoleEnergy.Zero;
			MoleEnergy zero = MoleEnergy.Zero;
			Atmosphere worldAtmosphere = ((_crewModule != null) ? _crewModule.InternalAtmosphere : base.GridController.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid));
			MoleEnergy moleEnergy2 = AtmosphereHelper.CalculateThingConvection(this, worldAtmosphere, base.NetworkAtmosphere);
			zero = moleEnergy2;
			MoleEnergy moleEnergy3 = AtmosphereHelper.CalculateThingEntropy(this, worldAtmosphere, base.NetworkAtmosphere);
			AtmosphereHelper.DoConvection(base.NetworkAtmosphere, worldAtmosphere, moleEnergy2, base.WorldGrid);
			MoleEnergy moleEnergy4 = moleEnergy3 - ((moleEnergy2 > MoleEnergy.Zero) ? moleEnergy2 : MoleEnergy.Zero);
			if (moleEnergy4 > MoleEnergy.Zero)
			{
				AtmosphereHelper.DoEntropy(base.NetworkAtmosphere, moleEnergy4);
				moleEnergy = moleEnergy4;
			}
			EnergyConvected = zero.ToFloat();
			EnergyRadiated = moleEnergy.ToFloat();
		}
	}
}
