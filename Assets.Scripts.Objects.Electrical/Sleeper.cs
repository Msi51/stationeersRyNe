using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class Sleeper : OccupantAtmospherics, ILifeSuspender
{
	[SerializeField]
	private Transform _exitPoint;

	[SerializeField]
	private Transform _cameraPoint;

	private Transform _cameraRig;

	private Vector3 ExitPosition
	{
		get
		{
			if (!_exitPoint)
			{
				return Transform.position + Transform.forward * 1.5f;
			}
			return _exitPoint.position;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			bool flag = base.IsInputValid && !base.IsUnsafeAtmosphere;
			if (Error == 1)
			{
				if (!flag)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (flag)
			{
				return true;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			return false;
		}
	}

	public bool IsSuspendingLife => Powered;

	public override CanConstructInfo CanConstruct()
	{
		if (HasFrameBelow())
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, new VolumeLitres(800.0), 0L);
		}
	}

	public override Vector3 GetExitPosition(Entity entity)
	{
		return ExitPosition;
	}

	public override Transform GetCameraPoint(Entity entity)
	{
		return _cameraPoint;
	}

	public override void AssessError()
	{
		bool flag = !base.IsInputValid || base.IsUnsafeAtmosphere;
		OutputNetwork = InputNetwork;
		if (GameManager.RunSimulation && HasErrorState && Error == 0 && flag)
		{
			if (Error != 1)
			{
				OnServer.Interact(base.InteractError, 1);
			}
		}
		else if (GameManager.RunSimulation && HasErrorState && Error == 1 && !flag && Error != 0)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	protected override void CheckConnections()
	{
		if (InputConnection != null && InputConnection.Parent == null)
		{
			List<PipeNetwork> connectedPipeNetworks = ConnectedPipeNetworks;
			InputNetwork = ((connectedPipeNetworks != null && connectedPipeNetworks.Count > 0) ? ConnectedPipeNetworks[0] : null);
		}
		else
		{
			InputNetwork = InputConnection?.GetINetworkedPipe()?.PipeNetwork;
			InputNetwork2 = InputConnection2?.GetINetworkedPipe()?.PipeNetwork;
			OutputNetwork = OutputConnection?.GetINetworkedPipe()?.PipeNetwork;
			OutputNetwork2 = OutputConnection2?.GetINetworkedPipe()?.PipeNetwork;
		}
		AssessError();
	}
}
