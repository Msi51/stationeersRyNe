using System;
using System.Collections.Generic;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using TMPro;
using TraderUI;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Motherboard;

public class MapMotherboardPanel : MonoBehaviour
{
	[Space(20f)]
	public ToggleButton DeepMinablesToggle;

	public ToggleButton TrackablesToggle;

	[Space(20f)]
	[SerializeField]
	private GameObject _mapScreen;

	[Space(20f)]
	[SerializeField]
	private RawImage _mapImage;

	[SerializeField]
	private RawImage _deepMinablesImage;

	[SerializeField]
	private RawImage _mapMask;

	[Space(20f)]
	[SerializeField]
	private RectTransform _mapRoot;

	[Space(20f)]
	[SerializeField]
	private BasicButton _navReset;

	[SerializeField]
	private BasicButton _navLeft;

	[SerializeField]
	private BasicButton _navRight;

	[SerializeField]
	private BasicButton _navUp;

	[SerializeField]
	private BasicButton _navDown;

	[SerializeField]
	private BasicButton _navZoomIn;

	[SerializeField]
	private BasicButton _navZoomOut;

	[Space(20f)]
	[SerializeField]
	private TabWell _tabWell;

	[SerializeField]
	private GameObject _mapTabPanel;

	[SerializeField]
	private TextMeshProUGUI _infoText;

	[Space(20f)]
	[SerializeField]
	private MapMotherboardPanelTrackableIcon _trackablePrefab;

	[SerializeField]
	private Transform _trackableParent;

	private Texture2D _maskTexture;

	private TrackableCollection _trackables;

	public const float SCAN_SPEED_MULTIPLIER = 0.1f;

	public const int MASK_TEX_SIZE = 64;

	private const byte MASK_BLACK = 1;

	private const byte MASK_CLEAR = 0;

	private List<MapMotherboardPanelTrackableIconData> _trackableData = new List<MapMotherboardPanelTrackableIconData>(16);

	public void Initialise()
	{
		_trackables = new TrackableCollection();
		Texture2D texture2D = WorldSetting.Current.Data.TerrainSettings.MiniMapTexture?.Texture;
		if (texture2D != null)
		{
			_mapImage.texture = texture2D;
		}
		Texture2D texture2D2 = WorldSetting.Current.Data.DeepMinablesRegionData?.RegionSet?.TextureReference?.Texture;
		if (texture2D2 != null)
		{
			_deepMinablesImage.texture = texture2D2;
		}
		BasicButton navReset = _navReset;
		navReset.OnClick = (Action)Delegate.Combine(navReset.OnClick, new Action(NavReset));
		BasicButton navLeft = _navLeft;
		navLeft.OnClick = (Action)Delegate.Combine(navLeft.OnClick, new Action(NavLeft));
		BasicButton navRight = _navRight;
		navRight.OnClick = (Action)Delegate.Combine(navRight.OnClick, new Action(NavRight));
		BasicButton navUp = _navUp;
		navUp.OnClick = (Action)Delegate.Combine(navUp.OnClick, new Action(NavUp));
		BasicButton navDown = _navDown;
		navDown.OnClick = (Action)Delegate.Combine(navDown.OnClick, new Action(NavDown));
		BasicButton navZoomIn = _navZoomIn;
		navZoomIn.OnClick = (Action)Delegate.Combine(navZoomIn.OnClick, new Action(ZoomIn));
		BasicButton navZoomOut = _navZoomOut;
		navZoomOut.OnClick = (Action)Delegate.Combine(navZoomOut.OnClick, new Action(ZoomOut));
		_tabWell.TabSelected.AddListener(TabSelected);
		ToggleButton deepMinablesToggle = DeepMinablesToggle;
		deepMinablesToggle.OnClick = (Action)Delegate.Combine(deepMinablesToggle.OnClick, new Action(ToggleDeepMinables));
		ToggleButton trackablesToggle = TrackablesToggle;
		trackablesToggle.OnClick = (Action)Delegate.Combine(trackablesToggle.OnClick, new Action(ToggleTrackables));
		CreateMaskTexture();
	}

	public void CleanUp()
	{
		BasicButton navReset = _navReset;
		navReset.OnClick = (Action)Delegate.Remove(navReset.OnClick, new Action(NavReset));
		BasicButton navLeft = _navLeft;
		navLeft.OnClick = (Action)Delegate.Remove(navLeft.OnClick, new Action(NavLeft));
		BasicButton navRight = _navRight;
		navRight.OnClick = (Action)Delegate.Remove(navRight.OnClick, new Action(NavRight));
		BasicButton navUp = _navUp;
		navUp.OnClick = (Action)Delegate.Remove(navUp.OnClick, new Action(NavUp));
		BasicButton navDown = _navDown;
		navDown.OnClick = (Action)Delegate.Remove(navDown.OnClick, new Action(NavDown));
		BasicButton navZoomIn = _navZoomIn;
		navZoomIn.OnClick = (Action)Delegate.Remove(navZoomIn.OnClick, new Action(ZoomIn));
		BasicButton navZoomOut = _navZoomOut;
		navZoomOut.OnClick = (Action)Delegate.Remove(navZoomOut.OnClick, new Action(ZoomOut));
		_tabWell.TabSelected.RemoveListener(TabSelected);
		ToggleButton deepMinablesToggle = DeepMinablesToggle;
		deepMinablesToggle.OnClick = (Action)Delegate.Remove(deepMinablesToggle.OnClick, new Action(ToggleDeepMinables));
		ToggleButton trackablesToggle = TrackablesToggle;
		trackablesToggle.OnClick = (Action)Delegate.Remove(trackablesToggle.OnClick, new Action(ToggleTrackables));
	}

