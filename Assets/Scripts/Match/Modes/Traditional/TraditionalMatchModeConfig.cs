using UnityEngine;

[CreateAssetMenu(fileName = "TraditionalMatchModeConfig", menuName = "FluidWeiqi/Match Mode Config/Traditional")]
public class TraditionalMatchModeConfig : MatchModeConfig
{
	protected override Match CreateMatchController(GameObject host)
	{
		return host.AddComponent<TraditionalMatch>();
	}
}
