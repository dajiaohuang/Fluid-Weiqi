using UnityEngine;

public class TraditionalTrainingUi : MonoBehaviour
{
	[SerializeField] TraditionalMatchEndingUi ending;

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
		// TODO
	}
}
