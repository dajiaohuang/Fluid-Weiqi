using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GameSettings", menuName = "FluidWeiqi/Game Settings")]
public class GameSettings : ScriptableObject
{
	const string GameSettingsResourcePath = "Game Settings";
	static GameSettings instance;
	public static GameSettings Instance
	{
		get
		{
			if(instance == null)
				instance = Resources.Load<GameSettings>(GameSettingsResourcePath);
			return instance;
		}
	}

	[SerializeField] string defaultMatchModeId;
	[SerializeField] GameObject defaultMatchSkinPrefab;
	[SerializeField] List<MatchModeConfig> legacyMatchModes = new();
	[SerializeField] List<AiConfig> legacyAis = new();

	public string DefaultMatchModeId => defaultMatchModeId;
	public GameObject DefaultMatchSkinPrefab => defaultMatchSkinPrefab;
	public IReadOnlyList<MatchModeConfig> LegacyMatchModes => legacyMatchModes;
	public IReadOnlyList<AiConfig> LegacyAis => legacyAis;
}
