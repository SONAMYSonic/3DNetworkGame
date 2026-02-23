using UnityEngine;

public class PlayerAttackAbility : MonoBehaviour
{
    private Animator _animator;

    private float ATTACK_COOLTIME = 0.6f;
    private float _attackTimer = 0f;

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }
    
    private void Update()
    {
        _attackTimer += Time.deltaTime;

        if (Input.GetMouseButton(0) && _attackTimer >= ATTACK_COOLTIME)
        {
            _attackTimer = 0f;
            
            _animator.SetTrigger($"Taunt{Random.Range(1, 4)}");
        }
    }
}
