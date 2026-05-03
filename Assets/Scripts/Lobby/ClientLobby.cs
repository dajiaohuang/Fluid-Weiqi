using UnityEngine;
using System;
using System.Collections.Generic;

public class ClientLobby : Lobby
{
	readonly List<PlayerDescriptor> players = new();
	readonly LobbyLocator locator;
	readonly PlayerLocator localPlayerLocator;

	LobbyVisibility visibility;
	MatchRule matchRule;

	public override LobbyVisibility Visibility => visibility;
	public override List<PlayerDescriptor> Players => players;
	public override MatchRule MatchRule => matchRule;
	public override PlayerLocator LocalPlayerLocator => localPlayerLocator;

	public ClientLobby(LobbyLocator locator, PlayerLocator localPlayerLocator, LobbyVisibility visibility, MatchRule matchRule, IReadOnlyList<PlayerDescriptor> initialPlayers)
	{
		this.locator = locator;
		this.localPlayerLocator = localPlayerLocator;
		this.visibility = visibility;
		this.matchRule = matchRule;
		ReplacePlayers(initialPlayers);
	}

	public LobbyLocator Locator => locator;

	public void ApplySnapshot(LobbyVisibility newVisibility, MatchRule newMatchRule, IReadOnlyList<PlayerDescriptor> snapshotPlayers)
	{
		bool changedVisibility = visibility != newVisibility;
		visibility = newVisibility;
		if(changedVisibility)
			OnVisibilityChanged?.Invoke();

		matchRule = newMatchRule;
		OnMatchRuleChanged?.Invoke();

		ReplacePlayers(snapshotPlayers);
	}

	void ReplacePlayers(IReadOnlyList<PlayerDescriptor> source)
	{
		players.Clear();
		if(source != null)
			players.AddRange(source);
		OnPlayersChanged?.Invoke();
	}

	public void NotifyLobbyDismissed()
	{
		OnDismissed?.Invoke();
	}

	public void NotifyMatchStarting()
	{
		OnStartingMatch?.Invoke();
	}

	public void NotifyMatchEnded()
	{
		OnMatchEnded?.Invoke();
	}
}