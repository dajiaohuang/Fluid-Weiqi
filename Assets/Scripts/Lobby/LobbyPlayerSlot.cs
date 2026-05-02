using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class LobbyPlayerSlot : MonoBehaviour
{
	static GameObject prefab;

	protected void Awake()
	{
		prefab ??= Resources.Load<GameObject>("UI/Lobby/Player Slot");
	}

	public static LobbyPlayerSlot Make(PlayerDescriptor descriptor, Transform root = null)
	{
		var go = Instantiate(prefab, root);
		var slot = go.GetComponent<LobbyPlayerSlot>();
		slot.Descriptor = descriptor;
		return slot;
	}

	public PlayerDescriptor Descriptor { get; set; }
	int Index => Descriptor?.Index ?? -1;
	readonly List<PlayerType> typeOptions = new() { PlayerType.Local, PlayerType.Ai };

	[SerializeField] Graphic colorGraphic;
	[SerializeField] Dropdown typeDropdown;
	[SerializeField] Text nameText;
	[SerializeField] Button removeButton;

	protected void Start()
	{
		if(Lobby.Current == null || Descriptor == null)
		{
			Destroy(this);
			return;
		}

		if(Lobby.Current.IsOnline)
			typeOptions.Add(PlayerType.Online);
		typeDropdown.options = typeOptions.Select(t => new Dropdown.OptionData(t.ToLocalizedString())).ToList();
		typeDropdown.value = typeOptions.IndexOf(Descriptor.type);

		colorGraphic.color = Descriptor.color;
		nameText.text = Descriptor.GetLocalizedName();
		if(Lobby.Current.Players.Count <= 2)
			removeButton.interactable = false;
		if(Descriptor.type == PlayerType.Local && Lobby.Current.Players.Count(p => p.type == PlayerType.Local) < 2)
			removeButton.interactable = false;

		if(!Lobby.Current.IsHost)
		{
			typeDropdown.interactable = false;
			removeButton.gameObject.SetActive(false);
		}
		else
		{
			typeDropdown.onValueChanged.AddListener(OnTypeDropdownValueChanged);
		}
	}

	void OnTypeDropdownValueChanged(int i)
	{
		HostLobby.Current?.SetPlayerType(Index, typeOptions[i]);
	}

	public void OnRemoveButtonClicked()
	{
		HostLobby.Current?.RemovePlayer(Index);
	}
}
