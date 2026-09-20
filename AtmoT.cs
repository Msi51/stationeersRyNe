using System;
using UnityEngine;

public class AtmoT : MonoBehaviour
{
	public GameObject m_sun;

	public Material m_groundMaterial;

	public Material m_skyMaterial;

	public float m_hdrExposure = 0.6f;

	public Vector3 m_atmoColor = new Vector3(0.8f, 0.7f, 0.45f);

	public float m_ESun = 20f;

	public float m_kr = 0.0025f;

	public float m_km = 0.001f;

	public float m_g = -0.99f;

	private const float m_outerScaleFactor = 1.025f;

	private float m_innerRadius;

	private float m_outerRadius;

	private float m_scaleDepth = 0.25f;

	private void Start()
	{
		m_sun = GameObject.FindGameObjectWithTag("Sun");
		m_outerRadius = 1.025f * (m_innerRadius = base.transform.localScale.x);
		InitMaterial(m_groundMaterial);
		InitMaterial(m_skyMaterial);
	}

	private void Update()
	{
		InitMaterial(m_groundMaterial);
		InitMaterial(m_skyMaterial);
	}

	private void InitMaterial(Material mat)
	{
		Vector3 vector = new Vector3(1f / Mathf.Pow(m_atmoColor.x, 4f), 1f / Mathf.Pow(m_atmoColor.y, 4f), 1f / Mathf.Pow(m_atmoColor.z, 4f));
		float num = 1f / (m_outerRadius - m_innerRadius);
		mat.SetVector("v3LightPos", m_sun.transform.forward * -1f);
		mat.SetVector("v3InvWavelength", vector);
		mat.SetFloat("fOuterRadius", m_outerRadius);
		mat.SetFloat("fOuterRadius2", m_outerRadius * m_outerRadius);
		mat.SetFloat("fInnerRadius", m_innerRadius);
		mat.SetFloat("fInnerRadius2", m_innerRadius * m_innerRadius);
		mat.SetFloat("fKrESun", m_kr * m_ESun);
		mat.SetFloat("fKmESun", m_km * m_ESun);
		mat.SetFloat("fKr4PI", m_kr * 4f * MathF.PI);
		mat.SetFloat("fKm4PI", m_km * 4f * MathF.PI);
		mat.SetFloat("fScale", num);
		mat.SetFloat("fScaleDepth", m_scaleDepth);
		mat.SetFloat("fScaleOverScaleDepth", num / m_scaleDepth);
		mat.SetFloat("fHdrExposure", m_hdrExposure);
		mat.SetFloat("g", m_g);
		mat.SetFloat("g2", m_g * m_g);
		mat.SetVector("v3LightPos", m_sun.transform.forward * -1f);
		mat.SetVector("v3Translate", base.transform.localPosition);
	}
}
