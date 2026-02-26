using UnityEngine;
using Photon.Pun;

public class ItemObjectFactory : MonoBehaviourPunCallbacks
{
    public static ItemObjectFactory Instance { get; private set; }

    private PhotonView _photonView;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _photonView = GetComponent<PhotonView>();
    }

    // 우리의 약속: 방장에게 룸 관련해서 뭔가 요청을 할 때는 메서드 명에 Request로 시작하는 게 유지보수가 편하다.
    public void RequestMakeScroreItems(Vector3 makePosition)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 방장이라면 바로 아이템 생성
            DropScoreItems(makePosition);
        }
        else
        {
            // 아니라면 방장에게 RPC로 요청
            _photonView.RPC(nameof(RPC_DropScoreItems), RpcTarget.MasterClient, makePosition.x, makePosition.y, makePosition.z);
        }
    }

    [PunRPC]
    private void RPC_DropScoreItems(float x, float y, float z)
    {
        DropScoreItems(new Vector3(x, y, z));
    }

    private void DropScoreItems(Vector3 makePosition)
    {
        int dropCount = Random.Range(3, 6); // 3~5개

        for (int i = 0; i < dropCount; i++)
        {
            // 캐릭터 주변 랜덤 위치 (반경 1m 원형)
            Vector2 randomCircle = Random.insideUnitCircle * 1f;
            Vector3 dropPos = makePosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

            // 소유자가 나가면 해당 네트워크 게임 오브젝트도 삭제된다.
            // 플레이어가 룸을 나가면 그 플레이어가 생성/소유한 모든 네트워크 게임 오브젝트가 삭제된다.
            // 즉, 플레이어 생명 주기를 가지고 있다...
            
            // 그래서 플레이어 생명 주기가 아닌 룸 생명 주기를 가지도록 룸 오브젝트로 생성해야 한다.
            PhotonNetwork.InstantiateRoomObject("Coin", dropPos, Quaternion.identity);
            
            // 포톤에는 룸 안에 방장(Master Client)이 있다.
            // 방을 만든 사람이 방장
            // 1. 방장을 양도할 수 있다.
            // 2. 방장이 나가면 자동으로 그 다음으로 들어온 사람으로 방장이 양도된다.
            
        }
    }
    
    public void RequestDeleteScoreItem(int viewID)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 방장이라면 바로 아이템 삭제
            DeleteScoreItem(viewID);
        }
        else
        {
            // 아니라면 방장에게 RPC로 요청
            _photonView.RPC(nameof(DeleteScoreItem), RpcTarget.MasterClient, viewID);
        }
    }
    
    [PunRPC]
    private void DeleteScoreItem(int viewID)
    {
        PhotonView target = Photon.Pun.PhotonView.Find(viewID);
        if (target == null) return;

        PhotonNetwork.Destroy(target.gameObject);
    }
}
