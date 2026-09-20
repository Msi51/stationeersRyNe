using Trading;

public interface IReferencable : IEvaluable
{
	const uint INVALID = 0u;

	string DisplayName { get; }

	ushort NetworkUpdateFlags { get; set; }

	long ReferenceId { get; set; }

	bool BeingDestroyed { get; set; }

	void PrintDebugInfo(bool verbose = false);

	void OnAssignedReference();
}
