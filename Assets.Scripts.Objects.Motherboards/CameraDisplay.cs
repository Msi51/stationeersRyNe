using System.Collections.Generic;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Motherboards;

public class CameraDisplay : Circuitboard
{
	[Header("Camera Display")]
	public RawImage ViewingScreen;

	[ReadOnly]
	public HashSet<SecurityCamera> SecurityCameras = new HashSet<SecurityCamera>();

	[ReadOnly]
	public SecurityCamera CurrentCamera;

	public override bool IsError => SecurityCameras.Count == 0;

	public override bool CanDeviceLink(Device device)
	{
		if (!(typeof(SecurityCamera) == device.GetType()))
		{
			return device.GetType().IsSubclassOf(typeof(SecurityCamera));
		}
		return true;
	}

	public override void SetDisplayName()
	{
		if ((bool)TitleText)
		{
			TitleText.text = (CurrentCamera ? CurrentCamera.DisplayName.ToUpper() : "NO FEED");
		}
	}

	public override void OnDeviceListChanged()
	{
		base.OnDeviceListChanged();
		SecurityCameras.Clear();
		foreach (Device linkedDevice in base.LinkedDevices)
		{
			SecurityCamera securityCamera = linkedDevice as SecurityCamera;
			if ((bool)securityCamera && IsDeviceConnected(securityCamera))
			{
				SecurityCameras.Add(securityCamera);
			}
		}
		SetCurrentCamera();
	}

	public void SetCurrentCamera()
	{
		if (SecurityCameras.Count > 0)
		{
			ViewingScreen.enabled = true;
			SecurityCamera currentCamera = CurrentCamera;
			CurrentCamera = SecurityCameras.First();
			if (CurrentCamera.HasAuthority)
			{
				OnServer.Interact(CurrentCamera, InteractableType.Mode, 1);
				if ((bool)currentCamera)
				{
					OnServer.Interact(currentCamera, InteractableType.Mode, 0);
				}
			}
			ViewingScreen.texture = CurrentCamera.RenderTexture;
		}
		else
		{
			ViewingScreen.enabled = false;
			CurrentCamera = null;
		}
		SetDisplayName();
	}

	public override void OnDeviceListChanged(Device device)
	{
		base.OnDeviceListChanged(device);
		SecurityCamera securityCamera = device as SecurityCamera;
		if ((bool)securityCamera && IsDeviceConnected(securityCamera))
		{
			SecurityCameras.Add(securityCamera);
		}
		else if ((bool)securityCamera)
		{
			SecurityCameras.Remove(securityCamera);
		}
		SetCurrentCamera();
	}
}
