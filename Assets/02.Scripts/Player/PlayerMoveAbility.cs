using UnityEngine;

public class PlayerMoveAbility : PlayerAbility
{
    
    private const float GRAVITY = 9.8f;
    private float _yVeocity = 0f;

    private CharacterController _characterController;
    private Animator _animator;

    private bool _isRunning = false;
    public bool IsRunning => _isRunning;
    public bool IsJumping => !_characterController.isGrounded;

    // 1. 중력을 적용하세요.
    // 2. 스페이스바를 누르면 점프하게 해주세요.
    // 3. 플레이어 이동을 카메라가 바라보는 방향 기준으로 해주세요.
    // 4. Idle/Run 애니메이션을 블렌드로 적용해주세요.
    // 5. PlayerAttackAbility 스크립트를 만들어서
    //    - 마우스 왼쪽 클릭시마다 Attack1 / Attack2 / Attack 3 애니메이션이 아래 옵션에 따라 실행시켜주세요.
    //    - 인스펙터 옵션 1. 순차적 ( 1 -> 2 -> 3 -> 1)
    //    -        옵션 2. 랜덤   (랜덤)
    
    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // 내꺼가 아니면 건들지 않는다!
        if (!_owner.PhotonView.IsMine) return;
        
        // 죽으면 멈춰!
        if (_owner.IsDead) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");


        Vector3 direction = new Vector3(h, 0, v);
        direction.Normalize();

        direction = Camera.main.transform.TransformDirection(direction);
        direction.y = 0f;
        direction.Normalize();

        _animator.SetFloat("Speed", direction.magnitude);

        if (_characterController.isGrounded) _yVeocity = -0.5f;
        else _yVeocity -= GRAVITY * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) && _characterController.isGrounded && _owner.Stat.CurrentStamina >= _owner.Stat.StaminaJumpCost)
        {
            _owner.Stat.CurrentStamina -= _owner.Stat.StaminaJumpCost;
            _yVeocity = _owner.Stat.JumpPower;
        }

        direction.y = _yVeocity;

        _isRunning = false;
        float targetSpeed = _owner.Stat.MoveSpeed;

        if (Input.GetKey(KeyCode.LeftShift) && _owner.Stat.CurrentStamina > 0f && direction.magnitude > 0f)
        {
            _isRunning = true;
            targetSpeed = _owner.Stat.RunSpeed;
            _owner.Stat.CurrentStamina -= _owner.Stat.StaminaRunCost * Time.deltaTime;
        }

        // 점프/달리기 중이 아닐 때 스태미나 회복
        if (!IsJumping && !_isRunning)
        {
            _owner.Stat.CurrentStamina += _owner.Stat.StaminaRecovery * Time.deltaTime;
        }

        _owner.Stat.CurrentStamina = Mathf.Clamp(_owner.Stat.CurrentStamina, 0f, _owner.Stat.MaxStamina);

        _characterController.Move(direction * Time.deltaTime * targetSpeed);
    }
}