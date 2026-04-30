using UnityEngine;

public enum MatchMode
{
	Traditional = 1,
	Training = 0xffff,
}

[System.Serializable]
public struct MatchRule
{
	public MatchMode mode;
	public int boardSize;
	public float stoneHardness;
}
