using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Serialization;
using Reagents;

namespace Assets.Scripts.Util;

public static class Serializers
{
	private static XmlSerializer _settingData;

	private static XmlSerializer _worldMetaData;

	private static XmlSerializer _worldData;

	private static XmlSerializer _gameData;

	private static XmlSerializer _voxelData;

	private static XmlSerializer _recipeData;

	private static XmlSerializer _processingData;

	private static XmlSerializer _reagentMixture;

	public static XmlSerializer SettingData
	{
		get
		{
			if (_settingData != null)
			{
				return _settingData;
			}
			_settingData = new XmlSerializer(typeof(Settings.SettingData), new Type[1] { typeof(KeyItem) });
			return _settingData;
		}
	}

	public static XmlSerializer WorldMetaData
	{
		get
		{
			if (_worldMetaData != null)
			{
				return _worldMetaData;
			}
			_worldMetaData = new XmlSerializer(typeof(XmlSaveLoad.WorldMetaData), XmlSaveLoad.ExtraTypes);
			return _worldMetaData;
		}
	}

	public static XmlSerializer WorldData
	{
		get
		{
			if (_worldData != null)
			{
				return _worldData;
			}
			_worldData = new XmlSerializer(typeof(XmlSaveLoad.WorldData), XmlSaveLoad.ExtraTypes);
			return _worldData;
		}
	}

	public static XmlSerializer GameData
	{
		get
		{
			if (_gameData != null)
			{
				return _gameData;
			}
			_gameData = new XmlSerializer(typeof(WorldManager.GameData), XmlSaveLoad.ExtraTypes);
			return _gameData;
		}
	}

	public static XmlSerializer RecipeData
	{
		get
		{
			if (_recipeData != null)
			{
				return _recipeData;
			}
			_recipeData = new XmlSerializer(typeof(List<WorldManager.RecipeData>), XmlSaveLoad.ExtraTypes);
			return _recipeData;
		}
	}

	public static XmlSerializer ProcessingData
	{
		get
		{
			if (_processingData != null)
			{
				return _processingData;
			}
			_processingData = new XmlSerializer(typeof(List<WorldManager.ProcessingData>), XmlSaveLoad.ExtraTypes);
			return _processingData;
		}
	}

	public static XmlSerializer ReagentMixture
	{
		get
		{
			if (_reagentMixture != null)
			{
				return _reagentMixture;
			}
			_reagentMixture = new XmlSerializer(typeof(List<ReagentMixture>), XmlSaveLoad.ExtraTypes);
			return _reagentMixture;
		}
	}
}
