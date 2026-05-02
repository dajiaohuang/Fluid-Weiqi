using UnityEngine;

public class TrainingMatch : Match
{
	#region Input
	protected override void OnRemove(Vector2 position)
	{
		if(Board.Current.State.TryRemoveStoneAtLogicalPosition(position, out BoardState nextState))
		{
			Board.Current.SetState(nextState);
			AudioManager.Instance.PlayCaptureSound();
		}
	}

	protected override void OnPass()
	{
		StepPlayerIndex();
	}
	#endregion
}
