using System.Collections.Generic;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SavePanel : MonoBehaviour
{
	[Header("Save")]
	[SerializeField]
	private Button _btnSave;

	[SerializeField]
	private TMP_InputField _saveNameInput;

	[SerializeField]
	private WorldSave _worldSavePrefab;

	[SerializeField]
	private Transform _worldSaveParent;

	[SerializeField]
	private List<WorldSave> _worldSaveItems = new List<WorldSave>();

	public void WorldSaveClicked(WorldSave worldSave)
	{
		_saveNameInput.text = worldSave.Filename.text;
	}

	private void Start()
	{
		_btnSave.onClick.AddListener(SaveButtonPressed);
		_saveNameInput.onSubmit.AddListener(delegate
		{
			SaveButtonPressed();
		});
		_saveNameInput.onValueChanged.AddListener(TextInputChanged);
	}

	private void OnEnable()
	{
		GetSavesList();
	}

	private void GetSavesList()
	{
		List<SaveFileInfo> manualSavesForCurrentStation = LoadHelper.GetManualSavesForCurrentStation();
		int num = Mathf.Max(_worldSaveItems.Count, manualSavesForCurrentStation.Count);
		foreach (WorldSave worldSaveItem in _worldSaveItems)
		{
			worldSaveItem.SetActive(active: false);
		}
		for (int i = 0; i < num; i++)
		{
			if (manualSavesForCurrentStation.Count > i && _worldSaveItems.Count > i)
			{
				_worldSaveItems[i].Show(manualSavesForCurrentStation[i]);
			}
			else if (manualSavesForCurrentStation.Count > i && _worldSaveItems.Count <= i)
			{
				WorldSave worldSave = Object.Instantiate(_worldSavePrefab, _worldSaveParent);
				worldSave.Initialize(this);
				_worldSaveItems.Add(worldSave);
				_worldSaveItems[i].Show(manualSavesForCurrentStation[i]);
			}
			else if (manualSavesForCurrentStation.Count <= i && _worldSaveItems.Count > i)
			{
				_worldSaveItems[i].Hide();
			}
		}
	}

	private void TextInputChanged(string value)
	{
		_btnSave.interactable = !string.IsNullOrWhiteSpace(value);
	}

	private void SaveButtonPressed()
	{
		string text = SaveHelper.SanitizeSaveName(_saveNameInput.text);
		if (!string.IsNullOrWhiteSpace(text))
		{
			_saveNameInput.text = string.Empty;
			SaveAsTask(text).Forget();
		}
	}

	private async UniTaskVoid SaveAsTask(string saveName)
	{
		SaveResult saveResult = await SaveHelper.SaveAs(XmlSaveLoad.Instance.CurrentStationName, saveName, default(CancellationToken));
		if (saveResult.Success)
		{
			GetSavesList();
		}
		else
		{
			ConsoleWindow.PrintError(saveResult.Message, suppressStacktrace: true);
		}
	}
}
