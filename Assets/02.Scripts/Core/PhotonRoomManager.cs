using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PhotonRoomManager : MonoBehaviourPunCallbacks
{
    public static PhotonRoomManager Instance { get; private set; }

    private Room _room;
    public Room Room => _room;
    
    public event Action OnDataChanged;          // 룸 정보가 바뀌었을때
    public event Action<Player> OnPlayerEnter;  // 플레이어가 들어왔을때
    public event Action<Player> OnPlayerLeft;   // 
    public event Action<string, string> OnPlayerDead;   // 플레이어가 죽었을때 (공격자, 피해자)
    public event Action<string> OnMonsterDead;             // 몬스터가 죽었을때 (처치자)
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // 방 입장에 성공하면 자동으로 호출되는 콜백 함수
    public override void OnJoinedRoom()
    {
        _room = PhotonNetwork.CurrentRoom;

        SceneManager.LoadScene("GameScene");
        
        OnDataChanged?.Invoke();
    }
    
    // 새로운 플레이어
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        OnDataChanged?.Invoke();
        OnPlayerEnter?.Invoke(newPlayer);
    }
    
    // 플레이어가 방에서 퇴장
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        OnDataChanged?.Invoke();
        OnPlayerLeft?.Invoke(otherPlayer);
    }

    public void OnPlayerDeath(int attackerActorNumber, string victimNickName)
    {
        // attackerActorNumber == -1이면 몬스터에게 죽은 것
        string attackerNickName = attackerActorNumber == -1
            ? "Monster"
            : _room.Players[attackerActorNumber].NickName;

        OnPlayerDead?.Invoke(attackerNickName, victimNickName);
    }

    /// <summary>
    /// 몬스터 처치 시 호출 (MonsterController에서 RPC로 호출)
    /// </summary>
    public void OnMonsterKill(string killerNickName)
    {
        OnMonsterDead?.Invoke(killerNickName);
    }
    
    
}
