using Photon.Pun;
using UnityEngine;

public class GameScene : MonoBehaviour
{
    void Start()
    {
        // 랜덤 스폰 포인트에서 플레이어 생성
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        Transform sp = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].transform;
        PhotonNetwork.Instantiate("Player", sp.position, sp.rotation);
    }
}
