using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PanelCloseButton : MonoBehaviour
{
	protected void Awake()
	{
		var button = GetComponent<Button>();
		if(button)
		{
			button.onClick.AddListener(() => UiManager.Instance.CloseCurrentPanel());
		}
	}
}
