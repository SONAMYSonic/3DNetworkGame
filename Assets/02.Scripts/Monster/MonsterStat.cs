using System;

[Serializable]
public class MonsterStat
{
    public float MaxHealth = 100f;
    public float CurrentHealth = 100f;

    public float MoveSpeed = 2f;        // 순찰 속도
    public float ChaseSpeed = 4.5f;     // 추적 속도

    public float Damage = 15f;
    public float AttackRange = 2f;      // 공격 사거리
    public float DetectRange = 10f;     // 플레이어 감지 범위
    public float AttackCooldown = 1.5f; // 공격 쿨다운

    public float PatrolRadius = 8f;     // 순찰 반경 (스폰 위치 기준)
    public float IdleTime = 3f;         // 대기 → 순찰 전환 시간
    public float HitStunTime = 0.5f;    // 피격 경직 시간
    public float RespawnTime = 10f;     // 리스폰 대기 시간
}
