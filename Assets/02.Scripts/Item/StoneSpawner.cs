using UnityEngine;
using Photon.Pun;

/// <summary>
/// 하늘에서 랜덤하게 Stone 오브젝트를 떨어뜨리는 스포너
/// 방장(MasterClient)만 생성을 담당하고, 네트워크로 자동 동기화된다.
/// </summary>
public class StoneSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private float _spawnInterval = 3f;      // 스폰 간격 (초)
    [SerializeField] private float _spawnHeight = 30f;        // 스폰 높이 (Y)
    [SerializeField] private int _maxStones = 20;             // 맵에 존재할 수 있는 최대 개수

    [Header("스폰 범위")]
    [SerializeField] private float _minX = -13.5f;
    [SerializeField] private float _maxX = 14.5f;
    [SerializeField] private float _minZ = 8.5f;
    [SerializeField] private float _maxZ = 32.5f;

    [Header("프리팹")]
    [SerializeField] private string[] _stonePrefabNames = { "Stone_00", "Stone_01", "Stone_02" };

    private float _timer;
    private int _currentStoneCount;

    private void Update()
    {
        // 방장만 스폰 담당
        if (!PhotonNetwork.IsMasterClient) return;

        _timer += Time.deltaTime;

        if (_timer >= _spawnInterval)
        {
            _timer = 0f;

            if (_currentStoneCount < _maxStones)
            {
                SpawnStone();
            }
        }
    }

    private void SpawnStone()
    {
        // 랜덤 위치
        float x = Random.Range(_minX, _maxX);
        float z = Random.Range(_minZ, _maxZ);
        Vector3 spawnPos = new Vector3(x, _spawnHeight, z);

        // 랜덤 프리팹
        string prefabName = _stonePrefabNames[Random.Range(0, _stonePrefabNames.Length)];

        PhotonNetwork.InstantiateRoomObject(prefabName, spawnPos, Quaternion.identity);
        _currentStoneCount++;
    }

    /// <summary>
    /// Stone이 제거될 때 호출 (StoneItem에서 호출)
    /// </summary>
    public void OnStoneDestroyed()
    {
        _currentStoneCount = Mathf.Max(0, _currentStoneCount - 1);
    }
}
