using UnityEngine;
using System.Collections;

public class MatchBuilder : MonoBehaviour
{
	#region References
	[SerializeField] Transform uiRoot;
	[SerializeField] Transform boardAnchor;

	GameObject standardBoardPrefab;

	Match match;
	Board board;
	#endregion

	#region Build
	void BuildMatch()
	{
		switch(Lobby.Current.MatchRule.mode)
		{
			case MatchMode.Traditional:
				MakeStandardBoard();
				break;

			case MatchMode.Training:
				MakeStandardBoard();
				break;

			default:
				// TODO
				throw new System.NotSupportedException($"Cannot build match for {Lobby.Current.MatchRule.mode}, not supported.");
		}
	}

	Board MakeStandardBoard()
	{
		if(standardBoardPrefab == null)
			standardBoardPrefab = Resources.Load<GameObject>("Prefabs/Boards/Standard");
		var go = Instantiate(standardBoardPrefab, boardAnchor);
		return go.GetComponent<Board>();
	}
	#endregion

	#region Unity ife cycle
	protected void Start()
	{
		if(Lobby.Current == null)
		{
			Debug.LogWarning("No lobby present, building default match.");
			GameManager.Instance.LoadDefaultLobby();
		}
		BuildMatch();
	}
	#endregion
}
