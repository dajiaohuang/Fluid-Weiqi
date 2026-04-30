using UnityEngine;
using UnityEngine.UI;

public class AboutPanel : MonoBehaviour
{
	[SerializeField] Text aboutText;

	protected void OnEnable()
	{
		aboutText.text = Resources.Load<TextAsset>("Misc/About Text").text;
	}
}
