using System;
using System.Collections;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class BasketHoop : Device
{
	[Header("Hoop Display")]
	private int _setting;

	public GameObject[] DigitMesh;

	public Collider RingCollider;

	private GameObject TargetBall;

	private int _hoopHash = Animator.StringToHash("Hoop");

	private int _backBoardHash = Animator.StringToHash("BackBoard");

	[ByteArraySync]
	public int Setting
	{
		get
		{
			return _setting;
		}
		set
		{
			if (_setting != value)
			{
				_setting = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 256;
				}
				OnSettingChanged();
			}
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteInt32(Setting);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			Setting = reader.ReadInt32();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt32(Setting);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Setting = reader.ReadInt32();
	}

	public void OnSettingChanged()
	{
		if (!GameManager.RunSimulation)
		{
			RenderText();
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		if (GameManager.RunSimulation && GameManager.GameState == GameState.Running)
		{
			RenderText();
		}
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (GameManager.RunSimulation && collider.bounds.center.y > RingCollider.bounds.center.y)
		{
			TargetBall = collider.gameObject;
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		if (GameManager.RunSimulation && collider.gameObject == TargetBall)
		{
			if (collider.bounds.center.y < RingCollider.bounds.center.y)
			{
				Setting += 2;
				RenderText();
				PlayNetworkSound(_hoopHash);
			}
			TargetBall = null;
		}
	}

	public virtual void OnCollisionEnter(Collision collision)
	{
		if (!GameManager.IsBatchMode)
		{
			float num = ((Time.deltaTime > 0f) ? Time.deltaTime : 1f);
			float volumeMultiplier = Mathf.Clamp01(collision.impulse.magnitude / num / 100f);
			PlaySound(_backBoardHash, volumeMultiplier);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		RenderText();
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		BasketBaseSaveData basketBaseSaveData = savedData as BasketBaseSaveData;
		if (GameManager.GameState != GameState.None && basketBaseSaveData != null)
		{
			basketBaseSaveData.Setting = Setting;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new BasketBaseSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is BasketBaseSaveData basketBaseSaveData)
		{
			Setting = basketBaseSaveData.Setting;
			StartCoroutine(WaitRenderText());
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (interactable.Action == InteractableType.Open)
		{
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (GameManager.RunSimulation)
			{
				Setting = 0;
			}
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.OnOff)
		{
			RenderText();
		}
	}

	private IEnumerator WaitRenderText(bool force = false)
	{
		while (GameManager.GameState != GameState.Running)
		{
			yield return new WaitForEndOfFrame();
		}
		yield return new WaitForEndOfFrame();
		RenderText();
	}

	public void SetDisplay()
	{
		if (Setting >= 999)
		{
			Setting = 999;
		}
		string text = Setting.ToString();
		GameObject[] digitMesh = DigitMesh;
		for (int i = 0; i < digitMesh.Length; i++)
		{
			digitMesh[i].SetActive(value: false);
		}
		if (Powered && OnOff)
		{
			int num = text.Length - 1;
			int num2 = 2;
			while (num >= 0)
			{
				DigitMesh[num2 * 10 + (text[num] - 48)].SetActive(value: true);
				num--;
				num2--;
			}
		}
	}

	public void RenderText()
	{
		if (ThreadedManager.IsThread)
		{
			UnityMainThreadDispatcher.Instance().Enqueue(WaitRenderText());
		}
		else
		{
			SetDisplay();
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

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return Setting;
		}
		return base.GetLogicValue(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (logicType == LogicType.Setting)
		{
			Setting = (int)Math.Max(value, 1.0);
			if (ThreadedManager.IsThread)
			{
				UnityMainThreadDispatcher.Instance().Enqueue(WaitRenderText());
			}
			else
			{
				RenderText();
			}
		}
		base.SetLogicValue(logicType, value);
	}
}
