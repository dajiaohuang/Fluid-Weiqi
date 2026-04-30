using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class LobbyUi : MonoBehaviour
{
	#region Unity life cycle
	protected void Awake()
	{
		playerSlotPrefab = Resources.Load<GameObject>("Prefabs/Player Slot");
	}

	protected void Start()
	{
#if DEBUG && UNITY_EDITOR
		// Debug
		if(Lobby.Current == null)
		{
			GameManager.Instance.CreateLobby();
			return;
		}
#endif

		// Lobby settings
		SetVisibilityOptions(allVisibilityOptions);
		visibilityDropdown.interactable = Lobby.Current.IsHost;
		visibilityDropdown.onValueChanged.AddListener(OnVisibilityDropdownValueChanged);
		lobbyNameInput.interactable = Lobby.Current.IsHost;
		Lobby.Current.OnVisibilityChanged += OnVisibilityChanged;
		RefreshLobbySettingsUi();

		// Player settings
		Lobby.Current.OnPlayersChanged += OnPlayersChanged;
		ReconstructPlayerSlots();

		// Match rule
		SetMatchModeOptions(allMatchModes);
		matchModeDropdown.onValueChanged.AddListener(OnMatchModeDropdownValueChanged);
		boardSizeSlider.onValueChanged.AddListener(OnBoardSizeSliderValueChanged);
		stoneHardnessSlider.onValueChanged.AddListener(OnStoneHardnessSliderValueChanged);
		Lobby.Current.OnMatchRuleChanged += OnMatchRuleChanged;
		RefreshMatchRuleArea();

		// Footer
		startButton.interactable = Lobby.Current.IsHost;
		RefreshFooterArea();
	}

	protected void OnDestroy()
	{
		if(Lobby.Current != null)
		{
			Lobby.Current.OnVisibilityChanged -= OnVisibilityChanged;
			Lobby.Current.OnPlayersChanged -= OnPlayersChanged;
			Lobby.Current.OnMatchRuleChanged -= OnMatchRuleChanged;
		}
	}
	#endregion

	#region Life cycle
	public void LeaveLobby()
	{
		// TODO: Notify host as player
		GameManager.Instance.SwitchScene(GameScene.StartMenu);
	}
	#endregion

	#region Lobby settings
	[Header("Lobby Settings")]
	[SerializeField] Dropdown visibilityDropdown;

	static readonly LobbyVisibility[] allVisibilityOptions = new LobbyVisibility[]
	{
		LobbyVisibility.Local,
		LobbyVisibility.Private,
		LobbyVisibility.Public,
	};

	public LobbyVisibility Visibility => visibilityOptions[visibilityDropdown.value];
	readonly List<LobbyVisibility> visibilityOptions = new();

	void SetVisibilityOptions(params LobbyVisibility[] value) => SetVisibilityOptions(value as IList<LobbyVisibility>);
	void SetVisibilityOptions(IList<LobbyVisibility> value)
	{
		visibilityOptions.Clear();
		visibilityOptions.AddRange(value);

		// Refresh dropdown
		visibilityDropdown.options = visibilityOptions
			.Select((LobbyVisibility v, int i) => new Dropdown.OptionData(v.ToLocalizedString()))
			.ToList();
		if(visibilityDropdown.value >= visibilityOptions.Count)
			visibilityDropdown.value = 0;
	}

	public void OnVisibilityDropdownValueChanged(int index)
	{
		HostLobby.Current?.SetVisibility(visibilityOptions[visibilityDropdown.value]);
	}

	void OnVisibilityChanged()
	{
		RefreshLobbySettingsUi();
		ReconstructPlayerSlots();
	}

	void RefreshLobbySettingsUi()
	{
		if(Lobby.Current.Visibility != LobbyVisibility.Public)
			lobbyNameRow.SetActive(false);
		else
		{
			lobbyNameRow.SetActive(true);
			// TODO: Fill in lobby name text.
		}

		if(Lobby.Current.Visibility != LobbyVisibility.Private)
			invitationCodeRow.SetActive(false);
		else
		{
			invitationCodeRow.SetActive(true);
			invitationCodeText.text = "KFCTHURVME50";  // TODO
		}
	}

	[SerializeField] GameObject lobbyNameRow;
	[SerializeField] InputField lobbyNameInput;

	[SerializeField] GameObject invitationCodeRow;
	[SerializeField] Text invitationCodeText;
	#endregion

	#region Player settings
	[Header("Player settings")]
	[SerializeField] Transform playerSlotList;
	static GameObject playerSlotPrefab;

	[SerializeField] Button addPlayerButton;

	void OnPlayersChanged()
	{
		ReconstructPlayerSlots();
	}

	void ReconstructPlayerSlots()
	{
		GameUtility.ClearChildren(playerSlotList);

		foreach(var player in Lobby.Current.Players)
		{
			var slotGo = Instantiate(playerSlotPrefab, playerSlotList);
			var slot = slotGo.GetComponent<LobbyPlayerSlot>();
			slot.Descriptor = player;
		}

		addPlayerButton.interactable = Lobby.Current.IsHost && Lobby.Current.Players.Count < 4;
	}

	public void OnAddPlayerButtonClicked()
	{
		HostLobby.Current?.AddPlayer();
	}
	#endregion

	#region Match rule
	[Header("Match Rule")]
	[SerializeField] Dropdown matchModeDropdown;
	[SerializeField] Text boardSizeText;
	[SerializeField] Slider boardSizeSlider;
	[SerializeField] Text stoneHardnessText;
	[SerializeField] Slider stoneHardnessSlider;

	static readonly MatchMode[] allMatchModes = new MatchMode[]
	{
		MatchMode.Traditional,
		MatchMode.Training,
	};

	readonly List<MatchMode> matchModeOptions = new();
	void SetMatchModeOptions(IList<MatchMode> options)
	{
		matchModeOptions.Clear();
		matchModeOptions.AddRange(options);
		matchModeDropdown.options = matchModeOptions
			.Select(m => new Dropdown.OptionData(m.ToLocalizedString()))
			.ToList();

		int index = matchModeOptions.IndexOf(Lobby.Current.MatchRule.mode);
		if(index == -1)
			index = 0;
		matchModeDropdown.value = index;
	}

	void OnMatchModeDropdownValueChanged(int index)
	{
		var rule = Lobby.Current.MatchRule;
		rule.mode = matchModeOptions[index];
		HostLobby.Current?.SetMatchRule(rule);
	}

	void OnBoardSizeSliderValueChanged(float value)
	{
		var rule = Lobby.Current.MatchRule;
		rule.boardSize = Mathf.RoundToInt(value);
		HostLobby.Current?.SetMatchRule(rule);
	}

	void OnStoneHardnessSliderValueChanged(float value)
	{
		var rule = Lobby.Current.MatchRule;
		rule.stoneHardness = value;
		HostLobby.Current?.SetMatchRule(rule);
	}

	void OnMatchRuleChanged()
	{
		RefreshMatchRuleArea();
	}

	void RefreshMatchRuleArea()
	{
		var rule = Lobby.Current.MatchRule;
		boardSizeText.text = rule.boardSize.ToString();
		boardSizeSlider.value = rule.boardSize;
		stoneHardnessText.text = rule.stoneHardness.ToString("F1");
		stoneHardnessSlider.value = rule.stoneHardness;
	}
	#endregion

	#region Footer
	[Header("Footer")]
	[SerializeField] Button startButton;
	[SerializeField] Text errorText;

	void RefreshFooterArea()
	{
		bool valid = ValidateStartingCondition(out string errorMessage);
		startButton.interactable = valid;
		errorText.gameObject.SetActive(!valid);
		errorText.text = errorMessage;
	}

	bool ValidateStartingCondition(out string errorMessage)
	{
		errorMessage = null;
		return true;
	}

	public void OnStartButtonClicked()
	{
		// TODO
	}
	#endregion
}
