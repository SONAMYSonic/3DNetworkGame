using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 몬스터 유한 상태 머신 (FSM).
/// 마스터 클라이언트에서만 AI 로직이 실행된다.
/// 비마스터 클라이언트에서는 CurrentState만 동기화되어 표시용으로 사용된다.
/// </summary>
public class MonsterFSM : MonoBehaviour
{
    public EMonsterState CurrentState = EMonsterState.Idle;

    private MonsterController _controller;
    private NavMeshAgent _agent;
    private MonsterStat _stat;

    // 타이머
    private float _stateTimer;
    private float _attackCooldownTimer;

    // 타겟
    private Transform _targetPlayer;

    // 피격 후 복귀할 상태
    private EMonsterState _stateBeforeHit;

    // 공격 1회 판정용 플래그
    private bool _hasDealtDamage;

    private void Awake()
    {
        _controller = GetComponent<MonsterController>();
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        // 마스터 클라이언트만 AI 실행
        if (!PhotonNetwork.IsMasterClient) return;
        if (_controller.IsDead) return;

        // 마스터 클라이언트가 전환되면 NavMeshAgent가 꺼져있을 수 있다 (Start에서 비마스터는 비활성화)
        if (!_agent.enabled)
        {
            _agent.enabled = true;
        }

        // NavMesh 위에 배치되지 않은 경우 스킵 (리스폰 중 등)
        if (!_agent.isOnNavMesh) return;

        _stat = _controller.Stat;

        switch (CurrentState)
        {
            case EMonsterState.Idle:
                UpdateIdle();
                break;
            case EMonsterState.Patrol:
                UpdatePatrol();
                break;
            case EMonsterState.Chase:
                UpdateChase();
                break;
            case EMonsterState.Attack:
                UpdateAttack();
                break;
            case EMonsterState.AttackWait:
                UpdateAttackWait();
                break;
            case EMonsterState.Hit:
                UpdateHit();
                break;
        }
    }

    // ─────────────────────────────────────────────
    // 상태별 업데이트
    // ─────────────────────────────────────────────

    private void UpdateIdle()
    {
        _controller.SetAnimSpeed(0f);
        _stateTimer += Time.deltaTime;

        // 플레이어 감지 → 추적
        if (TryDetectPlayer())
        {
            ChangeState(EMonsterState.Chase);
            return;
        }

        // 대기 시간 경과 → 순찰
        if (_stateTimer >= _stat.IdleTime)
        {
            SetPatrolDestination();
            ChangeState(EMonsterState.Patrol);
        }
    }

