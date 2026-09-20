using Assets.Scripts;
using UnityEngine;
using Util.Splines;

namespace Objects.Electrical;

[DefaultExecutionOrder(-100)]
public class PylonManager : MonoBehaviour
{
	[SerializeField]
	private Material _cableMaterial;

	private static readonly Color LinkValidColor = new Color(0.35f, 0.65f, 1f, 0.9f);

	private static readonly Color LinkInvalidColor = new Color(1f, 0.25f, 0.25f, 0.9f);

	private const int LinkPreviewSegments = 24;

	private void Start()
	{
		PylonHelper.SetCableMaterial(_cableMaterial);
	}

	private void OnRenderObject()
	{
		if ((!CameraController.Instance || !(Camera.current == CameraController.Instance.StormCardCamera)) && PylonHelper.CurrentNode != null && PylonHelper.TryGetLinkPreview(out var start, out var end, out var valid))
		{
			GLLines.SetPass();
			Spline spline = CableRenderData.BuildSpline(start, end);
			GL.Begin(1);
			GL.Color(valid ? LinkValidColor : LinkInvalidColor);
			Vector3 v = spline.GetPosition(0f);
			for (int i = 1; i <= 24; i++)
			{
				Vector3 position = spline.GetPosition((float)i / 24f);
				GL.Vertex(v);
				GL.Vertex(position);
				v = position;
			}
			GL.End();
		}
	}

	private void Update()
	{
		PylonHelper.ValidateLinkState();
	}
}
