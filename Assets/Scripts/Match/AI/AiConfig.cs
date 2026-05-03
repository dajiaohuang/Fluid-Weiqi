using UnityEngine;
using System.Collections.Generic;

public abstract class AiConfig : ScriptableObject
{
	[SerializeField] string aiId;
	[SerializeField] string aiName;

	public virtual string AiId => aiId;
	public virtual string AiName => aiName;

	// Override this when a specific AI prefers hard-coded supported mode ids.
	public virtual IEnumerable<string> EnumerateSupportedModeIds()
	{
		yield break;
	}

	public virtual bool SupportsMode(string modeId)
	{
		if(string.IsNullOrWhiteSpace(modeId))
			return false;

		foreach(string supportedModeId in EnumerateSupportedModeIds())
		{
			if(string.Equals(supportedModeId, modeId, System.StringComparison.Ordinal))
				return true;
		}
		return false;
	}

	public abstract AiPlayer CreatePlayer(Match match, int playerIndex, MatchRule rule);
}

public abstract class AiConfig<TAiConfig> : AiConfig
	where TAiConfig : AiConfig<TAiConfig>
{
}
