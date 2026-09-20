using UnityEngine;

public class SaturnGUI : MonoBehaviour
{
	public GameObject[] planets;

	private bool dispHelp;

	private bool hideGui;

	private GameObject displayedPlanet;

	private void Start()
	{
		dispHelp = false;
		hideGui = false;
		displayedPlanet = Object.Instantiate(planets[0], base.transform.position, planets[0].transform.rotation);
		displayedPlanet.transform.parent = base.transform;
	}

	private void Update()
	{
		if (Input.GetKeyUp(KeyCode.F1))
		{
			dispHelp = !dispHelp;
		}
		if (Input.GetKeyUp(KeyCode.F2))
		{
			hideGui = !hideGui;
		}
	}

	private void OnGUI()
	{
		if (hideGui)
		{
			return;
		}
		GUILayout.BeginArea(new Rect(Screen.width - 180, 20f, 150f, 500f));
		GUILayout.BeginVertical();
		GUILayout.Label("Press F2 to hide GUI");
		for (int i = 0; i < planets.Length; i++)
		{
			if (GUILayout.Button(planets[i].name))
			{
				Object.Destroy(displayedPlanet);
				displayedPlanet = Object.Instantiate(planets[i], base.transform.position, planets[i].transform.rotation);
				displayedPlanet.transform.parent = base.transform;
			}
		}
		GUILayout.EndVertical();
		GUILayout.EndArea();
		GUILayout.BeginArea(new Rect(30f, 20f, 250f, 500f));
		GUILayout.BeginVertical();
		if (dispHelp)
		{
			GUILayout.Label("Press F1 to hide help\n");
			GUILayout.Label("CAMERA FLYBY COMMANDS :");
			GUILayout.Label("Space: Move forward");
			GUILayout.Label("CC Move backward");
			GUILayout.Label("Up arrow: Move upward");
			GUILayout.Label("Down arrow: Move downward");
			GUILayout.Label("Left arrow: Move Left");
			GUILayout.Label("Right arrow: Move Right");
			GUILayout.Label("Q: Turn left");
			GUILayout.Label("D: Turn right");
			GUILayout.Label("Z: Turn up");
			GUILayout.Label("S: Turn down");
			GUILayout.Label("A: Rotate left");
			GUILayout.Label("E: Rotate right");
			GUILayout.Label("Del: Rotate Sun left");
			GUILayout.Label("Page Down: Rotate Sun right");
			GUILayout.Label("Home: Rotate Sun top");
			GUILayout.Label("End: Rotate Sun bottom");
			GUILayout.Label("Mouse scroll wheel: Zoom-In/Zoom-Out");
			GUILayout.Label("Left Click: Rotate Sun");
			GUILayout.Label("Right click: Rotate Earth");
		}
		else
		{
			GUILayout.Label("Press F1 to show help");
		}
		GUILayout.EndVertical();
		GUILayout.EndArea();
	}
}
