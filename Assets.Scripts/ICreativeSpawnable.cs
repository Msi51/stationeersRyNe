using System.Text;
using UnityEngine;

namespace Assets.Scripts;

public interface ICreativeSpawnable
{
	int SpawnId { get; }

	string DisplayName { get; }

	string SpawnableName { get; }

	Sprite GetThumbnail();

	void ToTooltip(StringBuilder sb);
}
