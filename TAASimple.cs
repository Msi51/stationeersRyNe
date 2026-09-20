using UnityEngine;

[ExecuteInEditMode]
public class TAASimple : MonoBehaviour
{
	private enum DilateMode
	{
		None,
		tap5,
		tap9
	}

	private enum ColorMode
	{
		RGB,
		YCoCg
	}

	private enum HistoryMode
	{
		None,
		AABBClamp,
		AABBClip,
		VarianceClip
	}

	private enum SharpenMode
	{
		None,
		PPSV2
	}

	public Shader taaShader;

	private Material taaMaterial;

	[SerializeField]
	[Range(0f, 0.95f)]
	private float JitterScale = 0.7f;

	[SerializeField]
	private DilateMode _dilateMode = DilateMode.tap5;

	[SerializeField]
	private ColorMode _colorMode = ColorMode.YCoCg;

	[SerializeField]
	private HistoryMode _historyMode = HistoryMode.AABBClip;

	[SerializeField]
	private SharpenMode _sharpenMode;

	[SerializeField]
	[Range(0f, 1f)]
	private float _sharpness = 0.25f;

	private Camera m_Camera;

	private int FrameCount;

	private Vector2 _Jitter;

	private bool m_ResetHistory = true;

	private RenderTexture[] m_HistoryTextures = new RenderTexture[2];

	private Vector2[] HaltonSequence = new Vector2[8]
	{
		new Vector2(0.5f, 1f / 3f),
		new Vector2(0.25f, 2f / 3f),
		new Vector2(0.75f, 1f / 9f),
		new Vector2(0.125f, 4f / 9f),
		new Vector2(0.625f, 7f / 9f),
		new Vector2(0.375f, 2f / 9f),
		new Vector2(0.875f, 5f / 9f),
		new Vector2(0.0625f, 8f / 9f)
	};

	public Material material
	{
		get
		{
			if (taaMaterial == null)
			{
				if (taaShader == null)
				{
					return null;
				}
				taaMaterial = new Material(taaShader);
			}
			return taaMaterial;
		}
	}

	public Camera camera
	{
		get
		{
			if (m_Camera == null)
			{
				m_Camera = GetComponent<Camera>();
			}
			return m_Camera;
		}
	}

	private void OnEnable()
	{
		camera.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
		camera.useJitteredProjectionMatrixForTransparentRendering = true;
	}

	private void OnPreCull()
	{
		Matrix4x4 projectionMatrix = camera.projectionMatrix;
		camera.nonJitteredProjectionMatrix = projectionMatrix;
		FrameCount++;
		int num = FrameCount % 8;
		_Jitter = new Vector2(2f * (HaltonSequence[num].x - 0.5f) / (float)camera.pixelWidth, 2f * (HaltonSequence[num].y - 0.5f) / (float)camera.pixelHeight);
		_Jitter *= JitterScale;
		projectionMatrix.m02 += _Jitter.x;
		projectionMatrix.m12 += _Jitter.y;
		camera.projectionMatrix = projectionMatrix;
	}

	private void OnPostRender()
	{
		camera.ResetProjectionMatrix();
	}

	private void OnRenderImage(RenderTexture source, RenderTexture dest)
	{
		RenderTexture renderTexture = m_HistoryTextures[FrameCount % 2];
		if (renderTexture == null || renderTexture.width != Screen.width || renderTexture.height != Screen.height)
		{
			if ((bool)renderTexture)
			{
				RenderTexture.ReleaseTemporary(renderTexture);
			}
			renderTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBHalf);
			m_HistoryTextures[FrameCount % 2] = renderTexture;
			m_ResetHistory = true;
		}
		RenderTexture renderTexture2 = m_HistoryTextures[(FrameCount + 1) % 2];
		if (renderTexture2 == null || renderTexture2.width != Screen.width || renderTexture2.height != Screen.height)
		{
			if ((bool)renderTexture2)
			{
				RenderTexture.ReleaseTemporary(renderTexture2);
			}
			renderTexture2 = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBHalf);
			m_HistoryTextures[(FrameCount + 1) % 2] = renderTexture2;
		}
		material.SetVector("_Jitter", _Jitter);
		material.SetTexture("_HistoryTex", renderTexture);
		material.SetInt("_IgnoreHistory", m_ResetHistory ? 1 : 0);
		material.SetInt("_DilateMode", (int)_dilateMode);
		material.SetInt("_ColorMode", (int)_colorMode);
		material.SetInt("_HistoryMode", (int)_historyMode);
		material.SetInt("_SharpMode", (int)_sharpenMode);
		material.SetFloat("_Sharpness", _sharpness);
		Graphics.Blit(source, renderTexture2, material, 0);
		Graphics.Blit(renderTexture2, dest);
		m_ResetHistory = false;
	}
}
