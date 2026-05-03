using System;
using System.Collections.Generic;

public interface ILobbyBrowser
{
	void QueryLobbies(int offset, int count, string nameFilter, Action<IReadOnlyList<LobbySnapshot>> onResult);
	void JoinLobby(string lobbyId, Action<bool> onResult);
}
