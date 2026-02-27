using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 몬스터의 메인 컨트롤러. 외부와의 소통, 네트워크 동기화, 데미지 처리를 담당한다.
/// 마스터 클라이언트만 AI 로직(FSM)을 실행하고, 나머지는 동기화된 데이터를 수신한다.
/// </summary>
public class MonsterController : MonoBehaviour, IPunObservable, IDamageable
{
    public PhotonView PhotonView { get; private set; }
    public MonsterStat Stat;
    public bool IsDead { get; set; }

    private Animator _animator;
    private NavMeshAgent _navMeshAgent;
    private MonsterFSM _fsm;

    // 스폰 위치 기억 (순찰/리스폰 기준점)
    public Vector3 SpawnPosition { get; private set; }

    private void Awake()
    {
        PhotonView = GetComponent<PhotonView>();
        _animator = GetComponent<Animator>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _fsm = GetComponent<MonsterFSM>();
    }

    private void Start()
    {
        SpawnPosition = transform.position;

        // Root Motion 비활성화 (1차 방어)
        _animator.applyRootMotion = false;

        // 비마스터 클라이언트에서는 NavMeshAgent를 꺼서 위치 충돌 방지
        // 위치는 PhotonTransformView가 동기화한다
        if (!PhotonNetwork.IsMasterClient)
        {
            _navMeshAgent.enabled = false;
        }
    }

    // ─────────────────────────────────────────────
    // ★ Root Motion 완전 차단 (2차 방어 - 핵심)
    // ─────────────────────────────────────────────
    //
    // applyRootMotion = false 만으로 안 되는 이유:
    //   Generic 리그(동물 모델)는 Humanoid와 달리 "루트 본"이 명확하지 않아서,
    //   애니메이션이 Hip/Pelvis 같은 자식 본을 직접 이동시키면
    //   applyRootMotion 설정과 무관하게 메시가 앞으로 밀린다.
    //
    // OnAnimatorMove()를 선언하면:
    //   Unity가 Animator의 루트 모션 적용을 이 함수에 "위임"한다.
    //   함수 본문이 비어있으면 → 루트 모션 데이터가 완전히 버려진다.
    //   즉, 애니메이션은 제자리에서 재생되고, 이동은 NavMeshAgent가 100% 담당.
    //
    private void OnAnimatorMove()
    {
        // 의도적으로 비워둠
        // → Animator가 transform.position을 건드리지 못하게 한다
    }

    // ─────────────────────────────────────────────
    // 애니메이션 동기화 (RPC)
    // ─────────────────────────────────────────────

    /// <summary>
    /// 마스터에서 호출: 트리거 애니메이션을 모든 클라이언트에 동기화
    /// </summary>
    public void PlayAnimationTrigger(string triggerName)
    {
        _animator.SetTrigger(triggerName);
        PhotonView.RPC(nameof(RPC_PlayAnimationTrigger), RpcTarget.Others, triggerName);
    }

    [PunRPC]
    private void RPC_PlayAnimationTrigger(string triggerName)
    {
        _animator.SetTrigger(triggerName);
    }

    /// <summary>
    /// 마스터에서 호출: Speed float 파라미터를 모든 클라이언트에 동기화
    /// </summary>
    public void SetAnimSpeed(float speed)
    {
        _animator.SetFloat("Speed", speed);
    }

    /// <summary>
    /// 마스터에서 호출: IsDead bool 파라미터를 모든 클라이언트에 동기화
    /// </summary>
    public void SetAnimDead(bool isDead)
    {
        _animator.SetBool("IsDead", isDead);
    }

    // ─────────────────────────────────────────────
    // 데미지 처리 (IDamageable)
    // ─────────────────────────────────────────────

    [PunRPC]
    public void TakeDamage(float damage, int attackerActorNumber)
    {
        if (IsDead) return;

        Stat.CurrentHealth -= damage;

        if (Stat.CurrentHealth <= 0)
        {
            Stat.CurrentHealth = 0;
            Die(attackerActorNumber);
        }
        else
        {
            // 마스터에서 FSM에 피격 알림
            if (PhotonNetwork.IsMasterClient)
            {
                _fsm.OnHit();
            }
        }
    }

