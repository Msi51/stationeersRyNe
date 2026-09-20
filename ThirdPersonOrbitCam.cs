using System;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using UnityEngine;

public class ThirdPersonOrbitCam : MonoBehaviour
{
	[Serializable]
	public class CameraState
	{
		public Vector3 Offset = new Vector3(0f, 0.7f, -3f);

		public Vector3 Pivot = new Vector3(0.5f, 1f, 0f);
	}

	[Serializable]
	public class CameraStates
	{
		public CameraState Normal = new CameraState();

		public CameraState AirLock = new CameraState();

		public CameraState LeftConer = new CameraState();

		public CameraState RightConer = new CameraState();

		public CameraState BackWall = new CameraState();
	}

	public CameraStates AllStates;

	public CameraState State;

	[HideInInspector]
	public float Horizontal;

	[HideInInspector]
	public float Vertical;

	private float ObstacleDistance = 1f;

	private Vector3 curPivot = new Vector3(0.5f, 1f, 0f);

	private Vector3 curOffset = new Vector3(0f, 0.7f, -3f);

	public static Vector3 zoomOffset = new Vector3(0f, 0f, 2f);

	private Vector3 playerPosition;

	private Quaternion playerRotation;

	private float playerPivotSpeed = 1f;

	private Vector3 obstacleFix;

	private Camera camera;

	private float playerFocusHeight;

	private RaycastHit[] Hits = new RaycastHit[64];

	private int layerMask;

	private float _horizontal;

	private float _vertical;

	private bool isControlHeld;

	private Transform Target
	{
		get
		{
			if (!InventoryManager.Parent)
			{
				return null;
			}
			return InventoryManager.Parent.transform;
		}
	}

	private void OnEnable()
	{
		ResetAll();
	}

