using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameScene
{
	StartMenu, Lobby, Match
}

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

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

	#region Lobby
	public Lobby Lobby { get; private set; } = null;

	public void CreateLobby()
	{
		Lobby = new HostLobby();
		SwitchScene(GameScene.Lobby);
	}

	public void ExitLobby()
	{
		SwitchScene(GameScene.StartMenu);
		Lobby = null;
	}
	#endregion

	#region Match
	public void StartMatch()
	{
		if(Lobby == null)
		{
			Debug.LogError($"Cannot start match, no active lobby.");
			return;
		}
		SwitchScene(GameScene.Match);
	}

	public void EndMatch()
	{
		SwitchScene(GameScene.Lobby);
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
