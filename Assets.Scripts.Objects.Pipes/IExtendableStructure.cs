using System.Collections.Generic;
using Trading;

namespace Assets.Scripts.Objects.Pipes;

public interface IExtendableStructure : IReferencable, IEvaluable
{
	List<IStructureExtension> Extensions { get; }

	List<StructureExtensionInfo> CompatibleExtensions { get; }

	bool FullyExtended { get; }

	void Extend(IStructureExtension extension);

	bool CanExtend(IStructureExtension extension);

	void RemoveExtension(IStructureExtension extension);
}
