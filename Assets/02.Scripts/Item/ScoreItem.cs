using System;
using Photon.Pun;
using UnityEngine;

public class ScoreItem : MonoBehaviourPun
{
    private int _itemScore = 100;

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어만 먹을 수 있음
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        // 내 캐릭터만 처리 (다른 사람 화면에서 내가 먹는 건 무시)
        if (!player.PhotonView.IsMine) return;

        // 죽은 상태면 못 먹음
        if (player.IsDead) return;

        // 점수 추가
        ScoreManager.Instance.AddScore(_itemScore);

        // ItemObjectFactory를 통해 방장에게 코인 삭제 요청
        ItemObjectFactory.Instance.RequestDeleteScoreItem(photonView.ViewID);
    }
}