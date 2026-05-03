using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public struct MatchRule
{
	public string modeId;
	public int boardSize;
	public float stoneHardness;
}

public struct PlayerInfo
{
	public string name;
	public Color color;
}

public abstract class Match : MonoBehaviour
{
	public static Match Current { get; private set; }
	public static Match Get<T>() where T : Match
		=> Current as T;
	public MatchRule Rule { get; set; }

	readonly List<MatchPlayer> players = new();
	bool isEnded;
	public bool IsEnded => isEnded;

	#region Unity life cycle
	protected void Awake()
	{
		Current = this;
	}

	protected void Start()
	{
		InitializePlayers();
		CurrentPlayerIndex = 0;
		BeginCurrentPlayerTurn();
	}

	protected void OnDestroy()
	{
		players.Clear();
	}
	#endregion

	#region Board
	protected Action onStateChanged;
	public event Action OnStateChanged
	{
		add => onStateChanged += value;
		remove => onStateChanged -= value;
	}

	protected bool LastPlacementSucceed { get; private set; } = false;

	protected Action onEnd;
	public event Action OnEnd
	{
		add => onEnd += value;
		remove => onEnd -= value;
	}

	protected void EndMatch()
	{
		if(isEnded)
			return;

		isEnded = true;
		CancelAllPlayers();
		onEnd?.Invoke();
	}
	#endregion

	#region Input
	public bool InputEnabled
	{
		get => !isEnded && players.Count > 0;
		set
		{
			if(!value)
			{
				CancelAllPlayers();
				return;
			}

			if(!isEnded)
				BeginCurrentPlayerTurn();
		}
	}

	public void ReceiveCursorEnter(Vector2 position)
	{
		OnCursorEnter(position);
	}

	public void ReceiveCursorMove(Vector2 position)
	{
		OnCursorMove(position);
	}

	public void ReceiveCursorExit()
	{
		OnCursorExit();
	}

	public bool ReceivePlace(Vector2 position)
	{
		OnPlace(position);
		return LastPlacementSucceed;
	}

	public void ReceiveRemove(Vector2 position)
	{
		OnRemove(position);
	}

	public void ReceivePass()
	{
		OnPass();
	}

	protected virtual void OnCursorEnter(Vector2 position)
	{
		TryPreviewStone(position);
	}

	protected virtual void OnCursorMove(Vector2 position)
	{
		TryPreviewStone(position);
	}

	protected virtual void OnCursorExit()
	{
		Board.Current.ClearPreview();
	}

	protected virtual void OnPlace(Vector2 position)
	{
		LastPlacementSucceed = false;

		Board board = Board.Current;
		if(board == null)
			return;

		BoardState currentState = board.State;
		AnalyzeState(currentState);

		LastPlacementSucceed = BoardUtility.TryPlaceStoneStandard(
			board.Caches, currentState, currentPlayerIndex, position, out BoardState nextState);
		if(!LastPlacementSucceed)
			return;

		if(AudioManager.Instance != null)
			AudioManager.Instance.PlayPlaceStoneSound();

		int capturedStoneCount = CountCapturedStones(currentState, nextState, currentPlayerIndex);
		if(capturedStoneCount > 0 && AudioManager.Instance != null)
			AudioManager.Instance.PlayCaptureSound();

		board.SetState(nextState);
		board.ClearPreview();
		onStateChanged?.Invoke();
	}

	protected virtual void OnRemove(Vector2 position) { }

	protected virtual void OnPass() { }

	bool TryPreviewStone(Vector2 position)
	{
		Board board = Board.Current;
		if(board == null)
			return false;

		BoardState state = board.State;

		if(IsOccupiedAtAbsolutePosition(board, state, position))
		{
			board.ClearPreview();
			return false;
		}

		if(position.x < 0 || position.x >= state.Size || position.y < 0 || position.y >= state.Size)
		{
			board.ClearPreview();
			return false;
		}

		BoardState previewState = new(state);
		previewState.AddStone(currentPlayerIndex, position);
		board.ShowPreview(previewState);
		return true;
	}

	bool IsOccupiedAtAbsolutePosition(Board board, BoardState renderState, Vector2 position)
	{
		AnalyzeState(renderState);
		if(board == null || board.Caches == null)
			return false;
		return BoardUtility.IsOccupiedAtAbsolutePosition(board.Caches, renderState, position);
	}

	void AnalyzeState(BoardState renderState)
	{
		Board board = Board.Current;
		if(renderState == null || board == null || board.Caches == null || !board.Caches.isInitialized)
			return;

		Color[] playerColors = new Color[Mathf.Min(PlayerCount, BoardUtility.MaxPlayers)];
		for(int i = 0; i < playerColors.Length; ++i)
			playerColors[i] = PlayerInfos[i].color;
		BoardUtility.RenderAnalysis(board.Caches, renderState, playerColors);
	}
	#endregion

	#region Players
	public IReadOnlyList<PlayerInfo> PlayerInfos { get; set; }
	public int PlayerCount => PlayerInfos.Count;

	int currentPlayerIndex = 0;
	public int CurrentPlayerIndex
	{
		get => currentPlayerIndex % PlayerCount;
		protected set
		{
			int playerCount = Mathf.Max(1, PlayerCount);
			currentPlayerIndex = ((value % playerCount) + playerCount) % playerCount;
			onCurrentPlayerChanged?.Invoke(currentPlayerIndex);
		}
	}

	protected Action<int> onCurrentPlayerChanged;
	public event Action<int> OnCurrentPlayerChanged
	{
		add => onCurrentPlayerChanged += value;
		remove => onCurrentPlayerChanged -= value;
	}

