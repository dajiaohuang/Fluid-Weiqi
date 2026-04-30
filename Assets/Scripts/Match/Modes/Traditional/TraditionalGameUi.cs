using UnityEngine;

public class TraditionalGameUi : MonoBehaviour
{
	[SerializeField] TraditionalGameEndingUi ending;

	protected void Awake()
	{
		ending.gameObject.SetActive(false);
	}

	public void ShowEnding()
	{
		ending.gameObject.SetActive(true);
	}

	public void ReturnToMenu()
	{
		GameManager.Instance.EndMatch();
	}
}
