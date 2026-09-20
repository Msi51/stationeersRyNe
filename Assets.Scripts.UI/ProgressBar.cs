using UnityEngine;

namespace Assets.Scripts.UI;

public class ProgressBar : MonoBehaviour
{
	public GameObject Bar;

	private Transform _billboardTarget;

	private void Start()
	{
		_billboardTarget = Camera.main.transform;
	}

	private void Update()
	{
		if (!GameManager.IsBatchMode && !(_billboardTarget == null) && !WorldManager.IsGamePaused)
		{
			base.transform.LookAt(base.transform.position + Camera.main.transform.rotation * -Vector3.forward, Camera.main.transform.rotation * Vector3.up);
		}
	}
}
