#if !DISABLESTEAMWORKS
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Steamworks;

/// <summary>
/// Steam-backed implementation of ILobbyService.
///
/// Lobby metadata keys:
///   "name"     – display name (host's Steam persona name)
///   "players"  – current member count (int, as string)
///   "max"      – max member count (int, as string)
///   "code"     – invitation code for private lobbies (only set when Private)
/// </summary>
public class SteamLobbyService : ILobbyService
{
	// Metadata key constants
	const string KeyName    = "name";
	const string KeyPlayers = "players";
	const string KeyMax     = "max";
	const string KeyCode    = "code";
	const string KeyGame    = "game";
	const string ValueGame  = "fluid_weiqi";
	const string KeyPlaying = "playing";
	const string KeyOpenOnlineSlots = "open_online_slots";

	// -------------------------------------------------------------------------
	// CreateLobby
	// -------------------------------------------------------------------------

	CallResult<LobbyCreated_t> createLobbyResult;

	public void CreateLobby(LobbyVisibility visibility, int maxMembers, Action<LobbyLocator> onCreated)
	{
		SteamAPICall_t call = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, maxMembers);
		createLobbyResult = CallResult<LobbyCreated_t>.Create((result, ioFailure) =>
		{
			if(ioFailure || result.m_eResult != EResult.k_EResultOK)
			{
				Debug.LogError($"[SteamLobbyService] CreateLobby failed: result={result.m_eResult} ioFailure={ioFailure}");
				onCreated?.Invoke(new LobbyLocator());
				return;
			}

			CSteamID steamLobbyId = new CSteamID(result.m_ulSteamIDLobby);
			// Write the host's persona name so clients can display it
			string hostName = SteamFriends.GetPersonaName();
			SteamMatchmaking.SetLobbyData(steamLobbyId, KeyName, hostName);
			SteamMatchmaking.SetLobbyData(steamLobbyId, KeyMax, maxMembers.ToString());
			SteamMatchmaking.SetLobbyData(steamLobbyId, KeyGame, ValueGame);
			SteamMatchmaking.SetLobbyData(steamLobbyId, KeyCode, string.Empty);
			SteamMatchmaking.SetLobbyData(steamLobbyId, KeyPlaying, "0");
			SteamMatchmaking.SetLobbyData(steamLobbyId, KeyOpenOnlineSlots, "0");

			onCreated?.Invoke(new LobbyLocator(result.m_ulSteamIDLobby.ToString()));
		});
		createLobbyResult.Set(call);
	}

	// -------------------------------------------------------------------------
	// QueryLobbies
	// -------------------------------------------------------------------------

	CallResult<LobbyMatchList_t> lobbyListResult;

	public void QueryLobbies(int offset, int count, string nameFilter, Action<IReadOnlyList<LobbySnapshot>> onResult)
	{
		SteamMatchmaking.AddRequestLobbyListStringFilter(KeyGame, ValueGame, ELobbyComparison.k_ELobbyComparisonEqual);
		SteamMatchmaking.AddRequestLobbyListResultCountFilter(Mathf.Max((offset + count) * 4, 32));

		SteamAPICall_t call = SteamMatchmaking.RequestLobbyList();
		lobbyListResult = CallResult<LobbyMatchList_t>.Create((result, ioFailure) =>
		{
			if(ioFailure)
			{
				Debug.LogError("[SteamLobbyService] QueryLobbies IO failure.");
				onResult?.Invoke(new List<LobbySnapshot>());
				return;
			}

			int total = (int)result.m_nLobbiesMatching;
			var snapshots = new List<LobbySnapshot>();
			int matchedCount = 0;

			for(int i = 0; i < total; ++i)
			{
				CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
				if(!lobbyId.IsValid())
					continue;

				if(!TryBuildPublicLobbySnapshot(lobbyId, out LobbySnapshot snapshot))
					continue;

				string lobbyName = snapshot.lobbyName;
				if(!string.IsNullOrEmpty(nameFilter) &&
				   !lobbyName.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
					continue;

				if(matchedCount++ < offset)
					continue;

				snapshots.Add(snapshot);
				if(snapshots.Count >= count)
					break;
			}

			onResult?.Invoke(snapshots);
		});
		lobbyListResult.Set(call);
	}

	// -------------------------------------------------------------------------
	// JoinLobby
	// -------------------------------------------------------------------------

	CallResult<LobbyEnter_t> joinLobbyResult;

	public void JoinLobby(string lobbyId, Action<JoinLobbyResult> onResult)
	{
		if(!ulong.TryParse(lobbyId, out ulong rawId))
		{
			Debug.LogError($"[SteamLobbyService] JoinLobby: invalid lobbyId '{lobbyId}'");
			onResult?.Invoke(new JoinLobbyResult { success = false });
			return;
		}

		CSteamID steamLobbyId = new CSteamID(rawId);
		SteamAPICall_t call = SteamMatchmaking.JoinLobby(steamLobbyId);
		if(call == SteamAPICall_t.Invalid)
		{
			Debug.LogError($"[SteamLobbyService] JoinLobby: SteamMatchmaking.JoinLobby returned Invalid for '{lobbyId}'");
			onResult?.Invoke(new JoinLobbyResult { success = false });
			return;
		}
		Debug.Log($"[SteamLobbyService] JoinLobby: issued for {lobbyId}, awaiting LobbyEnter_t...");
		joinLobbyResult = CallResult<LobbyEnter_t>.Create((result, ioFailure) =>
		{
			bool success = !ioFailure &&
			               result.m_ulSteamIDLobby != 0 &&
			               (EChatRoomEnterResponse)result.m_EChatRoomEnterResponse == EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess;

			if(!success)
			{
				Debug.LogError($"[SteamLobbyService] JoinLobby failed: ioFailure={ioFailure} response={result.m_EChatRoomEnterResponse}");
				onResult?.Invoke(new JoinLobbyResult { success = false });
				return;
			}

			Debug.Log($"[SteamLobbyService] JoinLobby succeeded: lobby={result.m_ulSteamIDLobby}");
			CSteamID joinedLobby = new CSteamID(result.m_ulSteamIDLobby);
			CSteamID localSteamId = SteamUser.GetSteamID();

			// Metadata is available immediately after LobbyEnter_t.
			// Read the snapshot to populate initial lobby state for the client.
			string snapshotJson = SteamMatchmaking.GetLobbyData(joinedLobby, "snapshot");
			LobbySyncSnapshot snapshot = string.IsNullOrEmpty(snapshotJson)
				? null
				: NetworkSerializer.DeserializeLobbySnapshot(snapshotJson);

			var joinResult = new JoinLobbyResult
			{
				success            = true,
				lobbyLocator       = new LobbyLocator(result.m_ulSteamIDLobby.ToString()),
				localPlayerLocator = new PlayerLocator(localSteamId.m_SteamID.ToString()),
				visibility         = snapshot != null ? snapshot.visibility : LobbyVisibility.Public,
				matchRule          = snapshot != null ? snapshot.matchRule  : default,
				players            = snapshot != null
					? NetworkSnapshotUtility.ToPlayerDescriptors(snapshot.players)
					: new List<PlayerDescriptor>(),
			};
			onResult?.Invoke(joinResult);
		});
		joinLobbyResult.Set(call);
	}

	// -------------------------------------------------------------------------
	// JoinLobbyByCode
	// -------------------------------------------------------------------------

	CallResult<LobbyMatchList_t> joinByCodeListResult;
	CallResult<LobbyEnter_t>     joinByCodeEnterResult;

	public void JoinLobbyByCode(string invitationCode, Action<JoinLobbyResult> onResult)
	{
		SteamMatchmaking.AddRequestLobbyListStringFilter(KeyGame, ValueGame, ELobbyComparison.k_ELobbyComparisonEqual);
		SteamMatchmaking.AddRequestLobbyListResultCountFilter(32);

		SteamAPICall_t call = SteamMatchmaking.RequestLobbyList();
		joinByCodeListResult = CallResult<LobbyMatchList_t>.Create((result, ioFailure) =>
		{
			if(ioFailure || result.m_nLobbiesMatching == 0)
			{
				Debug.LogWarning($"[SteamLobbyService] JoinLobbyByCode: no lobby found for code '{invitationCode}'");
				onResult?.Invoke(new JoinLobbyResult { success = false });
				return;
			}

			for(int i = 0; i < result.m_nLobbiesMatching; ++i)
			{
				CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
				if(!TryGetLobbyState(lobbyId, out LobbySyncSnapshot snapshot, out string codeValue, out bool isPlaying, out int openOnlineSlots))
					continue;
				if(snapshot == null || snapshot.visibility != LobbyVisibility.Private)
					continue;
				if(isPlaying || openOnlineSlots <= 0)
					continue;
				if(!string.Equals(codeValue, invitationCode, StringComparison.OrdinalIgnoreCase))
					continue;

				JoinLobby(lobbyId.m_SteamID.ToString(), onResult);
				return;
			}

			Debug.LogWarning($"[SteamLobbyService] JoinLobbyByCode: no joinable lobby found for code '{invitationCode}'");
			onResult?.Invoke(new JoinLobbyResult { success = false });
		});
		joinByCodeListResult.Set(call);
	}

	static bool TryBuildPublicLobbySnapshot(CSteamID lobbyId, out LobbySnapshot snapshot)
	{
		snapshot = default;
		if(!TryGetLobbyState(lobbyId, out LobbySyncSnapshot lobbyState, out string codeValue, out bool isPlaying, out int openOnlineSlots))
			return false;
		if(lobbyState == null || lobbyState.visibility != LobbyVisibility.Public)
			return false;
		if(!string.IsNullOrEmpty(codeValue) || isPlaying || openOnlineSlots <= 0)
			return false;

		string lobbyName = SteamMatchmaking.GetLobbyData(lobbyId, KeyName);
		if(string.IsNullOrWhiteSpace(lobbyName))
			return false;

		int currentPlayers = lobbyState.players != null ? lobbyState.players.Count : 0;
		int.TryParse(SteamMatchmaking.GetLobbyData(lobbyId, KeyMax), out int maxPlayers);
		if(maxPlayers <= 0)
			maxPlayers = Mathf.Max(currentPlayers, 1);

		snapshot = new LobbySnapshot
		{
			lobbyId = lobbyId.m_SteamID.ToString(),
			lobbyName = lobbyName,
			hostName = lobbyName,
			currentPlayers = currentPlayers,
			maxPlayers = maxPlayers,
		};
		return true;
	}

	static bool TryGetLobbyState(CSteamID lobbyId, out LobbySyncSnapshot snapshot, out string codeValue, out bool isPlaying, out int openOnlineSlots)
	{
		snapshot = null;
		codeValue = SteamMatchmaking.GetLobbyData(lobbyId, KeyCode) ?? string.Empty;
		isPlaying = SteamMatchmaking.GetLobbyData(lobbyId, KeyPlaying) == "1";
		openOnlineSlots = 0;

		string snapshotJson = SteamMatchmaking.GetLobbyData(lobbyId, "snapshot");
		if(string.IsNullOrWhiteSpace(snapshotJson))
			return false;

		snapshot = NetworkSerializer.DeserializeLobbySnapshot(snapshotJson);
		if(snapshot == null)
			return false;

		openOnlineSlots = CountOpenOnlineSlots(snapshot.players);
		return true;
	}

	static int CountOpenOnlineSlots(IReadOnlyList<LobbyPlayerSnapshot> players)
	{
		if(players == null)
			return 0;

		int open = 0;
		for(int i = 0; i < players.Count; ++i)
		{
			LobbyPlayerSnapshot player = players[i];
			if(player == null || player.type != PlayerType.Online)
				continue;
			if(!player.locator.IsValid || (player.locator.id != null && player.locator.id.StartsWith("remote-", StringComparison.Ordinal)))
				open += 1;
		}

		return open;
	}

	// -------------------------------------------------------------------------
	// RequestInvitationCode
	// -------------------------------------------------------------------------

	public void RequestInvitationCode(LobbyLocator lobbyLocator, Action<string> onResult)
	{
		if(!ulong.TryParse(lobbyLocator.id, out ulong rawId))
		{
			Debug.LogError($"[SteamLobbyService] RequestInvitationCode: invalid locator '{lobbyLocator.id}'");
			onResult?.Invoke(null);
			return;
		}

		// Generate a code and publish it as lobby metadata so JoinLobbyByCode can find it
		const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
		var sb = new StringBuilder(8);
		var rng = new System.Random();
		for(int i = 0; i < 8; ++i)
			sb.Append(chars[rng.Next(chars.Length)]);
		string code = sb.ToString();

		CSteamID steamLobbyId = new CSteamID(rawId);
		SteamMatchmaking.SetLobbyData(steamLobbyId, KeyCode, code);

		onResult?.Invoke(code);
	}

	// -------------------------------------------------------------------------
	// LeaveLobby
	// -------------------------------------------------------------------------

	public void LeaveLobby(LobbyLocator lobbyLocator)
	{
		if(!ulong.TryParse(lobbyLocator.id, out ulong rawId))
			return;
		SteamMatchmaking.LeaveLobby(new CSteamID(rawId));
	}
}
#endif
