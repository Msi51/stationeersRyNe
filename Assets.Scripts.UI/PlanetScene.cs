using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.UI;

public class PlanetScene : GameBase
{
	public Light Sun;

	public PreviewScene PreviewScene = new PreviewScene();

	public List<GameObject> Prefabs = new List<GameObject>();

	public override void SetVisible(bool isVisble)
	{
		if (!GameManager.IsBatchMode)
		{
			base.SetVisible(isVisble);
			if (isVisble)
			{
				HandleCamera();
			}
		}
	}

	private void HandleCamera()
	{
		if (!GameManager.IsBatchMode)
		{
			Camera currentCamera = CameraController.CurrentCamera;
			CursorManager.SetAtmosphericScattering(PreviewScene.EnableAtmosphericScattering);
			CameraController.SetFieldOfView(PreviewScene.FieldOfView);
			currentCamera.transform.SetPositionAndRotation(PreviewScene.CameraPosition, Quaternion.Euler(PreviewScene.CameraRotation));
			SkyBoxController.SetPosition(PreviewScene.CameraPosition);
			RenderSettings.skybox = PreviewScene.SkyBoxMat;
			RenderSettings.ambientMode = AmbientMode.Trilight;
		}
	}
}
