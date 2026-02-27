/// <summary>
/// 몬스터 FSM 상태 정의
/// </summary>
public enum EMonsterState : byte
{
    Idle,           // 대기 - 제자리에서 주변 감시
    Patrol,         // 순찰 - 랜덤 웨이포인트로 이동
    Chase,          // 추적 - 감지된 플레이어에게 이동
    Attack,         // 공격 - 공격 애니메이션 실행
    AttackWait,     // 공격 대기 - 공격 쿨다운
    Hit,            // 피격 - 피격 애니메이션 실행
    Death           // 죽음 - 사망 처리 후 리스폰
}
