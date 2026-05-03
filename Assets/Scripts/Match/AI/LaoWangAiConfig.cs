using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LaoWangAiConfig", menuName = "FluidWeiqi/AI/Lao Wang")]
public class LaoWangAiConfig : AiConfig<LaoWangAiConfig>
{
	[SerializeField] int sampleCount = 12;
	[SerializeField] public float startingIdealDistance = 3f;
	[SerializeField] public int startingTurnCount = 4;
	[SerializeField] float initialIdealDistance = 3f;
	[SerializeField] float finalIdealDistance = 0.75f;
	[SerializeField] float idealDistanceDecayRate = 0.2f;
	[SerializeField] float perCandidateEvaluationDelay = 0.08f;

	public int SampleCount => Mathf.Max(1, sampleCount);
	public float InitialIdealDistance => Mathf.Max(0.01f, initialIdealDistance);
	public float FinalIdealDistance => Mathf.Max(0.01f, finalIdealDistance);
	public float IdealDistanceDecayRate => Mathf.Max(0.001f, idealDistanceDecayRate);
	public float PerCandidateEvaluationDelay => Mathf.Max(0f, perCandidateEvaluationDelay);

	public override IEnumerable<string> EnumerateSupportedModeIds()
	{
		if(GameManager.Instance == null)
			yield break;

		IReadOnlyList<MatchModeConfig> modes = GameManager.Instance.LoadedMatchModeConfigs;
		if(modes == null)
			yield break;

		for(int i = 0; i < modes.Count; ++i)
		{
			MatchModeConfig mode = modes[i];
			if(mode == null || string.IsNullOrWhiteSpace(mode.ModeId) || !mode.IsTurnBased)
				continue;
			yield return mode.ModeId;
		}
	}

	public override bool SupportsMode(string modeId)
	{
		if(string.IsNullOrWhiteSpace(modeId) || GameManager.Instance == null)
			return false;
		if(!GameManager.Instance.TryGetMatchModeConfig(modeId, out MatchModeConfig modeConfig))
			return false;
		return modeConfig != null && modeConfig.IsTurnBased;
	}

	public override AiPlayer CreatePlayer(Match match, int playerIndex, MatchRule rule)
	{
		LaoWangAiPlayer player = match.gameObject.AddComponent<LaoWangAiPlayer>();
		player.Initialize(match, playerIndex, rule, this);
		return player;
	}
}
