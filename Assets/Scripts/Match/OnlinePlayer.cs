using UnityEngine;

public enum OnlinePlayerRole
{
	RemoteToLocal,
	LocalToRemote,
}

public class OnlinePlayer : MatchPlayer
{
	bool isConnected = true;
	OnlinePlayerRole role;

	public override bool IsAlive => isConnected;

	public void Initialize(Match match, int playerIndex, OnlinePlayerRole role)
	{
		base.Initialize(match, playerIndex);
		this.role = role;
	}

	public override void RequestMove(BoardState state)
	{
		// Networking is not wired yet. Keep turn flow alive with deterministic pass fallback.
		if(!isConnected || role == OnlinePlayerRole.RemoteToLocal)
		{
			Match.ReceivePass();
			if(!Match.IsEnded)
				NotifyMadeMove();
			return;
		}

		Match.ReceivePass();
		if(!Match.IsEnded)
			NotifyMadeMove();
	}

	public override void CancelMove()
	{
	}

	public void SetConnectionState(bool alive)
	{
		isConnected = alive;
	}
}