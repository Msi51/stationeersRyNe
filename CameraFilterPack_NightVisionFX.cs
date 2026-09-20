using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Camera Filter Pack/Night Vision/Night Vision FX")]
public class CameraFilterPack_NightVisionFX : MonoBehaviour
{
	public enum preset
	{
		Night_Vision_Personalized = -1,
		Night_Vision_FX,
		Night_Vision_Classic,
		Night_Vision_Full,
		Night_Vision_Dark,
		Night_Vision_Sharp,
		Night_Vision_BlueSky,
		Night_Vision_Low_Light,
		Night_Vision_Pinky,
		Night_Vision_RedBurn,
		Night_Vision_PurpleShadow,
		Night_Vision_UnderLava_UhOh
	}

	public Shader SCShader;

	public preset Preset;

	private preset PresetMemo;

	private float TimeX = 1f;

	private Vector4 ScreenResolution;

	private Material SCMaterial;

	[Range(0f, 1f)]
	public float OnOff;

	[Range(0f, 2f)]
	public float Greenness = 1f;

	[Range(0f, 1f)]
	public float Vignette = 1f;

	[Range(0f, 1f)]
	public float Vignette_Alpha = 1f;

	[Range(-10f, 10f)]
	public float Distortion = 1f;

	[Range(0f, 1f)]
	public float Noise = 1f;

	[Range(-2f, 1f)]
	public float Intensity = -1f;

	[Range(0f, 2f)]
	public float Light = 1f;

	[Range(0f, 1f)]
	public float Light2 = 1f;

	[Range(0f, 2f)]
	public float Line = 1f;

	[Range(-2f, 2f)]
	public float Color_R;

	[Range(-2f, 2f)]
	public float Color_G;

	[Range(-2f, 2f)]
	public float Color_B;

	[Range(0f, 1f)]
	public float _Binocular_Size = 0.499f;

	[Range(0f, 1f)]
	public float _Binocular_Smooth = 0.113f;

	[Range(0f, 1f)]
	public float _Binocular_Dist = 0.286f;

	private static float[] P1 = new float[12]
	{
		0.757f, 0.098f, 0.458f, -2.49f, 0.559f, -0.298f, 1.202f, 0.515f, 1f, 0f,
		0f, 0f
	};

	private static float[] P2 = new float[12]
	{
		0.2f, 0.202f, 0.68f, -1.49f, 0.084f, -0.019f, 2f, 0.166f, 1.948f, -0.1f,
		0.15f, -0.07f
	};

	private static float[] P3 = new float[12]
	{
		1.45f, 0.292f, 0.112f, -0.07f, 0.111f, -0.077f, 0.071f, 0f, 0.245f, -0.14f,
		0.27f, -1.37f
	};

	private static float[] P4 = new float[12]
	{
		0.779f, 0.185f, 0.706f, 1.21f, 0.24f, 0.138f, 2f, 0.07f, 1.224f, -0.21f,
		-0.34f, 0f
	};

	private static float[] P5 = new float[12]
	{
		0.2f, 0.028f, 0.706f, 1.21f, 0.397f, -0.24f, 2f, 0.298f, 1.224f, -0.08f,
		0.48f, -0.57f
	};

	private static float[] P6 = new float[12]
	{
		0.2f, 0.159f, 0.622f, -2.28f, 0.409f, -0.24f, 0.166f, 0.028f, 2f, -0.08f,
		0.22f, 0.57f
	};

	private static float[] P7 = new float[12]
	{
		2f, 0.054f, 1f, -2.28f, 0.409f, -1f, 2f, 0.187f, 0.241f, 0f,
		1.58f, 0.21f
	};

	private static float[] P8 = new float[12]
	{
		2f, 0.054f, 1f, 1.28f, 0.409f, -1f, 0.41f, 0.656f, 0.427f, 0.95f,
		-0.35f, 1.41f
	};

	private static float[] P9 = new float[12]
	{
		2f, 0.281f, 0.156f, 1.85f, 0.709f, -1f, 0.41f, 0.109f, 0.34f, 0.95f,
		0.36f, -0.14f
	};

