using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Rendering;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LogicDisplay : LogicUnitBase
{
	public enum DisplayMode
	{
		Default,
		Percent,
		Power,
		Kelvin,
		Celsius,
		Meters,
		Credits,
		Seconds,
		Minutes,
		Days,
		String,
		Fahrenheit,
		Litres,
		Mol,
		Pa,
		Newtons,
		Degrees
	}

	public struct DigitGlyph
	{
		public Mesh Mesh;

		public Vector3 LocalOffset;
	}

	[Header("Logic Display")]
	public Transform Screen;

	public Transform ScreenUp;

	public Transform DigitTransform;

	public MeshRenderer ScreenMesh;

	public Material DigitOn;

	public int StandardDigitMax = 99999;

	public int ExpDigits = 2;

	public float MaxPixelWidth;

	public static string[] DisplayModeStrings = Enum.GetNames(typeof(DisplayMode));

	private int _maxPixels;

	private Material _materialOn;

	private readonly DensePoolReference<LogicDisplay> _digitDisplayPool = new DensePoolReference<LogicDisplay>(LogicDisplayDigitRenderer.ActiveDisplays);

	private float _lastNumber;

	private bool _isTurnedOn;

	private readonly List<DigitGlyph> _digitGlyphs = new List<DigitGlyph>();

	private float _maxPercent;

	private char[] _displayString;

	public override string[] ModeStrings => DisplayModeStrings;

	public IReadOnlyList<DigitGlyph> DigitGlyphs => _digitGlyphs;

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		if (GameManager.RunSimulation && GameManager.GameState == GameState.Running)
		{
			OnServer.Interact(base.InteractColor, GameManager.GetColorIndex(DigitOn));
			RenderText().Forget();
		}
		if (!GameManager.IsBatchMode && PlacementType == PlacementSnap.FaceMount && ScreenUp != null)
		{
			float num = Vector3.Dot(Screen.transform.up, Vector3.up);
			float num2 = Vector3.Dot(-Screen.transform.up, Vector3.up);
			float num3 = Vector3.Dot(Screen.transform.right, Vector3.up);
			float num4 = Vector3.Dot(-Screen.transform.right, Vector3.up);
			float num5 = Mathf.Max(num, num2, num3, num4);
			if (num3 != num5 && num4 != num5 && num2 == num5)
			{
				ScreenUp.transform.Rotate(Vector3.forward, 180f, Space.Self);
			}
		}
	}

	public override void Awake()
	{
		base.Awake();
		_maxPercent = (float)StandardDigitMax * 0.1f - 1f;
		_maxPixels = (int)(MaxPixelWidth / GameManager.DigitPixel);
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		SetDigitMaterial(ColorState);
	}

	public override bool OnAddToPool(object densePool, int slot)
	{
		if (_digitDisplayPool.CanAddToPool(densePool))
		{
			return _digitDisplayPool.AddToPool(densePool, slot);
		}
		return base.OnAddToPool(densePool, slot);
	}

	public override void OnRemoveFromPool(object densePool)
	{
		base.OnRemoveFromPool(densePool);
		_digitDisplayPool.OnRemovedFrom(densePool);
	}

	public override void OnStartRender()
	{
		base.OnStartRender();
		if (!GameManager.IsBatchMode && !IsCursor)
		{
			LogicDisplayDigitRenderer.ActiveDisplays.Add(this);
		}
	}

	public override void OnStopRender()
	{
		base.OnStopRender();
		LogicDisplayDigitRenderer.ActiveDisplays.Remove(this);
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			LogicDisplayDigitRenderer.ActiveDisplays.Remove(this);
			base.OnDestroy();
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		RenderText().Forget();
	}

	public override void OnSettingChanged()
	{
		base.OnSettingChanged();
		RenderText().Forget();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Powered || interactable.Action == InteractableType.OnOff)
		{
			bool flag = interactable.State == 1 && OnOff && Powered;
			if (_isTurnedOn == flag && RocketMath.Approximately(_lastNumber, Setting))
			{
				return;
			}
			_isTurnedOn = flag;
			RenderText().Forget();
		}
		if (interactable.Action == InteractableType.Mode)
		{
			RenderText().Forget();
		}
		if (interactable.Action == InteractableType.Color)
		{
			SetDigitMaterial(interactable.State);
		}
	}

	private void SetDigitMaterial(int index)
	{
		ColorSwatch colorSwatch = GameManager.GetColorSwatch(index);
		if (colorSwatch != null && !(colorSwatch.Emissive == null))
		{
			DigitOn = colorSwatch.Emissive;
			RenderText().Forget();
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return Setting;
		}
		return base.GetLogicValue(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		if (logicType == LogicType.Setting)
		{
			Setting = value;
		}
	}

	public void SetDisplay(char[] displayString)
	{
		_digitGlyphs.Clear();
		if (!OnOff || !Powered || displayString == null || GameManager.GameState == GameState.None || IsCursor)
		{
			return;
		}
		float num = 0f;
		int num2 = 0;
		int num3 = displayString.Length;
		while (num3-- > 0)
		{
			char c = displayString[num3];
			if (c == '\0')
			{
				continue;
			}
			if (c == ',')
			{
				c = '.';
			}
			MeshDigit digitMesh = GameManager.GetDigitMesh(c);
			if (digitMesh != null)
			{
				num += GameManager.DigitPixel;
				if (digitMesh.DigitMesh != null)
				{
					_digitGlyphs.Add(new DigitGlyph
					{
						Mesh = digitMesh.DigitMesh,
						LocalOffset = new Vector3(num, 0f, 0.001f)
					});
				}
				num += (float)digitMesh.PixelWidth * GameManager.DigitPixel;
				num2 += digitMesh.PixelWidth + 1;
			}
		}
		float x = (float)Mathf.FloorToInt((float)(_maxPixels - num2) * 0.5f) * GameManager.DigitPixel;
		DigitTransform.localPosition = new Vector3(x, 0f, 0f);
	}

	private async UniTaskVoid RenderText()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		while (GameManager.GameState != GameState.Running)
		{
			await UniTask.NextFrame(cancelToken);
		}
		if (!cancelToken.IsCancellationRequested)
		{
			_displayString = (DisplayMode)Mode switch
			{
				DisplayMode.Percent => new StringBuilder().Append((Setting * 100.0).Clamp(0f - _maxPercent, _maxPercent).ToString("F0")).Append("%").ToString()
					.ToCharArray(), 
				DisplayMode.Power => Setting.ToStringPrefix("W").ToCharArray(), 
				DisplayMode.Kelvin => Setting.ToStringPrefix("K").ToCharArray(), 
				DisplayMode.Celsius => Setting.ToStringPrefix("°C").ToCharArray(), 
				DisplayMode.Fahrenheit => Setting.ToStringPrefix("°F").ToCharArray(), 
				DisplayMode.Meters => Setting.ToStringPrefix("m").ToCharArray(), 
				DisplayMode.Credits => Setting.ToStringPrefix("€").ToCharArray(), 
				DisplayMode.Seconds => Setting.ToStringSimple("sec").ToCharArray(), 
				DisplayMode.Minutes => Setting.ToStringSimple("min").ToCharArray(), 
				DisplayMode.Days => Setting.ToStringSimple("days").ToCharArray(), 
				DisplayMode.Litres => Setting.ToStringPrefix("L").ToCharArray(), 
				DisplayMode.Mol => Setting.ToStringPrefix("mol").ToCharArray(), 
				DisplayMode.Pa => Setting.ToStringPrefix("Pa").ToCharArray(), 
				DisplayMode.Newtons => Setting.ToStringPrefix("N").ToCharArray(), 
				DisplayMode.Degrees => Setting.ToStringPrefix("°").ToCharArray(), 
				DisplayMode.String => ProgrammableChip.UnpackAscii6(Setting, signed: true).ToCharArray(), 
				_ => Setting.ToStringDisplay(StandardDigitMax, ExpDigits).ToCharArray(), 
			};
			SetDisplay(_displayString);
		}
	}
}
