using UnityEngine;

public class StartMenu : MonoBehaviour
{
	public void OnCreateMatchButtonClicked()
	{
		GameManager.Instance.CreateLobby();
	}

	public void OnQuitGameButtonClicked()
	{
		GameManager.Instance.QuitGame();
	}

	public void OnBrowseLobbyButtonClicked()
	{
		GameManager.Instance.BrowseLobbies();
	}
}
