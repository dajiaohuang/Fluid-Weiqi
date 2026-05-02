using UnityEngine;

[CreateAssetMenu(fileName = "TrainingMatchModeConfig", menuName = "FluidWeiqi/Match Mode Config/Training")]
public class TrainingMatchModeConfig : MatchModeConfig
{
	protected override Match CreateMatchController(GameObject host)
	{
		return host.AddComponent<TrainingMatch>();
	}
}
