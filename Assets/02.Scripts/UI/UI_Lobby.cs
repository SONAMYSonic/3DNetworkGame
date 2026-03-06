using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Lobby : MonoBehaviour
{
    public GameObject MaleCharacter;
    public GameObject FemaleCharacter;

    public TMP_InputField NicknameInputField;
    public TMP_InputField RoomNameInputField;
    public Button CreateRoomButton;
    
    private ECharacterType _characterType;

    private void Start()
    {
        CreateRoomButton.onClick.AddListener(MakeRoom);
    }

    private void MakeRoom()
    {
        string nickname = NicknameInputField.text;
        string roomName = RoomNameInputField.text;

        if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(roomName))
        {
            return;
        }
        
        PhotonNetwork.NickName = nickname;
        
        // 룸 옵션 정의
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 20;  // 룸 최대 접속자 수
        roomOptions.IsVisible = true; // 로비에서 룸을 보여줄 것인지
        roomOptions.IsOpen = true;    // 룸의 오픈 여부

        // 커스텀 룸 프로퍼티: 방장 닉네임 저장
        roomOptions.CustomRoomProperties = new Hashtable
        {
            { "ownerName", nickname }
        };
        // 로비에서 보이게 할 키 등록 (이걸 안 하면 로비의 RoomInfo에서 읽을 수 없다)
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "ownerName" };

        // 룸 만들기
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }
    


    // Todo: 버튼 연결
    public void OnClickMale() => OnClickCharacterButton(ECharacterType.Male);
    public void OnClickFemale() => OnClickCharacterButton(ECharacterType.Female);
    private void OnClickCharacterButton(ECharacterType characterType)
    {
        _characterType = characterType;
        
        MaleCharacter.SetActive(_characterType == ECharacterType.Male);
        FemaleCharacter.SetActive(_characterType == ECharacterType.Female);
    }
}