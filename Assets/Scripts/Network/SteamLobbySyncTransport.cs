#if !DISABLESTEAMWORKS
using System;
using UnityEngine;
using Steamworks;

/// <summary>
/// Steam-backed implementation of ILobbySyncTransport.
///
/// Host writes the serialized LobbySyncSnapshot as a single Steam Lobby
/// metadata entry under the key "snapshot". Steam automatically pushes
/// LobbyDataUpdate_t to every member whenever any metadata changes, so no
/// manual per-client messaging is needed.
///
/// Member join/leave is detected via LobbyChatUpdate_t.
/// </summary>
public class SteamLobbySyncTransport : ILobbySyncTransport
{
	const string SnapshotKey = "snapshot";
	const string KeyCode = "code";
	const string KeyPlaying = "playing";
	const string KeyOpenSlots = "open_online_slots";
	const string KeyPlayers = "players";

	// -------------------------------------------------------------------------
	// ILobbySyncTransport
	// -------------------------------------------------------------------------

	public event Action<LobbySyncSnapshot> OnSnapshotReceived;
	public event Action<PlayerLocator> OnClientConnected;
	public event Action<PlayerLocator> OnClientDisconnected;
	public event Action OnLobbyClosed;

	public bool IsHost { get; private set; }
	public LobbyLocator LobbyLocator { get; private set; }
	public PlayerLocator LocalPlayerLocator { get; private set; }

	// -------------------------------------------------------------------------
	// Steam callbacks (registered on configure, disposed on detach)
	// -------------------------------------------------------------------------

	Callback<LobbyDataUpdate_t>  cbLobbyDataUpdate;
	Callback<LobbyChatUpdate_t>  cbLobbyChatUpdate;
	ulong remoteHostSteamId;

	// -------------------------------------------------------------------------
	// Configure
	// -------------------------------------------------------------------------

	public void ConfigureAsHost(LobbyLocator lobbyLocator, PlayerLocator hostLocator)
	{
		Detach();
		IsHost = true;
		LobbyLocator = lobbyLocator;
		LocalPlayerLocator = hostLocator;
		remoteHostSteamId = 0;
		RegisterCallbacks();
	}

	public void ConfigureAsClient(LobbyLocator lobbyLocator, PlayerLocator localPlayerLocator)
	{
		Detach();
		IsHost = false;
		LobbyLocator = lobbyLocator;
		LocalPlayerLocator = localPlayerLocator;
		remoteHostSteamId = ResolveLobbyOwnerSteamId(lobbyLocator);
		RegisterCallbacks();
	}

	// -------------------------------------------------------------------------
	// BroadcastSnapshot (host only)
	// Host writes to lobby metadata; Steam pushes LobbyDataUpdate_t to all members.
	// -------------------------------------------------------------------------

	public void BroadcastSnapshot(LobbySyncSnapshot snapshot)
	{
		if(!IsHost || !LobbyLocator.IsValid)
			return;
		if(!ulong.TryParse(LobbyLocator.id, out ulong rawId))
			return;

		CSteamID steamLobbyId = new CSteamID(rawId);

		// Always keep the lobby publicly listed on Steam so RequestLobbyList can
		// find it.  Privacy is enforced by the "code" metadata key:
		//   Public  → code = ""         (visible in public search)
		//   Private → code = <invite>   (hidden from public search, found by code)
		SteamMatchmaking.SetLobbyType(steamLobbyId, ELobbyType.k_ELobbyTypePublic);

		string codeValue = snapshot.visibility == LobbyVisibility.Private
			? (snapshot.invitationCode ?? "")
			: "";
		SteamMatchmaking.SetLobbyData(steamLobbyId, KeyCode, codeValue);

		int openOnlineSlots = CountOpenOnlineSlots(snapshot);
		int playerCount = snapshot.players != null ? snapshot.players.Count : 0;
		SteamMatchmaking.SetLobbyData(steamLobbyId, KeyPlaying, snapshot.isMatchInProgress ? "1" : "0");
		SteamMatchmaking.SetLobbyData(steamLobbyId, KeyOpenSlots, openOnlineSlots.ToString());
		SteamMatchmaking.SetLobbyData(steamLobbyId, KeyPlayers, playerCount.ToString());

		string json = NetworkSerializer.SerializeLobbySnapshot(snapshot);
		SteamMatchmaking.SetLobbyData(steamLobbyId, SnapshotKey, json);
	}

