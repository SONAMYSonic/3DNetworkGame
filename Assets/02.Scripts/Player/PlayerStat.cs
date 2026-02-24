using System;

[Serializable]
public class PlayerStat
{
    public float MaxHealth;
    public float CurrentHealth;
    public float MoveSpeed;
    public float RunSpeed;
    public float JumpPower;
    public float RotationSpeed;
    public float AttackSpeed;
    public float MaxStamina;
    public float CurrentStamina;
    public float StaminaRecovery    = 20f;
    public float StaminaJumpCost    = 20f;
    public float StaminaAttackCost  = 10f;
    public float StaminaRunCost     = 10f;
    public float Damage;
}