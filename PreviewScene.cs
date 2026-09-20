using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using ch.sycoforge.Flares;
using UnityEngine;

[XmlRoot]
public class PreviewScene
{
	[XmlElement("CameraPosition")]
	public Vector3 CameraPosition;

	[XmlElement("CameraRotation")]
	public Vector3 CameraRotation;

	[XmlElement("SunRotation")]
	public Vector3 SunRotation;

	[XmlElement("SunPrefab")]
	public string SunPrefab;

	[XmlElement("AtmosphericScattering")]
	public bool EnableAtmosphericScattering;

	[XmlElement("LensFlare")]
	public bool LensFlareEnabled;

	[XmlElement("LensFlareIntensity")]
	public float LensFlareIntensity;

	[XmlElement("SkyBoxMaterial")]
	public string SkyBoxMaterial = "Starfield Skybox";

	[XmlAttribute("fov")]
	public float FieldOfView = 38f;

	[XmlArray("Prefabs")]
	[XmlArrayItem("Prefab", Type = typeof(PlanetPrefab))]
	public List<PlanetPrefab> Prefabs = new List<PlanetPrefab>();

	[XmlIgnore]
	public GameObject Sun;

	[XmlIgnore]
	public EasyFlares SunEasyFlares;

	[XmlIgnore]
	public Material SkyBoxMat;

	public static List<GameObject> SceneSelectionPlanets = new List<GameObject>();

	public PreviewScene()
	{
	}

	public PreviewScene(PreviewScene previewScene)
	{
		CameraPosition = previewScene.CameraPosition;
		CameraRotation = previewScene.CameraRotation;
		FieldOfView = previewScene.FieldOfView;
		SunRotation = previewScene.SunRotation;
		Prefabs = new List<PlanetPrefab>(previewScene.Prefabs);
		EnableAtmosphericScattering = previewScene.EnableAtmosphericScattering;
		SkyBoxMat = Resources.Load<Material>("WorldEnvironment/SkyBoxes/" + previewScene.SkyBoxMaterial);
		LensFlareEnabled = previewScene.LensFlareEnabled;
		LensFlareIntensity = previewScene.LensFlareIntensity;
	}

	public void Apply(Light sun)
	{
		sun.transform.rotation = Quaternion.Euler(SunRotation);
		Sun = sun.gameObject;
		if ((bool)Sun)
		{
			SunEasyFlares = Sun.GetComponent<EasyFlares>();
		}
		if ((bool)SunEasyFlares && LensFlareEnabled && CursorManager.DefaultEasyFlareEnabled)
		{
			SunEasyFlares.enabled = LensFlareEnabled;
			SunEasyFlares.Opacity = LensFlareIntensity;
		}
	}
}
