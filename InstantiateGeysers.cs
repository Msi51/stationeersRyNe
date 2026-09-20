using UnityEngine;

public class InstantiateGeysers : MonoBehaviour
{
	public GameObject[] geysersPrefabs;

	private GameObject[] geysers;

	public bool disableBillboard;

	private void Start()
	{
		geysers = new GameObject[geysersPrefabs.Length];
		for (int i = 0; i < geysersPrefabs.Length; i++)
		{
			geysers[i] = Object.Instantiate(geysersPrefabs[i], base.transform.position, geysersPrefabs[i].transform.rotation);
			geysers[i].transform.parent = base.transform;
			geysers[i].transform.localScale = new Vector3(1f, 1f, 1f);
			if (disableBillboard)
			{
				geysers[i].GetComponent<GeyserLight>().setBillboard(value: true);
			}
			else
			{
				geysers[i].GetComponent<GeyserLight>().setBillboard(value: false);
			}
		}
	}

	private void Update()
	{
	}
}
