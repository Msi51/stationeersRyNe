using Assets.Scripts.Util;
using Assets.Scripts.Voxel;
using Cysharp.Threading.Tasks;
using TerrainSystem;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class OreScanner : Cartridge
{
	public enum ScannerType
	{
		Basic,
		Advanced
	}

	public static Color32[] OreColors = new Color32[29]
	{
		Color.black,
		Color.black,
		new Color32(byte.MaxValue, 0, byte.MaxValue, byte.MaxValue),
		new Color32(100, 150, byte.MaxValue, byte.MaxValue),
		new Color32(byte.MaxValue, byte.MaxValue, 0, byte.MaxValue),
		new Color32(150, 150, 150, byte.MaxValue),
		new Color32(byte.MaxValue, 128, 0, byte.MaxValue),
		new Color32(0, byte.MaxValue, 0, byte.MaxValue),
		new Color32(byte.MaxValue, 128, 0, byte.MaxValue),
		new Color32(128, 128, 0, byte.MaxValue),
		new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue),
		new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue),
		new Color32(0, byte.MaxValue, byte.MaxValue, byte.MaxValue),
		new Color32(byte.MaxValue, 0, 0, byte.MaxValue),
		new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue),
		new Color32(128, 128, byte.MaxValue, byte.MaxValue),
		new Color32(0, byte.MaxValue, 200, byte.MaxValue),
		new Color32(128, 64, 0, byte.MaxValue),
		new Color32(128, 128, byte.MaxValue, byte.MaxValue),
		new Color32(byte.MaxValue, 128, 128, byte.MaxValue),
		new Color32(128, byte.MaxValue, 128, byte.MaxValue),
		new Color32(byte.MaxValue, byte.MaxValue, 128, byte.MaxValue),
		new Color32(byte.MaxValue, 128, byte.MaxValue, byte.MaxValue),
		new Color32(128, byte.MaxValue, byte.MaxValue, byte.MaxValue),
		new Color32(64, 64, 64, byte.MaxValue),
		new Color32(128, 64, 64, byte.MaxValue),
		new Color32(64, 128, 128, byte.MaxValue),
		new Color32(128, 128, 64, byte.MaxValue),
		new Color32(64, 64, 128, byte.MaxValue)
	};

	private static readonly int SCANNER_INPUT = Shader.PropertyToID("_ScannerInput");

	private static readonly int TABLET_SCREEN_DIRECTION = Shader.PropertyToID("_TabletScreenDirection");

	private static readonly int WORLD_ORIGIN = Shader.PropertyToID("_WorldOrigin");

	private static readonly int TABLET_POSITION = Shader.PropertyToID("_TabletPosition");

	private static readonly int SCANNER_INPUT_OFFSET = Shader.PropertyToID("_ScannerInputOffset");

	private static readonly int SCANNER_TYPE = Shader.PropertyToID("_ScannerType");

	private static readonly int SCAN_SIZE = Shader.PropertyToID("_ScanSize");

	private const int RESOLUTION = 20;

	private const int HALF_RESOLUTION = 10;

	private bool _screenEnabled;

	public ScannerType Type;

	public Material RenderMaterial;

	public GameObject ErrorTextObject;

	public GameObject ScreenObject;

	private Texture3D LookupTexture;

	private bool _runningUpdateScan;

	public Vector3 ScanPosition => base.Position.GridCenter();

	public Color32[] ScanData { get; private set; }

	public Vector3 Offset => new Vector3(10f, 10f, 10f);

	public override void Awake()
	{
		base.Awake();
		if (!IsCursor)
		{
			Initialize();
		}
	}

	private void Initialize()
	{
		ScanData = new Color32[8000];
		if (LookupTexture == null)
		{
			LookupTexture = new Texture3D(20, 20, 20, TextureFormat.ARGB32, mipChain: false)
			{
				wrapMode = TextureWrapMode.Clamp
			};
		}
		UpdateLookupTable();
		RenderMaterial.SetTexture(SCANNER_INPUT, LookupTexture);
		RenderMaterial.SetFloat(SCAN_SIZE, 20f);
		RenderMaterial.SetVector(SCANNER_INPUT_OFFSET, ScanPosition);
		RenderMaterial.SetInt(SCANNER_TYPE, (int)Type);
		UpdateScan().Forget();
	}

	public async UniTaskVoid UpdateScan()
	{
		_runningUpdateScan = true;
		while (Tablet != null && Tablet.ParentSlot != null && Tablet.IsOperable && Tablet.OnOff)
		{
			UpdateLookupTable();
			await UniTask.Delay(1000);
			RenderMaterial.SetVector(SCANNER_INPUT_OFFSET, ScanPosition);
		}
		_runningUpdateScan = false;
	}

	public override void OnScreenUpdate()
	{
		base.OnScreenUpdate();
		UpdateCachedPosition();
		EnableScreen(enable: true);
		if (!_runningUpdateScan)
		{
			UpdateScan().Forget();
		}
		RenderMaterial.SetVector(TABLET_POSITION, base.transform.position);
		RenderMaterial.SetVector(WORLD_ORIGIN, Vector3.zero);
		RenderMaterial.SetVector(TABLET_SCREEN_DIRECTION, base.transform.right);
	}

	private void EnableScreen(bool enable)
	{
		if (_screenEnabled != enable)
		{
			ScreenObject.SetActive(enable);
			ErrorTextObject.SetActive(!enable);
			_screenEnabled = enable;
		}
	}

	private void UpdateLookupTable()
	{
		LookupTexture.SetPixels32(ScanData);
		LookupTexture.Apply();
		RenderMaterial.SetTexture(SCANNER_INPUT, LookupTexture);
	}

	public override void OnThreadUpdate()
	{
		base.OnThreadUpdate();
		CollectData();
	}

	public void CollectData()
	{
		if (GameManager.IsBatchMode || Tablet == null || !Tablet.OnOff || !Tablet.Powered)
		{
			return;
		}
		Vector3 zero = Vector3.zero;
		for (int i = 1; i < 19; i++)
		{
			for (int j = 1; j < 19; j++)
			{
				for (int k = 1; k < 19; k++)
				{
					zero.x = k;
					zero.y = j;
					zero.z = i;
					MinableType a = Vein.GetVeinAtPosition(ScanPosition + zero - Offset)?.Type ?? MinableType.None;
					Color32 color = OreColors[Mathf.Min((int)a, OreColors.Length - 1)];
					ScanData[i * 20 * 20 + j * 20 + k] = color;
				}
			}
		}
	}
}
