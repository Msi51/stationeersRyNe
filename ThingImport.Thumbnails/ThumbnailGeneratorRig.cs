using System;
using System.IO;
using Assets.Scripts.Objects;
using UnityEngine;

namespace ThingImport.Thumbnails;

public class ThumbnailGeneratorRig : MonoBehaviour
{
	[SerializeField]
	private Transform _cameraTransform;

	[SerializeField]
	private GameObject _cameraRig;

	[SerializeField]
	private Camera _camera;

	public Camera Camera => _camera;

	public Transform CameraRigTransform => _cameraRig.transform;

	public void SetRigActive(bool active)
	{
		_cameraRig.SetActive(active);
	}

	public void SetZoom(float zoom)
	{
		_cameraTransform.localPosition = new Vector3(0f, 0f, 0f - zoom);
	}

	public void RenderTo(RenderTexture target)
	{
		_camera.transparencySortMode = TransparencySortMode.Perspective;
		_camera.targetTexture = target;
		_camera.Render();
		_camera.targetTexture = null;
	}

	public string Generate(DynamicThing thing, float zoom)
	{
		string text;
		try
		{
			_cameraRig.SetActive(value: true);
			Vector3 position = thing.Transform.position;
			Quaternion rotation = thing.Transform.rotation;
			bool isKinematic = thing.RigidBody.isKinematic;
			int layer = thing.GameObject.layer;
			bool flag = thing.enabled;
			thing.enabled = false;
			thing.RigidBody.isKinematic = true;
			thing.Transform.position = -thing.Bounds.center;
			thing.Transform.rotation = Quaternion.identity;
			thing.GameObject.layer = Layers.ThumbnailCreation;
			_cameraTransform.localPosition = new Vector3(0f, 0f, 0f - zoom);
			Texture2D tex = MakeThumbnail();
			text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), thing.PrefabName + "_Thumb.png");
			File.WriteAllBytes(text, tex.EncodeToPNG());
			thing.enabled = flag;
			thing.RigidBody.isKinematic = isKinematic;
			thing.Transform.position = position;
			thing.Transform.rotation = rotation;
			thing.GameObject.layer = layer;
			_cameraRig.SetActive(value: false);
		}
		catch (Exception ex)
		{
			return "Error generating thumbnail. " + ex.Message;
		}
		return "Thumbnail generated at " + text;
	}

	private Texture2D MakeThumbnail()
	{
		int num = 256;
		RenderTexture renderTexture = new RenderTexture(num, num, 0, RenderTextureFormat.ARGB32);
		renderTexture.filterMode = FilterMode.Trilinear;
		renderTexture.antiAliasing = 8;
		renderTexture.anisoLevel = 9;
		_camera.transparencySortMode = TransparencySortMode.Perspective;
		_camera.targetTexture = renderTexture;
		_camera.Render();
		RenderTexture.active = renderTexture;
		Texture2D texture2D = new Texture2D(num, num, TextureFormat.ARGB32, mipChain: false);
		texture2D.ReadPixels(new Rect(0f, 0f, num, num), 0, 0);
		RenderTexture.active = null;
		_camera.targetTexture = null;
		return texture2D;
	}
}
