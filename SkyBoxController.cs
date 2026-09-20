using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Util;
using UnityEngine;

public class SkyBoxController : ManagerBase
{
	public static SkyBoxController Instance;

	public List<Transform> Planets = new List<Transform>();

	[SerializeField]
	public List<StarfieldSkybox> Starfields = new List<StarfieldSkybox>();

	public static GameObject PlanetsParent;

	[SerializeField]
	public OrbitRenderSetting _orbitRenderSetting = OrbitRenderSetting.NamesAndMarkers;

	private Transform _planetTransform;

	private bool _isEclipse;

	private bool? _starsInSpace;

	public static StarData DefaultData;

	public Transform PlanetaryAxis;

	private Vector3 _rotationAxis;

	public static OrbitDrawMode OrbitDrawMode;

	public static OrbitRenderSetting OrbitRenderSetting
	{
		get
		{
			return Instance?._orbitRenderSetting ?? OrbitRenderSetting.NamesOnly;
		}
		set
		{
			if ((object)Instance != null)
			{
				Instance._orbitRenderSetting = value;
			}
		}
	}

	public override void ManagerStart()
	{
		base.ManagerStart();
		if (!GameManager.IsBatchMode)
		{
			Instance = this;
			PlanetsParent = new GameObject("~Planets");
			_planetTransform = PlanetsParent.transform;
		}
	}

	public static void RegisterPlanet(Transform planet)
	{
		if (!Instance.Planets.Contains(planet))
		{
			Instance.Planets.Add(planet);
			planet.parent = Instance._planetTransform;
		}
	}

	public static void ClearPlanets()
	{
		if ((object)Instance == null)
		{
			return;
		}
		for (int num = Instance.Planets.Count - 1; num >= 0; num--)
		{
			Transform transform = Instance.Planets[num];
			Instance.Planets.RemoveAt(num);
			if ((object)transform != null)
			{
				Object.Destroy(transform.gameObject);
			}
		}
		Instance.Planets.Clear();
		if ((object)PlanetsParent != null)
		{
			Object.DestroyImmediate(PlanetsParent);
		}
		PlanetsParent = new GameObject("~Planets");
		Instance._planetTransform = PlanetsParent.transform;
	}

	public static void SetPosition(Vector3 worldPosition)
	{
		if ((object)Instance != null)
		{
			Instance.PlanetaryAxis.position = worldPosition;
			if ((bool)Instance._planetTransform)
			{
				Instance._planetTransform.position = worldPosition;
			}
		}
	}

	private void Update()
	{
		if (GameManager.IsBatchMode || !GameManager.IsRunning || !OrbitalSimulation.IsValid || (object)CursorManager.Instance == null)
		{
			return;
		}
		Vector3 cameraPosition = CameraController.CameraPosition;
		PlanetaryAxis.SetPositionAndRotation(cameraPosition, OrbitalSimulation.System.GetSceneRotation());
		_planetTransform.position = cameraPosition;
		foreach (StarfieldSkybox starfield in Starfields)
		{
			StarfieldSkybox.UpdateInGame(starfield.Material);
		}
		bool flag = !WorldManager.HasGravityAtHeight(InventoryManager.ParentPosition.y);
		if (_starsInSpace != flag)
		{
			_starsInSpace = flag;
			foreach (StarfieldSkybox starfield2 in Starfields)
			{
				StarfieldSkybox.SetHorizonHeight(starfield2.Material, flag ? 10000f : 0f);
			}
		}
		if (_isEclipse != OrbitalSimulation.IsEclipse)
		{
			_isEclipse = OrbitalSimulation.IsEclipse;
		}
	}

	public static void SetStars(WorldSetting worldSetting)
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		if (Instance != null)
		{
			Instance._starsInSpace = null;
		}
		DefaultData = new StarData();
		List<StarData> stars = worldSetting.Stars;
		if (stars == null || stars.Count == 0)
		{
			for (int i = 0; i < Instance.Starfields.Count; i++)
			{
				Instance.Starfields[i].Apply(DefaultData);
			}
			return;
		}
		for (int j = 0; j < Instance.Starfields.Count && j < stars.Count; j++)
		{
			if (j == 0)
			{
				DefaultData = stars[j];
			}
			Instance.Starfields[j].Apply(stars[j]);
		}
	}

	public static void ApplyNewWorldSetting()
	{
		if ((object)Instance == null)
		{
			return;
		}
		Instance._starsInSpace = null;
		foreach (StarfieldSkybox starfield in Instance.Starfields)
		{
			starfield.Initialize();
		}
	}

	public static void ApplyDefaults()
	{
		if ((object)Instance == null)
		{
			return;
		}
		Instance._starsInSpace = null;
		foreach (StarfieldSkybox starfield in Instance.Starfields)
		{
			starfield.SetDefaults();
		}
	}

	public static void SetPlanetaryAxis(Vector3 rotationAxis)
	{
		if ((object)Instance != null)
		{
			Instance._rotationAxis = rotationAxis;
			Instance.PlanetaryAxis.rotation = Quaternion.Euler(rotationAxis);
		}
	}
}