	// -------------------------------------------------------------------------
	// NotifyClientDisconnected (client only)
	// Clients just leave the Steam lobby; the host detects this via LobbyChatUpdate_t.
	// Nothing to send explicitly.
	// -------------------------------------------------------------------------

	public void NotifyClientDisconnected(PlayerLocator playerLocator)
	{
		// No-op: Steam lobby departure is detected on the host via LobbyChatUpdate_t.
	}

	// -------------------------------------------------------------------------
	// Internal
	// -------------------------------------------------------------------------

	void RegisterCallbacks()
	{
		cbLobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
		cbLobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
	}

	void Detach()
	{
		cbLobbyDataUpdate?.Dispose();
		cbLobbyChatUpdate?.Dispose();
		cbLobbyDataUpdate = null;
		cbLobbyChatUpdate = null;
	}

	void OnLobbyDataUpdate(LobbyDataUpdate_t data)
	{
		// Only care about metadata on our specific lobby (not member-level data)
		if(data.m_ulSteamIDLobby != data.m_ulSteamIDMember)
			return;
		if(!LobbyLocator.IsValid)
			return;
		if(!ulong.TryParse(LobbyLocator.id, out ulong rawId))
			return;
		if(data.m_ulSteamIDLobby != rawId)
			return;

		CSteamID steamLobbyId = new CSteamID(rawId);
		string json = SteamMatchmaking.GetLobbyData(steamLobbyId, SnapshotKey);
		if(string.IsNullOrEmpty(json))
			return;

		LobbySyncSnapshot snapshot = NetworkSerializer.DeserializeLobbySnapshot(json);
		if(snapshot != null)
			OnSnapshotReceived?.Invoke(snapshot);
	}

	void OnLobbyChatUpdate(LobbyChatUpdate_t data)
	{
		if(!LobbyLocator.IsValid)
			return;
		if(!ulong.TryParse(LobbyLocator.id, out ulong rawId))
			return;
		if(data.m_ulSteamIDLobby != rawId)
			return;

		const uint enteredFlags =
			(uint)EChatMemberStateChange.k_EChatMemberStateChangeEntered;

		if((data.m_rgfChatMemberStateChange & enteredFlags) != 0)
		{
			if(!IsHost)
				return;
			if(data.m_ulSteamIDUserChanged == SteamUser.GetSteamID().m_SteamID)
				return;

			var locator = new PlayerLocator(data.m_ulSteamIDUserChanged.ToString());
			OnClientConnected?.Invoke(locator);
			return;
		}

		// Fire for any state that means the member has left
		const uint leftFlags =
			(uint)EChatMemberStateChange.k_EChatMemberStateChangeLeft       |
			(uint)EChatMemberStateChange.k_EChatMemberStateChangeDisconnected |
			(uint)EChatMemberStateChange.k_EChatMemberStateChangeKicked      |
			(uint)EChatMemberStateChange.k_EChatMemberStateChangeBanned;

		if((data.m_rgfChatMemberStateChange & leftFlags) != 0)
		{
			if(!IsHost && remoteHostSteamId != 0 && data.m_ulSteamIDUserChanged == remoteHostSteamId)
			{
				OnLobbyClosed?.Invoke();
				return;
			}
			if(!IsHost)
				return;

			var locator = new PlayerLocator(data.m_ulSteamIDUserChanged.ToString());
			OnClientDisconnected?.Invoke(locator);
		}
	}

	static ulong ResolveLobbyOwnerSteamId(LobbyLocator lobbyLocator)
	{
		if(!lobbyLocator.IsValid)
			return 0;
		if(!ulong.TryParse(lobbyLocator.id, out ulong rawId))
			return 0;

		return SteamMatchmaking.GetLobbyOwner(new CSteamID(rawId)).m_SteamID;
	}

	static int CountOpenOnlineSlots(LobbySyncSnapshot snapshot)
	{
		if(snapshot?.players == null)
			return 0;

		int open = 0;
		for(int i = 0; i < snapshot.players.Count; ++i)
		{
			LobbyPlayerSnapshot player = snapshot.players[i];
			if(player == null || player.type != PlayerType.Online)
				continue;

			if(!player.locator.IsValid || (player.locator.id != null && player.locator.id.StartsWith("remote-", StringComparison.Ordinal)))
				open += 1;
		}

		return open;
	}
}
#endif
