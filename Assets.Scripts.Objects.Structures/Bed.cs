using Assets.Scripts.Objects.Electrical;

namespace Assets.Scripts.Objects.Structures;

public class Bed : Seat, ILifeSuspender
{
	public bool IsSuspendingLife => true;
}
