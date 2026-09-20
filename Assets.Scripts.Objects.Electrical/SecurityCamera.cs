using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class SecurityCamera : Device
{
	[Header("Security Camera")]
	public Camera CameraLens;

	public Material DefaultMaterial;

	[ReadOnly]
	public RenderTexture RenderTexture;

	[ReadOnly]
	public Material RenderMaterial;

	public static Coroutine RenderingCameraManager;

	public static List<SecurityCamera> AllCameras = new List<SecurityCamera>();

	private IEnumerator RenderScreen()
	{
		while (AllCameras.Count > 0)
		{
			int i = AllCameras.Count;
			while (i-- > 0)
			{
				SecurityCamera securityCamera = AllCameras[i];
				if (!securityCamera.IsOccluded)
				{
					securityCamera.CameraLens.Render();
					yield return Yielders.EndOfFrame;
				}
			}
			yield return new WaitForSecondsRealtime(0.1f);
		}
		RenderingCameraManager = null;
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		RenderTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
		CameraLens.targetTexture = RenderTexture;
		AllCameras.Add(this);
		if (RenderingCameraManager == null)
		{
			RenderingCameraManager = StartCoroutine(RenderScreen());
		}
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		AllCameras.Remove(this);
	}
}
