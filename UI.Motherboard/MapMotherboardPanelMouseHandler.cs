using System.Collections.Generic;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Util;
using TerrainSystem;
using Trading;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Motherboard;

public class MapMotherboardPanelMouseHandler : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	[SerializeField]
	private RectTransform _rectTransform;

	[SerializeField]
	private MapMotherboardPanel _mapMotherboardPanel;

	private StringBuilder _stringBuilder = new StringBuilder();

	private Vector2 GetLocalMousePosition(Vector2 screenPosition)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, screenPosition, CameraController.CurrentCamera, out var localPoint);
		Vector2 size = _rectTransform.rect.size;
		return new Vector2(localPoint.x / size.x, localPoint.y / size.y);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		_stringBuilder.Clear();
		Vector2 localMousePosition = GetLocalMousePosition(eventData.position);
		int coordsX = Mathf.RoundToInt(localMousePosition.x * (float)VoxelConstants.Size);
		int coordsY = Mathf.RoundToInt(localMousePosition.y * (float)VoxelConstants.Size);
		int x = Mathf.RoundToInt((localMousePosition.x + 0.5f) * 64f);
		int y = Mathf.RoundToInt((localMousePosition.y + 0.5f) * 64f);
		bool isRevealed = _mapMotherboardPanel.ReadMask(x, y).a < 0.5f;
		AppendCoordsInfo(coordsX, coordsY, _stringBuilder);
		_stringBuilder.AppendLine();
		AppendRegionInfo(isRevealed, coordsX, coordsY, _stringBuilder);
		_stringBuilder.AppendLine();
		AppendMinableInfo(isRevealed, coordsX, coordsY, _stringBuilder);
		_mapMotherboardPanel.SetInfoText(_stringBuilder.ToString());
	}

	private void AppendCoordsInfo(int coordsX, int coordsY, StringBuilder stringBuilder)
	{
		stringBuilder.Append("<color=grey>[");
		stringBuilder.Append(StringManager.Get(coordsX));
		stringBuilder.Append(", ");
		stringBuilder.Append(StringManager.Get(coordsY));
		stringBuilder.Append("]</color>");
	}

	private void AppendRegionInfo(bool isRevealed, int coordsX, int coordsY, StringBuilder stringBuilder)
	{
		if (!isRevealed)
		{
			stringBuilder.Append("<color=green>Unknown region</color>");
			return;
		}
		GeographicRegionData geographicRegionData = WorldSetting.Current.Data.GeographicRegionData;
		if (geographicRegionData != null && RegionManager.TryGetRegionAtWorldPosition(geographicRegionData.RegionSet, new Vector3(coordsX, 0f, coordsY), out var region))
		{
			stringBuilder.Append("<color=green>");
			stringBuilder.Append(region.Name);
			stringBuilder.Append("</color>");
		}
	}

	private void AppendMinableInfo(bool isRevealed, int coordsX, int coordsY, StringBuilder stringBuilder)
	{
		if (!isRevealed)
		{
			stringBuilder.Append("Unknown resources");
			return;
		}
		List<DeepMinablesGenerationData> deepMinablesData = WorldSetting.Current.Data.DeepMinablesData;
		bool flag = false;
		foreach (DeepMinablesGenerationData item in deepMinablesData)
		{
			if (item.Evaluate(new EvaluablePosition(coordsX, 0f, coordsY)))
			{
				item.ReagentAction.ToolTip(stringBuilder, 0);
				flag = true;
			}
		}
		if (!flag)
		{
			stringBuilder.Append("Unknown resources");
		}
	}
}
