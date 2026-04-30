using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public enum LobbyVisibility
{
	Local, Private, Public
}

public struct LobbyLocator
{
}

public enum PlayerType
{
	Local, Ai, Online
}

public struct PlayerLocator
{
}

public class PlayerDescriptor
{
	public PlayerType type;
	public bool isHost;
	public PlayerLocator locator;

	public int Index => Lobby.Current?.Players.IndexOf(this) ?? -1;

	public Color color;
	public string GetLocalizedName()
	{
		return type switch
		{
			PlayerType.Local => $"本地玩家 {Index}",
			PlayerType.Ai => $"AI 玩家 {Index}",  // TODO
			PlayerType.Online => $"网络玩家 {Index}",  // TODO
			_ => null,
		};
	}
}

public abstract class Lobby
{
	public static Lobby Current
	{
		get
		{
			if(GameManager.Instance == null)
				return null;
			return GameManager.Instance.Lobby;
		}
	}

	#region Online status
	public bool IsHost => this is HostLobby;
	public bool IsOnline => Visibility != LobbyVisibility.Local;
	#endregion

	#region Lobby settings
	public abstract LobbyVisibility Visibility { get; }
	public Action OnVisibilityChanged;
	#endregion

	#region Players
	public abstract List<PlayerDescriptor> Players { get; }
	public PlayerDescriptor HostPlayer => Players.FirstOrDefault(p => p.isHost);
	public IEnumerable<PlayerDescriptor> UniqueOnlinePlayers => Players.Where(p => p.type == PlayerType.Online).Distinct();
	public Action OnPlayersChanged;
	#endregion

	#region Match rules
	public abstract MatchRule MatchRule { get; }
	public Action OnMatchRuleChanged;
	#endregion
}

public class HostLobby : Lobby
{
	public new static HostLobby Current => Lobby.Current as HostLobby;

	#region Creation
	static IEnumerable<PlayerDescriptor> MakeDefaultPlayerList()
	{
		yield return new()
		{
			type = PlayerType.Local,
			isHost = true,

			color = Color.black,
		};
		yield return new()
		{
			type = PlayerType.Local,
			isHost = false,

			color = Color.white,
		};
	}

	static PlayerDescriptor MakeNewPlayer(int i)
	{
		return new()
		{
			type = PlayerType.Local,
			isHost = false,

			color = new Color[] { Color.red, Color.green, Color.blue, Color.yellow }[i],
		};
	}

	public HostLobby()
	{
		players.AddRange(MakeDefaultPlayerList());
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
		players[i].type = type;
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
	MatchRule matchRule = new()
	{
		mode = MatchMode.Traditional,
		boardSize = 19,
		stoneHardness = 1f,
	};
	public override MatchRule MatchRule => matchRule;
	public void SetMatchRule(MatchRule value)
	{
		matchRule = value;
		OnMatchRuleChanged?.Invoke();
	}
	#endregion
}