using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public interface IRobotInput
{
	Slot InputSlot { get; }

	Transform Transform { get; }

	Vector3 Position { get; }

	bool AllowInput { get; }
}
