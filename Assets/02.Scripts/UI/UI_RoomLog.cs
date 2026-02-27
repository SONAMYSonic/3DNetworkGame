using System;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class UI_RoomLog : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _logTextUI;

    private void Start()
    {
        _logTextUI.text = "방에 입장했습니다.";
        
        PhotonRoomManager.Instance.OnPlayerEnter += OnPlayerEnter;
        PhotonRoomManager.Instance.OnPlayerLeft  += OnPlayerLeft;
        PhotonRoomManager.Instance.OnPlayerDead  += PlayerDeathLog;
        PhotonRoomManager.Instance.OnMonsterDead += MonsterDeathLog;
    }

    private void OnPlayerEnter(Player newPlayer)
    {
        _logTextUI.text += "\n" + $"<color=green>{newPlayer.NickName}님이 입장하셨습니다.</color>";
    }
    
    private void OnPlayerLeft(Player otherPlayer)
    {
        _logTextUI.text += "\n" + $"<color=red>{otherPlayer.NickName}님이 퇴장하셨습니다.</color>";
    }
    
    private void PlayerDeathLog(string attackerNickName, string victimNickName)
    {
        _logTextUI.text += "\n" + $"<color=red>{attackerNickName}님이 {victimNickName}님을 처치하셨습니다.</color>";
    }

    private void MonsterDeathLog(string killerNickName)
    {
        _logTextUI.text += "\n" + $"<color=yellow>{killerNickName}님이 몬스터를 처치하셨습니다!</color>";
    }
}
