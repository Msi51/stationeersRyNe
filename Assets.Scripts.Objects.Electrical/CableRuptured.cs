using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class CableRuptured : SmallGrid
{
	public static List<CableRuptured> AllCableRuptured = new List<CableRuptured>();

	public static readonly int CableSparkHash = Animator.StringToHash("CableSpark");

	private static readonly int NUMBER_OF_SPARKS = 100;

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState == GameState.Running && !IsCursor && !GameManager.IsBatchMode)
		{
			ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
			{
				position = base.ThingTransformPosition
			};
			WorldManager.Instance.Sparker.Emit(emitParams, NUMBER_OF_SPARKS);
			Singleton<AudioManager>.Instance.PlayAudioClipsData(CableSparkHash, base.ThingTransformPosition);
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		AllCableRuptured.Add(this);
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		AllCableRuptured.Remove(this);
	}
}