	public void ResetAll()
	{
		layerMask = (1 << LayerMask.NameToLayer("Default")) | (1 << LayerMask.NameToLayer("TransparentFX")) | (1 << (int)Layers.PlayerInvisible) | (1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Terrain"));
		camera = GetComponent<Camera>();
		Horizontal = base.transform.eulerAngles.y;
		Vertical = 0f;
		isControlHeld = false;
		SetState();
		curOffset = State.Offset + zoomOffset;
		curPivot = State.Pivot;
		playerFocusHeight = Target.GetComponent<CapsuleCollider>().height * 0.5f;
		playerPosition = GetPosition();
		playerRotation = Target.rotation;
		Quaternion quaternion = Quaternion.Euler(Vertical, Horizontal, 0f);
		Vector3 position = playerRotation * curPivot + playerPosition + quaternion * curOffset;
		Vector3 worldPosition = playerPosition + curPivot + quaternion * Vector3.forward * 100f;
		base.transform.position = position;
		base.transform.LookAt(worldPosition);
	}

	private Vector3 GetPosition()
	{
		return Target.position + playerFocusHeight * Vector3.up;
	}

	public void UpdateCamera()
	{
		if (!(Target != null) || Cursor.visible || InventoryManager.Parent.Unconscious || InventoryManager.Parent.Dead)
		{
			return;
		}
		if (!KeyManager.GetButton(KeyMap.ThirdPersonControl))
		{
			if (isControlHeld)
			{
				Horizontal = _horizontal;
				Vertical = _vertical;
			}
			isControlHeld = false;
		}
		else if (!isControlHeld)
		{
			_horizontal = Horizontal;
			_vertical = Vertical;
			isControlHeld = true;
		}
		float num = (Settings.CurrentData.InvertMouse ? (-1f) : 1f);
		Horizontal = Mathf.LerpAngle(Horizontal, Horizontal + Singleton<InputManager>.Instance.GetAxis("LookX") * 1.5f * Time.timeScale, 1f);
		Vertical = Mathf.LerpAngle(Vertical, Vertical - Singleton<InputManager>.Instance.GetAxis("LookY") * 1.5f * Time.timeScale * num, 1f);
		Vertical = Mathf.Clamp(Vertical, -45f, 70f);
		SetState();
		Vector3 position = GetPosition();
		Quaternion rotation = Target.rotation;
		playerPosition = Vector3.Lerp(playerPosition, position, Time.deltaTime * 5f);
		playerRotation = Quaternion.Slerp(playerRotation, rotation, Time.deltaTime * 3f);
		Quaternion quaternion = Quaternion.Euler(Vertical, Horizontal, 0f);
		Vector3 vector = playerRotation * curPivot + playerPosition + quaternion * curOffset;
		Vector3 worldPosition = playerPosition + curPivot + quaternion * Vector3.forward * 100f;
		Vector3 b = CheckObstacles(vector, playerPosition + playerFocusHeight * Vector3.up, 0.1f);
		obstacleFix = Vector3.Lerp(obstacleFix, b, Time.deltaTime * 30f);
		vector += obstacleFix;
		base.transform.position = vector;
		base.transform.LookAt(worldPosition);
		CameraController.EffectiveCameraPosition = vector;
		CameraController.CameraPosition = vector;
		CameraController.CameraRotation = base.transform.rotation;
		float t = Time.deltaTime * 3f;
		curPivot = Vector3.Lerp(curPivot, State.Pivot, t);
		curOffset = Vector3.Lerp(curOffset, State.Offset + zoomOffset, t);
		if (!isControlHeld)
		{
			CameraController.Instance.RotationX = 0f - Vertical;
			if (!CameraController.IsPlayerOnLadder)
			{
				CameraController.Instance.RotationY = camera.transform.rotation.eulerAngles.y;
				CameraController.Instance.RotationY = InputHelpers.ClampAngle(CameraController.Instance.RotationY, -360f, 360f);
			}
		}
	}

	private void SetState()
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		Vector3 origin = Target.position + playerFocusHeight * Vector3.up;
		if (Physics.Raycast(origin, -Target.right, ObstacleDistance, layerMask))
		{
			flag = true;
		}
		if (Physics.Raycast(origin, Target.right, ObstacleDistance, layerMask))
		{
			flag2 = true;
		}
		if (Physics.Raycast(origin, -Target.forward, ObstacleDistance, layerMask))
		{
			flag3 = true;
		}
		if (flag3 && (flag || flag2) && flag != flag2)
		{
			State = (flag ? AllStates.LeftConer : AllStates.RightConer);
		}
		else if (flag3)
		{
			State = AllStates.BackWall;
		}
		else
		{
			State = AllStates.Normal;
		}
	}

	private Vector3 CheckObstacles(Vector3 camera, Vector3 target, float radius)
	{
		int num = 0;
		float num2 = Vector3.Distance(camera, target);
		Vector3 normalized = (target - camera).normalized;
		float num3 = 0f;
		if ((float)num < num2)
		{
			Vector3 origin = target - normalized * num;
			float num4 = Vector3.Distance(camera, target);
			int num5 = Physics.SphereCastNonAlloc(new Ray(origin, -normalized), radius, Hits, num4, layerMask);
			for (int i = 0; i < num5; i++)
			{
				RaycastHit raycastHit = Hits[i];
				if (!raycastHit.collider.isTrigger && !InHiearchyOf(raycastHit.collider.gameObject, Target.gameObject))
				{
					float num6 = Mathf.Clamp(num4 - raycastHit.distance, 0f, num4);
					if (num6 > num3)
					{
						num3 = num6;
					}
				}
			}
		}
		return num3 * normalized;
	}

	public static bool InHiearchyOf(GameObject target, GameObject parent)
	{
		GameObject gameObject = target;
		while (gameObject != null)
		{
			if (gameObject == parent)
			{
				return true;
			}
			gameObject = ((!(gameObject.transform.parent != null)) ? null : gameObject.transform.parent.gameObject);
		}
		return false;
	}
}
