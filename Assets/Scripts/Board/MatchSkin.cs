using UnityEngine;

public class MatchSkin : MonoBehaviour
{
	[SerializeField] Transform boardRoot;
	[SerializeField] Material skybox;

	public Transform BoardRoot => boardRoot != null ? boardRoot : transform;
	public Material Skybox => skybox;
}
