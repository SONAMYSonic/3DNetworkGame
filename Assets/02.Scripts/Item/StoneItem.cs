using UnityEngine;
using Photon.Pun;

// 하늘에서 떨어지는 Stone 점수 오브젝트
// 플레이어가 트리거에 닿으면 점수를 획득하고, 방장에게 삭제를 요청한다.
public class StoneItem : MonoBehaviour
{
    [SerializeField] private int _score = 100;
    [SerializeField] private float _lifeTime = 15f; // 일정 시간 후 자동 삭제

    private PhotonView _photonView;
    private bool _isCollected;
    private float _elapsedTime;

    private void Start()
    {
        _photonView = GetComponent<PhotonView>();
    }

    private void Update()
    {
        // 방장만 자동 삭제 타이머 관리
        if (!PhotonNetwork.IsMasterClient) return;

        _elapsedTime += Time.deltaTime;
        if (_elapsedTime >= _lifeTime)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // 스포너 카운트 감소
        StoneSpawner spawner = FindFirstObjectByType<StoneSpawner>();
        if (spawner != null)
        {
            spawner.OnStoneDestroyed();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isCollected) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;
        if (!player.PhotonView.IsMine) return;
        if (player.IsDead) return;

        _isCollected = true;

        // 점수 추가
        player.Score += _score;

        // ItemObjectFactory를 통해 방장에게 삭제 요청
        ItemObjectFactory.Instance.RequestDeleteScoreItem(_photonView.ViewID);
    }
}
