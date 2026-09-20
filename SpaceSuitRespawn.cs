using System.Collections;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using UnityEngine;

public class SpaceSuitRespawn : MonoBehaviour
{
	public float SpawnEffectTime = 5f;

	public float HideEffectTime = 3f;

	public float PauseTime = 1f;

	public AnimationCurve fadeIn;

	public GameObject Helmet;

	public GameObject Visor;

	public GameObject Glass;

	public Human ParentHuman;

	public bool HideEffect;

	private float timer;

	private Renderer _renderer;

	private Renderer _rendererHelmet;

	private Renderer _rendererVisor;

	private Renderer _rendererGlass;

	private int shaderProperty;

	private int shaderColorProperty;

	private int shaderMetallicProperty;

	private Color _visorColor;

	private bool _hideSuit;

	private bool _hideHelmet;

	private void Start()
	{
		shaderProperty = Shader.PropertyToID("_cutoff");
		shaderColorProperty = Shader.PropertyToID("_Color");
		shaderMetallicProperty = Shader.PropertyToID("_Metallic");
		_renderer = GetComponent<Renderer>();
		_rendererHelmet = Helmet.GetComponent<Renderer>();
		_rendererVisor = Visor.GetComponent<Renderer>();
		_rendererGlass = Glass.GetComponent<Renderer>();
		_visorColor = _rendererVisor.material.GetColor(shaderColorProperty);
		SetParentSuitVisibility(show: false);
		StartCoroutine(ShowSlots());
	}

	private void SetParentSuitVisibility(bool show, bool forceToUpdate = false)
	{
		if (ParentHuman == null)
		{
			return;
		}
		if ((bool)ParentHuman.Suit?.AsThing)
		{
			if (!show)
			{
				if (!_hideSuit || forceToUpdate)
				{
					_hideSuit = true;
					ParentHuman.Suit.ForceClothingVisible(show);
				}
			}
			else
			{
				ParentHuman.Suit.ForceClothingVisible(show);
			}
		}
		if ((bool)ParentHuman.HeadAsSpaceHelmet && (!_hideHelmet || forceToUpdate))
		{
			_hideHelmet = true;
			if (!GameManager.IsBatchMode)
			{
				bool hideOnPlayer = InventoryManager.ParentHuman == ParentHuman;
				ParentHuman.HeadAsSpaceHelmet.SetVisibility(show, hideOnPlayer);
			}
			else
			{
				ParentHuman.HeadAsSpaceHelmet.SetVisibility(show);
			}
			ParentHuman.UpdateHeadShadow();
		}
	}

	private IEnumerator ShowSlots()
	{
		yield return null;
		while (timer < SpawnEffectTime + PauseTime)
		{
			timer += Time.deltaTime;
			SetParentSuitVisibility(show: false);
			for (int i = 0; i < _renderer.materials.Length; i++)
			{
				_renderer.materials[i].SetFloat(shaderProperty, fadeIn.Evaluate(Mathf.InverseLerp(0f, SpawnEffectTime, timer)));
			}
			for (int j = 0; j < _rendererHelmet.materials.Length; j++)
			{
				_rendererHelmet.materials[j].SetFloat(shaderProperty, fadeIn.Evaluate(Mathf.InverseLerp(0f, SpawnEffectTime, timer)));
			}
			_visorColor.a = Mathf.Lerp(0f, 0.603f, timer / (SpawnEffectTime + PauseTime));
			_rendererVisor.material.SetColor(shaderColorProperty, _visorColor);
			_rendererVisor.material.SetFloat(shaderMetallicProperty, Mathf.Lerp(0f, 0.415f, timer / (SpawnEffectTime + PauseTime)));
			_rendererGlass.material.SetColor(shaderColorProperty, _visorColor);
			_rendererGlass.material.SetFloat(shaderMetallicProperty, Mathf.Lerp(0f, 0.415f, timer / (SpawnEffectTime + PauseTime)));
			yield return null;
		}
		timer = 0f;
		SetParentSuitVisibility(show: true, forceToUpdate: true);
		StartCoroutine(HideSlots());
	}

	private IEnumerator HideSlots()
	{
		yield return null;
		timer = HideEffectTime;
		while (timer > 0f)
		{
			timer -= Time.deltaTime;
			for (int i = 0; i < _renderer.materials.Length; i++)
			{
				_renderer.materials[i].SetFloat(shaderProperty, fadeIn.Evaluate(Mathf.InverseLerp(0f, SpawnEffectTime, timer)));
			}
			for (int j = 0; j < _rendererHelmet.materials.Length; j++)
			{
				_rendererHelmet.materials[j].SetFloat(shaderProperty, fadeIn.Evaluate(Mathf.InverseLerp(0f, SpawnEffectTime, timer)));
			}
			_visorColor.a = Mathf.Lerp(0f, 0.603f, timer / (SpawnEffectTime + PauseTime));
			_rendererVisor.material.SetColor(shaderColorProperty, _visorColor);
			_rendererVisor.material.SetFloat(shaderMetallicProperty, Mathf.Lerp(0f, 0.415f, timer));
			_rendererGlass.material.SetColor(shaderColorProperty, _visorColor);
			_rendererGlass.material.SetFloat(shaderMetallicProperty, Mathf.Lerp(0f, 0.415f, timer));
			yield return null;
		}
		Object.Destroy(base.gameObject);
	}
}
