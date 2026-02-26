using Unity.Cinemachine;
using UnityEngine;

public class PlayerRotateAbility : PlayerAbility
{
    public Transform CameraRoot; // 코
    
    private float _mx;
    private float _my;

    private void Start()
    {
        // 이게 포톤에서 가장 놓치기 쉽고 버그를 많이 일으키는 요소
        // 내꺼가 아니면 건들지 않는다!
        if (!_owner.PhotonView.IsMine) return;

        Cursor.lockState = CursorLockMode.Locked;

        CinemachineCamera vcam = GameObject.Find("FollowCamera").GetComponent<CinemachineCamera>();
        vcam.Follow = CameraRoot.transform;
    }

    private void Update()
    {
        // 내꺼가 아니면 건들지 않는다!
        if (!_owner.PhotonView.IsMine) return;

        // 죽으면 회전도 멈춰!
        if (_owner.IsDead) return;

        _mx += Input.GetAxis("Mouse X") * _owner.Stat.RotationSpeed * Time.deltaTime;
        _my += Input.GetAxis("Mouse Y") * _owner.Stat.RotationSpeed * Time.deltaTime;
        
        _my = Mathf.Clamp(_my, -90f, 90f);
        
        transform.eulerAngles    = new Vector3(0f, _mx, 0f);
        CameraRoot.localRotation = Quaternion.Euler(-_my, 0f, 0f);
    }
}