using ch.sycoforge.Flares;
using UnityEngine;

public class FlareInjection : MonoBehaviour
{
	public EasyFlares Flare;

	private bool renderScreenshot;

	private int frames;

	private string path;

	private const int FLAREFRAMES = 2;

	private void Start()
	{
		path = string.Format("{0}/{1}.png", Application.dataPath, "screenshot");
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.P))
		{
			renderScreenshot = true;
			frames = 0;
			ScreenCapture.CaptureScreenshot(path, 1);
		}
		if (renderScreenshot)
		{
			frames++;
		}
	}

	private void OnGUI()
	{
		if (!WorldManager.IsGamePaused && renderScreenshot && Event.current.type == EventType.Repaint)
		{
			Flare.LateUpdate();
			renderScreenshot = frames < 2;
		}
	}
}
