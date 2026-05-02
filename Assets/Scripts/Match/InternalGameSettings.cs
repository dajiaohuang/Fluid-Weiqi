using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "InternalGameSettings", menuName = "FluidWeiqi/Internal Game Settings")]
public class InternalGameSettings : ScriptableObject
{
	[SerializeField] string defaultMatchModeId;
	[SerializeField] GameObject defaultMatchSkinPrefab;
	[SerializeField] List<MatchModeConfig> legacyMatchModes = new();

	public string DefaultMatchModeId => defaultMatchModeId;
	public GameObject DefaultMatchSkinPrefab => defaultMatchSkinPrefab;
	public IReadOnlyList<MatchModeConfig> LegacyMatchModes => legacyMatchModes;
}
