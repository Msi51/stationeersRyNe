using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Pipes;
using Cysharp.Threading.Tasks;
using Trading;

namespace Networks;

public interface INetworkedAtmospherics : INetworkedStructure, INetworkMember, IReferencable, IEvaluable
{
	List<WorldGrid> CurrentGrids { get; set; }

	float EnergyConvected { get; set; }

	float EnergyRadiated { get; set; }

	VolumeLitres Volume { get; }

	Pipe.ContentType PipeContentType { get; }

	PipeBurst IsBurst { get; }

	bool HasOpenGrid { get; }

	bool Stressed { get; set; }

	PressurekPa MaxPressure { get; }

	PipeBurst DamageRecord { get; set; }

	void OnImGuiDraw();

	UniTaskVoid BurstPipe(PipeBurst damageSource);
}
