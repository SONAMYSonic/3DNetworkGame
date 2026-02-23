using System;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerRotateAbility : MonoBehaviour
{
    public Transform CameraRoot;
    public float RotateSpeed = 100f;

    private float _mx;
    private float _my;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        
        CinemachineCamera vcam = GameObject.Find("FollowCamera").GetComponent<CinemachineCamera>();
        vcam.Follow = CameraRoot.transform;
    }
    
    private void Update()
    {
        _mx += Input.GetAxis("Mouse X") * RotateSpeed * Time.deltaTime;
        _my -= Input.GetAxis("Mouse Y") * RotateSpeed * Time.deltaTime;

        _my = Mathf.Clamp(_my, -90f, 90f);

        transform.rotation = Quaternion.Euler(0, _mx, 0);
        CameraRoot.localRotation = Quaternion.Euler(_my, 0, 0);
    }
}
