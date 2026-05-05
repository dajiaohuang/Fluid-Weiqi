public abstract class AiPlayer : MatchPlayer
{
	protected MatchRule Rule { get; private set; }
	protected AiConfig Config { get; private set; }

	public override bool IsAlive => true;

	public virtual void Initialize(Match match, int playerIndex, MatchRule rule, AiConfig config)
	{
		base.Initialize(match, playerIndex);
		Rule = rule;
		Config = config;
	}
}
