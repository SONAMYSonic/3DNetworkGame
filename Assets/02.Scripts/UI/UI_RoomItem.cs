using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_RoomItem : MonoBehaviour
{
    public TextMeshProUGUI RoomNameTextUI;
    public TextMeshProUGUI RoomOwnerNameUI;
    public TextMeshProUGUI RoomPlayerCountUI;
    public Button RoomEnterButton;
    
    private RoomInfo _roomInfo;

    private void Start()
    {
        RoomEnterButton.onClick.AddListener(EnterRoom);
    }

    public void Init(RoomInfo roomInfo)
    {
        _roomInfo = roomInfo;
        
        RoomNameTextUI.text = roomInfo.Name;

        // CustomRoomPropertiesForLobby에 등록된 키만 로비에서 읽을 수 있다
        string ownerName = roomInfo.CustomProperties.ContainsKey("ownerName")
            ? (string)roomInfo.CustomProperties["ownerName"]
            : "알 수 없음";
        RoomOwnerNameUI.text = ownerName;

        RoomPlayerCountUI.text = $"{roomInfo.PlayerCount}/{roomInfo.MaxPlayers}";
    }

    private void EnterRoom()
    {
        if (_roomInfo == null) return;

        // 로비에서 닉네임 입력 필드를 찾아 닉네임 설정
        UI_Lobby lobby = FindObjectOfType<UI_Lobby>();
        if (lobby != null)
        {
            string nickname = lobby.NicknameInputField.text;
            if (string.IsNullOrEmpty(nickname))
            {
                Debug.LogWarning("닉네임을 입력해주세요.");
                return;
            }
            PhotonNetwork.NickName = nickname;
        }

        PhotonNetwork.JoinRoom(_roomInfo.Name);
    }
}
