using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public enum GameScene
{
	StartMenu, Lobby, Match
}

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	const string InternalGameSettingsResourcePath = "Internal Game Settings";
	readonly Dictionary<string, MatchModeConfig> matchModeConfigById = new();
	readonly List<MatchModeConfig> legacyMatchModeConfigs = new();
	public IReadOnlyList<MatchModeConfig> LegacyMatchModeConfigs => legacyMatchModeConfigs;
	public string DefaultMatchModeId { get; private set; }
	public GameObject DefaultMatchSkinPrefab { get; private set; }

	#region Game initialization
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void OnGameInitialize()
	{
		if(!FindAnyObjectByType<GameManager>())
		{
			var go = new GameObject("Game Manager");
			go.AddComponent<GameManager>();
		}
	}
	#endregion

	#region Unity life cycle
	protected void Awake()
	{
		Instance = this;
		DontDestroyOnLoad(gameObject);
		InitializeMatchModeConfigs();

		// Create audio manager
		if(AudioManager.Instance == null)
			gameObject.AddComponent<AudioManager>();
	}

	protected void OnDestroy()
	{
		if(Instance == this)
			Instance = null;
	}
	#endregion

	#region Match modes
	void InitializeMatchModeConfigs()
	{
		matchModeConfigById.Clear();
		legacyMatchModeConfigs.Clear();

		InternalGameSettings settings = Resources.Load<InternalGameSettings>(InternalGameSettingsResourcePath);
		if(settings == null)
		{
			DefaultMatchModeId = null;
			DefaultMatchSkinPrefab = null;
			Debug.LogError($"Internal game settings not found in Resources at '{InternalGameSettingsResourcePath}'.");
			return;
		}

		DefaultMatchModeId = settings.DefaultMatchModeId;
		DefaultMatchSkinPrefab = settings.DefaultMatchSkinPrefab;
		if(DefaultMatchSkinPrefab == null)
			Debug.LogError("Default match skin prefab is not configured in InternalGameSettings.");
		for(int i = 0; i < settings.LegacyMatchModes.Count; ++i)
		{
			MatchModeConfig config = settings.LegacyMatchModes[i];
			if(config == null)
				continue;

			if(string.IsNullOrWhiteSpace(config.ModeId))
			{
				Debug.LogError($"Match mode config '{config.name}' has empty mode id.");
				continue;
			}

			if(matchModeConfigById.ContainsKey(config.ModeId))
			{
				Debug.LogError($"Duplicated match mode id '{config.ModeId}'.");
				continue;
			}

			matchModeConfigById.Add(config.ModeId, config);
			legacyMatchModeConfigs.Add(config);
		}

		if(!string.IsNullOrWhiteSpace(DefaultMatchModeId) && !matchModeConfigById.ContainsKey(DefaultMatchModeId))
			Debug.LogError($"Default match mode id '{DefaultMatchModeId}' is not found in InternalGameSettings list.");
	}

	public bool TryGetMatchModeConfig(string modeId, out MatchModeConfig config)
	{
		if(string.IsNullOrWhiteSpace(modeId))
		{
			config = null;
			return false;
		}
		return matchModeConfigById.TryGetValue(modeId, out config);
	}
	#endregion

	#region Lobby
	public Lobby Lobby { get; private set; } = null;

	public void LoadDefaultLobby()
	{
		Lobby = new HostLobby(DefaultMatchModeId);
	}

	public void CreateLobby()
	{
		LoadDefaultLobby();
		SwitchScene(GameScene.Lobby);
	}

	public void ExitLobby()
	{
		SwitchScene(GameScene.StartMenu);
		Lobby = null;
	}
	#endregion

	#region Misc
	public void SwitchScene(GameScene scene)
	{
		string sceneName = scene switch
		{
			GameScene.StartMenu => "Start Menu",
			GameScene.Lobby => "Lobby",
			GameScene.Match => "Match",
			_ => throw new System.ArgumentOutOfRangeException()
		};
		SceneManager.LoadScene(sceneName);
	}

	public void QuitGame()
	{
#if UNITY_EDITOR
		if(UnityEditor.EditorApplication.isPlaying)
		{
			UnityEditor.EditorApplication.isPlaying = false;
			return;
		}
#endif
		Application.Quit();
	}
	#endregion
}