	protected Action<int, bool> onPlayerPassStateChanged;
	public event Action<int, bool> OnPlayerPassStateChanged
	{
		add => onPlayerPassStateChanged += value;
		remove => onPlayerPassStateChanged -= value;
	}

	protected void SetPlayerPassState(int playerIndex, bool passed)
	{
		onPlayerPassStateChanged?.Invoke(playerIndex, passed);
	}

	protected void StepPlayerIndex()
	{
		CurrentPlayerIndex = (CurrentPlayerIndex + 1) % PlayerCount;
	}

	void InitializePlayers()
	{
		players.Clear();

		for(int i = 0; i < PlayerCount; ++i)
		{
			MatchPlayer player = CreateRuntimePlayer(i);
			if(player == null)
				throw new MissingReferenceException($"Failed to create player runtime for index {i}.");

			int playerIndex = i;
			player.OnMadeMove += () => OnPlayerMadeMove(playerIndex);
			players.Add(player);
		}
	}

	LocalPlayer CreateLocalPlayerFallback(int playerIndex)
	{
		LocalPlayer fallback = gameObject.AddComponent<LocalPlayer>();
		fallback.Initialize(this, playerIndex);
		return fallback;
	}

	OnlinePlayer CreateOnlinePlayer(int playerIndex, OnlinePlayerRole role)
	{
		OnlinePlayer online = gameObject.AddComponent<OnlinePlayer>();
		online.Initialize(this, playerIndex, role);
		return online;
	}

	MatchPlayer CreateRuntimePlayer(int playerIndex)
	{
		if(Lobby.Current == null || Lobby.Current.Players == null || playerIndex < 0 || playerIndex >= Lobby.Current.Players.Count)
			return CreateLocalPlayerFallback(playerIndex);

		Lobby lobby = Lobby.Current;
		PlayerDescriptor descriptor = lobby.Players[playerIndex];
		bool ownedByLocal = lobby.IsOwnedByLocal(descriptor);
		PlayerType playerType = descriptor.type;
		switch(playerType)
		{
			case PlayerType.Local:
				if(!ownedByLocal)
					return CreateOnlinePlayer(playerIndex, OnlinePlayerRole.RemoteToLocal);

				LocalPlayer local = gameObject.AddComponent<LocalPlayer>();
				local.Initialize(this, playerIndex);
				return local;
			case PlayerType.Ai:
				if(!ownedByLocal)
				{
					Debug.LogWarning($"AI slot {playerIndex} is not owned by this client, fallback to OnlinePlayer proxy.");
					return CreateOnlinePlayer(playerIndex, OnlinePlayerRole.RemoteToLocal);
				}

				if(GameManager.Instance == null)
				{
					Debug.LogWarning($"GameManager is missing, fallback to LocalPlayer for AI slot {playerIndex}.");
					return CreateLocalPlayerFallback(playerIndex);
				}

				AiConfig aiConfig = null;
				if(!string.IsNullOrWhiteSpace(descriptor.aiId))
					GameManager.Instance.TryGetAiConfig(descriptor.aiId, out aiConfig);

				if(aiConfig == null)
					aiConfig = GameManager.Instance.FindFirstAiForMode(Rule.modeId);

				if(aiConfig == null)
				{
					Debug.LogWarning($"No AI config available for mode '{Rule.modeId}', fallback to LocalPlayer at index {playerIndex}.");
					return CreateLocalPlayerFallback(playerIndex);
				}

				if(!aiConfig.SupportsMode(Rule.modeId))
				{
					Debug.LogWarning($"AI '{aiConfig.AiName}' ({aiConfig.AiId}) does not support mode '{Rule.modeId}', fallback to LocalPlayer at index {playerIndex}.");
					return CreateLocalPlayerFallback(playerIndex);
				}

				return aiConfig.CreatePlayer(this, playerIndex, Rule);
			case PlayerType.Online:
				return CreateOnlinePlayer(playerIndex, ownedByLocal ? OnlinePlayerRole.LocalToRemote : OnlinePlayerRole.RemoteToLocal);
			default:
				return CreateLocalPlayerFallback(playerIndex);
		}
	}

	void OnPlayerMadeMove(int playerIndex)
	{
		if(isEnded)
			return;
		if(playerIndex != CurrentPlayerIndex)
			return;

		StepPlayerIndex();
		BeginCurrentPlayerTurn();
	}

	void BeginCurrentPlayerTurn()
	{
		if(isEnded || players.Count == 0)
			return;

		CancelAllPlayers();
		SetPlayerPassState(CurrentPlayerIndex, false);

		int safeIndex = Mathf.Clamp(CurrentPlayerIndex, 0, players.Count - 1);
		BoardState state = Board.Current?.State;
		BoardState snapshot = state != null ? new BoardState(state) : null;
		players[safeIndex].RequestMove(snapshot);
	}

	void CancelAllPlayers()
	{
		for(int i = 0; i < players.Count; ++i)
			players[i]?.CancelMove();
	}

	int CountCapturedStones(BoardState oldState, BoardState newState, int placedPlayer)
	{
		int oldOpponentTotal = CountOpponentStones(oldState, placedPlayer);
		int newOpponentTotal = CountOpponentStones(newState, placedPlayer);
		return Mathf.Max(0, oldOpponentTotal - newOpponentTotal);
	}

	int CountOpponentStones(BoardState state, int excludedPlayer)
	{
		int count = 0;
		for(int player = 0; player < state.PlayerCount; ++player)
		{
			if(player == excludedPlayer)
				continue;

			var stones = state.GetStones(player);
			count += stones.Count;
		}
		return count;
	}
	#endregion
}
