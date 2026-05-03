using UnityEngine;

public class TraditionalMatch : Match
{
	#region Input
	protected override void OnPlace(Vector2 position)
	{
		base.OnPlace(position);

		if(LastPlacementSucceed)
			passCount = 0;
	}

	int passCount = 0;
	protected override void OnPass()
	{
		Board.Current.ClearPreview();

		if(AudioManager.Instance != null)
			AudioManager.Instance.PlaySkipSound();

		++passCount;
		if(passCount == PlayerCount)
		{
			EndMatch();
			return;
		}
	}
	#endregion
}
