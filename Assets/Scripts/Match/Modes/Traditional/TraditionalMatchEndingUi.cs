using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class TraditionalMatchEndingUi : MonoBehaviour
{
	[SerializeField] Text resultText;
	[SerializeField] GameObject panelRoot;

	protected void Awake()
	{
		if(panelRoot == null && resultText != null)
			panelRoot = resultText.transform.parent != null ? resultText.transform.parent.gameObject : null;

		if(panelRoot != null && panelRoot != gameObject)
			panelRoot.SetActive(false);

		if(Match.Current != null)
			Match.Current.OnEnd += OnMatchEnded;
	}

	protected void Start()
	{
		gameObject.SetActive(false);
	}

	protected void OnDestroy()
	{
		if(Match.Current != null)
			Match.Current.OnEnd -= OnMatchEnded;
	}

	void OnMatchEnded()
	{
		if(panelRoot != null)
			panelRoot.SetActive(true);

		float[] results = BoardUtility.GetPlayerAreasByDominance(Board.Current, Match.Current.PlayerCount);
		List<string> lines = new();

		lines.Add(string.Join("\n", results.Select(
			(float area, int i) => $"{Match.Current.PlayerInfos[i].name}：{area.ToString("F2")} 目"
		)));

		float maxArea = results.Max();
		var winners = results
			.Select((float area, int i) => (area, i))
			.Where(pair => pair.area >= maxArea)
			.Select(pair => pair.i)
			.ToArray();
		if(winners.Length == results.Length)
			lines.Add("平局");
		else
			lines.Add($"{string.Join("、", winners.Select(i => Match.Current.PlayerInfos[i].name))}胜");

		resultText.text = string.Join("\n", lines);
	}

	public void OnEndButtonClicked()
	{
		// TODO
		GameManager.Instance.SwitchScene(GameScene.StartMenu);
	}
}