	public void UpdateTrackables()
	{
		_trackableData.Clear();
		if (TrackablesToggle.On)
		{
			foreach (ITrackable trackable in ITrackable.Trackables)
			{
				if (trackable is Beacon { ParentSlot: null, OnOff: not false, Powered: not false } beacon)
				{
					_trackableData.Add(new MapMotherboardPanelTrackableIconData
					{
						Id = beacon.ReferenceId,
						WorldPosition = beacon.Transform.position,
						Scale = _mapRoot.localScale.x,
						Color = (beacon.CustomColor?.Color ?? Color.green)
					});
				}
				if (trackable is FixedBeacon { OnOff: not false, Powered: not false } fixedBeacon)
				{
					_trackableData.Add(new MapMotherboardPanelTrackableIconData
					{
						Id = fixedBeacon.ReferenceId,
						WorldPosition = fixedBeacon.Transform.position,
						Scale = _mapRoot.localScale.x,
						Color = (fixedBeacon.CustomColor?.Color ?? Color.green)
					});
				}
			}
		}
		_trackables.Update(_trackableData, _trackablePrefab, _trackableParent);
	}

	public void UpdateMask(int x, int y)
	{
		if (x >= 0 && x < 64 && y >= 0 && y < 64)
		{
			_maskTexture.SetPixel(x, y, Color.clear);
			_maskTexture.Apply();
		}
	}

	public Color ReadMask(int x, int y)
	{
		return _maskTexture.GetPixel(x, y);
	}

	public string GetMaskTextureSaveData()
	{
		if (_maskTexture == null)
		{
			return string.Empty;
		}
		byte[] array = new byte[4096];
		int num = 0;
		for (int i = 0; i < 64; i++)
		{
			for (int j = 0; j < 64; j++)
			{
				array[num++] = ((!(_maskTexture.GetPixel(i, j).a < 0.5f)) ? ((byte)1) : ((byte)0));
			}
		}
		return Convert.ToBase64String(NetworkManager.Compress(array));
	}

	public void LoadToggles(bool deepMinables, bool players)
	{
		DeepMinablesToggle.Set(deepMinables);
		TrackablesToggle.Set(players);
		ToggleDeepMinables();
		ToggleTrackables();
	}

	public void LoadMaskTextureSaveData(string data)
	{
		if (!(_maskTexture == null) && !string.IsNullOrWhiteSpace(data))
		{
			byte[] array = NetworkManager.Decompress(Convert.FromBase64String(data));
			for (int i = 0; i < array.Length; i++)
			{
				int x = i / 64;
				int y = i % 64;
				_maskTexture.SetPixel(x, y, (array[i] == 1) ? Color.black : Color.clear);
			}
			_maskTexture.Apply();
		}
	}

	public void SetInfoText(string text)
	{
		_infoText.text = text;
	}

	private void TabSelected(int index)
	{
		if (index == 0)
		{
			_mapTabPanel.SetActive(value: true);
		}
	}

	private void NavReset()
	{
		_mapRoot.localScale = Vector3.one;
		_mapRoot.localPosition = Vector3.zero;
	}

	private void ZoomIn()
	{
		float num = Mathf.Min(_mapRoot.localScale.x + 0.1f, 2f);
		_mapRoot.localScale = Vector3.one * num;
	}

	private void ZoomOut()
	{
		float num = Mathf.Max(_mapRoot.localScale.x - 0.1f, 0.9f);
		_mapRoot.localScale = Vector3.one * num;
	}

	private void NavLeft()
	{
		Nav(1, 0);
	}

	private void NavRight()
	{
		Nav(-1, 0);
	}

	private void NavUp()
	{
		Nav(0, -1);
	}

	private void NavDown()
	{
		Nav(0, 1);
	}

	private void Nav(int x, int y)
	{
		_mapRoot.localPosition += new Vector3(x, y, 0f) * 30f;
	}

	private void ToggleDeepMinables()
	{
		_deepMinablesImage.enabled = DeepMinablesToggle.On;
	}

	private void ToggleTrackables()
	{
	}

	private void CreateMaskTexture()
	{
		int num = 64;
		_maskTexture = new Texture2D(num, num, TextureFormat.Alpha8, mipChain: false);
		_maskTexture.filterMode = FilterMode.Point;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				_maskTexture.SetPixel(j, i, Color.black);
			}
		}
		_maskTexture.Apply();
		_mapMask.texture = _maskTexture;
	}
}
