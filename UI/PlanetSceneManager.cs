using Assets.Scripts;
using Assets.Scripts.UI;
using UnityEngine;

namespace UI;

public class PlanetSceneManager : GameBase
{
	public static PlanetSceneManager Instance;

	[SerializeField]
	private PlanetScene _planetScenePrefab;

	private void Awake()
	{
		Instance = this;
	}

	public PlanetScene Create(WorldSetting worldSetting)
	{
		PlanetScene planetScene = Object.Instantiate(_planetScenePrefab, Transform);
		planetScene.name = "PlanetScene" + worldSetting.Id;
		foreach (PlanetPrefab prefab in worldSetting.PreviewScene.Prefabs)
		{
			if ((object)prefab?.GameObject != null)
			{
				GameObject gameObject = Object.Instantiate(prefab.GameObject, planetScene.Transform);
				gameObject.name = prefab.Name;
				gameObject.gameObject.transform.position = prefab.Position;
				gameObject.gameObject.transform.rotation = Quaternion.Euler(prefab.Rotation);
				gameObject.gameObject.transform.localScale = prefab.Scale;
				planetScene.Prefabs.Add(gameObject);
			}
		}
		GameObject gameObject2 = Object.Instantiate(worldSetting.PreviewScene.Sun, planetScene.Transform);
		gameObject2.name = worldSetting.PreviewScene.Sun.name;
		planetScene.Sun = gameObject2.GetComponent<Light>();
		planetScene.Sun.cullingMask = CursorManager.Instance.PlanetarySunMask;
		planetScene.Sun.intensity = 1f;
		worldSetting.PreviewScene.Apply(planetScene.Sun);
		planetScene.PreviewScene = worldSetting.PreviewScene;
		planetScene.SetVisible(isVisble: false);
		return planetScene;
	}
}
