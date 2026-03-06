using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class ScoreManager : MonoBehaviourPunCallbacks
{
    public static ScoreManager Instance { get; private set; }

    private int _score;

    private Dictionary<int, ScoreData> _scores = new();
    public ReadOnlyDictionary<int, ScoreData> Scores => new ReadOnlyDictionary<int, ScoreData>(_scores);

    public static event Action OnDataChanged;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // OnJoinedRoom은 LobbyScene에서 호출되므로, GameScene에 있는 ScoreManager는 받을 수 없다.
        // 따라서 Start()에서 방에 이미 들어와 있는 상태라면 초기화한다.
        if (PhotonNetwork.InRoom)
        {
            _score = 0;
            Refresh();
            LoadExistingPlayerScores();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // 새 플레이어가 들어오면 0점으로 추가
        _scores[newPlayer.ActorNumber] = new ScoreData
        {
            Nickname = newPlayer.NickName,
            Score = 0
        };

        OnDataChanged?.Invoke();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // 나간 플레이어의 점수 데이터 제거
        _scores.Remove(otherPlayer.ActorNumber);

        OnDataChanged?.Invoke();
    }

    public void AddScore(int score)
    {
        _score += score;
        Refresh();
    }

    public void DeathScore()
    {
        _score /= 2;
        Refresh();
    }

    private void Refresh()
    {
        Hashtable hashtable = new Hashtable();
        hashtable.Add("score", _score);

        PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable);
    }

    // 플레이어의 커스텀 프로퍼티가 변경되면 자동으로 호출되는 함수
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!changedProps.ContainsKey("score")) return;

        ScoreData scoreData = new ScoreData()
        {
            Nickname = targetPlayer.NickName,
            Score = (int)changedProps["score"]
        };

        _scores[targetPlayer.ActorNumber] = scoreData;

        OnDataChanged?.Invoke();
    }

    /// <summary>
    /// 방 입장 시 기존 플레이어들의 커스텀 프로퍼티에서 점수를 읽어온다.
    /// </summary>
    private void LoadExistingPlayerScores()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int score = 0;

            if (player.CustomProperties.ContainsKey("score"))
            {
                score = (int)player.CustomProperties["score"];
            }

            _scores[player.ActorNumber] = new ScoreData
            {
                Nickname = player.NickName,
                Score = score
            };
        }

        OnDataChanged?.Invoke();
    }
}
