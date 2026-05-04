using System;

public interface ILobbySyncTransport
{
	event Action<LobbySyncSnapshot> OnSnapshotReceived;
	event Action<PlayerLocator> OnClientConnected;
	event Action<PlayerLocator> OnClientDisconnected;
	event Action OnLobbyClosed;

	bool IsHost { get; }
	LobbyLocator LobbyLocator { get; }
	PlayerLocator LocalPlayerLocator { get; }

	void ConfigureAsHost(LobbyLocator lobbyLocator, PlayerLocator hostLocator);
	void ConfigureAsClient(LobbyLocator lobbyLocator, PlayerLocator localPlayerLocator);

	void BroadcastSnapshot(LobbySyncSnapshot snapshot);
	void NotifyClientDisconnected(PlayerLocator playerLocator);
}
