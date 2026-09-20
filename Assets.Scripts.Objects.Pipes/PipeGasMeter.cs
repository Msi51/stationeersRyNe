using Assets.Scripts.Atmospherics;
using Networks;
using Objects.Rockets;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class PipeGasMeter : DevicePipeMounted, IRocketInternals, IRocketComponent
{
	[Header("Pipe Meter")]
	[Tooltip("Affects how fast the needle will move towards the current pressure")]
	public float LerpSpeed = 2f;

	[Tooltip("Minimum degrees rotation on local Y")]
	public float NeedleMinimum;

	[Tooltip("Maximum degrees rotation on local Y")]
	public float NeedleMaximum;

	[Tooltip("The needle (required)")]
	public GameObject Needle;

	private Transform _needleTransform;

	private float _lastAngle;

	private PressurekPa _pressureRating;

	private float _needleRotation;

	public PressurekPa PressureSetting
	{
		get
		{
			if (base.SmallCell?.Pipe != null)
			{
				return base.SmallCell.Pipe.MaxPressure;
			}
			return Chemistry.Limits.MAXPressureGasPipe;
		}
	}

	public new RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public new bool StrictlyInternal => false;

	public new RocketNetwork RocketNetwork { get; set; }

	public override void Awake()
	{
		base.Awake();
		_needleTransform = Needle.transform;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		if (!IsValidPipe())
		{
			return base.GetPassiveTooltip(hitCollider);
		}
		result.Title = DisplayName;
		result.Extended = AtmosphericsManager.DisplayBasicAtmosphere(base.NetworkAtmosphere);
		return result;
	}

	public override void OnThreadUpdate()
	{
		_pressureRating = (HasReadableAtmosphere ? (base.NetworkAtmosphere.PressureGassesAndLiquids / PressureSetting) : PressurekPa.Zero);
		if (_pressureRating.IsNaN())
		{
			_pressureRating = PressurekPa.Zero;
		}
		_needleRotation = Mathf.Lerp(NeedleMinimum, NeedleMaximum, _pressureRating.ToFloat());
	}

	public override void UpdateEachFrame()
	{
		if (!WorldManager.IsGamePaused)
		{
			base.UpdateEachFrame();
			if (!IsOccluded && IsValidPipe())
			{
				_lastAngle = Mathf.Lerp(_lastAngle, _needleRotation, Time.deltaTime * LerpSpeed);
				_needleTransform.localRotation = Quaternion.AngleAxis(_lastAngle, Vector3.up);
			}
		}
	}

	public new void OnLaunch(bool immediate = false)
	{
	}

	public new void OnLanded(bool immediate = false)
	{
	}
}
