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

	#region Status
	public bool IsHost => this is HostLobby;
	public bool IsOnline => Visibility != LobbyVisibility.Local;

	public Action OnDismissed;
	public Action OnStartingMatch;
	#endregion

	#region Lobby settings
	public abstract LobbyVisibility Visibility { get; }
	public Action OnVisibilityChanged;

	public string GetInvitationCode()
	{
		if(Visibility != LobbyVisibility.Private)
			return null;
		return "KFVTHURVME50";  // TODO
	}
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

	public bool ValidateStartingCondition(out string errorMessage)
	{
		errorMessage = null;

		if(Players == null || Players.Count < 2)
		{
			errorMessage = "至少需要 2 名玩家才能开始对局。";
			return false;
		}

		if(GameManager.Instance == null)
		{
			errorMessage = "GameManager 未初始化。";
			return false;
		}

		if(string.IsNullOrWhiteSpace(MatchRule.modeId))
		{
			errorMessage = "未设置对局模式。";
			return false;
		}

		if(!GameManager.Instance.TryGetMatchModeConfig(MatchRule.modeId, out MatchModeConfig modeConfig))
		{
			errorMessage = $"未找到对局模式配置: {MatchRule.modeId}";
			return false;
		}

		if(IsOnline && modeConfig.IsDlcMode)
		{
			errorMessage = "联机模式暂不支持 DLC 对局模式。";
			return false;
		}

		if(!modeConfig.ValidateRules(MatchRule, this, out errorMessage))
			return false;

		return true;
	}
	#endregion
}