	private static float[] P10 = new float[12]
	{
		0.905f, 0.281f, 0.156f, 1.85f, 0.558f, -0.974f, 1.639f, 0.252f, 1.074f, 0.46f,
		0.95f, 0.58f
	};

	private static float[] P11 = new float[12]
	{
		0f, 0.621f, 0.43f, 10f, 0f, 0.02f, 0.02f, 0.1f, 0.47f, 1.39f,
		0f, -2f
	};

	private static float[] PR = new float[12];

	private Material material
	{
		get
		{
			if (SCMaterial == null)
			{
				SCMaterial = new Material(SCShader);
				SCMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return SCMaterial;
		}
	}

	private void Awake()
	{
		SCShader = Shader.Find("CameraFilterPack/NightVisionFX");
		if (!SystemInfo.supportsImageEffects)
		{
			base.enabled = false;
		}
	}

	private void OnRenderImage(RenderTexture sourceTexture, RenderTexture destTexture)
	{
		if (SCShader != null)
		{
			TimeX += Time.deltaTime;
			if (TimeX > 100f)
			{
				TimeX = 0f;
			}
			material.SetFloat("_TimeX", TimeX);
			material.SetFloat("_OnOff", OnOff);
			material.SetFloat("_Greenness", Greenness);
			material.SetFloat("_Vignette", Vignette);
			material.SetFloat("_Vignette_Alpha", Vignette_Alpha);
			material.SetFloat("_Distortion", Distortion);
			material.SetFloat("_Noise", Noise);
			material.SetFloat("_Intensity", Intensity);
			material.SetFloat("_Light", Light);
			material.SetFloat("_Light2", Light2);
			material.SetFloat("_Line", Line);
			material.SetFloat("_Color_R", Color_R);
			material.SetFloat("_Color_G", Color_G);
			material.SetFloat("_Color_B", Color_B);
			material.SetFloat("_Size", _Binocular_Size);
			material.SetFloat("_Dist", _Binocular_Dist);
			material.SetFloat("_Smooth", _Binocular_Smooth);
			material.SetVector("_ScreenResolution", new Vector2(Screen.width, Screen.height));
			Graphics.Blit(sourceTexture, destTexture, material);
		}
		else
		{
			Graphics.Blit(sourceTexture, destTexture);
		}
	}

	private void Update()
	{
		if (PresetMemo != Preset)
		{
			PresetMemo = Preset;
			if (Preset == preset.Night_Vision_FX)
			{
				PR = P1;
			}
			if (Preset == preset.Night_Vision_Classic)
			{
				PR = P2;
			}
			if (Preset == preset.Night_Vision_Full)
			{
				PR = P3;
			}
			if (Preset == preset.Night_Vision_Dark)
			{
				PR = P4;
			}
			if (Preset == preset.Night_Vision_Sharp)
			{
				PR = P5;
			}
			if (Preset == preset.Night_Vision_BlueSky)
			{
				PR = P6;
			}
			if (Preset == preset.Night_Vision_Low_Light)
			{
				PR = P7;
			}
			if (Preset == preset.Night_Vision_Pinky)
			{
				PR = P8;
			}
			if (Preset == preset.Night_Vision_RedBurn)
			{
				PR = P9;
			}
			if (Preset == preset.Night_Vision_PurpleShadow)
			{
				PR = P10;
			}
			if (Preset == preset.Night_Vision_UnderLava_UhOh)
			{
				PR = P11;
				OnOff = 0.803f;
			}
			if (Preset != preset.Night_Vision_Personalized)
			{
				Greenness = PR[0];
				Vignette = PR[1];
				Vignette_Alpha = PR[2];
				Distortion = PR[3];
				Noise = PR[4];
				Intensity = PR[5];
				Light = PR[6];
				Light2 = PR[7];
				Line = PR[8];
				Color_R = PR[9];
				Color_G = PR[10];
				Color_B = PR[11];
			}
		}
	}

	private void OnDisable()
	{
		if ((bool)SCMaterial)
		{
			Object.DestroyImmediate(SCMaterial);
		}
	}
}
