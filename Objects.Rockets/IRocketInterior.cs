namespace Objects.Rockets;

public interface IRocketInterior : IRocketInternals, IRocketComponent
{
	CrewModule CrewModule { get; }
}
