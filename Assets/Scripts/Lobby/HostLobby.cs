using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class HostLobby : Lobby
{
	public new static HostLobby Current => Lobby.Current as HostLobby;
	public static readonly PlayerLocator HostPlayerLocator = new("host");

	#region Creation
	static PlayerLocator MakeRemotePlayerLocator(int i)
	{
		return new PlayerLocator($"remote-{i}");
	}

	static IEnumerable<PlayerDescriptor> MakeDefaultPlayerList()
	{
		yield return new()
		{
			type = PlayerType.Local,
			isHost = true,
			locator = HostPlayerLocator,

			color = Color.black,
		};
		yield return new()
		{
			type = PlayerType.Ai,
			isHost = false,
			locator = HostPlayerLocator,
			aiId = LaoSongAiConfig.Id,

			color = Color.white,
		};
	}

	static PlayerDescriptor MakeNewPlayer(int i)
	{
		return new()
		{
			type = PlayerType.Local,
			isHost = false,
			locator = HostPlayerLocator,

			color = new Color[] { Color.red, Color.green, Color.blue, Color.yellow }[i],
		};
	}

	public HostLobby(string defaultMatchModeId)
	{
		players.AddRange(MakeDefaultPlayerList());
		matchRule = new MatchRule
		{
			modeId = defaultMatchModeId,
			boardSize = 19,
			stoneHardness = 1f,
		};
	}
	#endregion

	#region Status
	public void StartMatch()
	{
		// TODO
		OnStartingMatch?.Invoke();
	}

	public void Dismiss()
	{
		// TODO
		OnDismissed?.Invoke();
	}

	public void EndMatch()
	{
		OnMatchEnded?.Invoke();
	}
	#endregion

	#region Lobby settings
	LobbyVisibility visibility = LobbyVisibility.Local;
	public override LobbyVisibility Visibility => visibility;
	public void SetVisibility(LobbyVisibility value)
	{
		visibility = value;
		OnVisibilityChanged?.Invoke();
	}
	#endregion

	#region Players
	readonly List<PlayerDescriptor> players = new();
	public override List<PlayerDescriptor> Players => players;
	public override PlayerLocator LocalPlayerLocator => HostPlayerLocator;

	public void SetPlayerType(int i, PlayerType type)
	{
		if(!players.IsValidIndex(i))
		{
			Debug.LogWarning($"Failed to set player #{i}'s type to {type.ToLocalizedString()}.");
			return;
		}
		if(!IsOnline && type == PlayerType.Online)
		{
			Debug.LogWarning($"Cannot set player #{i}'s type to {type.ToLocalizedString()} because the lobby is offline.");
			return;
		}

		PlayerDescriptor player = players[i];
		player.type = type;
		player.locator = type == PlayerType.Online
			? MakeRemotePlayerLocator(i)
			: HostPlayerLocator;
		if(type == PlayerType.Ai)
		{
			if(string.IsNullOrWhiteSpace(player.aiId) && GameManager.Instance != null)
			{
				AiConfig defaultAi = GameManager.Instance.FindFirstAiForMode(matchRule.modeId);
				player.aiId = defaultAi != null ? defaultAi.AiId : null;
			}
		}
		else
		{
			player.aiId = null;
		}

		players[i] = player;
		OnPlayersChanged?.Invoke();
	}

	public void RemovePlayer(int i)
	{
		if(!players.IsValidIndex(i))
		{
			Debug.LogWarning($"Failed to remove player #{i} because there are only {players.Count} players.");
			return;
		}
		if(players.Count <= 2)
		{
			Debug.LogWarning($"Cannot remove player #{i} because a match must have at least 2 players. Current player count = {players.Count}.");
			return;
		}
		players.RemoveAt(i);
		OnPlayersChanged?.Invoke();
	}

	public void AddPlayer()
	{
		if(players.Count >= 4)
		{
			Debug.LogWarning($"Cannot add new player because a match can have at most 4 players. Current player count = {players.Count}.");
			return;
		}
		players.Add(MakeNewPlayer(players.Count));
		OnPlayersChanged?.Invoke();
	}
	#endregion

	#region Match rules
	MatchRule matchRule;
	public override MatchRule MatchRule => matchRule;
	public void SetMatchRule(MatchRule value)
	{
		matchRule = value;
		OnMatchRuleChanged?.Invoke();
	}
	#endregion
}