using System.Collections.Generic;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class MyGraphicsRaycaster : GraphicRaycaster
{
	public struct CameraInfoStruct
	{
		public Camera GraphicRaycastCamera;

		public Transform CameraTransform;
	}

	private static CameraInfoStruct _cameraInfo;

	public static bool ShouldCast;

	public static CameraInfoStruct CameraInfo
	{
		get
		{
			if ((_cameraInfo.GraphicRaycastCamera != null && _cameraInfo.CameraTransform != null) || Singleton<GameManager>.Instance == null)
			{
				return _cameraInfo;
			}
			_cameraInfo.GraphicRaycastCamera = Singleton<GameManager>.Instance.GraphicRaycastCamera;
			if (_cameraInfo.GraphicRaycastCamera != null)
			{
				_cameraInfo.CameraTransform = _cameraInfo.GraphicRaycastCamera.transform;
			}
			return _cameraInfo;
		}
	}

	public override Camera eventCamera => CameraInfo.GraphicRaycastCamera;

	public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
	{
		if (ShouldCast)
		{
			base.Raycast(eventData, resultAppendList);
		}
	}
}