    private void Die(int attackerActorNumber)
    {
        IsDead = true;
        SetAnimDead(true);

        // NavMeshAgent 정지
        if (_navMeshAgent.enabled)
        {
            _navMeshAgent.isStopped = true;
            _navMeshAgent.ResetPath();
        }

        // 마스터에서만 사망 처리
        if (PhotonNetwork.IsMasterClient)
        {
            _fsm.OnDeath();

            // 코인 드랍 (기존 ItemObjectFactory 재활용)
            if (ItemObjectFactory.Instance != null)
            {
                ItemObjectFactory.Instance.RequestMakeScroreItems(transform.position + new Vector3(0, 0.5f, 0));
            }

            // 킬 로그 (attackerActorNumber가 유효한 플레이어일 때)
            if (attackerActorNumber > 0)
            {
                PhotonView.RPC(nameof(RPC_NotifyMonsterKill), RpcTarget.All, attackerActorNumber);
            }

            // 리스폰 코루틴
            StartCoroutine(RespawnAfterDelay());
        }
    }

    [PunRPC]
    private void RPC_NotifyMonsterKill(int attackerActorNumber)
    {
        if (PhotonNetwork.CurrentRoom.Players.TryGetValue(attackerActorNumber, out var attacker))
        {
            // 공격자 점수 추가
            if (PhotonNetwork.LocalPlayer.ActorNumber == attackerActorNumber)
            {
                // 로컬 플레이어가 공격자라면 점수 추가
                PlayerController localPlayer = FindLocalPlayer();
                if (localPlayer != null)
                {
                    localPlayer.Score += 200;
                }
            }

            PhotonRoomManager.Instance?.OnMonsterKill(attacker.NickName);
        }
    }

    private PlayerController FindLocalPlayer()
    {
        foreach (var pc in FindObjectsOfType<PlayerController>())
        {
            if (pc.PhotonView.IsMine) return pc;
        }
        return null;
    }

    private IEnumerator RespawnAfterDelay()
    {
        // 죽은 후 잠시 대기 (사망 애니메이션 재생)
        yield return new WaitForSeconds(2f);

        // 모델 숨기기
        PhotonView.RPC(nameof(RPC_SetVisible), RpcTarget.All, false);

        // 리스폰 대기
        yield return new WaitForSeconds(Stat.RespawnTime - 2f);

        // 스탯 초기화
        Stat.CurrentHealth = Stat.MaxHealth;
        IsDead = false;
        SetAnimDead(false);

        // 스폰 위치로 복귀
        _navMeshAgent.enabled = false;
        transform.position = SpawnPosition;
        _navMeshAgent.enabled = true;
        _navMeshAgent.isStopped = false;

        // 모델 다시 보이기
        PhotonView.RPC(nameof(RPC_SetVisible), RpcTarget.All, true);

        // FSM 리셋
        _fsm.ResetToIdle();
    }

    [PunRPC]
    private void RPC_SetVisible(bool visible)
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = visible;
        }
        // 콜라이더도 같이 끄기 (사라진 상태에서 충돌 방지)
        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.enabled = visible;
        }
    }

    // ─────────────────────────────────────────────
    // 몬스터 → 플레이어 공격 (마스터에서 실행)
    // ─────────────────────────────────────────────

    /// <summary>
    /// 공격 범위 내 플레이어에게 데미지를 준다. (마스터에서 호출)
    /// </summary>
    public void DealDamageToNearbyPlayers()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, Stat.AttackRange);
        foreach (var hit in hits)
        {
            PlayerController player = hit.GetComponentInParent<PlayerController>();
            if (player == null || player.IsDead) continue;

            // 모든 클라이언트에서 플레이어의 TakeDamage 실행
            // attackerActorNumber = -1 → 몬스터 공격
            player.PhotonView.RPC(nameof(player.TakeDamage), RpcTarget.All, Stat.Damage, -1);
        }
    }

    // ─────────────────────────────────────────────
    // 네트워크 동기화 (IPunObservable)
    // ─────────────────────────────────────────────

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 마스터 → 다른 클라이언트로 데이터 전송
            stream.SendNext(Stat.CurrentHealth);
            stream.SendNext(IsDead);
            stream.SendNext((byte)_fsm.CurrentState);
            stream.SendNext(_animator.GetFloat("Speed"));
            stream.SendNext(_animator.GetBool("IsDead"));
        }
        else if (stream.IsReading)
        {
            // 다른 클라이언트가 데이터 수신
            Stat.CurrentHealth = (float)stream.ReceiveNext();
            IsDead = (bool)stream.ReceiveNext();
            _fsm.CurrentState = (EMonsterState)(byte)stream.ReceiveNext();
            _animator.SetFloat("Speed", (float)stream.ReceiveNext());
            _animator.SetBool("IsDead", (bool)stream.ReceiveNext());
        }
    }
}
