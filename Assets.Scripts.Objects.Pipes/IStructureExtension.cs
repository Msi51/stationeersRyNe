using Assets.Scripts.GridSystem;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public interface IStructureExtension : IReferencable, IEvaluable
{
	Thing SourcePrefab { get; }

	IExtendableStructure ExtendableParent { get; set; }

	Vector3 Position { get; }

	Quaternion ThingTransformRotation { get; }

	Direction ParentDirection { get; }
}
