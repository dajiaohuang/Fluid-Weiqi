using UnityEngine;
using UnityEngine.UI;
using System;

public class LobbyRow : MonoBehaviour
{
	[SerializeField] Text lobbyNameText;
	[SerializeField] Text hostNameText;
	[SerializeField] Button joinButton;

	Action onJoin;

	public void Bind(LobbySnapshot snapshot, Action onJoinCallback)
	{
		gameObject.SetActive(true);
		lobbyNameText.text = snapshot.lobbyName;
		hostNameText.text = snapshot.hostName;
		onJoin = onJoinCallback;
		joinButton.interactable = true;
	}

	public void Hide()
	{
		gameObject.SetActive(false);
		onJoin = null;
	}

	public void OnJoinButtonClicked()
	{
		onJoin?.Invoke();
	}
}