    private void UpdatePatrol()
    {
        _agent.speed = _stat.MoveSpeed;
        _controller.SetAnimSpeed(0.5f); // Walk

        // 플레이어 감지 → 추적
        if (TryDetectPlayer())
        {
            ChangeState(EMonsterState.Chase);
            return;
        }

        // 목적지 도착 → 대기
        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.1f)
        {
            ChangeState(EMonsterState.Idle);
        }
    }

    private void UpdateChase()
    {
        // 타겟 유효성 검사
        if (!IsTargetValid())
        {
            _targetPlayer = null;
            ChangeState(EMonsterState.Idle);
            return;
        }

        float distance = Vector3.Distance(transform.position, _targetPlayer.position);

        // 감지 범위 밖 → 대기
        if (distance > _stat.DetectRange * 1.5f)
        {
            _targetPlayer = null;
            ChangeState(EMonsterState.Idle);
            return;
        }

        // 공격 범위 진입 → 공격
        if (distance <= _stat.AttackRange)
        {
            _agent.isStopped = true;
            ChangeState(EMonsterState.Attack);
            return;
        }

        // 추적 이동
        _agent.speed = _stat.ChaseSpeed;
        _agent.isStopped = false;
        _agent.SetDestination(_targetPlayer.position);
        _controller.SetAnimSpeed(1f); // Run

        // 타겟 방향으로 회전
        LookAtTarget();
    }

    private void UpdateAttack()
    {
        _controller.SetAnimSpeed(0f);
        _agent.isStopped = true;

        // 타겟 방향으로 회전
        if (_targetPlayer != null)
        {
            LookAtTarget();
        }

        _stateTimer += Time.deltaTime;

        // 공격 애니메이션 중 데미지 판정 (1회만)
        if (!_hasDealtDamage && _stateTimer >= 0.6f)
        {
            _hasDealtDamage = true;
            _controller.DealDamageToNearbyPlayers();
        }

        // 공격 애니메이션 완료 (약 1초)
        if (_stateTimer >= 1f)
        {
            ChangeState(EMonsterState.AttackWait);
        }
    }

    private void UpdateAttackWait()
    {
        _controller.SetAnimSpeed(0f);
        _attackCooldownTimer += Time.deltaTime;

        // 쿨다운 완료
        if (_attackCooldownTimer >= _stat.AttackCooldown)
        {
            _attackCooldownTimer = 0f;

            // 타겟이 유효한지 확인
            if (!IsTargetValid())
            {
                _targetPlayer = null;
                ChangeState(EMonsterState.Idle);
                return;
            }

            float distance = Vector3.Distance(transform.position, _targetPlayer.position);

            // 공격 범위 내 → 다시 공격
            if (distance <= _stat.AttackRange)
            {
                ChangeState(EMonsterState.Attack);
            }
            // 감지 범위 내 → 추적
            else if (distance <= _stat.DetectRange)
            {
                ChangeState(EMonsterState.Chase);
            }
            // 범위 밖 → 대기
            else
            {
                _targetPlayer = null;
                ChangeState(EMonsterState.Idle);
            }
        }
    }

    private void UpdateHit()
    {
        _stateTimer += Time.deltaTime;

        if (_stateTimer >= _stat.HitStunTime)
        {
            // 피격 전 상태가 전투 중이었으면 추적, 아니면 대기
            if (_targetPlayer != null && IsTargetValid())
            {
                ChangeState(EMonsterState.Chase);
            }
            else
            {
                ChangeState(EMonsterState.Idle);
            }
        }
    }

    // ─────────────────────────────────────────────
    // 상태 전환
    // ─────────────────────────────────────────────

    private void ChangeState(EMonsterState newState)
    {
        CurrentState = newState;
        _stateTimer = 0f;

        switch (newState)
        {
            case EMonsterState.Idle:
                _agent.isStopped = true;
                _agent.ResetPath();
                break;

            case EMonsterState.Patrol:
                _agent.isStopped = false;
                break;

            case EMonsterState.Chase:
                _agent.isStopped = false;
                break;

            case EMonsterState.Attack:
                _agent.isStopped = true;
                _hasDealtDamage = false;
                // 랜덤 공격 애니메이션
                _controller.PlayAnimationTrigger("Attack");
                break;

            case EMonsterState.AttackWait:
                _attackCooldownTimer = 0f;
                break;

            case EMonsterState.Hit:
                _agent.isStopped = true;
                _controller.PlayAnimationTrigger("Hit");
                break;

            case EMonsterState.Death:
                _agent.isStopped = true;
                _agent.ResetPath();
                break;
        }
    }

    // ─────────────────────────────────────────────
    // 외부에서 호출되는 이벤트
    // ─────────────────────────────────────────────

    /// <summary>
    /// 피격 시 MonsterController에서 호출
    /// </summary>
    public void OnHit()
    {
        if (CurrentState == EMonsterState.Death) return;

        _stateBeforeHit = CurrentState;
        ChangeState(EMonsterState.Hit);
    }

    /// <summary>
    /// 사망 시 MonsterController에서 호출
    /// </summary>
    public void OnDeath()
    {
        ChangeState(EMonsterState.Death);
    }

    /// <summary>
    /// 리스폰 시 MonsterController에서 호출
    /// </summary>
    public void ResetToIdle()
    {
        _targetPlayer = null;
        _stateTimer = 0f;
        _attackCooldownTimer = 0f;
        ChangeState(EMonsterState.Idle);
    }

    // ─────────────────────────────────────────────
    // 유틸리티
    // ─────────────────────────────────────────────

    /// <summary>
    /// 감지 범위 내에서 가장 가까운 살아있는 플레이어를 찾는다.
    /// </summary>
    private bool TryDetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _stat.DetectRange);
        float closestDistance = float.MaxValue;
        Transform closestPlayer = null;

        foreach (var hit in hits)
        {
            PlayerController player = hit.GetComponentInParent<PlayerController>();
            if (player == null || player.IsDead) continue;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestPlayer = player.transform;
            }
        }

        if (closestPlayer != null)
        {
            _targetPlayer = closestPlayer;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 순찰 목적지를 스폰 위치 기준으로 랜덤 설정
    /// </summary>
    private void SetPatrolDestination()
    {
        Vector3 spawnPos = _controller.SpawnPosition;
        Vector2 randomCircle = Random.insideUnitCircle * _stat.PatrolRadius;
        Vector3 randomPoint = spawnPos + new Vector3(randomCircle.x, 0f, randomCircle.y);

        // NavMesh 위의 유효한 지점으로 보정
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit navHit, _stat.PatrolRadius, NavMesh.AllAreas))
        {
            _agent.SetDestination(navHit.position);
        }
    }

    /// <summary>
    /// 타겟 플레이어가 유효한지 확인 (null이 아니고, 살아있는지)
    /// </summary>
    private bool IsTargetValid()
    {
        if (_targetPlayer == null) return false;

        PlayerController player = _targetPlayer.GetComponent<PlayerController>();
        return player != null && !player.IsDead;
    }

    /// <summary>
    /// 타겟 방향으로 부드럽게 회전
    /// </summary>
    private void LookAtTarget()
    {
        if (_targetPlayer == null) return;

        Vector3 direction = (_targetPlayer.position - transform.position).normalized;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }
}
