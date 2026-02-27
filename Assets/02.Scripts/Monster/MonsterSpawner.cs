using UnityEngine;
using Photon.Pun;

/// <summary>
/// 몬스터를 스폰하는 스포너. 마스터 클라이언트만 생성을 담당한다.
/// StoneSpawner 패턴을 참고하여 동일한 구조로 작성.
/// </summary>
public class MonsterSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private int _maxMonsters = 5;                // 최대 마리 수
    [SerializeField] private float _spawnInterval = 10f;          // 스폰 간격 (초)
    [SerializeField] private string _monsterPrefabName = "Monster_Cat";

    [Header("스폰 범위")]
    [SerializeField] private float _minX = -10f;
    [SerializeField] private float _maxX = 10f;
    [SerializeField] private float _minZ = 10f;
    [SerializeField] private float _maxZ = 30f;
    [SerializeField] private float _spawnY = 0.5f;

    private float _timer;
    private int _currentMonsterCount;

    private void Update()
    {
        // 방장만 스폰 담당
        if (!PhotonNetwork.IsMasterClient) return;

        _timer += Time.deltaTime;

        if (_timer >= _spawnInterval)
        {
            _timer = 0f;

            if (_currentMonsterCount < _maxMonsters)
            {
                SpawnMonster();
            }
        }
    }

    private void SpawnMonster()
    {
        float x = Random.Range(_minX, _maxX);
        float z = Random.Range(_minZ, _maxZ);
        Vector3 spawnPos = new Vector3(x, _spawnY, z);

        // 룸 오브젝트로 생성 (방 생명주기)
        PhotonNetwork.InstantiateRoomObject(_monsterPrefabName, spawnPos, Quaternion.identity);
        _currentMonsterCount++;
    }

    /// <summary>
    /// 몬스터가 영구 제거될 때 호출 (현재는 리스폰 방식이므로 사용하지 않음)
    /// </summary>
    public void OnMonsterDestroyed()
    {
        _currentMonsterCount = Mathf.Max(0, _currentMonsterCount - 1);
    }
}
