using System;
using UnityEngine;

public class PlayerMoveAbility : MonoBehaviour
{
    public float MoveSpeed = 7f;
    public float JumpForce = 2.5f;
    private const float GRAVITY = 9.81f;
    private float _yVelocity = 0f;
    
    private CharacterController _characterController;
    private Animator _playerAnimator;
    
    // 1. 중력 적용
    // 2. 점프 구현
    // 3. 이동 구현
    
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _playerAnimator = GetComponent<Animator>();
    }

    private void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        
        Vector3 direction = new Vector3(h, 0, v);
        direction.Normalize();
        
        // 카메라가 바라보는 방향 기준으로 수정하기
        direction = Camera.main.transform.TransformDirection(direction);
        _playerAnimator.SetFloat("Speed", direction.magnitude);
        
        _yVelocity -= GRAVITY * Time.deltaTime;
        direction.y = _yVelocity;

        if (Input.GetKey(KeyCode.Space))
        {
            _yVelocity = JumpForce;
        }
        
        _characterController.Move(direction * Time.deltaTime * MoveSpeed);
    }
}
