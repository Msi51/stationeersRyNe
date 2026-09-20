using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Objects;

public interface IRotatable
{
	Thing GetAsThing { get; }

	RotatableBehaviour RotatableBehaviour { get; set; }

	double Vertical { get; set; }

	double Horizontal { get; set; }

	float RotationTolerance { get; }

	double MaximumVertical { get; }

	double MaximumHorizontal { get; }

	float MovementSpeedHorizontal { get; }

	float MovementSpeedVertical { get; }

	void RunAfterAnimation();

	UniTaskVoid UpdateAnimator();

	bool CanRotate();
}
