using UnityEngine;

public class TraditionalMatch : Match
{
	#region UI
	TraditionalTrainingUi ui;

	protected override GameObject MakeUi()
	{
		var go = Instantiate(Resources.Load<GameObject>("UI/Match/Traditional"), transform);
		ui = go.GetComponent<TraditionalTrainingUi>();
		return go;
	}
	#endregion

	#region Input
	protected override void OnPlace(Vector2 position)
	{
		base.OnPlace(position);

		if(LastPlacementSucceed)
		{
			passCount = 0;
			StepPlayerIndex();
		}
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
			ui.ShowEnding();
			InputEnabled = false;
			return;
		}
		// TODO: Show pass UI

		StepPlayerIndex();
	}
	#endregion
}
